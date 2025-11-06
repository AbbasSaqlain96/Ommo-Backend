
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class CompanyRepository : GenericRepository<Company>, ICompanyRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<CompanyRepository> _logger;
        /// <summary>
        /// Initializes a new instance of the CompanyRepository class with the specified AppDbContext.
        /// </summary>
        public CompanyRepository(AppDbContext dbContext, ILogger<CompanyRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Checks for the uniqueness of the email and phone number among companies.
        /// Ensures that the email and phone number are not duplicated within the company entities.
        /// </summary>
        /// <param name="email">The email to check for duplicates.</param>
        /// <param name="phone">The phone number to check for duplicates.</param>
        /// <returns>A tuple containing a boolean indicating whether a duplicate email exists, and a boolean indicating whether a duplicate phone number exists.</returns>
        public async Task<(bool IsEmailDuplicate, bool IsPhoneDuplicate)> CheckDuplicateEmailAndPhoneInCompanyAsync(string email, string phone)
        {
            try
            {
                _logger.LogInformation("Checking for duplicate email ({Email}) and phone ({Phone}).", email, phone);

                bool isEmailDuplicate = false;
                bool isPhoneDuplicate = false;

                if (!string.IsNullOrWhiteSpace(email))
                {
                    // Check if the email already exists in the database
                    isEmailDuplicate = await _dbContext.company
                        .AnyAsync(c => c.email == email);
                }

                if (!string.IsNullOrWhiteSpace(phone))
                {
                    // Check if the phone number already exists in the database
                    isPhoneDuplicate = await _dbContext.company
                        .AnyAsync(c => c.phone == phone);
                }

                _logger.LogInformation("Duplicate check results - Email: {IsEmailDuplicate}, Phone: {IsPhoneDuplicate}", isEmailDuplicate, isPhoneDuplicate);

                return (isEmailDuplicate, isPhoneDuplicate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while checking for duplicate email ({Email}) or phone ({Phone}).", email, phone);
                throw new Exception("An error occurred while checking for duplicate email or phone.", ex);
            }
        }

        /// <summary>
        /// Checks for the uniqueness of the MC number among carriers, applicable only for companies of type 1.
        /// Ensures that the MC number is not duplicated within the carrier entities.
        /// </summary>
        /// <param name="mcNumber">The MC number to check for duplicates.</param>
        /// <param name="companyType">The type of the company, where type 1 indicates a carrier.</param>
        /// <returns>A boolean indicating whether a duplicate MC number exists for the specified company type.</returns>
        public async Task<bool> CheckDuplicateMCNumberAsync(string mcNumber, int companyType)
        {
            try
            {
                _logger.LogInformation("Checking for duplicate MC number ({MCNumber}) for company type {CompanyType}.", mcNumber, companyType);

                // Check for unique MC number, applicable only for CompanyType 1
                var hasDuplicateMC = companyType == 1 && !string.IsNullOrWhiteSpace(mcNumber) &&
                                     await _dbContext.carrier.AnyAsync(c => c.mc_number == mcNumber);

                _logger.LogInformation("Duplicate MC number check result for {MCNumber}: {HasDuplicateMC}", mcNumber, hasDuplicateMC);

                return hasDuplicateMC;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while checking for duplicate MC number ({MCNumber}) for company type {CompanyType}.", mcNumber, companyType);
                throw new Exception("An error occurred while checking for duplicate MC number.", ex);
            }
        }

        public async Task<CompanyCreationResult> CreateCompanyAsync(CreateCompanyRequest createCompanyRequest)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                try
                {
                    _logger.LogInformation("Starting transaction to create a new company: {CompanyName}", createCompanyRequest.Name);

                    // Insert company
                    var company = new Company
                    {
                        name = createCompanyRequest.Name,
                        email = string.IsNullOrWhiteSpace(createCompanyRequest.Email) ? null : createCompanyRequest.Email,
                        address = createCompanyRequest.Address,
                        phone = string.IsNullOrWhiteSpace(createCompanyRequest.Phone) ? null : createCompanyRequest.Phone,
                        company_type = createCompanyRequest.CompanyType,
                        parent_id = Convert.ToInt32(createCompanyRequest.ParentId),
                        category_type = createCompanyRequest.CategoryType ?? 1,
                        status = string.IsNullOrWhiteSpace(createCompanyRequest.Status) ? CompanyStatus.active : Enum.Parse<CompanyStatus>(createCompanyRequest.Status, ignoreCase: true)
                    };

                    await _dbContext.company.AddAsync(company);
                    await _dbContext.SaveChangesAsync();

                    var CompanyId = company.company_id;
                    _logger.LogInformation("Company created successfully with ID: {CompanyId}", CompanyId);

                    // Insert carrier or dispatch service depending on the company type
                    if (createCompanyRequest.CompanyType == 1)
                    {
                        _logger.LogInformation("Creating carrier for company ID: {CompanyId} with MCNumber: {MCNumber}", CompanyId, createCompanyRequest.MCNumber);
                        await CreateCarrierAsync(CompanyId, createCompanyRequest.MCNumber);
                    }

                    else if (createCompanyRequest.CompanyType == 2)
                    {
                        _logger.LogInformation("Creating dispatch service for company ID: {CompanyId}", CompanyId);
                        await CreateDispatchServiceAsync(CompanyId);
                    }

                    // Commit transaction if everything succeeds
                    await transaction.CommitAsync();
                    _logger.LogInformation("Transaction committed successfully for company ID: {CompanyId}", CompanyId);

                    return new CompanyCreationResult
                    {
                        CompanyId = CompanyId,
                        Success = true
                    };
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Handle concurrency issues
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Concurrency error occurred while creating company: {CompanyName}", createCompanyRequest.Name);
                    return new CompanyCreationResult
                    {
                        Success = false,
                        ErrorMessage = "Concurrency error occurred. Please try again."
                    };
                }
                catch (DbUpdateException ex)
                {
                    // Handle errors that occur when saving to the database
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Database update error occurred while creating company: {CompanyName}", createCompanyRequest.Name);
                    return new CompanyCreationResult
                    {
                        Success = false,
                        ErrorMessage = "Database update error occurred. Please check your inputs."
                    };
                }
                catch (ArgumentNullException ex)
                {
                    // Handle null argument exceptions
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "A required argument was null while creating company: {CompanyName}", createCompanyRequest.Name);
                    return new CompanyCreationResult
                    {
                        Success = false,
                        ErrorMessage = "A required argument was null."
                    };
                }
                catch (Exception ex)
                {
                    // Rollback the transaction in case of any exception
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "An unexpected error occurred while creating company: {CompanyName}", createCompanyRequest.Name);

                    // Log or handle the exception as needed
                    return new CompanyCreationResult
                    {
                        Success = false,
                        ErrorMessage = "An unexpected error occurred while creating the company. Please try again."
                    };
                }
                finally
                {
                    if (transaction != null)
                    {
                        await transaction.DisposeAsync();
                    }
                }
            });
        }

        private async Task CreateCarrierAsync(int companyId, string? mcNumber)
        {
            try
            {
                _logger.LogInformation("Creating carrier for company ID: {CompanyId} with MCNumber: {MCNumber}", companyId, mcNumber);

                var carrier = new Carrier
                {
                    company_id = companyId,
                    mc_number = mcNumber
                };
                await _dbContext.carrier.AddAsync(carrier);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Carrier created successfully for company ID: {CompanyId}", companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating carrier for company ID: {CompanyId}", companyId);
                throw;
            }
        }

        private async Task CreateDispatchServiceAsync(int companyId)
        {
            try
            {
                _logger.LogInformation("Creating dispatch service for company ID: {CompanyId}", companyId);

                var dispatchService = new DispatchService
                {
                    company_id = companyId
                };
                await _dbContext.dispatch_service.AddAsync(dispatchService);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Dispatch service created successfully for company ID: {CompanyId}", companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating dispatch service for company ID: {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<CompanyDialInfoDto?> GetCompanyDialInfoAsync(int companyId)
        {

            return await _dbContext.company
            .AsNoTracking()
            .Where(c => c.company_id == companyId)
            .Select(c => new CompanyDialInfoDto(c.name, c.twilio_number ?? ""))
            .FirstOrDefaultAsync();
        }
        public async Task<CompanyProfileDto> GetCompanyProfileAsync(int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching company profile for company ID: {CompanyId}", companyId);

                var company = await (
                    from c in _dbContext.company
                    join cr in _dbContext.carrier
                        on c.company_id equals cr.company_id into carrierJoin
                    from cr in carrierJoin.DefaultIfEmpty()
                    where c.company_id == companyId
                    select new CompanyProfileDto
                    {
                        CompanyId = c.company_id,
                        Name = c.name,
                        Address = c.address,
                        Email = c.email,
                        Phone = c.phone,
                        UserCount = _dbContext.users.Count(u => u.company_id == companyId),
                        CompanyType = c.company_type == 1 ? "Carrier" : "Dispatch Service",
                        CategoryType = c.category_type == 1
                            ? "Independent"
                            : c.category_type == 2
                            ? "Parent"
                            : c.category_type == 3
                            ? "Subsidy"
                            : "Unknown",
                        CreatedAt = c.created_at,
                        TaxID = c.tax_id,
                        DOTNumber = c.dot_number,
                        Logo = c.logo,
                        MCNumber = cr.mc_number
                    })
                    .FirstOrDefaultAsync();

                if (company == null)
                {
                    _logger.LogWarning("Company with ID {CompanyId} does not exist.", companyId);
                    throw new KeyNotFoundException($"Company with id {companyId} does not exist.");
                }

                _logger.LogInformation("Company profile fetched successfully for company ID: {CompanyId}", companyId);
                return company;
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Company with ID {CompanyId} not found.", companyId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the company profile for company ID: {CompanyId}", companyId);
                throw new Exception("An error occurred while fetching the company profile.");
            }
        }

        public async Task<bool> CheckCompanyExistsAsync(int? parentId)
        {
            return await _dbContext.company.AnyAsync(c => c.parent_id == parentId);
        }

        public async Task<bool> ExistsAsync(int companyId)
        {
            try
            {
                _logger.LogInformation("Checking if company exists with CompanyId: {CompanyId}", companyId);

                var exists = await _dbContext.company.AnyAsync(c => c.company_id == companyId);

                _logger.LogInformation("Company existence check for CompanyId {CompanyId}: {Exists}", companyId, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if CompanyId: {CompanyId} exists", companyId);
                throw;
            }
        }
    }
}