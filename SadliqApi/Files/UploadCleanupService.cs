using DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.EntityFrameworkCore;

namespace SadliqApi.Files
{
    // Yükləmə açıq endpoint olduğu üçün disk zibillənə bilər: anket yarımçıq
    // qalanda yüklənmiş fayl heç bir saraya bağlanmır. Bu xidmət gündə bir dəfə
    // 24 saatdan köhnə, bazada istifadə olunmayan faylları silir.
    public class UploadCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<UploadCleanupService> _logger;

        public UploadCleanupService(
            IServiceScopeFactory scopeFactory,
            IWebHostEnvironment environment,
            ILogger<UploadCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _environment = environment;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Sahibsiz fayllar təmizlənə bilmədi.");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task CleanAsync(CancellationToken token)
        {
            var root = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads");
            if (!Directory.Exists(root)) return;

            var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                .Where(f => File.GetCreationTimeUtc(f) < DateTime.UtcNow.AddHours(-24))
                .ToList();

            if (files.Count == 0) return;

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SadliqDbContext>();

            // Bazada işlənən bütün fayl adları bir dəfə yığılır — hər fayl üçün
            // ayrıca sorğu getməsin
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Collect(IEnumerable<string?> urls)
            {
                foreach (var url in urls)
                    if (!string.IsNullOrWhiteSpace(url))
                        used.Add(Path.GetFileName(url));
            }

            // Silinmiş sətirlərin faylları da qalmalıdır: bərpa mümkündür
            Collect(await context.Venues.IgnoreQueryFilters()
                .Select(v => v.CoverImageUrl).ToListAsync(token));
            Collect(await context.Venues.IgnoreQueryFilters()
                .Select(v => v.VideoUrl).ToListAsync(token));
            Collect(await context.VenueImages.IgnoreQueryFilters()
                .Select(i => i.Url).ToListAsync(token));
            Collect(await context.HallMedia.IgnoreQueryFilters()
                .Select(m => m.Url).ToListAsync(token));
            Collect(await context.Menus.IgnoreQueryFilters()
                .Select(m => m.ImageUrl).ToListAsync(token));

            var removed = 0;

            foreach (var path in files)
            {
                if (used.Contains(Path.GetFileName(path))) continue;

                try
                {
                    File.Delete(path);
                    removed++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Fayl silinmədi: {Path}", path);
                }
            }

            if (removed > 0)
                _logger.LogInformation("{Count} sahibsiz fayl silindi.", removed);
        }
    }
}
