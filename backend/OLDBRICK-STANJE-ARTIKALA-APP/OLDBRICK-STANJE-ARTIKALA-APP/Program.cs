using Microsoft.EntityFrameworkCore;
using OLDBRICK_STANJE_ARTIKALA_APP.Data;
using OLDBRICK_STANJE_ARTIKALA_APP.Services.Auth;
using Npgsql;
using OLDBRICK_STANJE_ARTIKALA_APP.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using OLDBRICK_STANJE_ARTIKALA_APP.Services.User;
using Microsoft.OpenApi.Models;
using OLDBRICK_STANJE_ARTIKALA_APP.Services.BeerServices;
using OLDBRICK_STANJE_ARTIKALA_APP.Services.DailyReports;
using OLDBRICK_STANJE_ARTIKALA_APP.Custom_items;

namespace OLDBRICK_STANJE_ARTIKALA_APP
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services
                .AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new FloatConverter());
                });


            var cs = builder.Configuration.GetConnectionString("DefaultConnection");
            Console.WriteLine("CS: " + cs?.Replace("Password=", "Password=***"));

            //try
            //{
            //    using var conn = new NpgsqlConnection(cs);
            //    conn.Open();

            //    using var cmd = new NpgsqlCommand("select current_user", conn);
            //    var user = cmd.ExecuteScalar()?.ToString();

            //    Console.WriteLine("CONNECTED as: " + user);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("DB CONNECT FAIL: " + ex.GetType().FullName);
            //    Console.WriteLine(ex.Message);
            //    Console.WriteLine(ex.InnerException?.Message);
            //    throw;
            //}

            builder.Services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
            var connectionstring = builder.Configuration.GetConnectionString("DefaultConnection");
            Console.WriteLine(">>> DefaultConnection from config: " + connectionstring);

            var jwtSettings = new JwtSettings();
            builder.Configuration.GetSection("Jwt").Bind(jwtSettings);


            //swagger
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Oldbrick API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Unesi samo token"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type= ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });


            builder.Services.AddSingleton(jwtSettings);
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IPasswordService, PasswordService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IBeerService, BeerService>();
            builder.Services.AddScoped<IDailyReportService, DailyReportService>();
            builder.Services.AddScoped<IDailyBeerStateService, DailyBeerStateService>();
            builder.Services.AddScoped<IProsutoService, ProsutoService>();
            


            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),


                        ClockSkew = TimeSpan.Zero
                    };
                });

            builder.Services.AddAuthorization();


            // Add services to the container.

            builder.Services.AddControllers();

            var corsPolicyName = "FrontendCors";

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(corsPolicyName, policy =>
                {
                    policy.WithOrigins("http://localhost:5173")
                    .AllowAnyHeader()
                   
                    .AllowAnyMethod();
                });
            });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();
            app.UseDeveloperExceptionPage();

            //app.UseHttpsRedirection();

            app.UseCors(corsPolicyName);

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            //{
            //    app.UseSwagger();
            //    app.UseSwaggerUI();
            //}
            app.UseSwagger();
            app.UseSwaggerUI();


            app.UseAuthentication();

            app.UseAuthorization();


            app.MapControllers();

            app.MapGet("/health", () => Results.Ok("OK"));
            app.Run();
        }
    }
}
