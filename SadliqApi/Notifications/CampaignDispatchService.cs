using Business.Abstract;

namespace SadliqApi.Notifications
{
    // Planlaşdırılmış bildirişləri vaxtı çatanda göndərir.
    // Beş dəqiqədən bir yoxlayır: elan üçün bu dəqiqlik kifayətdir.
    public class CampaignDispatchService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CampaignDispatchService> _logger;

        public CampaignDispatchService(IServiceScopeFactory scopeFactory, ILogger<CampaignDispatchService> logger)
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
                    var service = scope.ServiceProvider.GetRequiredService<INotificationCampaignService>();

                    var sent = await service.DispatchDueAsync(stoppingToken);
                    if (sent > 0)
                        _logger.LogInformation("{Count} planlaşdırılmış bildiriş göndərildi.", sent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Planlaşdırılmış bildirişlər göndərilə bilmədi.");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
