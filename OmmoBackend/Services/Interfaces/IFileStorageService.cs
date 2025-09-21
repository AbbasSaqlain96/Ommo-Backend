using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Services.Interfaces
{
    public interface IFileStorageService
    {
        //Task<string> SaveProfileImageAsync(IFormFile file, HttpRequest request, int userId, int? companyId);
        Task<string> SaveProfileImageAsync(IFormFile file, int companyId, int userId);

        Task<string?> UpdateProfileImageAsync(IFormFile? file, int? companyId, int userId);

        Task DeleteProfileImageAsync(int userId);
    }
}