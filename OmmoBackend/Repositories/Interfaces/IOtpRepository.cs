using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IOtpRepository
    {
        Task<int> SaveOtpAsync(int otp, string receiver, DateTime generateTime, int? companyId, OtpSubject subject);
        Task<int> SaveOtpAsync(int otp, string receiver, DateTime generateTime, int companyId);
        Task<Otp> GetOtpByIdAsync(int otpId);
    }
}