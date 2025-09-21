using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class DocInspectionRepository : GenericRepository<DocInspection>, IDocInspectionRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DocInspectionRepository> _logger;

        public DocInspectionRepository(AppDbContext dbContext, IConfiguration configuration, ILogger<DocInspectionRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<DocInspection> GetDocInspectionByEventIdAsync(int eventId)
        {
            var docInspection = _dbContext.doc_inspection
                .Where(x => x.event_id == eventId)
                .FirstOrDefault();

            return docInspection;
        }

        public async Task UpdateDotInspectionDocsAsync(int companyId, int eventId, int docInspectionId, IFormFile docInspectionDoc, string docNumber)
        {
            string baseFolderPath = _configuration.GetValue<string>("AppSettings:ServerDocumentDirectory");

            if (string.IsNullOrWhiteSpace(baseFolderPath))
                throw new InvalidOperationException("Server document directory is not configured.");

            string folderPath = Path.Combine(baseFolderPath, "Event", "Dot_Inspection", companyId.ToString(), "Dot_Inspection_Document", docInspectionId.ToString());

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Get inspection ID for document
            var docInspection = await _dbContext.doc_inspection
                .FirstOrDefaultAsync(x => x.event_id == eventId);

            if (docInspection == null)
                throw new Exception($"No DocInspection found for event_id = {eventId}");

            // Remove existing document (if any)
            var existingDoc = await _dbContext.doc_inspection_documents
                .FirstOrDefaultAsync(d => d.doc_inspection_id == docInspectionId);

            if (existingDoc != null)
            {
                var oldFileName = Path.GetFileName(existingDoc.path);
                var oldFilePath = Path.Combine(folderPath, oldFileName);
                if (File.Exists(oldFilePath))
                    File.Delete(oldFilePath);

                _dbContext.doc_inspection_documents.Remove(existingDoc);
            }

            string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");

            // Save new file
            var uniqueName = $"{Guid.NewGuid()}_{docInspectionDoc.FileName}";
            var fullFilePath = Path.Combine(folderPath, uniqueName);

            using (var stream = new FileStream(fullFilePath, FileMode.Create))
            {
                await docInspectionDoc.CopyToAsync(stream);
            }

            // Save record in DB
            _dbContext.doc_inspection_documents.Add(new DocInspectionDocument
            {
                doc_type_id = 29,
                doc_inspection_id = docInspectionId,
                doc_number = docNumber,
                path = $"{serverUrl}/Documents/Event/Dot_Inspection/{companyId}/Dot_Inspection_Document/{uniqueName}",
                status = "uploaded"
            });

            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> DocInspectionDocumentExist(int docInspectionId) 
        {
            return await _dbContext.doc_inspection_documents
                .AnyAsync(d => d.doc_inspection_id == docInspectionId);
        }
    }
}
