using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string email, int otpCode);

        Task SendAsync(string to, string subject, string body);
    }
}