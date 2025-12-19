using Microsoft.EntityFrameworkCore;
using OLDBRICK_STANJE_ARTIKALA_APP.Entities;

namespace OLDBRICK_STANJE_ARTIKALA_APP.Data
{
    public class AppDbContext : DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Users> Users => Set<Users>();

        public DbSet<Beer> Beers => Set<Beer>();

        public DbSet<DailyReport> DailyReports => Set<DailyReport>();

        public DbSet<DailyBeerState> DailyBeerStates => Set<DailyBeerState>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Users>(e =>
            {
                e.ToTable("users"); 
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.Username).IsUnique();

                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.Username).HasColumnName("username");
                e.Property(x => x.PasswordHash).HasColumnName("password_hash");
                e.Property(x => x.Role).HasColumnName("role");
                e.Property(x => x.IsActive).HasColumnName("is_active");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
            });

            modelBuilder.Entity<Beer>(e =>
            {
                e.ToTable("TAB1");
                e.Property(e => e.Id).HasColumnName("id");
                e.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()")
                .ValueGeneratedOnAdd();
                e.Property(e => e.NazivPiva).HasColumnName("naziv_piva");
                e.Property(e => e.TipMerenja).HasColumnName("tip_merenja");
            });

            modelBuilder.Entity<DailyReport>(e =>
            {
                e.ToTable("TAB2");

                e.HasKey(e => e.IdNaloga);

                e.Property(e => e.IdNaloga).HasColumnName("id_naloga");
                e.Property(e => e.CreatedAt).HasColumnName("created_at")
                .HasDefaultValueSql("now()")
                .ValueGeneratedOnAdd();
                e.Property(e => e.Datum).HasColumnName("datum");
                e.Property(e => e.Prosuto).HasColumnName("prosuto");
            });

            modelBuilder.Entity<DailyBeerState>(e =>
            {
                e.ToTable("TAB3");
                e.HasKey(e => e.IdStanja);

                e.Property(e => e.IdStanja).HasColumnName("id_stanja");

                e.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("now()")
                    .ValueGeneratedOnAdd();

                e.Property(e => e.IdNaloga).HasColumnName("id_naloga");
                e.Property(e => e.IdPiva).HasColumnName("id_piva");

                e.Property(e => e.Izmereno).HasColumnName("izmereno");
                e.Property(e => e.StanjeUProgramu).HasColumnName("stanje_u_programu");

                // preporuka (nije obavezno za start, ali je dobra):
                e.HasIndex(e => new { e.IdNaloga, e.IdPiva }).IsUnique();

            });
        }
    }
}
