using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class RoleModuleRelationshipRepository : GenericRepository<RoleModuleRelationship>, IRoleModuleRelationshipRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<RoleModuleRelationshipRepository> _logger;
        public RoleModuleRelationshipRepository(AppDbContext dbContext, ILogger<RoleModuleRelationshipRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> UserHasPermissionAsync(int roleId, string moduleName, int requiredAccessLevel)
        {
            try
            {
                _logger.LogInformation("Checking permission for Role ID: {RoleId}, Module: {ModuleName}, Required Access Level: {RequiredAccessLevel}", roleId, moduleName, requiredAccessLevel);

                // Fetch the module's ID based on the provided module name
                var module = await _dbContext.module
                    .FirstOrDefaultAsync(m => m.module_name == moduleName);

                if (module == null)
                {
                    _logger.LogWarning("Module '{ModuleName}' not found. Permission check failed for Role ID: {RoleId}", moduleName, roleId);
                    // If the module does not exist, return false
                    return false;
                }

                // Check if the role has the required access level for the module
                var hasPermission = await _dbContext.role_module_relationship
                    .AnyAsync(rmr =>
                        rmr.role_id == roleId &&
                        rmr.module_id == module.module_id &&
                        (int)rmr.access_level >= requiredAccessLevel);

                _logger.LogInformation("Permission check result for Role ID: {RoleId}, Module: {ModuleName}: {HasPermission}", roleId, moduleName, hasPermission);

                return hasPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking permissions for Role ID: {RoleId}, Module: {ModuleName}", roleId, moduleName);
                // Log exception (consider logging in a real scenario)
                throw new Exception("An error occurred while checking permissions.", ex);
            }
        }
    }
}