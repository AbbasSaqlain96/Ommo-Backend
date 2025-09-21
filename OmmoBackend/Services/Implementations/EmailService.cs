using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendOtpEmailAsync(string toEmail, int otpCode)
        {
            _logger.LogInformation("Starting OTP email sending process to {ToEmail}", toEmail);

            try
            {
                var smtpServer = _configuration["EmailSettings:Server"];
                var smtpPort = int.Parse(_configuration["EmailSettings:Port"]);
                var smtpUsername = _configuration["EmailSettings:Username"];
                var smtpPassword = _configuration["EmailSettings:Password"];
                var emailFrom = _configuration["EmailSettings:EmailFrom"];

                if (string.IsNullOrWhiteSpace(smtpServer) || string.IsNullOrWhiteSpace(emailFrom))
                {
                    _logger.LogError("SMTP configuration is missing or invalid.");
                    throw new InvalidOperationException("SMTP configuration is missing.");
                }

                using var smtpClient = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(emailFrom),
                    Subject = "Ommo OTP",
                    Body = $"Your OTP is: {otpCode}",
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(toEmail);

                _logger.LogInformation("Sending OTP email to {ToEmail} with OTP: {OtpCode}", toEmail, otpCode);

                // Sending email asynchronously
                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("OTP email successfully sent to {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {ToEmail}", toEmail);
                throw;
            }
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            _logger.LogInformation("Starting email sending process to {To}", to);

            try
            {
                var smtpServer = _configuration["EmailSettings:Server"];
                var smtpPort = int.Parse(_configuration["EmailSettings:Port"]);
                var smtpUsername = _configuration["EmailSettings:Username"];
                var smtpPassword = _configuration["EmailSettings:Password"];
                var emailFrom = _configuration["EmailSettings:EmailFrom"];

                if (string.IsNullOrWhiteSpace(smtpServer) || string.IsNullOrWhiteSpace(emailFrom))
                {
                    _logger.LogError("SMTP configuration is missing or invalid.");
                    throw new InvalidOperationException("SMTP configuration is missing.");
                }

                using var smtpClient = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(emailFrom),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true, // Assume HTML is allowed; could make configurable
                };

                mailMessage.To.Add(to);

                _logger.LogInformation("Sending email to {To} with subject: {Subject}", to, subject);

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Email successfully sent to {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
                throw;
            }
        }
    }
}