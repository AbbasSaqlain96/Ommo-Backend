using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class DriverRepository : GenericRepository<Driver>, IDriverRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<DriverRepository> _logger;
        public DriverRepository(AppDbContext dbContext, ILogger<DriverRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Driver?> GetDriverInfoByUnitIdAsync(int unitId)
        {
            try
            {
                _logger.LogInformation("Fetching driver info for unit ID {UnitId}", unitId);

                // Fetch Driver Id from Unit table
                var unit = await _dbContext.unit
                    .Where(u => u.unit_id == unitId)
                    .FirstOrDefaultAsync();

                if (unit == null || unit.driver_id == 0)
                {
                    _logger.LogWarning("No driver found for unit ID {UnitId}", unitId);
                    return null!;
                }

                // Fetch Driver object from Driver table based on Driver Id
                var driver = await _dbContext.driver
                    .Where(d => d.driver_id == unit.driver_id)
                    .FirstOrDefaultAsync();

                if (driver != null)
                {
                    _logger.LogInformation("Driver found for unit ID {UnitId}: Driver ID {DriverId}", unitId, driver.driver_id);
                }
                else
                {
                    _logger.LogWarning("Driver ID {DriverId} not found in database for unit ID {UnitId}", unit.driver_id, unitId);
                }

                return driver!;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Database operation failed while fetching driver info for unit ID {UnitId}", unitId);
                throw new InvalidOperationException(ErrorMessages.DatabaseOperationFailed, ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update exception occurred while fetching driver info for unit ID {UnitId}", unitId);
                // Handle potential database update issues
                throw new DbUpdateException(ErrorMessages.DatabaseOperationFailed, ex);
            }
        }

        public async Task<IEnumerable<DriverListDto>> GetDriverListByCompanyIdAsync(int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching driver list for company ID {CompanyId}", companyId);

                var drivers = await _dbContext.driver
                     .Where(d => d.company_id == companyId)
                     .Select(d => new DriverListDto
                     {
                         DriverId = d.driver_id,
                         DriverName = d.driver_name,
                         Status = d.status.ToString(),
                         Rating = d.rating
                     })
                     .ToListAsync();

                _logger.LogInformation("Retrieved {Count} drivers for company ID {CompanyId}", drivers.Count, companyId);
                return drivers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching driver list for company ID {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<DriverDetailDto?> GetDriverDetailAsync(int driverId)
        {
            try
            {
                _logger.LogInformation("Fetching driver details for driver ID {DriverId}", driverId);

                var driverDetail = await _dbContext.driver
                       .Where(d => d.driver_id == driverId)
                       .Select(d => new DriverDetailDto
                       {
                           DriverName = d.driver_name,
                           DriverLastName = d.last_name,
                           EmploymentType = d.employment_type.ToString(),
                           CDLLicenseNumber = d.cdl_license_number,
                           Address = d.address,
                           Status = d.status.ToString(),
                           HiringStatus = d.hiring_status.ToString(),
                           LicenseState = d.license_state.ToString(),
                           Email = d.email,
                           PhoneNumber = d.phone_number,
                           Rating = d.rating,
                           CompanyId = d.company_id
                       })
                       .FirstOrDefaultAsync();

                if (driverDetail == null)
                {
                    _logger.LogWarning("Driver ID {DriverId} not found", driverId);
                }
                else
                {
                    _logger.LogInformation("Driver details retrieved successfully for driver ID {DriverId}", driverId);
                }

                return driverDetail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching driver details for driver ID {DriverId}", driverId);
                throw;
            }
        }

        public async Task<List<int>> GetRequiredDocumentTypesAsync()
        {
            try
            {
                _logger.LogInformation("Fetching required document types for drivers.");

                var documentTypes = await _dbContext.document_type
                .Where(dt => dt.doc_type == DocType.driver_doc)
                .Select(dt => dt.doc_type_id)
                .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} required document types.", documentTypes.Count);
                return documentTypes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching required document types.");
                return new List<int>();
            }
        }

        public async Task<Driver> AddDriverAsync(int companyId, DriverInfoDto driverInfo)
        {
            try
            {
                _logger.LogInformation("Adding a new driver for company ID {CompanyId}.", companyId);

                // Validate the enum values for EmploymentType, Status, HiringStatus, and LicenseState
                if (!Enum.TryParse(driverInfo.EmploymentType, true, out EmploymentType employmentType))
                    throw new ArgumentException($"Invalid employment type: {driverInfo.EmploymentType}");

                if (!Enum.TryParse(driverInfo.Status, true, out DriverStatus driverStatus))
                    throw new ArgumentException($"Invalid status: {driverInfo.Status}");

                if (!Enum.TryParse(driverInfo.HiringStatus, true, out HiringStatus hiringStatus))
                    throw new ArgumentException($"Invalid hiring status: {driverInfo.HiringStatus}");

                if (!Enum.TryParse(driverInfo.LicenseState, true, out LicenseState licenseState))
                    throw new ArgumentException($"Invalid license state: {driverInfo.LicenseState}");

                // Create driver entity and save it to the database
                var driver = new Driver
                {
                    driver_name = driverInfo.DriverName,
                    last_name = driverInfo.DriverLastName,
                    employment_type = employmentType,
                    cdl_license_number = driverInfo.CDLLicenseNumber,
                    address = driverInfo.Address,
                    status = driverStatus,
                    hiring_status = hiringStatus,
                    license_state = licenseState,
                    email = driverInfo.Email,
                    phone_number = driverInfo.PhoneNumber,
                    rating = driverInfo.Rating,
                    is_assign = false,
                    company_id = companyId
                };

                // Add to DB context and save changes
                _dbContext.driver.Add(driver);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Driver successfully added with ID {DriverId} for company ID {CompanyId}.", driver.driver_id, companyId);
                // Return the driver on successful insertion
                return driver;
            }
            catch (ArgumentException ex) // Catch validation errors with a meaningful message
            {
                _logger.LogWarning(ex, "Validation error while adding driver for company ID {CompanyId}.", companyId);
                throw new InvalidOperationException($"Error while adding driver: {ex.Message}");
            }
            catch (Exception ex) // General exception handling
            {
                _logger.LogError(ex, "Error occurred while adding driver for company ID {CompanyId}.", companyId);
                throw new Exception(ex.Message);
            }
        }

        public async Task AddDriverDocumentAsync(int driverId, int docTypeId, string path)
        {
            try
            {
                _logger.LogInformation("Adding document for driver ID {DriverId}, document type ID {DocTypeId}.", driverId, docTypeId);

                var driverDoc = new DriverDoc
                {
                    driver_id = driverId,
                    doc_type_id = docTypeId,
                    file_path = path,
                    status = DriverDocStatus.active
                };

                _dbContext.driver_doc.Add(driverDoc);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Document successfully added for driver ID {DriverId}.", driverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding document for driver ID {DriverId}.", driverId);
                throw;
            }
        }

        public async Task<DriverInfoDto?> GetDriverByCDLOrEmailAsync(string cdlLicenseNumber, string email, string phoneNumber)
        {
            try
            {
                _logger.LogInformation("Fetching driver by CDL License Number {CDL}, Email {Email}, or Phone Number {Phone}.", cdlLicenseNumber, email, phoneNumber);

                var driver = await _dbContext.driver
                    .Where(d => d.cdl_license_number == cdlLicenseNumber || d.email == email || d.phone_number == phoneNumber)
                    .Select(d => new DriverInfoDto
                    {
                        CDLLicenseNumber = d.cdl_license_number,
                        Email = d.email,
                        PhoneNumber = d.phone_number

                    })
                    .FirstOrDefaultAsync();
                
                if (driver != null)
                {
                    _logger.LogInformation("Driver found with CDL {CDL} or Email {Email}.", cdlLicenseNumber, email);
                }
                else
                {
                    _logger.LogWarning("No driver found with CDL {CDL} or Email {Email}.", cdlLicenseNumber, email);
                }
                return driver;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching driver by CDL License Number {CDL} or Email {Email}.", cdlLicenseNumber, email);
                throw;
            }
        }

        public async Task<bool> IsValidDriverIdAsync(int driverId, int companyId)
        {
            return await _dbContext.driver.AnyAsync(d => d.driver_id == driverId && d.company_id == companyId);
        }

        public async Task<bool> ExistsAsync(int driverId)
        {
            return await _dbContext.driver
                .AnyAsync(d => d.driver_id == driverId);
        }
    }
}