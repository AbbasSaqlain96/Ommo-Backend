using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmmoBackend.Dtos;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IDriverRepository : IGenericRepository<Driver>
    {
        Task<Driver> GetDriverInfoByUnitIdAsync(int unitId);
        Task<IEnumerable<DriverListDto>> GetDriverListByCompanyIdAsync(int companyId);
        Task<DriverDetailDto?> GetDriverDetailAsync(int driverId);


        Task<List<int>> GetRequiredDocumentTypesAsync();
        Task<Driver> AddDriverAsync(int companyId, DriverInfoDto driverInfo);
        //Task<string> SaveDocumentAsync(int companyId, int driverId, DocumentDto document);
        Task AddDriverDocumentAsync(int driverId, int docTypeId, string path);

        Task<DriverInfoDto?> GetDriverByCDLOrEmailAsync(string cdlLicenseNumber, string email, string phoneNumber);

        Task<bool> IsValidDriverIdAsync(int driverId, int companyId);


        Task<bool> ExistsAsync(int driverId);
    }
}