using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;

namespace CustomerPortal.Services
{
    public class EmailService
    {
        private readonly string _emailUserName;
        private readonly string _emailPassword;

        public EmailService(string emailUserName, string emailPassword)
        {
            _emailUserName = emailUserName;
            _emailPassword = emailPassword;
        }

        private static string EmailSignature => "";

        public void SendHtmlEmail(string title, string body, string emailAddressesString)
        {
            var emailAddresses = emailAddressesString?.Split(';').Where(e => !string.IsNullOrWhiteSpace(e)).ToList() ??
                                 new List<string>();

            if (!emailAddresses.Any())
            {
                return;
            }

            var mailMessage = new MailMessage
            {
                From = new MailAddress("notifications@savanta.com", "Customer portal"),
                Subject = title,
                Body = body + EmailSignature,
                IsBodyHtml = true
            };

            foreach (var address in emailAddresses)
            {
                mailMessage.To.Add(new MailAddress(address));
            }

            using var smtpClient = new SmtpClient("smtp.office365.com")
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_emailUserName, _emailPassword)
            };
            smtpClient.Send(mailMessage);
        }
    }
}
