using Business.Abstract;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Business.Utilities.Email
{
    public class SmtpEmailService : IEmailService
    {
        private readonly EmailOptions _options;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        // SMTP konfiqurasiya olunmayıbsa məktub göndərilmir, məzmunu log-a yazılır ki,
        // development mühitində link əl ilə götürülə bilsin
        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            if (!_options.IsConfigured)
            {
                _logger.LogWarning(
                    "SMTP konfiqurasiya olunmayıb. Göndərilməyən məktub:\nKimə: {To}\nMövzu: {Subject}\n{Body}",
                    to, subject, htmlBody);
                return;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_options.From, _options.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(to);

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                Credentials = new NetworkCredential(_options.User, _options.Password)
            };

            try
            {
                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                // Məktub gedə bilməsə də əməliyyat pozulmamalıdır — istifadəçiyə eyni cavab qayıdır
                _logger.LogError(ex, "E-poçt göndərilə bilmədi: {To}", to);
            }
        }
    }
}
