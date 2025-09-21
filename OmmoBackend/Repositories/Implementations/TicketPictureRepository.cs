using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OmmoBackend.Data;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using System.ComponentModel.Design;
using static System.Net.Mime.MediaTypeNames;

namespace OmmoBackend.Repositories.Implementations
{
    public class TicketPictureRepository : GenericRepository<TicketPicture>, ITicketPictureRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TicketPictureRepository> _logger;
        private readonly ITicketDocRepository _ticketDocRepository;

        public TicketPictureRepository(AppDbContext dbContext, IConfiguration configuration, ILogger<TicketPictureRepository> logger, ITicketDocRepository ticketDocRepository) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
            _ticketDocRepository = ticketDocRepository;
        }

        public async Task UpdateImagesAsync(int companyId, int ticketId, int eventId, List<IFormFile> newImages)
        {
            string baseFolderPath = _configuration.GetValue<string>("AppSettings:ServerDocumentDirectory");
            if (string.IsNullOrWhiteSpace(baseFolderPath))
                throw new InvalidOperationException("Server document directory is not configured.");

            string folderPath = Path.Combine(baseFolderPath, "Event", "Ticket", companyId.ToString(), "Ticket_Image", ticketId.ToString());

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

            // 1. Get existing images for this ticket
            var existingImages = await _dbContext.ticket_pictures
                .Where(p => p.ticket_id == ticketId)
                .ToListAsync();

            var newFileNames = newImages.Select(f => f.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 2. Delete removed images
            var toDelete = existingImages
                .Where(e => !newFileNames.Contains(Path.GetFileName(e.picture_url)))
                .ToList();

            foreach (var image in toDelete)
            {
                string fileName = Path.GetFileName(image.picture_url);
                string filePath = Path.Combine(folderPath, fileName);

                if (File.Exists(filePath))
                    File.Delete(filePath);

                _dbContext.ticket_pictures.Remove(image);
            }

            // 3. Add new images
            foreach (var file in newImages)
            {
                if (existingImages.Any(e => Path.GetFileName(e.picture_url).Equals(file.FileName, StringComparison.OrdinalIgnoreCase)))
                    continue;

                //var uniqueName = $"{Guid.NewGuid()}_{file.FileName}";
                var fileExtension = Path.GetExtension(file.FileName);
                //var fileName = $"{lastTicketDocId}_{driverId}_{Guid.NewGuid()}{fileExtension}";
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var savePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string serverUrl = _configuration.GetValue<string>("AppSettings:ServerUrl");
                if (string.IsNullOrWhiteSpace(serverUrl))
                    throw new InvalidOperationException("Server URL is not configured.");

                var relativePath = $"/Documents/Event/Ticket/{companyId}/Ticket_Image/{ticketId}/{fileName}";
                var fullPath = $"{serverUrl}{relativePath}";

                _dbContext.ticket_pictures.Add(new TicketPicture
                {
                    ticket_id = ticketId,
                    picture_url = fullPath,
                    upload_date = DateTime.UtcNow
                });
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
