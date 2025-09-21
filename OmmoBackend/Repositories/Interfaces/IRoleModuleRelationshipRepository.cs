using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IRoleModuleRelationshipRepository: IGenericRepository<RoleModuleRelationship>
    {
        Task<bool> UserHasPermissionAsync(int roleId, string moduleName, int requiredAccessLevel);
    }
}