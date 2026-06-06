namespace OmmoBackend.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string email, int otpCode);

        Task SendAsync(string to, string subject, string body);

        Task SendWelcomeVerificationEmailAsync(string toEmail, string companyName);
    }
}