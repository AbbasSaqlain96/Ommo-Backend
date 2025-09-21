using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class ModuleRepository : GenericRepository<Module>, IModuleRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<ModuleRepository> _logger;
        public ModuleRepository(AppDbContext dbContext, ILogger<ModuleRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<ModuleDto>> GetModulesWithComponentsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching modules with their components.");

                var modulesWithComponents = await _dbContext.Set<Module>()
                .Select(module => new ModuleDto
                {
                    ModuleId = module.module_id,
                    ModuleName = module.module_name,
                    Components = _dbContext.Set<Component>()
                        .Where(c => c.module_id == module.module_id)
                        .Select(c => new CompDto
                        {
                            ComponentId = c.component_id,
                            ComponentName = c.component_name
                        })
                        .ToList()
                })
                .ToListAsync();

                _logger.LogInformation("Successfully fetched {Count} modules with components.", modulesWithComponents.Count);

                return modulesWithComponents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching modules with components.");
                throw;
            }
        }
    }
}