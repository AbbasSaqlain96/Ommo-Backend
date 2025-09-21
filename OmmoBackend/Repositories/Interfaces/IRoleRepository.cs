using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IRoleRepository : IGenericRepository<Role>
    {
        Task<bool> RoleExistsAsync(string roleName, int companyId);
        Task AddRoleWithRelationshipsAsync(Role role, IEnumerable<RoleModuleRelationship> relationships);
        Task<IEnumerable<RoleModuleRelationship>> GetRoleModuleRelationshipsAsync(string roleName, int companyId);
        Task<IEnumerable<Role>> GetRolesByCompanyIdAsync(int companyId);
        Task<bool> RoleExistsAsync(int roleId);
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<Role> CreateRoleAsync(Role role);
        Task UpdateRoleModuleRelationshipAsync(int roleId, int moduleId, int accessLevel);
        Task UpdateRoleComponentRelationshipAsync(int roleId, int componentId, int accessLevel);
        Task<IEnumerable<Role>> GetRolesAsync(int companyId);
        Task<Role?> GetRoleByIdAsync(int roleId);
        Task<bool> HasAccessToDeleteRoleAsync(int userRoleId);
        Task DeleteRoleAsync(int roleId);


        Task<bool> ModuleExistsAsync(int moduleId);
        Task<bool> ComponentExistsAsync(int componentId);
        Task<bool> ComponentBelongsToModuleAsync(int componentId, int moduleId);
    }
}