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

        public DbSet<Restock> Restocks => Set<Restock>();

        public DbSet<InventoryReset> InventoryResets => Set<InventoryReset>();

        public DbSet<InventoryResetItem> InventoryResetItems => Set<InventoryResetItem>();

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
                e.Property(e => e.TotalProsuto).HasColumnName("total_prosuto");
                e.Property(e => e.TotalPotrosenoVaga).HasColumnName("total_potroseno_na_vagi");
                e.Property(e => e.TotalPotrosenoProgram).HasColumnName("total_potroseno_u_programu");
                e.Property(e => e.IzmerenoProsuto).HasColumnName("vaga_izmereno_prosuto");
                e.Property(e => e.ProsutoRazlika).HasColumnName("prosuto_razlika");
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
                e.Property(e => e.NazivPiva).HasColumnName("naziv_piva");
                e.Property(e => e.ProsutoJednogPiva).HasColumnName("prosuto_jednog_piva");

             
                e.HasIndex(e => new { e.IdNaloga, e.IdPiva }).IsUnique();

            });

            modelBuilder.Entity<Restock>(e =>
            {
                e.ToTable("restocks");
                e.HasKey(e => e.Id);

                e.Property(e => e.Id).HasColumnName("id");

                e.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()")
                .ValueGeneratedOnAdd();

                e.Property(e => e.IdNaloga).HasColumnName("id_naloga");
                e.Property(e => e.IdPiva).HasColumnName("id_piva");
                e.Property(e => e.Quantity).HasColumnName("quantity");
            });

            modelBuilder.Entity<InventoryReset>(e =>
            {
                e.ToTable("inventory_resets");
                e.HasKey(e => e.Id);
                e.Property(e => e.Id).HasColumnName("id");

                e.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()")
                .ValueGeneratedOnAdd();

                e.Property(e => e.DatumPopisa).HasColumnName("datum_popisa");
                e.Property(e => e.Napomena).HasColumnName("napomena");
            });

            modelBuilder.Entity<InventoryResetItem>(e =>
            {
                e.ToTable("inventory_reset_items");
                e.HasKey(e => e.Id);
                e.Property(e => e.Id).HasColumnName("id");
                e.Property(e => e.InventoryResetId).HasColumnName("inventory_reset_id");
                e.Property(e => e.IdPiva).HasColumnName("id_piva");
                e.Property(e => e.NazivPiva).HasColumnName("naziv_piva");
                e.Property(e => e.IzmerenoSnapshot).HasColumnName("izmereno_snapshot");
                e.Property(e => e.PosSnapshot).HasColumnName("pos_snapshot");
                e.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()")
                 .ValueGeneratedOnAdd();

            });
        }
    }
}
