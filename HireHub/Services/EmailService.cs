using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HireHub.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public Task<bool> SendAsync(string toEmail, string subject, string body)
        {
            // 🚀 For now: just log instead of real SMTP/SendGrid
            _logger.LogInformation("Pretend sending email to {Email}: {Subject} - {Body}", toEmail, subject, body);

            // Always return success for now
            return Task.FromResult(true);
        }
    }
}
