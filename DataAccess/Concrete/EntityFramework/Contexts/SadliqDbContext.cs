using Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Concrete.EntityFramework.Contexts
{
    public class SadliqDbContext : DbContext
    {
        public SadliqDbContext(DbContextOptions<SadliqDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Venue> Venues => Set<Venue>();
        public DbSet<VenueImage> VenueImages => Set<VenueImage>();
        public DbSet<Hall> Halls => Set<Hall>();
        public DbSet<HallMedia> HallMedia => Set<HallMedia>();
        public DbSet<Menu> Menus => Set<Menu>();
        public DbSet<MenuItem> MenuItems => Set<MenuItem>();
        public DbSet<HallAvailability> HallAvailabilities => Set<HallAvailability>();
        public DbSet<Reservation> Reservations => Set<Reservation>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            // Email unikal olmalıdır
            b.Entity<User>().HasIndex(u => u.Email).IsUnique();

            // ƏSAS QAYDA: eyni zal + eyni tarix təkrarlana bilməz
            b.Entity<HallAvailability>()
                .HasIndex(a => new { a.HallId, a.Date })
                .IsUnique();

            // Rezervlərdə silinmə zənciri problemi olmasın
            b.Entity<Reservation>()
                .HasOne(r => r.Venue).WithMany()
                .HasForeignKey(r => r.VenueId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<Reservation>()
                .HasOne(r => r.Hall).WithMany()
                .HasForeignKey(r => r.HallId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<Reservation>()
                .HasOne(r => r.Menu).WithMany()
                .HasForeignKey(r => r.MenuId)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(b);
        }
    }
}
