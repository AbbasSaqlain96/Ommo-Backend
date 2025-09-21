using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;

namespace OmmoBackend.Services.Interfaces
{
    public interface IRoleService
    {
        Task<ServiceResponse<RoleCreationResult>> CreateRoleAsync(CreateRoleRequest createRoleRequest);
        Task<ServiceResponse<IEnumerable<RoleDto>>> GetRolesByCompanyIdAsync(int companyId);
        Task<ServiceResponse<RoleCreationResult>> CreateSuperAdminRoleAsync(int companyId);
        Task<ServiceResponse<Role>> CreateRoleAsync(CreateRoleDto createRoleDto, int companyId);
        Task<ServiceResponse<IEnumerable<RoleDto>>> GetRolesAsync(int companyId);
        Task<ServiceResponse<string>> DeleteRoleAsync(int roleId, int companyId, int userRoleId);
    }
}