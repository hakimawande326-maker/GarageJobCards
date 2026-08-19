using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace GarageJobCards.Infrastructure
{
    public static class EmailHelper
    {
        // Reads SMTP host/port/credentials from Web.config's <system.net><mailSettings>
        // section automatically — no code changes needed when you plug in real
        // provider details, just edit Web.config.
        public static void SendPasswordResetEmail(string toEmail, string resetLink)
        {
            var fromAddress = ConfigurationManager.AppSettings["SmtpFromAddress"];
            var fromName = ConfigurationManager.AppSettings["SmtpFromName"];

            if (string.IsNullOrEmpty(fromAddress))
                fromAddress = "noreply@philasauto.co.za";
            if (string.IsNullOrEmpty(fromName))
                fromName = "Phila's Auto Repair Shop";

            var message = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName),
                Subject = "Reset your password - Phila's Auto Repair Shop",
                IsBodyHtml = true,
                Body =
                    "<p>We received a request to reset your password.</p>" +
                    "<p><a href=\"" + resetLink + "\">Click here to set a new password</a></p>" +
                    "<p>This link expires in 1 hour. If you didn't request this, you can safely ignore this email.</p>"
            };
            message.To.Add(toEmail);

            using (var client = new SmtpClient())
            {
                client.Send(message);
            }
        }
    }
}
