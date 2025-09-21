using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class RoleModuleService : IRoleModuleService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<RoleModuleService> _logger;

        public RoleModuleService(AppDbContext dbContext, ILogger<RoleModuleService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }


        public async Task<bool> HasAccessAsync(int roleId, string moduleName, int accessLevel)
        {
            try
            {
                _logger.LogInformation("Checking access for Role ID {RoleId} on Module {ModuleName} with Access Level {AccessLevel}", roleId, moduleName, accessLevel);

                var module = await _dbContext.module
                    .FirstOrDefaultAsync(m => m.module_name == moduleName);


                if (module == null)
                {
                    _logger.LogWarning("Module {ModuleName} not found.", moduleName);
                    // Module not found
                    return false;
                }

                var moduleId = module.module_id;

                // Check if the role has access to the module
                var hasModuleAccess = await _dbContext.role_module_relationship
                    .AnyAsync(rmr => rmr.role_id == roleId &&
                                     rmr.module_id == moduleId &&
                                     (int)rmr.access_level >= accessLevel);

                if (!hasModuleAccess)
                {
                    _logger.LogWarning("Role ID {RoleId} does not have sufficient access to Module {ModuleName}.", roleId, moduleName);
                    return false;
                }

                _logger.LogInformation("Role ID {RoleId} has module-level access to {ModuleName}. Checking component access...", roleId, moduleName);

                // Retrieve components linked to the module
                var componentIds = await _dbContext.component
                    .Where(c => c.module_id == moduleId)
                    .Select(c => c.component_id)
                    .ToListAsync();

                if (!componentIds.Any())
                {
                    _logger.LogInformation("Module {ModuleName} has no linked components. Access granted.", moduleName);
                    // No components are linked to the module, so module access is sufficient
                    return true;
                }

                // Check if the role has access to any components related to the module 
                var roleComponents = await _dbContext.role_component_relationship
                    .Where(rcr => rcr.role_id == roleId && componentIds.Contains(rcr.component_id))
                    .ToListAsync();

                // If the module has components, validate access to them
                foreach (var component in roleComponents)
                {
                    // Ensure the component is tied to a valid access level and belongs to the module
                    if ((int)component.access_level >= accessLevel)
                    {
                        _logger.LogInformation("Role ID {RoleId} has component-level access to {ModuleName}.", roleId, moduleName);
                        return true;
                    }
                }

                _logger.LogWarning("Role ID {RoleId} does not have sufficient access to any components in Module {ModuleName}.", roleId, moduleName);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while checking access for Role ID {RoleId} on Module {ModuleName}.", roleId, moduleName);
                return false;
            }
        }

        public async Task<bool> HasComponentAccessAsync(int roleId, string componentName, int accessLevel)
        {
            try
            {
                _logger.LogInformation("Checking component access for Role ID {RoleId} on Component {ComponentName} with Access Level {AccessLevel}", roleId, componentName, accessLevel);

                var hasAccess = await (from rcr in _dbContext.role_component_relationship
                                       join c in _dbContext.component on rcr.component_id equals c.component_id
                                       where rcr.role_id == roleId
                                             && c.component_name == componentName
                                             && (int)rcr.access_level >= accessLevel
                                       select rcr)
                              .AnyAsync();

                if (!hasAccess)
                {
                    _logger.LogWarning("Role ID {RoleId} does not have sufficient access to Component {ComponentName}.", roleId, componentName);
                }
                else
                {
                    _logger.LogInformation("Role ID {RoleId} has access to Component {ComponentName}.", roleId, componentName);
                }

                return hasAccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while checking component access for Role ID {RoleId} on Component {ComponentName}.", roleId, componentName);
                return false;
            }
        }
    }
}