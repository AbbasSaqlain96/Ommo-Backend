using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Services.Interfaces
{
    public interface IModuleService
    {
        Task<bool> ModuleExists(int moduleId);
        Task<ServiceResponse<List<ModuleDto>>> GetModulesAsync();
    }
}