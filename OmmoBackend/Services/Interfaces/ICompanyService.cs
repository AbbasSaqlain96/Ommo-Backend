using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Services.Implementations;

namespace OmmoBackend.Services.Interfaces
{
    public interface ICompanyService
    {
        Task<ServiceResponse<CompanyCreationResult>> CreateCompanyAsync(CreateCompanyRequest createCompanyRequest);
        // Task<ChildCompanyCreationResult> CreateChildCompanyAsync(CreateChildCompanyRequest createChildCompanyRequest);
        // Task<ChildCompanyRemovalResult> RemoveChildCompanyAsync(RemoveChildCompanyRequest removeChildCompanyRequest);
        Task<bool> CompanyIdExist(int companyId);

        Task<DuplicateCheckResult> CheckDuplicateEmailAndPhoneAsync(string email, string phone);

        Task<ServiceResponse<CompanyProfileDto>> GetCompanyProfileAsync(int companyId);

        Task<ServiceResponse<CompanyProfileDto>> UpdateCompanyProfileAsync(int companyId, UpdateCompanyProfileDto updateDto);
    }
}