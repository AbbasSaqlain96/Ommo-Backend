using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Exceptions;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using Twilio.Http;

namespace OmmoBackend.Repositories.Implementations
{
    public class IssueTicketRepository : GenericRepository<IssueTicket>, IIssueTicketRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IssueTicketRepository> _logger;

        public IssueTicketRepository(AppDbContext dbContext, IConfiguration configuration, ILogger<IssueTicketRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IEnumerable<IssueTicket>> GetTicketsByCompanyIdAsync(int carrierId)
        {
            _logger.LogInformation("Fetching issue tickets for Carrier ID: {CarrierId}", carrierId);

            try
            {
                var tickets = await _dbContext.issue_ticket
                    .Where(t => t.carrier_id == carrierId)
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} tickets for Carrier ID: {CarrierId}", tickets.Count, carrierId);
                return tickets;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while fetching issue tickets for Carrier ID: {CarrierId}", carrierId);
                throw new DataAccessException("An error occurred while accessing the issue tickets. Please try again later.", dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching issue tickets for Carrier ID: {CarrierId}", carrierId);
                throw new DataAccessException(ErrorMessages.GenericOperationFailed, ex);
            }
        }

        public async Task<List<IssueTicket>> GetTicketsByCategoryIdAsync(int categoryId)
        {
            _logger.LogInformation("Fetching issue tickets for Category ID: {CategoryId}", categoryId);

            try
            {
                var tickets = await _dbContext.issue_ticket
                    .Where(t => t.category_id == categoryId)
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} tickets for Category ID: {CategoryId}", tickets.Count, categoryId);
                return tickets;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while fetching issue tickets for Category ID: {CategoryId}", categoryId);
                throw new DataAccessException("An error occurred while accessing the issue tickets. Please try again later.", dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching issue tickets for Category ID: {CategoryId}", categoryId);
                throw new DataAccessException(ErrorMessages.GenericOperationFailed, ex);
            }
        }

        public async Task<List<IssueTicketResponseDto>> GetIssueTicketsAsync(int companyId)
        {
            _logger.LogInformation("Fetching issue tickets for Company ID: {CompanyId}", companyId);

            try
            {

                var issueTickets = await (from it in _dbContext.issue_ticket
                                          join mc in _dbContext.maintenance_category on it.category_id equals mc.category_id
                                          join user in _dbContext.users on it.assigned_user equals user.user_id
                                          where it.company_id == companyId
                                          select new IssueTicketResponseDto
                                          {
                                              TicketId = it.ticket_id,
                                              CatagoryId = mc.category_id,
                                              CatagoryName = mc.category_name,
                                              VehicleId = it.vehicle_id,
                                              ScheduleDate = it.schedule_date ?? null,
                                              NextScheduleDate = it.next_schedule_date ?? null,
                                              CompleteDate = it.completed_date,
                                              Priority = it.priority.ToString(),
                                              Status = it.status.ToString(),
                                              AssignedUserId = user.user_id,
                                              AssignedUser = user.user_name,
                                              IsManagedRecurringly = it.ismanaged_recurringly,
                                              RecurrentType = it.recurrent_type.ToString(),
                                              TimeInterval = it.time_interval,
                                              MileageInterval = it.mileage_interval,
                                              CurrentMileage = it.current_mileage,
                                              NextMileage = it.next_mileage,
                                              UpdatedAt = it.updated_at,
                                              CreatedBy = it.created_by,
                                              ImageFiles = _dbContext.ticket_file
                                                  .Where(tf => tf.ticket_id == it.ticket_id)
                                                  .Select(tf => new TicketFileResponseDto { Filepath = tf.path })
                                                  .ToList()
                                          }).ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} issue tickets for Company ID: {CompanyId}", issueTickets.Count, companyId);
                return issueTickets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching issue tickets for Company ID: {CompanyId}", companyId);
                throw;
            }
        }

        //public async Task<bool> ValidateCompanyEntities(int vehicleId, int userId, int categoryId, int carrierId)
        //{
        //    var vehicle = await _dbContext.vehicle.FirstOrDefaultAsync(v => v.vehicle_id == vehicleId && v.carrier_id == carrierId);
        //    var user = await _dbContext.users.FirstOrDefaultAsync(u => u.user_id == userId && u.Carrier_ID == carrierId);
        //    var category = await _dbContext.maintenance_category.FirstOrDefaultAsync(c => c.category_id == categoryId && c.carrier_id == carrierId);

        //    return vehicle != null && user != null && category != null;
        //}

        public async Task<int> CreateIssueTicketAsync(IssueTicket issueTicket)
        {
            _logger.LogInformation("Creating a new issue ticket for Company ID: {CompanyId}, Vehicle ID: {VehicleId}", issueTicket.company_id, issueTicket.vehicle_id);

            try
            {
                _dbContext.issue_ticket.Add(issueTicket);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Issue ticket created successfully with Ticket ID: {TicketId}", issueTicket.ticket_id);
                return issueTicket.ticket_id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating an issue ticket for Company ID: {CompanyId}, Vehicle ID: {VehicleId}", issueTicket.company_id, issueTicket.vehicle_id);
                throw;
            }
        }

        public async Task<List<string>> SaveTicketFilesAsync(int ticketId, List<IFormFile> fileNames, int companyId, int? vehicleId)
        {
            _logger.LogInformation("Saving {FileCount} files for Ticket ID: {TicketId}, Company ID: {CompanyId}, Vehicle ID: {VehicleId}", fileNames.Count, ticketId, companyId, vehicleId);

            var filePaths = new List<string>();

            try
            {
                foreach (var file in fileNames)
                {
                    var filePath = await SaveFileAsync(file, ticketId, companyId, vehicleId);
                    _dbContext.ticket_file.Add(new TicketFile
                    {
                        ticket_id = ticketId,
                        path = filePath
                    });
                    filePaths.Add(filePath);
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Successfully saved {FileCount} files for Ticket ID: {TicketId}", filePaths.Count, ticketId);

                return filePaths;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving files for Ticket ID: {TicketId}, Company ID: {CompanyId}", ticketId, companyId);
                throw;
            }
        }

        public async Task<(bool VehicleValid, bool UserValid, bool CategoryValid)> ValidateCompanyEntitiesDetailed(
            int vehicleId, int userId, int categoryId, int carrierId)
        {
            _logger.LogInformation("Validating company entities for Vehicle ID: {VehicleId}, User ID: {UserId}, Category ID: {CategoryId}, Carrier ID: {CarrierId}", vehicleId, userId, categoryId, carrierId);

            try
            {
                // Fetch the company ID from the carrier table
                var companyId = await _dbContext.carrier
                .Where(c => c.carrier_id == carrierId)
                .Select(c => c.company_id)
                .FirstOrDefaultAsync();

                bool vehicleValid = await _dbContext.vehicle
           .AnyAsync(v => v.vehicle_id == vehicleId && v.carrier_id == carrierId);

                bool userValid = await _dbContext.users
                    .AnyAsync(u => u.user_id == userId && u.company_id == companyId);

                bool categoryValid = await _dbContext.maintenance_category
                    .AnyAsync(c => c.category_id == categoryId &&
                                   (c.cat_type == Helpers.Enums.MaintenanceCategoryType.standard || c.carrier_id == carrierId));

                return (vehicleValid, userValid, categoryValid);

                //if (companyId == null)
                //{
                //    _logger.LogWarning("Validation failed: No associated company found for Carrier ID: {CarrierId}", carrierId);
                //    return false; // Invalid carrier, no associated company
                //}

                //// Check if the Vehicle exists and belongs to the same Carrier (Company)
                //bool vehicleExists = await _dbContext.vehicle
                //    .AnyAsync(v => v.vehicle_id == vehicleId && v.carrier_id == carrierId);

                //// Check if the User exists and belongs to the same Company
                //bool userExists = await _dbContext.users
                //    .AnyAsync(u => u.user_id == userId && u.company_id == companyId);

                //// Check if the Category exists and is either standard or belongs to the user's company
                //bool categoryExists = await _dbContext.maintenance_category
                //    .AnyAsync(c => c.category_id == categoryId &&
                //                  (c.cat_type == Helpers.Enums.MaintenanceCategoryType.standard || c.carrier_id == carrierId));

                //// True only if all entities exist and belong to the same Carrier/Company
                //bool isValid = vehicleExists && userExists && categoryExists;

                //_logger.LogInformation("Validation result for Carrier ID: {CarrierId} - Vehicle Exists: {VehicleExists}, User Exists: {UserExists}, Category Exists: {CategoryExists}", carrierId, vehicleExists, userExists, categoryExists);

                //return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Validation error in ValidateCompanyEntitiesDetailed");
                throw;
            }
        }

        private async Task<string> SaveFileAsync(IFormFile file, int ticketid, int companyId, int? vehicleId)
        {
            _logger.LogInformation("Saving file for Ticket ID: {TicketId}, Company ID: {CompanyId}, Vehicle ID: {VehicleId}", ticketid, companyId, vehicleId);

            // Get the base folder path from AppSettings
            string baseFolderPath = _configuration.GetValue<string>("AppSettings:ServerDocumentDirectory");

            if (string.IsNullOrWhiteSpace(baseFolderPath))
            {
                _logger.LogError("Server document directory is not configured.");
                throw new InvalidOperationException("Server document directory is not configured.");
            }

            // Construct the complete directory path
            string folderPath = Path.Combine(baseFolderPath, "ShopTicketFile", companyId.ToString(), vehicleId.ToString()!);

            // Create the directory if it doesn't exist
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                _logger.LogInformation("Created directory: {FolderPath}", folderPath);
            }
            // Generate the file name
            string fileExtension = Path.GetExtension(file.FileName);
            string fileName = $"{Guid.NewGuid()}{fileExtension}";
            string filePath = Path.Combine(folderPath, fileName);

            try
            {
                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation("File saved successfully: {FilePath}", filePath);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error writing document to the server: {FilePath}", filePath);
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
            string issueTicketDocumentUrl = $"{serverUrl}/Documents/ShopTicketFile/{companyId}/{vehicleId}/{fileName}";
            _logger.LogInformation("File accessible at: {FileUrl}", issueTicketDocumentUrl);

            return issueTicketDocumentUrl;
        }

        public async Task<int> GetCompanyIdByTicketIdAsync(int ticketId)
        {
            _logger.LogInformation("Fetching Company ID for Ticket ID: {TicketId}", ticketId);

            var ticketWithCompany = await (from ticket in _dbContext.issue_ticket
                                           join carrier in _dbContext.carrier on ticket.carrier_id equals carrier.carrier_id
                                           where ticket.ticket_id == ticketId
                                           select new
                                           {
                                               CompanyId = carrier.company_id
                                           }).FirstOrDefaultAsync();

            if (ticketWithCompany == null)
            {
                _logger.LogWarning("No Company ID found for Ticket ID: {TicketId}", ticketId);
                throw new InvalidOperationException("Company ID not found for the given Ticket ID.");
            }

            _logger.LogInformation("Company ID {CompanyId} retrieved for Ticket ID: {TicketId}", ticketWithCompany.CompanyId, ticketId);
            return ticketWithCompany.CompanyId;
        }


        public async Task<(bool vehicleValid, bool userValid, bool categoryValid)> ValidateCompanyEntitiesToUpdateIssueTicket(
            int? vehicleId, int? userId, int? categoryId, int carrierId)
        {

            _logger.LogInformation("Validating entities for updating Issue Ticket - Carrier ID: {CarrierId}, Vehicle ID: {VehicleId}, User ID: {UserId}, Category ID: {CategoryId}", carrierId, vehicleId, userId, categoryId);

            // Fetch the company ID from the carrier table
            var companyId = await _dbContext.carrier
                .Where(c => c.carrier_id == carrierId)
                .Select(c => c.company_id)
                .FirstOrDefaultAsync();

            if (companyId == null)
            {
                _logger.LogWarning("Validation failed: No associated company found for Carrier ID: {CarrierId}", carrierId);
                return (false, false, false); // Invalid carrier, no associated company
            }

            // Check if the Vehicle exists and belongs to the same Carrier (Company) if provided
            bool vehicleExists = !vehicleId.HasValue || await _dbContext.vehicle
                .AnyAsync(v => v.vehicle_id == vehicleId && v.carrier_id == carrierId);

            // Check if the User exists and belongs to the same Company if provided
            bool userExists = !userId.HasValue || await _dbContext.users
                .AnyAsync(u => u.user_id == userId && u.company_id == companyId);

            // Check if the Category exists and is either standard or belongs to the user's company
            bool categoryExists = !categoryId.HasValue || await _dbContext.maintenance_category
                .AnyAsync(c => c.category_id == categoryId &&
                               (c.cat_type == Helpers.Enums.MaintenanceCategoryType.standard || c.carrier_id == carrierId));

            _logger.LogInformation("Validation results - Vehicle Exists: {VehicleExists}, User Exists: {UserExists}, Category Exists: {CategoryExists}", vehicleExists, userExists, categoryExists);

            return (vehicleExists, userExists, categoryExists);
        }


        //public async Task<bool> ValidateCompanyEntitiesToUpdateIssueTicket(int? vehicleId, int? userId, int? categoryId, int carrierId)
        //{
        //    // Fetch the company ID from the carrier table
        //    var companyId = await _dbContext.carrier
        //        .Where(c => c.carrier_id == carrierId)
        //        .Select(c => c.company_id)
        //        .FirstOrDefaultAsync();

        //    if (companyId == null)
        //    {
        //        return false; // Invalid carrier, no associated company
        //    }

        //    // Check if the Vehicle exists and belongs to the same Carrier (Company) if provided
        //    bool vehicleExists = !vehicleId.HasValue || await _dbContext.vehicle
        //        .AnyAsync(v => v.vehicle_id == vehicleId && v.carrier_id == carrierId);

        //    // Check if the User exists and belongs to the same Company if provided
        //    bool userExists = !userId.HasValue || await _dbContext.users
        //        .AnyAsync(u => u.user_id == userId && u.company_id == companyId);

        //    // Check if the Category exists and is either standard or belongs to the user's company
        //    bool categoryExists = await _dbContext.maintenance_category
        //        .AnyAsync(c => c.category_id == categoryId &&
        //                      (c.cat_type == Helpers.Enums.MaintenanceCategoryType.standard || c.carrier_id == carrierId));

        //    // Return true only if all entities exist and belong to the same Carrier/Company
        //    return vehicleExists || userExists || categoryExists;
        //}
    }
}