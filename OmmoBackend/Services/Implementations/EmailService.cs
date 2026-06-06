using MailKit.Security;
using MimeKit;
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

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Ommo", emailFrom));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = "Ommo OTP";

                message.Body = new TextPart("html")
                {
                    Text = $"Your OTP is: <b>{otpCode}</b>"
                };

                using var client = new MailKit.Net.Smtp.SmtpClient();

                await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(smtpUsername, smtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

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

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Ommo", emailFrom));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;

                message.Body = new TextPart("html")
                {
                    Text = body
                };

                using var client = new MailKit.Net.Smtp.SmtpClient();

                await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(smtpUsername, smtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email successfully sent to {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
                throw;
            }
        }

        public async Task SendWelcomeVerificationEmailAsync(string toEmail, string companyName)
        {
            var subject = "Welcome to ommo — Your account is being verified";

            var body = $@"
                Hi {companyName},
                
                Your company account has been successfully created on ommo.
                
                We are currently reviewing and verifying your account. This process typically takes 24 to 48 hours. Once verification is complete, you will receive a follow-up email and can continue with onboarding.
                
                If you have any questions in the meantime, feel free to reach out.
                
                Welcome aboard,<br/>
                The ommo Team
                ";

            await SendAsync(toEmail, subject, body);
        }
    }
}