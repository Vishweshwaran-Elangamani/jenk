using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace ReferralManagement.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var host = _config["Smtp:Host"];
            var portStr = _config["Smtp:Port"];
            var user = _config["Smtp:User"];
            var pass = _config["Smtp:Pass"];

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
                throw new InvalidOperationException("SMTP configuration is missing (Smtp:Host/User/Pass)");

            int port = 587;
            if (!string.IsNullOrWhiteSpace(portStr) && !int.TryParse(portStr, out port))
            {
                port = 587;
            }

            using var smtp = new SmtpClient(host)
            {
                Port = port,
                Credentials = new NetworkCredential(user, pass),
                EnableSsl = true
            };

            using var mail = new MailMessage(user, to ?? throw new ArgumentNullException(nameof(to)), subject ?? string.Empty, body ?? string.Empty);
            await smtp.SendMailAsync(mail);
        }
    }
}
