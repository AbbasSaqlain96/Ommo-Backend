using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OmmoBackend.Data;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using System.ComponentModel.Design;

namespace OmmoBackend.Repositories.Implementations
{
    public class IncidentPicturesRepository : GenericRepository<IncidentPicture>, IIncidentPicturesRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IncidentPicturesRepository> _logger;
        public IncidentPicturesRepository(AppDbContext dbContext, IConfiguration configuration, ILogger<IncidentPicturesRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task UpdateImagesAsync(int eventId, List<IFormFile> newImages)
        {
            string baseFolderPath = _configuration.GetValue<string>("AppSettings:ServerDocumentDirectory");
            if (string.IsNullOrWhiteSpace(baseFolderPath))
            {
                _logger.LogError("Server incident directory is not configured.");
                throw new InvalidOperationException("Server incident directory is not configured.");
            }

            string folderPath = Path.Combine(baseFolderPath, "Event", "Incident", "Incident_Pictures", eventId.ToString());
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    _logger.LogInformation("Created directory: {FolderPath}", folderPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create directory: {FolderPath}", folderPath);
                throw;
            }

            // 1. Get all incident IDs for this event
            var incidentIds = await _dbContext.incident
                .Where(i => i.event_id == eventId)
                .Select(i => i.incident_id)
                .ToListAsync();

            if (!incidentIds.Any())
                return;

            // 2. Get existing images from DB
            var existingImages = await _dbContext.incident_pictures
                .Where(p => incidentIds.Contains(p.incident_id))
                .ToListAsync();

            // 3. Prepare filenames for comparison
            var newFileNames = newImages.Select(f => f.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 4. Delete removed images
            var toDelete = existingImages
                .Where(e => !newFileNames.Contains(Path.GetFileName(e.picture_url)))
                .ToList();

            foreach (var image in toDelete)
            {
                // Delete physical file
                var filePath = Path.Combine(folderPath, Path.GetFileName(image.picture_url));
                if (File.Exists(filePath))
                    File.Delete(filePath);

                _dbContext.incident_pictures.Remove(image);
            }

            // 5. Add new images
            foreach (var file in newImages)
            {
                if (existingImages.Any(e => Path.GetFileName(e.picture_url).Equals(file.FileName, StringComparison.OrdinalIgnoreCase)))
                    continue; // already exists

                var uniqueName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(folderPath, uniqueName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Get the server URL from configuration
                string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");
                if (string.IsNullOrWhiteSpace(serverUrl))
                {
                    _logger.LogError("Server URL is not configured.");
                    throw new InvalidOperationException("Server URL is not configured.");
                }

                // Save one image per incident
                foreach (var incidentId in incidentIds)
                {
                    _dbContext.incident_pictures.Add(new IncidentPicture
                    {
                        incident_id = incidentId,
                        picture_url = $"{serverUrl}/Documents/Event/Incident/Incident_Pictures/{eventId}/{uniqueName}"
                    });
                }
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
