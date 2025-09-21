using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OmmoBackend.Data;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class RoleRepository : GenericRepository<Role>, IRoleRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<RoleRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the RoleRepository class with the specified database context.
        /// </summary>
        public RoleRepository(AppDbContext dbContext, ILogger<RoleRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Checks if a role with the specified name exists within a given company.
        /// </summary>
        /// <param name="roleName">The name of the role to check.</param>
        /// <param name="companyId">The Id of the company to check within.</param>
        /// <returns>True if the role exists; otherwise, false.</returns>
        public async Task<bool> RoleExistsAsync(string roleName, int companyId)
        {
            _logger.LogInformation("Checking if role '{RoleName}' exists for Company ID: {CompanyId}", roleName, companyId);

            try
            {
                return await _dbContext.role
                    .AnyAsync(r => r.role_name == roleName && r.company_id == companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking role existence for '{RoleName}' in Company ID: {CompanyId}", roleName, companyId);
                throw;
            }
        }

        /// <summary>
        /// Adds a new role and its associated role-module relationships to the database, ensuring atomicity with a transaction.
        /// </summary>
        /// <param name="role">The role to be added.</param>
        /// <param name="relationships">The role-module relationships to be associated with the role.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AddRoleWithRelationshipsAsync(Role role, IEnumerable<RoleModuleRelationship> relationships)
        {
            _logger.LogInformation("Adding role '{RoleName}' with {RelationshipCount} relationships for Company ID: {CompanyId}", role.role_name, relationships.Count(), role.company_id);

            // Start a database transaction
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // Add the role to the database
                await _dbContext.role.AddAsync(role);
                await _dbContext.SaveChangesAsync();

                // Add each role-module relationship to the database
                foreach (var relationship in relationships)
                {
                    relationship.role_id = role.role_id; // Set the role Id for the relationship
                    await _dbContext.role_module_relationship.AddAsync(relationship);
                }

                // Save all changes to the database
                await _dbContext.SaveChangesAsync();

                // Commit the transaction
                await transaction.CommitAsync();

                _logger.LogInformation("Successfully added role '{RoleName}' and committed transaction.", role.role_name);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update error while adding role '{RoleName}'. Rolling back transaction.", role.role_name);

                // Rollback the transaction
                await transaction.RollbackAsync();
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while adding role '{RoleName}'. Rolling back transaction.", role.role_name);

                // Rollback the transaction
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding role '{RoleName}'. Rolling back transaction.", role.role_name);

                // Rollback the transaction if an error occurs
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Retrieves the list of RoleModuleRelationship entities for a specific role and company.
        /// This method performs a LINQ join between the RoleModuleRelationship and Role tables
        /// to fetch relationships associated with a specified role name and company ID.
        /// </summary>
        /// <param name="roleName">The name of the role for which to retrieve module relationships.</param>
        /// <param name="companyId">The Id of the company associated with the role.</param>
        /// <returns>A collection of RoleModuleRelationship entities associated with the given role and company.</returns>
        public async Task<IEnumerable<RoleModuleRelationship>> GetRoleModuleRelationshipsAsync(string roleName, int companyId)
        {
            _logger.LogInformation("Fetching role-module relationships for Role '{RoleName}' in Company ID: {CompanyId}", roleName, companyId);

            try
            {
                // Fetch existing role-module relationships for a given role and company
                // Perform a join between RoleModuleRelationship and Role tables
                var relationships = await _dbContext.role_module_relationship
                    .Join(
                        _dbContext.role,
                        rmr => rmr.role_id,      // Foreign key in RoleModuleRelationship
                        r => r.role_id,          // Primary key in Role
                        (rmr, r) => new { rmr, r } // Project into a new anonymous type
                    )
                    .Where(joined => joined.r.role_name == roleName && joined.r.company_id == companyId)
                    .Select(joined => joined.rmr) // Select the RoleModuleRelationship records
                    .ToListAsync();

                _logger.LogInformation("Retrieved {RelationshipCount} relationships for Role '{RoleName}' in Company ID: {CompanyId}", relationships.Count, roleName, companyId);
                return relationships;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while retrieving relationships for Role '{RoleName}' in Company ID: {CompanyId}", roleName, companyId);
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while retrieving relationships for Role '{RoleName}' in Company ID: {CompanyId}", roleName, companyId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving relationships for Role '{RoleName}' in Company ID: {CompanyId}", roleName, companyId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a list of roles associated with a specified company from the database asynchronously.
        /// </summary>
        /// <param name="companyId">The Id of the company for which to retrieve roles.</param>
        /// <returns>An enumerable collection of <see cref="Role"/> representing the roles of the company.</returns>
        public async Task<IEnumerable<Role>> GetRolesByCompanyIdAsync(int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching roles for CompanyId: {CompanyId}", companyId);

                // Fetch roles associated with the given Company Id
                var roles = await _dbContext.role
                            .Where(role => role.company_id == companyId)
                            .ToListAsync();

                _logger.LogInformation("Successfully retrieved {RoleCount} roles for CompanyId: {CompanyId}", roles.Count, companyId);

                // Return the list of roles
                return roles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching roles for CompanyId: {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<bool> RoleExistsAsync(int roleId)
        {
            try
            {
                _logger.LogInformation("Checking if role exists with RoleId: {RoleId}", roleId);

                var exists = await _dbContext.role.AnyAsync(r => r.role_id == roleId);

                _logger.LogInformation("Role existence check for RoleId {RoleId}: {Exists}", roleId, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if RoleId: {RoleId} exists", roleId);
                throw;
            }
        }


        /**/
        // public async Task<IDbContextTransaction> BeginTransactionAsync()
        // {
        //     return await _dbContext.Database.BeginTransactionAsync();
        // }

        // public async Task<Role> CreateRoleAsync(Role role)
        // {
        //     _dbContext.role.Add(role);
        //     await _dbContext.SaveChangesAsync();
        //     return role;
        // }

        // public async Task UpdateRoleModuleRelationshipAsync(int roleId, Dictionary<int, int> accessLevels)
        // {
        //     var roleModuleRelationships = accessLevels.Select(al =>
        //     {
        //         if (!Enum.IsDefined(typeof(AccessLevel), al.Value))
        //             throw new ArgumentException($"Invalid access level: {al.Value}");

        //         return new RoleModuleRelationship
        //         {
        //             role_id = roleId,
        //             module_id = al.Key,
        //             access_level = (AccessLevel)al.Value
        //         };
        //     });

        //     _dbContext.role_module_relationship.AddRange(roleModuleRelationships);
        //     await _dbContext.SaveChangesAsync();
        // }

        // public async Task UpdateRoleComponentRelationshipAsync(int roleId, Dictionary<int, int> accessLevels)
        // {
        //     var roleComponentRelationships = accessLevels.Select(al =>
        //     {
        //         if (!Enum.IsDefined(typeof(AccessLevel), al.Value))
        //             throw new ArgumentException($"Invalid access level: {al.Value}");

        //         return new RoleComponentRelationship
        //         {
        //             role_id = roleId,
        //             component_id = al.Key,
        //             access_level = (AccessLevel)al.Value
        //         };
        //     });

        //     _dbContext.role_component_relationship.AddRange(roleComponentRelationships);
        //     await _dbContext.SaveChangesAsync();
        // }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            try
            {
                _logger.LogInformation("Starting database transaction...");
                return await _dbContext.Database.BeginTransactionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while starting a database transaction");
                throw;
            }
        }

        public async Task<Role> CreateRoleAsync(Role role)
        {
            try
            {
                _logger.LogInformation("Creating a new role with RoleName: {RoleName}", role.role_name);

                _dbContext.role.Add(role);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully created role with RoleId: {RoleId}", role.role_id);
                return role;
            }
            catch (DbUpdateException e)
            {
                _logger.LogError(e, "Database update error occurred while creating role: {RoleName}", role.role_name);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while creating role: {RoleName}", role.role_name);
                throw;
            }
        }

        public async Task UpdateRoleModuleRelationshipAsync(int roleId, int moduleId, int accessLevel)
        {
            try
            {
                _logger.LogInformation("Updating role-module relationship. RoleId: {RoleId}, ModuleId: {ModuleId}, AccessLevel: {AccessLevel}", roleId, moduleId, accessLevel);

                if (!Enum.IsDefined(typeof(AccessLevel), accessLevel))
                {
                    _logger.LogWarning("Invalid access level provided: {AccessLevel}", accessLevel);
                    throw new ArgumentException($"Invalid access level: {accessLevel}");
                }

                var roleModuleRelationship = new RoleModuleRelationship
                {
                    role_id = roleId,
                    module_id = moduleId,
                    access_level = (AccessLevel)accessLevel
                };

                // Explicit conversion to integer if necessary
                roleModuleRelationship.access_level = (AccessLevel)Enum.ToObject(typeof(AccessLevel), accessLevel);

                _dbContext.role_module_relationship.Add(roleModuleRelationship);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully updated role-module relationship. RoleId: {RoleId}, ModuleId: {ModuleId}", roleId, moduleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating role-module relationship. RoleId: {RoleId}, ModuleId: {ModuleId}, AccessLevel: {AccessLevel}", roleId, moduleId, accessLevel);
                throw;
            }
        }

        public async Task UpdateRoleComponentRelationshipAsync(int roleId, int componentId, int accessLevel)
        {
            try
            {
                _logger.LogInformation("Updating role-component relationship. RoleId: {RoleId}, ComponentId: {ComponentId}, AccessLevel: {AccessLevel}", roleId, componentId, accessLevel);

                if (!Enum.IsDefined(typeof(AccessLevel), accessLevel))
                {
                    _logger.LogWarning("Invalid access level provided: {AccessLevel}", accessLevel);
                    throw new ArgumentException($"Invalid access level: {accessLevel}");
                }

                var roleComponentRelationship = new RoleComponentRelationship
                {
                    role_id = roleId,
                    component_id = componentId,
                    access_level = (AccessLevel)accessLevel
                };

                // Explicit conversion to integer if necessary
                roleComponentRelationship.access_level = (AccessLevel)Enum.ToObject(typeof(AccessLevel), accessLevel);

                _dbContext.role_component_relationship.Add(roleComponentRelationship);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully updated role-component relationship. RoleId: {RoleId}, ComponentId: {ComponentId}", roleId, componentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating role-component relationship. RoleId: {RoleId}, ComponentId: {ComponentId}, AccessLevel: {AccessLevel}", roleId, componentId, accessLevel);
                throw;
            }
        }


        public async Task<IEnumerable<Role>> GetRolesAsync(int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching roles for CompanyId: {CompanyId}", companyId);

                // Build query based on filters
                var query = _dbContext.role.AsQueryable();

                // Include standard roles if no companyId exists for them
                if (companyId > 0)
                {
                    query = query.Where(role =>
                        (role.company_id == null && role.role_cat == RoleCategory.standard) ||
                        (role.company_id == companyId)
                    );
                }

                // // Apply role_cat filter if provided
                // if (!string.IsNullOrEmpty(roleCat))
                // {
                //     // Convert roleCat to uppercase/lowercase for enum comparison (if needed)
                //     if (Enum.TryParse(typeof(RoleCategory), roleCat, true, out var parsedRoleCat))
                //     {
                //         query = query.Where(role => role.role_cat == (RoleCategory)parsedRoleCat);
                //     }
                //     else
                //     {
                //         throw new ArgumentException($"Invalid role category: {roleCat}");
                //     }
                // }

                // Fetch results
                var roles = await query.ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} roles for CompanyId: {CompanyId}", roles.Count, companyId);

                return roles;
            }
            catch (InvalidCastException ex)
            {
                _logger.LogError(ex, "Invalid cast error while fetching roles. Ensure proper null handling.");
                throw new InvalidCastException("Error in handling null values. Ensure proper null handling in your database fields.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving roles for CompanyId: {CompanyId}", companyId);
                throw new Exception("An error occurred while retrieving roles.", ex);
            }
        }

        public async Task<Role?> GetRoleByIdAsync(int roleId)
        {
            try
            {
                _logger.LogInformation("Fetching role details for RoleId: {RoleId}", roleId);
                var role = await _dbContext.role.FirstOrDefaultAsync(r => r.role_id == roleId);

                if (role == null)
                {
                    _logger.LogWarning("No role found with RoleId: {RoleId}", roleId);
                }
                else
                {
                    _logger.LogInformation("Successfully retrieved role with RoleId: {RoleId}", roleId);
                }
                
                return role;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching role details for RoleId: {RoleId}", roleId);
                throw new Exception("An error occurred while fetching the role details.", ex);
            }
        }

        public async Task<bool> HasAccessToDeleteRoleAsync(int userRoleId)
        {
            try
            {
                _logger.LogInformation("Checking delete access for UserRoleId: {UserRoleId}", userRoleId);

                // Check if the user has access to the "Setting" module
                var hasModuleAccess = await _dbContext.role_module_relationship
                    .AnyAsync(rmr => rmr.role_id == userRoleId &&
                                     rmr.module_id == _dbContext.module
                                        .Where(m => m.module_name == "Setting")
                                        .Select(m => m.module_id)
                                        .FirstOrDefault());

                if (!hasModuleAccess)
                {
                    _logger.LogWarning("UserRoleId: {UserRoleId} does not have module access to delete roles.", userRoleId);
                    return false;
                }

                // Check if the user has access to the "Role" component
                var hasComponentAccess = await _dbContext.role_component_relationship
                    .AnyAsync(rcr => rcr.role_id == userRoleId &&
                                     rcr.component_id == _dbContext.component
                                        .Where(c => c.component_name == "Role")
                                        .Select(c => c.component_id)
                                        .FirstOrDefault());

                bool hasAccess = hasModuleAccess && hasComponentAccess;

                if (hasAccess)
                {
                    _logger.LogInformation("UserRoleId: {UserRoleId} has access to delete roles.", userRoleId);
                }
                else
                {
                    _logger.LogWarning("UserRoleId: {UserRoleId} does not have component access to delete roles.", userRoleId);
                }

                return hasAccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while checking delete permissions for UserRoleId: {UserRoleId}", userRoleId);
                throw new Exception("An error occurred while checking user permissions.", ex);
            }
        }

        public async Task DeleteRoleAsync(int roleId)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                try
                {
                    _logger.LogInformation("Attempting to delete RoleId: {RoleId}", roleId);

                    // Check if the role exists
                    var role = await _dbContext.role.FindAsync(roleId);
                    if (role == null)
                    {
                        _logger.LogWarning("RoleId: {RoleId} does not exist. Aborting deletion.", roleId);
                        throw new Exception($"Role with ID {roleId} does not exist.");
                    }

                    // Reload the role to avoid stale data
                    _dbContext.Entry(role).Reload();

                    // Find an admin role ID to assign (modify this logic as per your admin role criteria)
                    var adminRole = await _dbContext.role.FirstOrDefaultAsync(r => r.role_name == "Admin");
                    if (adminRole == null)
                    {
                        _logger.LogError("Admin role not found. Cannot proceed with role deletion.");
                        throw new Exception("Admin role not found. Cannot proceed with role deletion.");
                    }

                    // Update users with the deleted role to admin role
                    await _dbContext.Database.ExecuteSqlRawAsync("UPDATE users SET role_id = {0} WHERE role_id = {1}", adminRole.role_id, roleId);

                    // Delete related relationships using raw SQL
                    await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM role_module_relationship WHERE role_id = {0}", roleId);
                    await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM role_component_relationship WHERE role_id = {0}", roleId);

                    // Delete the role
                    _dbContext.role.Remove(role);

                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Successfully deleted RoleId: {RoleId}", roleId);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Concurrency issue while deleting RoleId: {RoleId}. Transaction rolled back.", roleId);
                    throw new Exception("Concurrency issue occurred. Please refresh and try again.", ex);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Unexpected error while deleting RoleId: {RoleId}. Transaction rolled back.", roleId);
                    throw new Exception("An unexpected error occurred.", ex);
                }
            });
        }

        public async Task<bool> ModuleExistsAsync(int moduleId)
        {
            return await _dbContext.module.AnyAsync(m => m.module_id == moduleId);
        }

        public async Task<bool> ComponentExistsAsync(int componentId)
        {
            return await _dbContext.component.AnyAsync(c => c.component_id == componentId);
        }

        public async Task<bool> ComponentBelongsToModuleAsync(int componentId, int moduleId)
        {
            return await _dbContext.component.AnyAsync(c => c.component_id == componentId && c.module_id == moduleId);
        }
    }
}