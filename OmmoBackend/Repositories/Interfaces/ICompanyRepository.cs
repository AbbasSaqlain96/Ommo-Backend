using OmmoBackend.Dtos;
using OmmoBackend.Models;
using System.ComponentModel.Design;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface ICompanyRepository : IGenericRepository<Company>
    {
        Task<(bool IsEmailDuplicate, bool IsPhoneDuplicate)> CheckDuplicateEmailAndPhoneInCompanyAsync(string email, string phone);
        Task<bool> CheckDuplicateMCNumberAsync(string mcNumber, int companyType);
        Task<CompanyCreationResult> CreateCompanyAsync(CreateCompanyRequest createCompanyRequest);
        Task<CompanyProfileDto> GetCompanyProfileAsync(int companyId);
        Task<bool> CheckCompanyExistsAsync(int? parentId);
        Task<CompanyDialInfoDto?> GetCompanyDialInfoAsync(int companyId);
        Task<bool> ExistsAsync(int companyId);
        Task UpdatePhoneNumberAsync(int companyId, string phoneNumber);
        Task<string?> GetCompanyEmailAsync(int companyId);
        Task<bool> IsCompanyVerifiedAsync(int companyId);
    }
}