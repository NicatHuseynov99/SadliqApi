using Business.Abstract;
using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.Extensions.Logging;

namespace Business.Concrete.Notifications
{
    // Saytdaxili kanal: bildiriş sətir kimi bazaya düşür və zəng siyahısında görünür
    public class InAppNotificationChannel : INotificationChannel
    {
        private readonly INotificationDal _dal;

        public InAppNotificationChannel(INotificationDal dal)
        {
            _dal = dal;
        }

        public Entities.Concrete.NotificationChannels Kind => Entities.Concrete.NotificationChannels.InApp;

        public async Task SendAsync(User user, NotificationMessage message, CancellationToken token = default)
        {
            await _dal.AddAsync(new Notification
            {
                UserId = user.Id,
                Type = message.Type,
                Title = message.Title,
                Body = message.Body,
                Link = message.Link,
                CampaignId = message.CampaignId,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    // E-poçt kanalı: mətn eyni şablona geydirilir ki, hər manager öz HTML-ini yazmasın
    public class EmailNotificationChannel : INotificationChannel
    {
        private readonly IEmailService _email;

        public EmailNotificationChannel(IEmailService email)
        {
            _email = email;
        }

        public Entities.Concrete.NotificationChannels Kind => Entities.Concrete.NotificationChannels.Email;

        public async Task SendAsync(User user, NotificationMessage message, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(user.Email)) return;

            var button = string.IsNullOrWhiteSpace(message.Link)
                ? string.Empty
                : $@"<p style='margin:22px 0'>
                       <a href='{message.Link}'
                          style='background:#721438;color:#fff;text-decoration:none;
                                 padding:11px 22px;border-radius:9px;display:inline-block'>
                         Ətraflı bax
                       </a>
                     </p>";

            var body = $@"
                <div style='font-family:Segoe UI,Arial,sans-serif;color:#1a1118;max-width:560px'>
                  <h2 style='color:#721438;margin:0 0 12px'>{message.Title}</h2>
                  <p style='line-height:1.6;white-space:pre-line'>{message.Body}</p>
                  {button}
                  <hr style='border:none;border-top:1px solid #efe7ea;margin:24px 0 12px' />
                  <p style='color:#a6969c;font-size:12px'>TOYUN</p>
                </div>";

            await _email.SendAsync(user.Email, message.Title, body);
        }
    }

    // SMS kanalı: provayder hesabı bağlananadək mesaj yalnız jurnala yazılır.
    // Bu sayədə axının qalan hissəsi indidən hazırdır.
    public class SmsNotificationChannel : INotificationChannel
    {
        private readonly ILogger<SmsNotificationChannel> _logger;

        public SmsNotificationChannel(ILogger<SmsNotificationChannel> logger)
        {
            _logger = logger;
        }

        public Entities.Concrete.NotificationChannels Kind => Entities.Concrete.NotificationChannels.Sms;

        public Task SendAsync(User user, NotificationMessage message, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(user.Phone)) return Task.CompletedTask;

            _logger.LogInformation("SMS (provayder qoşulmayıb) → {Phone}: {Title} — {Body}",
                user.Phone, message.Title, message.Body);

            return Task.CompletedTask;
        }
    }
}
