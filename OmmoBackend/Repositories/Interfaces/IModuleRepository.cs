using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IModuleRepository : IGenericRepository<Module>
    {
        Task<List<ModuleDto>> GetModulesWithComponentsAsync();
    }
}