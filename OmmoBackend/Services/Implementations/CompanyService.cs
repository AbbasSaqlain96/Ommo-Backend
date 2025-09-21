using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Implementations;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class CompanyService : ICompanyService
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly ICarrierRepository _carrierRepository;
        private readonly IDispatchServiceRepository _dispatchServiceRepository;
        private readonly ILogger<CompanyService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the CompanyService class with the specified repositories.
        /// </summary>
        public CompanyService(
            ICompanyRepository companyRepository,
            ICarrierRepository carrierRepository,
            IDispatchServiceRepository dispatchServiceRepository,
            ILogger<CompanyService> logger,
            IConfiguration configuration,
            IUnitOfWork unitOfWork,
            AppDbContext dbContext)
        {
            _companyRepository = companyRepository;
            _carrierRepository = carrierRepository;
            _dispatchServiceRepository = dispatchServiceRepository;
            _logger = logger;
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Creates a new company asynchronously and handles additional setup based on the company type.
        /// </summary>
        /// <param name="createCompanyRequest">The request data for creating a company.</param>
        /// <returns>A CompanyCreationResult indicating the outcome of the company creation operation.</returns>
        public async Task<ServiceResponse<CompanyCreationResult>> CreateCompanyAsync(CreateCompanyRequest createCompanyRequest)
        {
            _logger.LogInformation("Attempting to create a new company with MC Number: {MCNumber} and Company Type: {CompanyType}", createCompanyRequest.MCNumber, createCompanyRequest.CompanyType);

            try
            {
                // Required: Company Name
                if (string.IsNullOrWhiteSpace(createCompanyRequest.Name))
                    return ServiceResponse<CompanyCreationResult>.ErrorResponse("Company name is required.", 400);

                // Validate contact methods
                if (string.IsNullOrWhiteSpace(createCompanyRequest.Email) && string.IsNullOrWhiteSpace(createCompanyRequest.Phone))
                    return ServiceResponse<CompanyCreationResult>.ErrorResponse("At least one contact method (email or phone number) is required.", 400);

                if (!string.IsNullOrWhiteSpace(createCompanyRequest.Email) && !ValidationHelper.IsValidEmail(createCompanyRequest.Email))
                    return ServiceResponse<CompanyCreationResult>.ErrorResponse("Invalid email address format.", 400);

                if (!string.IsNullOrWhiteSpace(createCompanyRequest.Phone) && !ValidationHelper.IsValidPhoneNumber(createCompanyRequest.Phone))
                    return ServiceResponse<CompanyCreationResult>.ErrorResponse("Invalid phone number format.", 400);

                // Check for duplicates
                var duplicateCheckResult = await CheckDuplicateEmailAndPhoneAsync(createCompanyRequest.Email, createCompanyRequest.Phone);
                if (duplicateCheckResult.HasDuplicate)
                    return ServiceResponse<CompanyCreationResult>.ErrorResponse(duplicateCheckResult.Message!, 400);

                // Validate company type
                if (createCompanyRequest.CompanyType != 1 && createCompanyRequest.CompanyType != 2)
                    return ServiceResponse<CompanyCreationResult>.ErrorResponse("Invalid company type. Must be 1 (Carrier) or 2 (Dispatcher).", 422);

                // Validate category type
                if (createCompanyRequest.CategoryType != 1 && createCompanyRequest.CategoryType != 2 && createCompanyRequest.CategoryType != 3)
                    return ServiceResponse<CompanyCreationResult>.ErrorResponse("Invalid category type. Must be 1 (Independent), 2 (Parent), or 3 (Subsidized).", 422);

                // Check MC number for carrier
                if (createCompanyRequest.CompanyType == 1)
                {
                    if (string.IsNullOrWhiteSpace(createCompanyRequest.MCNumber))
                        return ServiceResponse<CompanyCreationResult>.ErrorResponse("MC number is required for carrier companies.", 400);

                    bool isMCNumberDuplicate = await _companyRepository.CheckDuplicateMCNumberAsync(createCompanyRequest.MCNumber, createCompanyRequest.CompanyType);
                    if (isMCNumberDuplicate)
                        return ServiceResponse<CompanyCreationResult>.ErrorResponse("MC number already exists for another carrier", 409);
                }

                // Validate ParentId
                if (createCompanyRequest.ParentId != 0)
                {
                    bool parentExists = await _companyRepository.CheckCompanyExistsAsync(createCompanyRequest.ParentId);
                    if (!parentExists)
                        return ServiceResponse<CompanyCreationResult>.ErrorResponse("Parent company not found.", 404);
                }


                // Call the repository to create a company
                var companyResult = await _companyRepository.CreateCompanyAsync(createCompanyRequest);

                if (!companyResult.Success)
                {
                    _logger.LogError("Company creation failed: {ErrorMessage}", companyResult.ErrorMessage);
                    return ServiceResponse<CompanyCreationResult>.ErrorResponse(companyResult.ErrorMessage ?? "Company creation failed.", 400);
                }

                _logger.LogInformation("Company created successfully with ID: {CompanyId}", companyResult.CompanyId);
                return ServiceResponse<CompanyCreationResult>.SuccessResponse(companyResult, "Company created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Operation could not be completed due to an invalid request.");
                return ServiceResponse<CompanyCreationResult>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        /// <summary>
        /// Creates a child company (Carrier or Dispatch Service) from its parent company asynchronously.
        /// </summary>
        /// <param name="createChildCompanyRequest">The request data for adding the child company.</param>
        /// <returns>A ChildCompanyCreationResult indicating the outcome of the addition operation.</returns>
        // public async Task<ChildCompanyCreationResult> CreateChildCompanyAsync(CreateChildCompanyRequest createChildCompanyRequest)
        // {
        //     // Retrieve the parent company by its Id from the repository
        //     var parent = await _parentCompanyRepository.GetByIdAsync(createChildCompanyRequest.ParentId);

        //     // If the parent company does not exist, return a failure result with an appropriate error message
        //     if (parent is null)
        //     {
        //         return new ChildCompanyCreationResult
        //         {
        //             Success = false,
        //             ErrorMessage = "Parent Company not found."
        //         };
        //     }

        //     // Check if the request is to create a Carrier type (Type 1)
        //     if (createChildCompanyRequest.Type == 1)
        //     {
        //         // Retrieve the carrier by its ID from the repository
        //         var carrier = await _carrierRepository.GetByIdAsync(createChildCompanyRequest.CarrierOrDispatchId);

        //         // If the carrier does not exist, return a failure result with an error message
        //         if (carrier == null)
        //         {
        //             return new ChildCompanyCreationResult
        //             {
        //                 Success = false,
        //                 ErrorMessage = "Carrier not found."
        //             };
        //         }

        //         // Assign the parent company Id to the carrier and update it in the repository
        //         carrier.parent_company_id = createChildCompanyRequest.ParentId;
        //         await _carrierRepository.UpdateAsync(carrier);
        //     }
        //     // Check if the request is to create a Dispatch Service type (Type 2)
        //     else if (createChildCompanyRequest.Type == 2)
        //     {
        //         // Retrieve the dispatch service by its Id from the repository
        //         var dispatchService = await _dispatchServiceRepository.GetByIdAsync(createChildCompanyRequest.CarrierOrDispatchId);

        //         // If the dispatch service does not exist, return a failure result with an error message
        //         if (dispatchService == null)
        //         {
        //             return new ChildCompanyCreationResult
        //             {
        //                 Success = false,
        //                 ErrorMessage = "Dispatch Service not found."
        //             };
        //         }

        //         // Assign the parent company Id to the dispatch service and update it in the repository
        //         dispatchService.parent_company_id = createChildCompanyRequest.ParentId;
        //         await _dispatchServiceRepository.UpdateAsync(dispatchService);
        //     }

        //     // Return a success result indicating that the child company was created successfully
        //     return new ChildCompanyCreationResult
        //     {
        //         Success = true
        //     };
        // }

        /// <summary>
        /// Removes a child company (Carrier or Dispatch Service) from its parent company asynchronously.
        /// </summary>
        /// <param name="removeChildCompanyRequest">The request data for removing the child company.</param>
        /// <returns>A ChildCompanyRemovalResult indicating the outcome of the removal operation.</returns>
        // public async Task<ChildCompanyRemovalResult> RemoveChildCompanyAsync(RemoveChildCompanyRequest removeChildCompanyRequest)
        // {
        //     // Retrieve the parent company by Id; return an error if the parent company is not found
        //     var parent = await _parentCompanyRepository.GetByIdAsync(removeChildCompanyRequest.ParentId);
        //     if (parent is null)
        //     {
        //         return new ChildCompanyRemovalResult
        //         {
        //             Success = false,
        //             ErrorMessage = "Parent Company not found."
        //         };
        //     }

        //     // If the type is Carrier (Type 1), attempt to find the Carrier and verify the relationship with the parent company
        //     if (removeChildCompanyRequest.Type == 1)
        //     {
        //         var carrier = await _carrierRepository.GetByIdAsync(removeChildCompanyRequest.CarrierOrDispatchId);
        //         if (carrier == null || carrier.parent_company_id != removeChildCompanyRequest.ParentId)
        //         {
        //             return new ChildCompanyRemovalResult
        //             {
        //                 Success = false,
        //                 ErrorMessage = "Carrier not found or does not belong to the specified Parent Company."
        //             };
        //         }

        //         // Remove the parent company association by setting the Parent Company Id to null
        //         carrier.parent_company_id = null;
        //         await _carrierRepository.UpdateAsync(carrier);
        //     }
        //     // If the type is Dispatch Service (Type 2), attempt to find the Dispatch Service and verify the relationship with the parent company
        //     else if (removeChildCompanyRequest.Type == 2)
        //     {
        //         var dispatchService = await _dispatchServiceRepository.GetByIdAsync(removeChildCompanyRequest.CarrierOrDispatchId);
        //         if (dispatchService == null || dispatchService.parent_company_id != removeChildCompanyRequest.ParentId)
        //         {
        //             return new ChildCompanyRemovalResult
        //             {
        //                 Success = false,
        //                 ErrorMessage = "Dispatch Service not found or does not belong to the specified Parent Company."
        //             };
        //         }

        //         // Remove the parent company association by setting the Parent Company Id to null
        //         dispatchService.parent_company_id = null;
        //         await _dispatchServiceRepository.UpdateAsync(dispatchService);
        //     }

        //     // Return success if the removal process completed without issues
        //     return new ChildCompanyRemovalResult
        //     {
        //         Success = true
        //     };
        // }

        /// <summary>
        /// Checks if a company with the specified Id exists.
        /// </summary>
        /// <param name="companyId">The Id of the company to check.</param>
        /// <returns>True if the company exists; otherwise, false.</returns>
        public async Task<bool> CompanyIdExist(int companyId)
        {
            _logger.LogInformation("Checking if company with ID {CompanyId} exists.", companyId);

            var company = await _companyRepository.GetByIdAsync(companyId);
            bool exists = company != null;

            _logger.LogInformation("Company existence check for ID {CompanyId}: {Exists}", companyId, exists);
            return exists;
        }

        public async Task<DuplicateCheckResult> CheckDuplicateEmailAndPhoneAsync(string email, string phone)
        {
            _logger.LogInformation("Checking for duplicate email {Email} and phone {Phone}", email, phone);

            var (isEmailDuplicate, isPhoneDuplicate) = await _companyRepository.CheckDuplicateEmailAndPhoneInCompanyAsync(email!, phone!);

            if (isEmailDuplicate && isPhoneDuplicate)
            {
                _logger.LogWarning("Duplicate email {Email} and phone {Phone} found.", email, phone);
                return new DuplicateCheckResult
                {
                    HasDuplicate = true,
                    Message = "Duplicate email and phone number found."
                };
            }

            if (isEmailDuplicate)
            {
                _logger.LogWarning("Duplicate email {Email} found.", email);
                return new DuplicateCheckResult
                {
                    HasDuplicate = true,
                    Message = "The email is already registered with another company."
                };
            }

            if (isPhoneDuplicate)
            {
                _logger.LogWarning("Duplicate phone {Phone} found.", phone);
                return new DuplicateCheckResult
                {
                    HasDuplicate = true,
                    Message = "The phone number is already registered with another company."
                };
            }

            _logger.LogInformation("No duplicates found for email {Email} and phone {Phone}.", email, phone);
            return new DuplicateCheckResult
            {
                HasDuplicate = false,
                Message = null
            };
        }

        public async Task<ServiceResponse<CompanyProfileDto>> GetCompanyProfileAsync(int companyId)
        {
            _logger.LogInformation("Fetching company profile for ID {CompanyId}.", companyId);

            try
            {
                var profile = await _companyRepository.GetCompanyProfileAsync(companyId);

                if (profile == null)
                {
                    _logger.LogWarning("Company profile not found for ID {CompanyId}.", companyId);
                    return ServiceResponse<CompanyProfileDto>.ErrorResponse("Company not found.", StatusCodes.Status404NotFound);
                }

                _logger.LogInformation("Company profile retrieved for ID {CompanyId}.", companyId);
                return ServiceResponse<CompanyProfileDto>.SuccessResponse(profile, "Company profile retrieved successfully.");
            }
            catch (KeyNotFoundException knfEx)
            {
                _logger.LogWarning(knfEx, "Company profile not found for ID {CompanyId}.", companyId);
                return ServiceResponse<CompanyProfileDto>.ErrorResponse(knfEx.Message, StatusCodes.Status404NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving company profile for ID {CompanyId}.");
                return ServiceResponse<CompanyProfileDto>.ErrorResponse("Server is temporarily unavailable. Please try again later.", StatusCodes.Status503ServiceUnavailable);
            }
        }

        public async Task<ServiceResponse<CompanyProfileDto>> UpdateCompanyProfileAsync(int companyId, UpdateCompanyProfileDto updateDto)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _unitOfWork.BeginTransactionAsync();

                _logger.LogInformation("Updating company profile for ID {CompanyId}.", companyId);

                try
                {
                    var company = await _companyRepository.GetByIdAsync(companyId);

                    if (company == null)
                    {
                        _logger.LogWarning("Company with ID {CompanyId} does not exist.", companyId);
                        return ServiceResponse<CompanyProfileDto>.ErrorResponse("Company with the provided ID does not exist.", StatusCodes.Status404NotFound);
                    }

                    // Validate email format
                    if (!string.IsNullOrEmpty(updateDto.Email) && !ValidationHelper.IsValidEmail(updateDto.Email))
                    {
                        _logger.LogWarning("Invalid email format for Company ID {CompanyId}.", companyId);
                        return ServiceResponse<CompanyProfileDto>.ErrorResponse("Invalid email address format.", StatusCodes.Status400BadRequest);
                    }

                    // Validate phone format
                    if (!string.IsNullOrEmpty(updateDto.Phone) && !ValidationHelper.IsValidPhoneNumber(updateDto.Phone))
                    {
                        _logger.LogWarning("Invalid phone format for Company ID {CompanyId}.", companyId);
                        return ServiceResponse<CompanyProfileDto>.ErrorResponse("Invalid phone number format.", StatusCodes.Status400BadRequest);
                    }

                    // Update only provided fields
                    if (!string.IsNullOrEmpty(updateDto.Name))
                        company.name = updateDto.Name;

                    if (!string.IsNullOrEmpty(updateDto.Address))
                        company.address = updateDto.Address;

                    if (!string.IsNullOrEmpty(updateDto.Phone))
                        company.phone = updateDto.Phone;

                    if (!string.IsNullOrEmpty(updateDto.Email))
                        company.email = updateDto.Email;

                    if (!string.IsNullOrEmpty(updateDto.TaxID))
                        company.tax_id = updateDto.TaxID;

                    if (!string.IsNullOrEmpty(updateDto.DOTNumber))
                        company.dot_number = updateDto.DOTNumber;

                    // Handle logo upload
                    if (updateDto.Logo != null)
                    {

                        if (!ValidationHelper.IsValidImageFormat(updateDto.Logo, new[] { ".jpeg", ".jpg", ".png", ".webp" }))
                        {
                            return ServiceResponse<CompanyProfileDto>.ErrorResponse("Invalid image format. Only JPEG, JPG, PNG, and WEBP formats are allowed.", 400);
                        }

                        string logoPath = await UpdateLogoAsync(updateDto.Logo, companyId);
                        company.logo = logoPath;
                    }

                    // --- Update Carrier.MCNumber if provided ---
                    if (!string.IsNullOrEmpty(updateDto.MCNumber))
                    {
                        var carrier = await _carrierRepository.GetCarrierByCompanyIdAsync(companyId);

                        if (carrier != null)
                        {
                            carrier.mc_number = updateDto.MCNumber;
                            await _carrierRepository.UpdateAsync(carrier);
                        }
                        else if (!string.IsNullOrEmpty(updateDto.MCNumber))
                        {
                            // If no carrier row exists, create one
                            carrier = new Carrier
                            {
                                company_id = companyId,
                                mc_number = updateDto.MCNumber
                            };
                            await _carrierRepository.AddAsync(carrier);
                        }
                    }

                    // Save changes
                    await _companyRepository.UpdateAsync(company);

                    await transaction.CommitAsync();

                    _logger.LogInformation("Company profile updated successfully for ID {CompanyId}.", companyId);
                    return ServiceResponse<CompanyProfileDto>.SuccessResponse(null, "Company profile updated successfully.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    _logger.LogError(ex, "Unexpected error while updating company profile for ID {CompanyId}.", companyId);
                    return ServiceResponse<CompanyProfileDto>.ErrorResponse("Server is temporarily unavailable. Please try again later.", StatusCodes.Status503ServiceUnavailable);
                }
            });
        }

        public async Task<string> UpdateLogoAsync(IFormFile logo, int companyId)
        {
            string baseFolderPath = _configuration.GetValue<string>("AppSettings:ServerLogoDirectory");

            if (string.IsNullOrWhiteSpace(baseFolderPath))
                throw new InvalidOperationException("Server logo directory is not configured.");

            string folderPath = Path.Combine(baseFolderPath);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");

            // Save new file
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(logo.FileName)}";
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await logo.CopyToAsync(stream);
            }

            return $"{serverUrl}/Logo/{companyId}/{fileName}";
        }
    }
}