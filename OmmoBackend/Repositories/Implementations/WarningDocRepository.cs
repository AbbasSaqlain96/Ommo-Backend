using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OmmoBackend.Data;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using System.ComponentModel.Design;
using Twilio.Http;

namespace OmmoBackend.Repositories.Implementations
{
    public class WarningDocRepository : GenericRepository<WarningDocument>, IWarningDocRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WarningDocRepository> _logger;
        public WarningDocRepository(AppDbContext dbContext, IConfiguration configuration, ILogger<WarningDocRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task UpdateWarningDocsAsync(int companyId, int eventId, int warningId, IFormFile warningDoc, string docNumber)
        {
            string baseFolderPath = _configuration.GetValue<string>("AppSettings:ServerDocumentDirectory");

            if (string.IsNullOrWhiteSpace(baseFolderPath))
                throw new InvalidOperationException("Server document directory is not configured.");

            string folderPath = Path.Combine(baseFolderPath, "Event", "Warning", companyId.ToString(), "Warning_Document", warningId.ToString());

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Remove existing document (if any)
            var existingDoc = await _dbContext.warning_documents
                .Where(d => d.warning_id == warningId).ToListAsync();

            if (!existingDoc.Any())
            {
                string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");

                // Save new file
                var uniqueName = $"{Guid.NewGuid()}_{warningDoc.FileName}";
                var fullFilePath = Path.Combine(folderPath, uniqueName);

                using (var stream = new FileStream(fullFilePath, FileMode.Create))
                {
                    await warningDoc.CopyToAsync(stream);
                }

                // Save record in DB
                _dbContext.warning_documents.Add(new WarningDocument
                {
                    doc_type_id = 30,
                    warning_id = warningId,
                    doc_number = docNumber,
                    path = $"{serverUrl}/Documents/Event/Warning/{companyId}/Warning_Document/{warningId}/{uniqueName}",
                    status = "uploaded"
                });
            }
            else 
            {
                var doc = existingDoc.FirstOrDefault(d => d.doc_type_id == 30);

                // remove doc
                var oldFileName = Path.GetFileName(doc.path);
                var oldFilePath = Path.Combine(folderPath, oldFileName);
                if (File.Exists(oldFilePath))
                    File.Delete(oldFilePath);

                _dbContext.warning_documents.Remove(doc);

                // Save new file
                string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");

                // Save new file
                var uniqueName = $"{Guid.NewGuid()}_{warningDoc.FileName}";
                var fullFilePath = Path.Combine(folderPath, uniqueName);

                using (var stream = new FileStream(fullFilePath, FileMode.Create))
                {
                    await warningDoc.CopyToAsync(stream);
                }

                // Save record in DB
                _dbContext.warning_documents.Add(new WarningDocument
                {
                    doc_type_id = 30,
                    warning_id = warningId,
                    doc_number = docNumber,
                    path = $"{serverUrl}/Documents/Event/Warning/{companyId}/Warning_Document/{warningId}/{uniqueName}",
                    status = "uploaded"
                });
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
