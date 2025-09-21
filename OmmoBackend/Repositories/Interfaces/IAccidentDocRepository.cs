using Microsoft.EntityFrameworkCore;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IAccidentDocRepository : IGenericRepository<AccidentDoc>
    {
        Task<int> GetLastAccidentDocIdAsync();
        Task AddMultipleAccidentDocsAsync(List<AccidentDoc> accidentDocs);
        Task<List<AccidentDoc>> GetAccidentDocumentsAsync(int accidentId);
    }
}
