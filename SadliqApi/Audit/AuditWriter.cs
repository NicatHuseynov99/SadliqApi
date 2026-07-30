using DataAccess.Abstract;
using Entities.Concrete;
using System.Threading.Channels;

namespace SadliqApi.Audit
{
    // Jurnal yazısı istifadəçinin cavabını gecikdirməsin deyə növbəyə atılır,
    // arxa fon xidməti isə onu bazaya yazır.
    public interface IAuditWriter
    {
        void Enqueue(AuditLog log);
    }

    public class AuditWriter : IAuditWriter
    {
        private readonly Channel<AuditLog> _channel =
            Channel.CreateBounded<AuditLog>(new BoundedChannelOptions(5000)
            {
                // Növbə dolarsa ən köhnə yazı atılır — jurnal ucbatından sistem dayanmamalıdır
                FullMode = BoundedChannelFullMode.DropOldest
            });

        public ChannelReader<AuditLog> Reader => _channel.Reader;

        public void Enqueue(AuditLog log) => _channel.Writer.TryWrite(log);
    }

    // Növbəni oxuyub bazaya yazan arxa fon xidməti
    public class AuditBackgroundService : BackgroundService
    {
        private readonly AuditWriter _writer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuditBackgroundService> _logger;

        public AuditBackgroundService(
            IAuditWriter writer,
            IServiceScopeFactory scopeFactory,
            ILogger<AuditBackgroundService> logger)
        {
            _writer = (AuditWriter)writer;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var log in _writer.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dal = scope.ServiceProvider.GetRequiredService<IAuditLogDal>();
                    await dal.AddAsync(log);
                }
                catch (Exception ex)
                {
                    // Jurnal yazıla bilməsə də əsas iş dayanmamalıdır
                    _logger.LogError(ex, "Jurnal yazısı bazaya yazıla bilmədi: {Action}", log.Action);
                }
            }
        }
    }
}
