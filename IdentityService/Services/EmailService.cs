using System.Net;
using System.Net.Mail;
using Infrastructure.Configuration;

namespace IdentityService.Services
{
    public class EmailService(SystemConfig systemConfig)
    {

        private readonly SystemConfig _systemConfig = systemConfig;
        private const string SenderDisplayName = "ACouldDrive";

        public async Task SendMailAsync(string to, string subject, string body, bool isBodyHtml = false)
        {
            await SendMailAsync(new MailAddress(to), subject, body, isBodyHtml);
        }

        public async Task SendMailAsync(MailAddress to, string subject, string body, bool isBodyHtml = false)
        {
            var smtpClient = new SmtpClient
            {
                Host = _systemConfig.SmtpServerHost,
                Port = _systemConfig.SmtpServerPort,
                UseDefaultCredentials = false,
                EnableSsl = true,
                Credentials = new NetworkCredential(_systemConfig.EmailAccount, _systemConfig.EmailPassword)
            };

            var message = new MailMessage(new MailAddress(_systemConfig.EmailAccount, SenderDisplayName), to)
            {
                Subject = subject,
                IsBodyHtml = isBodyHtml,
                Body = body
            };

            await smtpClient.SendMailAsync(message);
        }

    }
}
