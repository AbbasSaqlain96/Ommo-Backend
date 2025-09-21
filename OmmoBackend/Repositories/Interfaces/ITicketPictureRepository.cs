using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface ITicketPictureRepository : IGenericRepository<TicketPicture>
    {
        Task UpdateImagesAsync(int companyId, int ticketId, int eventId, List<IFormFile> newImages);
    }
}
