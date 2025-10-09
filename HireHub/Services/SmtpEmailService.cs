using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace HireHub.API.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly ILogger<SmtpEmailService> _logger;
        private readonly string _host;
        private readonly int _port;
        private readonly bool _useSsl;
        private readonly string _user;
        private readonly string _pass;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public SmtpEmailService(ILogger<SmtpEmailService> logger, IConfiguration config)
        {
            _logger = logger;
            _host = config["Smtp:Host"] ?? "localhost";
            _port = int.TryParse(config["Smtp:Port"], out var p) ? p : 587;
            _useSsl = bool.TryParse(config["Smtp:UseSsl"], out var s) ? s : true;
            _user = config["Smtp:User"];
            _pass = config["Smtp:Pass"];
            _fromEmail = config["Smtp:FromEmail"] ?? "no-reply@hirehub.local";
            _fromName = config["Smtp:FromName"] ?? "HireHub";
        }

        public async Task<bool> SendAsync(string toEmail, string subject, string body)
        {
            try
            {
                var msg = new MimeMessage();
                msg.From.Add(new MailboxAddress(_fromName, _fromEmail));
                msg.To.Add(MailboxAddress.Parse(toEmail));
                msg.Subject = subject;

                var builder = new BodyBuilder
                {
                    HtmlBody = body,
                    TextBody = System.Text.RegularExpressions.Regex.Replace(
                                                                                body ?? string.Empty,
                                                                                "<.*?>",
                                                                                string.Empty,
                                                                                 System.Text.RegularExpressions.RegexOptions.None,
                                                                                 TimeSpan.FromSeconds(2))
                };

                msg.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
             
                var secure = _useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
                await client.ConnectAsync(_host, _port, secure);

                if (!string.IsNullOrWhiteSpace(_user))
                    await client.AuthenticateAsync(_user, _pass);

                await client.SendAsync(msg);
                await client.DisconnectAsync(true);

                _logger.LogInformation("SMTP: sent email to {Email}", toEmail);
                return true;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "SMTP send failed for {Email}", toEmail);
                return false;
            }
        }
    }
}
