using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class IncidentDocRepository : GenericRepository<IncidentDoc>, IIncidentDocRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IncidentDocRepository> _logger;

        public IncidentDocRepository(AppDbContext dbContext, IConfiguration configuration, ILogger<IncidentDocRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task UpdateDocsAsync(int eventId, int incidentId, int driverId, List<UpdateDocumentRequest> newDocs)
        {
            string baseFolderPath = _configuration.GetValue<string>("AppSettings:ServerDocumentDirectory");

            if (string.IsNullOrWhiteSpace(baseFolderPath))
            {
                _logger.LogError("Server document directory is not configured.");
                throw new InvalidOperationException("Server document directory is not configured.");
            }

            // Construct the complete directory path
            string folderPath = Path.Combine(baseFolderPath, "Event", "Incident", "Incident_Doc", eventId.ToString());

            // Ensure the directory exists
            if (!Directory.Exists(folderPath))
            {
                _logger.LogInformation("Created directory: {FolderPath}", folderPath);
                Directory.CreateDirectory(folderPath);
            }

            // 1. Get related incident IDs for the event
            var incidentIds = await _dbContext.incident
                .Where(i => i.event_id == eventId)
                .Select(i => i.incident_id)
                .ToListAsync();

            if (!incidentIds.Any())
                return;

            // 2. Get existing documents from DB
            var existingDocs = await _dbContext.incident_doc
                .Where(d => incidentIds.Contains(d.incident_id))
                .ToListAsync();

            var newDocNumbers = newDocs.Select(d => d.DocNumber).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 3. Remove deleted docs
            var toDelete = existingDocs
                .Where(e => !newDocNumbers.Contains(e.doc_number))
                .ToList();

            foreach (var doc in toDelete)
            {
                var filePath = Path.Combine(folderPath, Path.GetFileName(doc.file_path));
                if (File.Exists(filePath))
                    File.Delete(filePath);

                _dbContext.incident_doc.Remove(doc);
            }

            // 4. Add new docs (if not already in DB)
            foreach (var newDoc in newDocs)
            {
                if (existingDocs.Any(e => e.doc_number.Equals(newDoc.DocNumber, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var fileName = $"{incidentId}_{newDoc.DocTypeId}_{driverId}.pdf";
                var filePath = Path.Combine(folderPath, fileName);

                //var targetPath = Path.Combine(folderPath, fileName);
                //if (!File.Exists(targetPath))
                //    continue; // skip if physical file is missing (optional check)

                // Get the server URL from configuration
                string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");
                if (string.IsNullOrWhiteSpace(serverUrl))
                {
                    _logger.LogError("Server URL is not configured.");
                    throw new InvalidOperationException("Server URL is not configured.");
                }

                foreach (var incId in incidentIds)
                {
                    _dbContext.incident_doc.Add(new IncidentDoc
                    {
                        incident_id = incId,
                        doc_number = newDoc.DocNumber,
                        file_path = $"{serverUrl}/Documents/Event/Incident/Incident_Doc/{eventId}/{fileName}",
                        doc_type_id = 28,
                        status = "Active"
                    });
                }
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
