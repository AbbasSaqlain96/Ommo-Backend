using Microsoft.EntityFrameworkCore.Storage;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IUnitOfWork
    {
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}
