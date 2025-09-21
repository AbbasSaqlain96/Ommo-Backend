using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IIncidentPicturesRepository : IGenericRepository<IncidentPicture>
    {
        Task UpdateImagesAsync(int eventId, List<IFormFile> newImages);
    }
}
