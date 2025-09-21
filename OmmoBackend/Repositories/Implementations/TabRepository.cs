using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class TabRepository : ITabRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<TabRepository> _logger;
        public TabRepository(AppDbContext dbContext, ILogger<TabRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<IEnumerable<ModuleComponentDto>> GetModulesWithComponentsByRoleAsync(int roleId)
        {
            _logger.LogInformation("Fetching modules with components for RoleId: {RoleId}", roleId);

            try
            {
                var modulesWithComponents = await (from rmr in _dbContext.role_module_relationship
                                                   join m in _dbContext.module on rmr.module_id equals m.module_id
                                                   join c in _dbContext.component on m.module_id equals c.module_id into cJoin
                                                   from cItem in cJoin.DefaultIfEmpty() // Left join to include modules without components
                                                   join rcr in _dbContext.role_component_relationship
                                                       on new { ComponentId = cItem != null ? cItem.component_id : 0, RoleId = rmr.role_id }
                                                       equals new { ComponentId = rcr.component_id, RoleId = rcr.role_id } into rcrJoin
                                                   from rcrItem in rcrJoin.DefaultIfEmpty() // Left join for role_component_relationship
                                                   where rmr.role_id == roleId // Role fetched from the token
                                                   select new ModuleComponentDto
                                                   {
                                                       ModuleName = m.module_name,
                                                       ComponentName = cItem != null ? cItem.component_name : null, // Null if no component
                                                       AccessLevel = ((AccessLevel)rmr.access_level).ToString(), // Convert enum to string
                                                       ComponentAccessLevel = rcrItem != null
                                                                  ? (int)rcrItem.access_level
                                                                  : (int)AccessLevel.none // Default access level for unauthorized components
                                                   })
                    .ToListAsync();

                // Ensure all modules are included even if they lack components
                var groupedModules = modulesWithComponents
                    .GroupBy(x => x.ModuleName)
                    .SelectMany(group =>
                    {
                        var components = group.Where(x => x.ComponentName != null).ToList();

                        // If no components exist for the module, add a placeholder with no components
                        if (!components.Any())
                        {
                            components.Add(new ModuleComponentDto
                            {
                                ModuleName = group.Key,
                                ComponentName = null,
                                AccessLevel = group.First().AccessLevel,
                                ComponentAccessLevel = (int)AccessLevel.none
                            });
                        }

                        return components;
                    });

                _logger.LogInformation("Successfully retrieved {Count} modules with components for RoleId: {RoleId}", groupedModules.Count(), roleId);

                return groupedModules;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database update error while retrieving modules and components for RoleId: {RoleId}", roleId);
                throw new ApplicationException(ErrorMessages.DatabaseOperationFailed, dbEx);
            }
            catch (Exception ex) // error handling for unexpected issues
            {
                _logger.LogError(ex, "Unexpected error occurred while fetching modules and components for RoleId: {RoleId}", roleId);
                throw new ApplicationException("An unexpected error occurred while fetching modules and components.", ex);
            }
        }
    }
}