using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Services.Interfaces
{
    public interface IRoleModuleService
    {
        Task<bool> HasAccessAsync(int roleId, string module, int accessLevel);
        Task<bool> HasComponentAccessAsync(int roleId, string component, int accessLevel);
    }
}