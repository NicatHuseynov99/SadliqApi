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
        public DbSet<VenueFeature> VenueFeatures => Set<VenueFeature>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<ReviewReport> ReviewReports => Set<ReviewReport>();
        public DbSet<Favorite> Favorites => Set<Favorite>();
        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
        public DbSet<VenueStaff> VenueStaff => Set<VenueStaff>();
        public DbSet<VenueStaffHall> VenueStaffHalls => Set<VenueStaffHall>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<PageView> PageViews => Set<PageView>();

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

            // Eyni istifadəçi eyni sarayı iki dəfə seçilmişlərə əlavə edə bilməz
            b.Entity<Favorite>()
                .HasIndex(f => new { f.UserId, f.VenueId })
                .IsUnique();

            // Eyni istifadəçi eyni saraya iki dəfə təyin edilə bilməz
            b.Entity<VenueStaff>()
                .HasIndex(s => new { s.VenueId, s.UserId })
                .IsUnique();

            // Eyni zal eyni administratora iki dəfə verilə bilməz
            b.Entity<VenueStaffHall>()
                .HasIndex(h => new { h.VenueStaffId, h.HallId })
                .IsUnique();

            // Zal silinsə, administratorun həmin zala təyinatı da silinir
            b.Entity<VenueStaffHall>()
                .HasOne(h => h.Hall).WithMany()
                .HasForeignKey(h => h.HallId)
                .OnDelete(DeleteBehavior.Cascade);

            // Jurnal sorğuları əsasən tarixə, istifadəçiyə və saraya görə süzülür
            b.Entity<AuditLog>().HasIndex(a => a.CreatedAt);
            b.Entity<AuditLog>().HasIndex(a => a.UserId);
            b.Entity<AuditLog>().HasIndex(a => a.VenueId);
            b.Entity<AuditLog>().HasIndex(a => a.Action);

            // Ziyarət hesabatları tarix, sessiya və saray üzrə qruplaşdırılır
            b.Entity<PageView>().HasIndex(p => p.CreatedAt);
            b.Entity<PageView>().HasIndex(p => p.SessionId);
            b.Entity<PageView>().HasIndex(p => p.VenueId);

            // İstifadəçi silinsə, ziyarət tarixçəsi qonaq baxışı kimi qalır
            b.Entity<PageView>()
                .HasOne(p => p.User).WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Token axtarışı hash üzrə gedir
            b.Entity<PasswordResetToken>()
                .HasIndex(t => t.TokenHash);

            // Rəy yazan istifadəçi silinsə, rəy qalsın (müəllif adı onsuz da saxlanılır)
            b.Entity<Review>()
                .HasOne(r => r.Customer).WithMany()
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Gizlədilmiş rəy heç bir adi sorğuda görünmür və reytinq ortalamasına
            // düşmür. Moderasiya paneli IgnoreQueryFilters() ilə hamısını görür.
            b.Entity<Review>().HasQueryFilter(r => !r.IsHidden);

            // Şikayət gizlədilmiş rəylərə də aid ola bilər — filtri sıfırlayırıq,
            // yoxsa EF valideynin filtrini şikayətlərə də tətbiq edər.
            b.Entity<ReviewReport>().HasQueryFilter(_ => true);

            b.Entity<ReviewReport>()
                .HasOne(r => r.Review).WithMany()
                .HasForeignKey(r => r.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);

            // Şikayət edən hesab silinsə, şikayət anonim olaraq qalır
            b.Entity<ReviewReport>()
                .HasOne(r => r.ReporterUser).WithMany()
                .HasForeignKey(r => r.ReporterUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Eyni istifadəçi eyni rəyi iki dəfə şikayət edə bilməz
            b.Entity<ReviewReport>()
                .HasIndex(r => new { r.ReviewId, r.ReporterUserId })
                .IsUnique();

            b.Entity<Review>().HasIndex(r => r.IsHidden);

            base.OnModelCreating(b);
        }
    }
}
