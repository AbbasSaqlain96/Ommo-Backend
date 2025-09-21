using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IOtpVerificationService
    {
        Task<ServiceResponse<string>> VerifyOtpAsync(int otpId, int otpNumber);
    }
}