using Microsoft.EntityFrameworkCore;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IAccidentPicturesRepository : IGenericRepository<AccidentPicture>
    {
        Task<List<AccidentPicture>> GetAccidentImagesByAccidentIdAsync(int accidentId);
    }
}
