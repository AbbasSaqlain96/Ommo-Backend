using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Implementations;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;
using System.Text.Json;

namespace OmmoBackend.Services.Implementations
{
    public class IntegrationService : IIntegrationService
    {
        private readonly IIntegrationRepository _integrationRepository;
        private readonly IUserService _userService;
        private readonly ILogger<IntegrationService> _logger;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly ISendEmailRepository _sendEmailRepository;
        private readonly IEncryptionService _encryption;
        private readonly IGlobalIntegrationCredentialRepository _globalIntegrationCredentialRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _dbContext;

        public IntegrationService(IIntegrationRepository integrationRepository, IUserService userService, ILogger<IntegrationService> logger, IConfiguration config, IEmailService emailService, ISendEmailRepository sendEmailRepository, IEncryptionService encryption, IGlobalIntegrationCredentialRepository globalIntegrationCredentialRepository, IUnitOfWork unitOfWork, AppDbContext dbContext)
        {
            _integrationRepository = integrationRepository;
            _userService = userService;
            _logger = logger;
            _config = config;
            _emailService = emailService;
            _sendEmailRepository = sendEmailRepository;
            _encryption = encryption;
            _globalIntegrationCredentialRepository = globalIntegrationCredentialRepository;
            _unitOfWork = unitOfWork;
            _dbContext = dbContext;
        }

        public async Task<ServiceResponse<List<IntegrationDto>>> GetIntegrationsAsync(int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching Integrations");

                var integrations = await _integrationRepository.GetIntegrationsByCompanyAsync(companyId);

                if (integrations == null || !integrations.Any())
                {
                    _logger.LogWarning("No integrations found for this company");
                    return ServiceResponse<List<IntegrationDto>>.ErrorResponse("No integrations found for this company.", 404);
                }

                _logger.LogInformation("Successfully retrieved Integrations");
                return ServiceResponse<List<IntegrationDto>>.SuccessResponse(integrations, "Integrations retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching integrations");
                return ServiceResponse<List<IntegrationDto>>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<List<DefaultIntegrationDto>>> GetDefaultIntegrationsAsync(int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching Default Integrations");

                var result = await _integrationRepository.GetDefaultIntegrationsAsync(companyId);

                if (result == null || !result.Any())
                {
                    _logger.LogWarning("No default integrations found for this company");
                    return new ServiceResponse<List<DefaultIntegrationDto>>
                    {
                        Success = true,
                        Data = new List<DefaultIntegrationDto>(),
                        Message = "No integrations found."
                    };
                }

                _logger.LogInformation("Successfully retrieved Default Integrations");
                return new ServiceResponse<List<DefaultIntegrationDto>>
                {
                    Success = true,
                    Data = result,
                    Message = "Fetched successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching default integrations");
                return ServiceResponse<List<DefaultIntegrationDto>>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        // ---------- Send Integration Request ----------
        public async Task<ServiceResponse<object>> SendIntegrationRequestAsync(int userId, int companyId, IntegrationRequestDto request)
        {
            // Validate conditional fields
            if (request.Loadboard == LoadboardType.DAT && string.IsNullOrWhiteSpace(request.ServiceEmail))
                return ServiceResponse<object>.ErrorResponse("ServiceEmail is required for DAT integration.", 400);

            // Validate conditional fields
            if (request.Loadboard == LoadboardType.Truckstop && string.IsNullOrWhiteSpace(request.ServiceEmail))
                return ServiceResponse<object>.ErrorResponse("ServiceEmail (username) is required for Truckstop integration.", 400);

            if (!request.IsNew && string.IsNullOrWhiteSpace(request.ExistingEmail))
                return ServiceResponse<object>.ErrorResponse("ExistingEmail is required for existing account setup.", 400);

            var user = await _userService.GetCurrentUserAsync(userId);
            if (user == null || user.CompanyId != companyId)
                return ServiceResponse<object>.ErrorResponse("User or Company not found.", 404);

            // Check if integration already exists for this company & loadboard
            var dbLoadboardId = request.Loadboard.ToDbId();

            var existingIntegration = await _integrationRepository.GetByCompanyAndLoadboardAsync(companyId, dbLoadboardId);
            if (existingIntegration != null)
            {
                return ServiceResponse<object>.ErrorResponse($"An integration for {request.Loadboard} already exists for this company.", 400);
            }

            var customerName = user.CompanyName;
            var mainContactName = user.UserName;
            var mainContactPhone = user.Phone;
            //var identifier = $"{user.CompanyMCNumber ?? ""} / {user.CompanyDotNumber ?? ""} / {user.CompanyAddress ?? ""}";
            var companyMCNumber = $"{user.CompanyMCNumber ?? ""}";
            var companyDotNumber = $"{user.CompanyDotNumber ?? ""}";
            var companyAddress = $"{user.CompanyAddress ?? ""}";
            var destinationEmail = "sarwaich@ommo.ai";

            // Use EF execution strategy for resilience (e.g., Azure SQL transient retries)
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                // Start transaction (unit of work)
                await using var transaction = await _unitOfWork.BeginTransactionAsync();

                try
                {
                    // Re-check inside transaction to avoid race conditions
                    var existingInsideTx = await _integrationRepository.GetByCompanyAndLoadboardAsync(companyId, dbLoadboardId);
                    if (existingInsideTx != null)
                    {
                        // No DB changes yet; rollback and return
                        await transaction.RollbackAsync();
                        _logger.LogWarning("Duplicate integration request prevented for company {CompanyId}, loadboard {LoadboardDbId}", companyId, dbLoadboardId);
                        return ServiceResponse<object>.ErrorResponse($"An integration for {request.Loadboard} already exists for this company.", 400);
                    }

                    // Build subject & body (HTML) and credentials depending on loadboard
                    string subject;
                    string body;
                    string? credentialsJson = null;

                    if (request.Loadboard == LoadboardType.DAT)
                    {
                        if (string.IsNullOrWhiteSpace(request.ServiceEmail))
                            throw new ArgumentException("Service_Email is required for DAT integration.", nameof(request.ServiceEmail));

                        if (request.IsNew)
                        {
                            subject = $"New DAT Account & Service Account Setup – {customerName}";
                            body = $@"
                                <p>Hello,</p>
                    
                                <p>Please create a new DAT account and associated Service Account for the following customer to enable REST API integration with our platform:</p>
                                
                                <ul>
                                    <li><b>Customer Name:</b> {customerName}</li>
                                    <li><b>Main Contact:</b> {mainContactName}, {mainContactPhone}</li>
                                    <li><b>Service Account Email (New, Unused in DAT):</b> {request.ServiceEmail}</li>
                                    <li><b>Company MCNumber:</b> {companyMCNumber}</li>
                                    <li><b>Company DotNumber:</b> {companyDotNumber}</li>
                                    <li><b>Company Address:</b> {companyAddress}</li>
                                    <li><b>Integration Service & Interface:</b> DAT – REST API</li>
                                </ul>
                                
                                <p>Thank you,<br/>Ommo AI</p>";
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(request.ExistingEmail))
                                throw new ArgumentException("existing_email is required for isnew = false (DAT).", nameof(request.ExistingEmail));

                            subject = $"DAT Service Account Setup – {customerName}";
                            body = $@"
                                <p>Hello,</p>
                    
                                <p>Please set up a Service Account for the following existing DAT customer to enable REST API integration with our platform:</p>
                                
                                <ul>
                                    <li><b>Customer Name: {customerName}</li>
                                    <li><b>Main Contact: {mainContactName}, {mainContactPhone}</li>
                                    <li><b>DAT Login Email (Existing): {request.ExistingEmail}</li>
                                    <li><b>Service Account Email (New, Unused in DAT): {request.ServiceEmail}</li>
                                    <li><b>Company MCNumber:</b> {companyMCNumber}</li>
                                    <li><b>Company DotNumber:</b> {companyDotNumber}</li>
                                    <li><b>Company Address:</b> {companyAddress}</li>
                                    <li><b>Integration Service & Interface: DAT – REST API</li>
                                </ul>
                                
                                <p>Thank you,<br/>Ommo AI</p>";
                        }

                        // Encrypt and prepare credentials
                        var encryptedServiceEmail = _encryption.Encrypt(request.ServiceEmail!); // non-null asserted due to validation
                        var credentials = new Dictionary<string, string> { ["ServiceEmail"] = encryptedServiceEmail };
                        credentialsJson = JsonSerializer.Serialize(credentials);
                    }
                    else if (request.Loadboard == LoadboardType.Truckstop)
                    {
                        if (string.IsNullOrWhiteSpace(request.ServiceEmail))
                            throw new ArgumentException("Service_Email (username) is required for Truckstop integration.", nameof(request.ServiceEmail));

                        // fetch IntegrationID from global table
                        var globalCred = await _globalIntegrationCredentialRepository.GetByIntegrationIdAsync(3);
                        if (globalCred == null)
                            throw new InvalidOperationException("Truckstop IntegrationID not found.");

                        var integrationID = globalCred.credential_value;

                        if (request.IsNew)
                        {
                            subject = $"New Account & SOAP API Credentials Request – {customerName}";
                            body = $@"
                                <p>Hello,</p>
                    
                                <p>Please create a new Truckstop account and SOAP API credentials for the following customer to enable integration with our Loadboard SaaS platform:</p>
                                
                                <ul>
                                    <li><b>Customer Name: {customerName}</li>
                                    <li><b>Main Contact: {mainContactName}, {mainContactPhone}</li>
                                    <li><b>Company MCNumber:</b> {companyMCNumber}</li>
                                    <li><b>Company DotNumber:</b> {companyDotNumber}</li>
                                    <li><b>Company Address:</b> {companyAddress}</li>
                                    <li><b>Integration Service & Interface: Truckstop – SOAP API</li>
                                    <li><b>IntegrationID: {integrationID}</li>
                                </ul>                       

                                <p>Thank you,<br/>Ommo AI</p>";
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(request.ExistingEmail))
                                throw new ArgumentException("existing_email is required for isnew = false (Truckstop).", nameof(request.ExistingEmail));

                            subject = $"SOAP API Credentials Request – {customerName}";
                            body = $@"
                                <p>Hello,</p>
                                
                                <p>Please set up SOAP API credentials for the following existing Truckstop customer to enable integration with our Loadboard SaaS platform:</p>
                                
                                <ul>
                                    <li><b>Customer Name: {customerName}</li>
                                    <li><b>Main Contact: {mainContactName}, {mainContactPhone}</li>
                                    <li><b>Truckstop Login Email (Existing): {request.ExistingEmail}</li>
                                    <li><b>Company MCNumber:</b> {companyMCNumber}</li>
                                    <li><b>Company DotNumber:</b> {companyDotNumber}</li>
                                    <li><b>Company Address:</b> {companyAddress}</li>
                                    <li><b>Integration Service & Interface: Truckstop – SOAP API</li>
                                    <li><b>IntegrationID: {integrationID}</li>
                                </ul>                        

                                <p>Thank you,<br/>Ommo AI</p>";
                        }

                        var encryptedServiceEmail = _encryption.Encrypt(request.ServiceEmail!); // non-null asserted due to validation
                        var credentials = new Dictionary<string, string> { ["ServiceEmail"] = encryptedServiceEmail };
                        credentialsJson = JsonSerializer.Serialize(credentials);

                        //// Store integrationID in credentials for traceability (encrypted)
                        //var encryptedIntegrationId = _encryption.Encrypt(integrationID ?? string.Empty);
                        //var credentials = new Dictionary<string, string> { ["IntegrationID"] = encryptedIntegrationId };
                        //credentialsJson = JsonSerializer.Serialize(credentials);
                    }
                    else
                    {
                        throw new NotSupportedException($"Loadboard {request.Loadboard} not supported.");
                    }

                    // Send email (to placeholder)
                    await _emailService.SendAsync(destinationEmail, subject, body);

                    // Create sendemail record (status 'sent' to match your existing behavior)
                    var sendEmailEntry = new SendEmail
                    {
                        send_to = destinationEmail,
                        subject = request.Loadboard == LoadboardType.DAT ? "Request-Integration-DAT" : "Request-Integration-TruckStop",
                        status = "sent",
                        created_at = DateTime.UtcNow
                    };

                    var emailId = await _sendEmailRepository.InsertAsync(sendEmailEntry);

                    // Build integration model and insert
                    var integration = new Integrations
                    {
                        default_integration_id = dbLoadboardId,
                        integration_status = "pending", // lowercase to satisfy DB CHECK
                        //credentials = !string.IsNullOrWhiteSpace(credentialsJson) ? JsonDocument.Parse(credentialsJson) : null,
                        credentials = null,
                        company_id = user.CompanyId,
                        last_updated = DateTime.UtcNow,
                        requested_by_email = user.UserEmail
                    };

                    await _integrationRepository.AddIntegrationAsync(integration);

                    // Commit transaction
                    await transaction.CommitAsync();

                    _logger.LogInformation("Integration request processed successfully. CompanyId={CompanyId}, UserId={UserId}, Loadboard={Loadboard}, IntegrationId={IntegrationId}",
                    companyId, userId, request.Loadboard, integration.integration_id);

                    return ServiceResponse<object>.SuccessResponse(new { integrationId = integration.integration_id }, "Integration request submitted successfully.");

                }
                catch (ArgumentException aex)
                {
                    // Input validation / bad argument
                    await transaction.RollbackAsync();
                    _logger.LogWarning(aex, "Validation failed for integration request. CompanyId={CompanyId}, UserId={UserId}, Loadboard={Loadboard}",
                        companyId, userId, request?.Loadboard);
                    return ServiceResponse<object>.ErrorResponse(aex.Message, 400);
                }
                catch (JsonException jex)
                {
                    // JSON parsing/serialization issues
                    await transaction.RollbackAsync();
                    _logger.LogError(jex, "JSON error while building credentials for CompanyId={CompanyId}, Loadboard={Loadboard}", companyId, request?.Loadboard);
                    return ServiceResponse<object>.ErrorResponse("Internal error while preparing integration data.", 500);
                }
                catch (DbUpdateException dbex)
                {
                    // Database-level error
                    await transaction.RollbackAsync();
                    _logger.LogError(dbex, "Database error while creating integration for CompanyId={CompanyId}, Loadboard={Loadboard}", companyId, request?.Loadboard);
                    return ServiceResponse<object>.ErrorResponse("Database error while processing integration request. Please try again later.", 500);
                }
                catch (InvalidOperationException iex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(iex, "Invalid operation during integration processing for CompanyId={CompanyId}", companyId);
                    return ServiceResponse<object>.ErrorResponse(iex.Message, 400);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Unhandled error while processing integration request for CompanyId={CompanyId}, UserId={UserId}, Loadboard={Loadboard}",
                        companyId, userId, request?.Loadboard);
                    return ServiceResponse<object>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
                }
            });
        }

        public async Task<ServiceResponse<object>> ToggleStatusAsync(ToggleIntegrationStatusRequest request)
        {
            var integration = await _integrationRepository.GetByIdAsync(request.IntegrationId);
            if (integration == null)
                return ServiceResponse<object>.ErrorResponse("Integration not found.");

            integration.integration_status =
                integration.integration_status == "active" ? "inactive" : "active";

            integration.last_updated = DateTime.UtcNow;
            integration.requested_by_email = request.RequestedByEmail;

            await _integrationRepository.UpdateAsync(integration);

            var result = new
            {
                integration_id = integration.integration_id,
                newStatus = integration.integration_status,
                last_updated = integration.last_updated
            };

            return ServiceResponse<object>.SuccessResponse(result, "Integration status updated successfully.");
        }

    }
}
