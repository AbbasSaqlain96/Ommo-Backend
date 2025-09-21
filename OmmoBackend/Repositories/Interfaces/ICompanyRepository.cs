using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OmmoBackend.Dtos;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface ICompanyRepository : IGenericRepository<Company>
    {
        Task<(bool IsEmailDuplicate, bool IsPhoneDuplicate)> CheckDuplicateEmailAndPhoneInCompanyAsync(string email, string phone);
        Task<bool> CheckDuplicateMCNumberAsync(string mcNumber, int companyType);
        Task<CompanyCreationResult> CreateCompanyAsync(CreateCompanyRequest createCompanyRequest);

        Task<CompanyProfileDto> GetCompanyProfileAsync(int companyId);

        Task<bool> CheckCompanyExistsAsync(int? parentId);
        Task<bool> ExistsAsync(int companyId);
    }
}