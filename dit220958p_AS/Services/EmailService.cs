using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;

namespace dit220958p_AS.Services
{
    public class EmailService
    {
        private readonly string _sendGridApiKey;
        private readonly string _fromEmail;

        public EmailService(IConfiguration configuration)
        {
            _sendGridApiKey = configuration["SendGrid:ApiKey"];
            _fromEmail = configuration["SendGrid:FromEmail"];
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string message)
        {
            if (string.IsNullOrEmpty(_sendGridApiKey))
            {
                throw new Exception("SendGrid API key is not configured.");
            }

            var client = new SendGridClient(_sendGridApiKey);
            var from = new EmailAddress(_fromEmail, "AceJobAgency");
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, message, message);

            // Log the email details
            Console.WriteLine("Sending Email:");
            Console.WriteLine($"From: {from.Email} ({from.Name})");
            Console.WriteLine($"To: {to.Email}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine($"Message: {message}");

            var response = await client.SendEmailAsync(msg);

            Console.WriteLine($"Email Sent Status Code: {response.StatusCode}");

            return response.StatusCode == System.Net.HttpStatusCode.Accepted;
        }
    }
}
