using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using System.ComponentModel.Design;

namespace OmmoBackend.Repositories.Implementations
{
    public class AssetRepository : GenericRepository<Vehicle>, IAssetRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AssetRepository> _logger;
        public AssetRepository(AppDbContext dbContext, IConfiguration configuration, ILogger<AssetRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AssetDetailTruckDto> GetTruckByVehicleIdAsync(int vehicleId)
        {
            _logger.LogInformation("Fetching truck details for VehicleId: {VehicleId}", vehicleId);

            try
            {
                var truck = await _dbContext.truck
                .Where(t => t.vehicle_id == vehicleId)
                .Select(t => new AssetDetailTruckDto
                {
                    Brand = t.brand,
                    Model = t.model,
                    FuelType = t.fuel_type.ToString(),
                    Color = t.color,
                    TruckStatus = t.truck_status.ToString()
                }).FirstOrDefaultAsync();

                if (truck == null)
                    _logger.LogWarning("No truck found for VehicleId: {VehicleId}", vehicleId);

                return truck;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching truck details for VehicleId: {VehicleId}", vehicleId);
                throw;
            }
        }

        public async Task<AssetDetailTrailerDto> GetTrailerByVehicleIdAsync(int vehicleId)
        {
            _logger.LogInformation("Fetching trailer details for VehicleId: {VehicleId}", vehicleId);

            try
            {
                var trailer = await _dbContext.trailer
                .Where(t => t.vehicle_id == vehicleId)
                .Select(t => new AssetDetailTrailerDto
                {
                    Trailer_Type = t.trailer_type.ToString()
                }).FirstOrDefaultAsync();

                if (trailer == null)
                    _logger.LogWarning("No trailer found for VehicleId: {VehicleId}", vehicleId);

                return trailer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching trailer details for VehicleId: {VehicleId}", vehicleId);
                throw;
            }
        }

        public async Task<List<VehicleAttributeDto>> GetVehicleAttributesAsync(int vehicleId)
        {
            _logger.LogInformation("Fetching vehicle attributes for VehicleId: {VehicleId}", vehicleId);

            try
            {
                var attributes = await _dbContext.vehicle_attributes
                .Where(a => a.vehicle_id == vehicleId)
                .Select(a => new VehicleAttributeDto
                {
                    AttributeId = a.attribute_id,
                    VehicleId = a.vehicle_id,
                    IsHeadrake = a.is_headrake,
                    HaveFlatbed = a.have_flatbed,
                    HaveLoadbar = a.have_loadbar,
                    HaveVanStraps = a.have_van_straps,
                    Weight = a.weight,
                    AxleSpacing = a.axle_spacing,
                    NumOfAxles = a.num_of_axles,
                    VehicleType = a.vehicle_type.ToString(),
                    UpdatedAt = a.updated_at
                }).ToListAsync();

                _logger.LogInformation("Found {Count} vehicle attributes for VehicleId: {VehicleId}", attributes.Count, vehicleId);
                return attributes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching vehicle attributes for VehicleId: {VehicleId}", vehicleId);
                throw;
            }
        }

        public async Task<List<VehicleDocumentDto>> GetVehicleDocumentsAsync(int vehicleId)
        {
            _logger.LogInformation("Fetching vehicle documents for VehicleId: {VehicleId}", vehicleId);

            try
            {
                var documents = await _dbContext.vehicle_document
                .Where(d => d.vehicle_id == vehicleId)
                .Join(_dbContext.document_type,
                      doc => doc.doc_type_id,
                      type => type.doc_type_id,
                      (doc, type) => new VehicleDocumentDto
                      {
                          DocTypeName = type.doc_name,
                          Path = doc.path,
                          State = doc.state_code.ToString()
                      }).ToListAsync();

                _logger.LogInformation("Found {Count} vehicle documents for VehicleId: {VehicleId}", documents.Count, vehicleId);
                return documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching vehicle documents for VehicleId: {VehicleId}", vehicleId);
                throw;
            }
        }

        public async Task<List<AssetDetailIssueTicketDto>> GetShopHistoryByVehicleIdAsync(int vehicleId)
        {
            _logger.LogInformation("Fetching shop history for VehicleId: {VehicleId}", vehicleId);

            try
            {
                var shopHistory = await _dbContext.issue_ticket
                .Where(it => it.vehicle_id == vehicleId)
                .Select(it => new AssetDetailIssueTicketDto
                {
                    TicketId = it.ticket_id,
                    //IssueId = it.issue_id,
                    NextScheduleDate = it.next_schedule_date,
                    ScheduleDate = it.schedule_date ?? null, // Ensuring nullable handling
                    CompletedDate = it.completed_date ?? null,
                    VehicleId = it.vehicle_id,
                    Priority = it.priority.ToString(),
                    Status = it.status.ToString(),
                    AssignedUser = it.assigned_user,
                    IsmanagedRecurringly = it.ismanaged_recurringly,
                    CarrierId = it.carrier_id,
                    RecurrentType = it.recurrent_type.ToString(),
                    TimeInterval = it.time_interval,
                    MileageInterval = it.mileage_interval,
                    CurrentMileage = it.current_mileage,
                    NextMileage = it.next_mileage,
                }).ToListAsync();

                _logger.LogInformation("Found {Count} shop history records for VehicleId: {VehicleId}", shopHistory.Count, vehicleId);
                return shopHistory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching shop history for VehicleId: {VehicleId}", vehicleId);
                throw;
            }
        }

        public async Task<AssetResponseDto> AddAssetAsync(AddAssetRequestDto dto, int companyId)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                try
                {
                    _logger.LogInformation("Starting AddAssetAsync for companyId: {CompanyId}", companyId);

                    var vehicle = new Vehicle
                    {
                        plate_number = dto.PlateNumber,
                        license_plate_state = Enum.TryParse<LicensePlateState>(dto.PlateState, true, out var state) ? state : throw new Exception("Invalid PlateState"),
                        vin_number = dto.VinNumber,
                        vehicle_type = Enum.TryParse<VehicleType>(dto.VehicleType, true, out var type) ? type : throw new Exception("Invalid VehicleType"),
                        year = dto.Year,
                        vehicle_trademark = dto.Trademark,
                        carrier_id = GetCurrentUserCarrierId(companyId),
                        rating = 5,
                        is_assigned = false,
                        created_at = DateTime.UtcNow,
                        status = VehicleStatus.active
                    };

                    _dbContext.vehicle.Add(vehicle);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("Vehicle added with ID: {VehicleId}", vehicle.vehicle_id);

                    var vehicleAttributes = new VehicleAttribute
                    {
                        vehicle_id = vehicle.vehicle_id,
                        is_headrake = dto.IsHeadrake,
                        have_flatbed = dto.HaveFlatbed,
                        have_loadbar = dto.HaveLoadbar,
                        have_van_straps = dto.HaveVanStraps,
                        weight = dto.Weight,
                        axle_spacing = dto.AxleSpacing,
                        num_of_axles = dto.NumOfAxles,
                        vehicle_type = Enum.TryParse<VehicleType>(dto.VehicleType, true, out var vehicleType) ? vehicleType : throw new Exception("Invalid VehicleType"),
                        updated_at = DateTime.UtcNow
                    };

                    _dbContext.vehicle_attributes.Add(vehicleAttributes);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("Vehicle attributes added for vehicle ID: {VehicleId}", vehicle.vehicle_id);

                    if (vehicleType == VehicleType.truck)
                    {
                        _logger.LogInformation("Processing truck-specific fields for vehicle ID: {VehicleId}", vehicle.vehicle_id);

                        if (string.IsNullOrEmpty(dto.Brand) || string.IsNullOrEmpty(dto.Model) ||
                            string.IsNullOrEmpty(dto.Color) || dto.FuelType == null)
                        {
                            _logger.LogWarning("Truck-specific fields missing for vehicle ID: {VehicleId}", vehicle.vehicle_id);

                            return new AssetResponseDto
                            {
                                Success = false,
                                ErrorMessage = "Truck-specific fields must be provided."
                            };
                        }

                        if (!Enum.TryParse(dto.FuelType, true, out TruckFuelType fuelType))
                        {
                            _logger.LogWarning("Invalid fuel type provided: {FuelType}", dto.FuelType);
                            return new AssetResponseDto { Success = false, ErrorMessage = "Invalid Fuel Type." };
                        }

                        var truck = new Truck
                        {
                            vehicle_id = vehicle.vehicle_id,
                            brand = dto.Brand,
                            model = dto.Model,
                            color = dto.Color,
                            fuel_type = fuelType,
                            truck_status = TruckStatus.idle,
                            unit_id = null
                        };

                        _dbContext.truck.Add(truck);
                        _logger.LogInformation("Truck added for vehicle ID: {VehicleId}", vehicle.vehicle_id);
                    }
                    else if (dto.VehicleType == VehicleType.trailer.ToString())
                    {
                        _logger.LogInformation("Processing trailer-specific fields for vehicle ID: {VehicleId}", vehicle.vehicle_id);

                        if (dto.TrailerType == null)
                        {
                            _logger.LogWarning("Trailer type is missing for vehicle ID: {VehicleId}", vehicle.vehicle_id);
                            return new AssetResponseDto { Success = false, ErrorMessage = "Trailer type must be provided." };
                        }
                        var trailer = new Trailer
                        {
                            vehicle_id = vehicle.vehicle_id,
                            trailer_type = Enum.TryParse<TrailerType>(dto.TrailerType, true, out var trailerType) ? trailerType : throw new Exception("Invalid TrailerType")
                        };

                        _dbContext.trailer.Add(trailer);
                        _logger.LogInformation("Trailer added for vehicle ID: {VehicleId}", vehicle.vehicle_id);
                    }
                    await _dbContext.SaveChangesAsync();

                    foreach (var file in dto.Documents)
                    {
                        _logger.LogInformation("Processing document for vehicle ID: {VehicleId}", vehicle.vehicle_id);

                        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.File.FileName);
                        var filePath = await SaveFileAsync(file.File, companyId, vehicle.vehicle_id);
                        var document = new VehicleDocument
                        {
                            vehicle_id = vehicle.vehicle_id,
                            doc_type_id = file.DocTypeId,
                            state_code = Enum.TryParse<USState>(file.State, true, out var stateCode) ? stateCode : throw new Exception("Invalid StateCode"),
                            start_date = DateTime.UtcNow, // You can modify as needed
                            end_date = DateTime.UtcNow.AddYears(1), // Example: 1-year validity
                            path = filePath,
                            updated_at = DateTime.UtcNow,
                            status = VehicleDocumentStatus.active
                        };

                        _dbContext.vehicle_document.Add(document);
                    }
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("All documents processed for vehicle ID: {VehicleId}", vehicle.vehicle_id);

                    await transaction.CommitAsync();
                    _logger.LogInformation("Transaction committed successfully for vehicle ID: {VehicleId}", vehicle.vehicle_id);

                    return new AssetResponseDto
                    {
                        VehicleId = vehicle.vehicle_id,
                        Success = true
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Failed to add asset for company ID: {CompanyId}", companyId);

                    return new AssetResponseDto
                    {
                        Success = false,
                        ErrorMessage = "Failed to Add Asset, please check the provided information, and try again." + ex.Message
                    };
                }
            });
        }

        private int GetDocumentTypeId(string fileName)
        {
            _logger.LogInformation("Fetching document type ID for file: {FileName}", fileName);

            var documentType = _dbContext.document_type.FirstOrDefault(d => d.doc_name.ToLower() == fileName.ToLower());
            if (documentType == null)
            {
                _logger.LogWarning("Document type not found for file: {FileName}", fileName);
                throw new Exception($"Document type not found for file: {fileName}");
            }

            _logger.LogInformation("Found document type ID: {DocumentTypeId} for file: {FileName}", documentType.doc_type_id, fileName);
            return documentType.doc_type_id;
        }

        private int GetCurrentUserCarrierId(int companyId)
        {
            _logger.LogInformation("Fetching carrier ID for company ID: {CompanyId}", companyId);

            var carrier = _dbContext.carrier.FirstOrDefault(c => c.company_id == companyId);
            if (carrier == null)
            {
                _logger.LogError("Carrier not found for company ID: {CompanyId}", companyId);
                throw new Exception($"Carrier not found for company ID: {companyId}");
            }

            _logger.LogInformation("Found carrier ID: {CarrierId} for company ID: {CompanyId}", carrier.carrier_id, companyId);
            return carrier!.carrier_id;
        }

        private async Task<string> SaveFileAsync(IFormFile file, int companyId, int vehicleId)
        {
            _logger.LogInformation("Saving file: {FileName} for company ID: {CompanyId}, vehicle ID: {VehicleId}", file.FileName, companyId, vehicleId);

            // Get the base folder path from AppSettings
            string baseFolderPath = _configuration.GetValue<string>("AppSettings:ServerDocumentDirectory");

            if (string.IsNullOrWhiteSpace(baseFolderPath))
            {
                _logger.LogError("Server document directory is not configured.");
                throw new InvalidOperationException("Server document directory is not configured.");
            }

            // Construct the complete directory path
            //string folderPath = Path.Combine(baseFolderPath, "VehicleDoc");
            string folderPath = Path.Combine(baseFolderPath, "VehicleDoc", companyId.ToString(), vehicleId.ToString(), "Documents");

            // Create the directory if it doesn't exist
            if (!Directory.Exists(folderPath))
            {
                _logger.LogInformation("Creating directory: {FolderPath}", folderPath);
                Directory.CreateDirectory(folderPath);
            }

            // Generate the file name
            var fileName = $"asset_{file.FileName}";
            string filePath = Path.Combine(folderPath, fileName);

            try
            {
                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                _logger.LogInformation("File successfully saved at {FilePath}", filePath);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error writing document to the server: {Message}", ex.Message);
                throw new InvalidOperationException("Error writing document to the server: " + ex.Message);
            }

            // Get the server URL from configuration
            string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");
            if (string.IsNullOrWhiteSpace(serverUrl))
            {
                _logger.LogError("Server URL is not configured.");
                throw new InvalidOperationException("Server URL is not configured.");
            }

            // Construct the public URL dynamically
            //string vehicleDocumentUrl = $"{serverUrl}/Documents/VehicleDoc/{fileName}";
            string vehicleDocumentUrl = $"{serverUrl}/Documents/VehicleDoc/{companyId}/{vehicleId}/Documents/{fileName}";
            _logger.LogInformation("File accessible at URL: {FileUrl}", vehicleDocumentUrl);

            return vehicleDocumentUrl;
        }


        public async Task<bool> VinExistsAsync(string vin)
        {
            return await _dbContext.vehicle.AnyAsync(v => v.vin_number == vin);
        }

        public async Task<bool> PlateNumberStateExistsAsync(string plateNumber, string plateState)
        {
            return await _dbContext.vehicle.AnyAsync(v =>
                v.plate_number == plateNumber &&
                v.license_plate_state.ToString() == plateState);
        }
    }
}