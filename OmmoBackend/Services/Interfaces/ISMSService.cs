using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Services.Interfaces
{
    public interface ISMSService
    {
        Task SendOtpSMSAsync(string phoneNumber, int otpCode);
    }
}