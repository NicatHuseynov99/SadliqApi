using Business.Abstract;
using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;

namespace SadliqApi.Visits
{
    public class VisitWriter : IVisitWriter
    {
        private readonly Channel<PageView> _channel =
            Channel.CreateBounded<PageView>(new BoundedChannelOptions(10000)
            {
                // Növbə dolarsa ən köhnə baxış atılır — statistika ucbatından sayt yavaşlamamalıdır
                FullMode = BoundedChannelFullMode.DropOldest
            });

        public ChannelReader<PageView> Reader => _channel.Reader;

        public void Enqueue(PageView view) => _channel.Writer.TryWrite(view);
    }

    // Növbəni oxuyub bazaya yazan arxa fon xidməti
    public class VisitBackgroundService : BackgroundService
    {
        private readonly VisitWriter _writer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<VisitBackgroundService> _logger;

        public VisitBackgroundService(
            IVisitWriter writer,
            IServiceScopeFactory scopeFactory,
            ILogger<VisitBackgroundService> logger)
        {
            _writer = (VisitWriter)writer;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var view in _writer.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dal = scope.ServiceProvider.GetRequiredService<IPageViewDal>();
                    await dal.AddAsync(view);
                }
                catch (Exception ex)
                {
                    // Statistika yazıla bilməsə də sayt işləməyə davam etməlidir
                    _logger.LogError(ex, "Ziyarət qeydi bazaya yazıla bilmədi: {Path}", view.Path);
                }
            }
        }
    }

    // IP ünvanı şəxsi məlumatdır — köhnə ziyarət qeydləri silinir. Saxlanma müddəti
    // admin paneldən tənzimlənir (ilkin 90 gün). Gündə bir dəfə işə düşür.
    public class VisitCleanupService : BackgroundService
    {
        private const int DefaultRetentionDays = 90;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<VisitCleanupService> _logger;

        public VisitCleanupService(IServiceScopeFactory scopeFactory, ILogger<VisitCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider
                        .GetRequiredService<DataAccess.Concrete.EntityFramework.Contexts.SadliqDbContext>();

                    var settings = scope.ServiceProvider.GetRequiredService<IPlatformSettingsService>();
                    var retentionDays = await settings.GetIntAsync("visit_retention_days", DefaultRetentionDays);

                    var limit = DateTime.UtcNow.AddDays(-retentionDays);
                    var removed = await context.PageViews
                        .Where(p => p.CreatedAt < limit)
                        .ExecuteDeleteAsync(stoppingToken);

                    if (removed > 0)
                        _logger.LogInformation("{Count} köhnə ziyarət qeydi silindi.", removed);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Köhnə ziyarət qeydləri silinə bilmədi.");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
