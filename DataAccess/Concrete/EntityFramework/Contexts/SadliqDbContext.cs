using Core.Entities;
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
        public DbSet<ReviewEdit> ReviewEdits => Set<ReviewEdit>();
        public DbSet<PlatformSetting> PlatformSettings => Set<PlatformSetting>();
        public DbSet<City> Cities => Set<City>();
        public DbSet<Feature> Features => Set<Feature>();
        public DbSet<AppText> AppTexts => Set<AppText>();
        public DbSet<Favorite> Favorites => Set<Favorite>();
        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
        public DbSet<VenueStaff> VenueStaff => Set<VenueStaff>();
        public DbSet<VenueStaffHall> VenueStaffHalls => Set<VenueStaffHall>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<PageView> PageViews => Set<PageView>();

        // Silmək fiziki deyil, işarələmədir. Ona görə bazanın öz kaskad silinməsi işə
        // düşmür — valideyn işarələnəndə uşaqları burada, bir yerdə işarələyirik.
        // Geri qaytaranda da eyni məntiq əks istiqamətdə işləyir.
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var venues = CollectSoftDeleteChanges<Venue>();
            var halls = CollectSoftDeleteChanges<Hall>();
            var menus = CollectSoftDeleteChanges<Menu>();

            var saved = await base.SaveChangesAsync(cancellationToken);

            foreach (var (id, deleted) in venues)
            {
                await CascadeAsync(Halls.Where(h => h.VenueId == id), deleted, cancellationToken);
                await CascadeAsync(Menus.Where(m => m.VenueId == id), deleted, cancellationToken);
                await CascadeAsync(VenueImages.Where(i => i.VenueId == id), deleted, cancellationToken);
                await CascadeAsync(VenueFeatures.Where(f => f.VenueId == id), deleted, cancellationToken);
                await CascadeAsync(Reviews.Where(r => r.VenueId == id), deleted, cancellationToken);
            }

            foreach (var (id, deleted) in halls)
                await CascadeAsync(HallMedia.Where(m => m.HallId == id), deleted, cancellationToken);

            foreach (var (id, deleted) in menus)
                await CascadeAsync(MenuItems.Where(i => i.MenuId == id), deleted, cancellationToken);

            return saved;
        }

        public override int SaveChanges() => SaveChangesAsync().GetAwaiter().GetResult();

        // Bu yaddaşa yazmada IsDeleted dəyəri dəyişən sətirlər
        private List<(int Id, bool Deleted)> CollectSoftDeleteChanges<T>() where T : class, IEntity, ISoftDelete
        {
            var list = new List<(int, bool)>();

            foreach (var entry in ChangeTracker.Entries<T>())
            {
                if (entry.State != EntityState.Modified) continue;

                var property = entry.Property(nameof(ISoftDelete.IsDeleted));
                if (!property.IsModified) continue;

                var id = (int)entry.Property("Id").CurrentValue!;
                list.Add((id, entry.Entity.IsDeleted));
            }

            return list;
        }

        private static async Task CascadeAsync<T>(IQueryable<T> query, bool deleted, CancellationToken token)
            where T : class, ISoftDelete
        {
            await query
                .IgnoreQueryFilters()
                .Where(x => x.IsDeleted != deleted)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.IsDeleted, deleted)
                    .SetProperty(x => x.DeletedAt, deleted ? DateTime.UtcNow : null), token);
        }

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

            // Silinmiş sətirlər heç bir adi sorğuda görünmür. Silmək fiziki deyil,
            // işarələmədir — baza idarəsi bölməsi IgnoreQueryFilters() ilə hamısını görür.
            b.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
            b.Entity<Venue>().HasQueryFilter(v => !v.IsDeleted);
            b.Entity<Hall>().HasQueryFilter(h => !h.IsDeleted);
            b.Entity<Menu>().HasQueryFilter(m => !m.IsDeleted);
            b.Entity<MenuItem>().HasQueryFilter(m => !m.IsDeleted);
            b.Entity<VenueImage>().HasQueryFilter(i => !i.IsDeleted);
            b.Entity<HallMedia>().HasQueryFilter(m => !m.IsDeleted);
            b.Entity<VenueFeature>().HasQueryFilter(f => !f.IsDeleted);
            b.Entity<Reservation>().HasQueryFilter(r => !r.IsDeleted);
            b.Entity<VenueStaff>().HasQueryFilter(s => !s.IsDeleted);

            // Gizlədilmiş rəy heç bir adi sorğuda görünmür və reytinq ortalamasına
            // düşmür. Moderasiya paneli IgnoreQueryFilters() ilə hamısını görür.
            b.Entity<Review>().HasQueryFilter(r => !r.IsHidden && !r.IsDeleted);

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

            // Düzəliş tarixçəsi silinmiş rəy üçün də görünməlidir
            b.Entity<ReviewEdit>().HasQueryFilter(_ => true);

            b.Entity<ReviewEdit>()
                .HasOne(e => e.Review).WithMany()
                .HasForeignKey(e => e.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);

            // Düzəlişi edən hesab silinsə də qeyd qalır — adı onsuz da saxlanılıb
            b.Entity<ReviewEdit>()
                .HasOne(e => e.EditedByUser).WithMany()
                .HasForeignKey(e => e.EditedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<ReviewEdit>().HasIndex(e => e.ReviewId);

            // Parametr açarı təkrarlana bilməz
            b.Entity<PlatformSetting>().HasIndex(s => s.Key).IsUnique();

            // Kitabçalarda eyni ad iki dəfə olmamalıdır
            b.Entity<City>().HasIndex(c => c.Name).IsUnique();
            b.Entity<Feature>().HasIndex(f => f.Name).IsUnique();

            b.Entity<City>().HasQueryFilter(c => !c.IsDeleted);
            b.Entity<Feature>().HasQueryFilter(f => !f.IsDeleted);

            // Mətn açarı kateqoriya daxilində unikaldır
            b.Entity<AppText>().HasIndex(t => new { t.Category, t.Key }).IsUnique();

            // Şəhər və özəllik adları mətn olduğu üçün süzgəc sorğuları indeks istəyir
            b.Entity<Venue>().HasIndex(v => v.City);

            // Xəritə sorğusu koordinat aralığı üzrə süzür
            b.Entity<Venue>().HasIndex(v => new { v.Latitude, v.Longitude });
            b.Entity<VenueFeature>().HasIndex(f => f.Name);

            base.OnModelCreating(b);
        }
    }
}
