using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Exceptions;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class OtpRepository : IOtpRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<OtpRepository> _logger;

        public OtpRepository(AppDbContext dbContext, ILogger<OtpRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<int> SaveOtpAsync(int otp, string receiver, DateTime generateTime, int companyId)
        {
            try
            {
                _logger.LogInformation("Saving OTP for receiver: {Receiver}, companyId: {CompanyId}", receiver, companyId);

                var otpEntry = new Otp
                {
                    otp_code = otp,
                    receiver = receiver,
                    generate_time = generateTime,
                    company_id = companyId
                };

                _dbContext.otp.Add(otpEntry);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("OTP saved successfully with ID: {OtpId}", otpEntry.otp_id);

                return otpEntry.otp_id;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Error saving OTP entry for receiver: {Receiver}", receiver);
                throw new InvalidOperationException("Error saving OTP entry to the database.", dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while saving the OTP for receiver: {Receiver}", receiver);
                throw new Exception("An unexpected error occurred while saving the OTP.", ex);
            }
        }

        public async Task<Otp?> GetOtpByIdAsync(int otpId)
        {
            try
            {
                _logger.LogInformation("Fetching OTP with ID: {OtpId}", otpId);

                var otp = await _dbContext.otp
                .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.otp_id == otpId);

                if (otp != null)
                {
                    _logger.LogInformation("OTP found with ID: {OtpId}", otpId);
                }
                else
                {
                    _logger.LogWarning("No OTP found with ID: {OtpId}", otpId);
                }

                return otp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving OTP with ID: {OtpId}", otpId);
                throw new Exception("An error occurred while retrieving the OTP.", ex);
            }
        }

        public async Task<int> SaveOtpAsync(int otp, string receiver, DateTime generateTime, int? companyId, OtpSubject subject)
        {
            try
            {
                _logger.LogInformation("Saving OTP for receiver: {Receiver}, companyId: {CompanyId}, subject: {Subject}", receiver, companyId, subject);

                // Convert Enum to string in the proper casing (ensuring it matches PostgreSQL's enum format)
                string subjectString = subject.ToString();

                var otpEntry = new Otp
                {
                    otp_code = otp,
                    receiver = receiver,
                    generate_time = generateTime,
                    company_id = companyId,
                    subject = Enum.Parse<OtpSubject>(subjectString)  // Store enum value directly as OtpSubject
                };

                _dbContext.otp.Add(otpEntry);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("OTP saved successfully with ID: {OtpId}", otpEntry.otp_id);

                return otpEntry.otp_id;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Error saving OTP entry for receiver: {Receiver}, subject: {Subject}", receiver, subject);
                throw new InvalidOperationException("Error saving OTP entry to the database.", dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while saving the OTP for receiver: {Receiver}, subject: {Subject}", receiver, subject);
                throw new Exception("An unexpected error occurred while saving the OTP.", ex);
            }
        }
    }
}