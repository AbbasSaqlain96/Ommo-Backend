using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IOtpService
    {
        Task<ServiceResponse<OtpResult>> GenerateOtpAsync(string receiver, string subject);
        Task<ServiceResponse<OtpResult>> GenerateOtpForSignupAsync(string receiver);
    }
}