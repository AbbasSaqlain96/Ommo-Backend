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
    public class TicketDocRepository : GenericRepository<TicketDoc>, ITicketDocRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TicketDocRepository> _logger;

        public TicketDocRepository(AppDbContext dbContext, ILogger<TicketDocRepository> logger, IConfiguration configuration) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<int> GetLastTicketDocIdAsync()
        {
            _logger.LogInformation("Fetching the last ticket document ID.");

            try
            {
                var lastTicketDoc = await _dbContext.ticket_doc
                .OrderByDescending(t => t.ticket_doc_id)
                .FirstOrDefaultAsync();

                int lastId = lastTicketDoc?.ticket_doc_id ?? 0;
                _logger.LogInformation("Retrieved last ticket document ID: {LastId}", lastId);

                return lastId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the last ticket document ID.");
                throw new ApplicationException("An unexpected error occurred while retrieving the last ticket document ID.", ex);
            }
        }

        public async Task UpdateDocsAsync(int companyId, int ticketId, int eventId, string docNumber, IFormFile document)
        {
            string baseFolderPath = _configuration.GetValue<string>("AppSettings:ServerDocumentDirectory");

            if (string.IsNullOrWhiteSpace(baseFolderPath))
                throw new InvalidOperationException("Server document directory is not configured.");

            string folderPath = Path.Combine(baseFolderPath, "Event", "Ticket", companyId.ToString(), "Ticket_Doc", ticketId.ToString());

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Remove existing document (if any)
            var existingDoc = await _dbContext.ticket_doc
                .FirstOrDefaultAsync(d => d.ticket_id == ticketId);

            if (existingDoc != null)
            {
                var oldFileName = Path.GetFileName(existingDoc.file_path);
                var oldFilePath = Path.Combine(folderPath, oldFileName);

                if (File.Exists(oldFilePath))
                    File.Delete(oldFilePath);

                _dbContext.ticket_doc.Remove(existingDoc);
            }

            string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");

            // Save new file
            //var ticketFileName = $"{ticketDocId}_{driverId}.pdf";
            var ticketFileName = $"{Guid.NewGuid()}.pdf";
            var savePath = Path.Combine(folderPath, ticketFileName);

            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await document.CopyToAsync(stream);
            }

            var relativePath = $"/Documents/Event/Ticket/{companyId}/Ticket_Doc/{ticketId}/{ticketFileName}";
            var fullPath = $"{serverUrl}{relativePath}";

            // Insert new doc record
            _dbContext.ticket_doc.Add(new TicketDoc
            {
                doc_type_id = 26,
                ticket_id = ticketId,
                doc_number = docNumber,
                file_path = fullPath,
                status = TicketDocStatus.uploaded
            });

            await _dbContext.SaveChangesAsync();
        }
    }
}
