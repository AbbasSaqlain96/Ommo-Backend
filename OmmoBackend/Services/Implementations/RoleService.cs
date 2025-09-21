using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IModuleRepository _moduleRepository;
        private readonly IModuleService _moduleService;
        private readonly ICompanyService _companyService;
        private readonly AppDbContext _dbContext;
        private readonly IRoleModuleRelationshipRepository _roleModuleRelationshipRepository;
        private readonly ILogger<RoleService> _logger;

        /// <summary>
        /// Initializes a new instance of the RoleService class with the specified repositories.
        /// </summary>
        public RoleService(
            IRoleRepository roleRepository,
            ICompanyRepository companyRepository,
            IModuleRepository moduleRepository,
            IModuleService moduleService,
            ICompanyService companyService,
            IRoleModuleRelationshipRepository roleModuleRelationshipRepository,
            AppDbContext dbContext,
            ILogger<RoleService> logger)
        {
            _roleRepository = roleRepository;
            _companyRepository = companyRepository;
            _moduleRepository = moduleRepository;
            _moduleService = moduleService;
            _companyService = companyService;
            _roleModuleRelationshipRepository = roleModuleRelationshipRepository;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<ServiceResponse<Role>> CreateRoleAsync(CreateRoleDto createRoleDto, int companyId)
        {
            // Use the execution strategy provided by the database context
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _roleRepository.BeginTransactionAsync();

                try
                {
                    _logger.LogInformation("Creating a new role with name {RoleName} for company {CompanyId}", createRoleDto.RoleName, companyId);

                    // Parse the role category (always "Custom" in this scenario)
                    if (!Enum.TryParse<RoleCategory>("Custom", true, out var roleCategory))
                    {
                        _logger.LogWarning("Invalid role category specified.");
                        return ServiceResponse<Role>.ErrorResponse("Invalid role category specified.", 400);
                    }

                    // Insert role into the Role table
                    var role = new Role
                    {
                        company_id = companyId,
                        role_name = createRoleDto.RoleName,
                        role_cat = roleCategory
                    };

                    var createdRole = await _roleRepository.CreateRoleAsync(role);
                    _logger.LogInformation("Role {RoleId} created successfully.", createdRole.role_id);

                    // Update role-module relationships
                    foreach (var module in createRoleDto.Modules)
                    {
                        if (module.AccessLevel != 1 && module.AccessLevel != 2)
                            return ServiceResponse<Role>.ErrorResponse("Access level must be either 1 (Read) or 2 (Write).", 400);

                        var moduleExists = await _roleRepository.ModuleExistsAsync(module.ModuleId);

                        if (!moduleExists)
                            return ServiceResponse<Role>.ErrorResponse("The specified module does not exist.", 404);

                        _logger.LogInformation("Assigning module {ModuleId} with access level {AccessLevel} to role {RoleId}", module.ModuleId, module.AccessLevel, createdRole.role_id);

                        await _roleRepository.UpdateRoleModuleRelationshipAsync(createdRole.role_id, module.ModuleId, module.AccessLevel);

                        // Update role-component relationships for the module's components
                        foreach (var component in module.Components)
                        {
                            if (component.AccessLevel != 1 && component.AccessLevel != 2)
                                return ServiceResponse<Role>.ErrorResponse("Access level must be either 1 (Read) or 2 (Write).", 400);

                            _logger.LogInformation("Assigning component {ComponentId} with access level {AccessLevel} to role {RoleId}", component.ComponentId, component.AccessLevel, createdRole.role_id);

                            var componentExists = await _roleRepository.ComponentExistsAsync(component.ComponentId);

                            if(!componentExists)
                                return ServiceResponse<Role>.ErrorResponse("The specified component does not exist.", 404);

                            var isComponentInModule = await _roleRepository.ComponentBelongsToModuleAsync(component.ComponentId, module.ModuleId);

                            if (!isComponentInModule)
                                return ServiceResponse<Role>.ErrorResponse("The specified component does not belong to the selected module.", 404);

                            await _roleRepository.UpdateRoleComponentRelationshipAsync(createdRole.role_id, component.ComponentId, component.AccessLevel);
                        }
                    }

                    await transaction.CommitAsync();
                    _logger.LogInformation("Transaction committed successfully for role {RoleId}.", createdRole.role_id);
                    return ServiceResponse<Role>.SuccessResponse(createdRole, "Role created successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while creating role {RoleName} for company {CompanyId}", createRoleDto.RoleName, companyId);
                    await transaction.RollbackAsync();
                    return ServiceResponse<Role>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
                }
            });
            // using var transaction = await _roleRepository.BeginTransactionAsync();

            // try
            // {
            //     if (!Enum.TryParse<RoleCategory>("Custom", true, out var roleCategory))
            //     {
            //         throw new ArgumentException("Invalid role category specified.");
            //     }

            //     // Insert role into the Role table  
            //     var role = new Role
            //     {
            //         company_id = createRoleDto.CompanyId,
            //         role_name = createRoleDto.RoleName,
            //         role_cat = roleCategory
            //     };

            //     var createdRole = await _roleRepository.CreateRoleAsync(role);

            //     // Update role-module relationships
            //     await _roleRepository.UpdateRoleModuleRelationshipAsync(createdRole.role_id, createRoleDto.AccessLevel);

            //     // Update component-module relationships
            //     await _roleRepository.UpdateRoleComponentRelationshipAsync(createdRole.role_id, createRoleDto.AccessLevel);

            //     await transaction.CommitAsync();
            //     return ServiceResponse<Role>.SuccessResponse(createdRole);
            // }
            // catch (Exception ex)
            // {
            //     await transaction.RollbackAsync();
            //     return ServiceResponse<Role>.ErrorResponse("An error occurred while creating the role: " + ex.Message);
            // }
        }

        /// <summary>
        /// Creates a new role asynchronously, including role-module relationships.
        /// </summary>
        /// <param name="createRoleRequest">The request data for creating a role.</param>
        /// <returns>A RoleCreationResult indicating the outcome of the role creation operation.</returns>
        public async Task<ServiceResponse<RoleCreationResult>> CreateRoleAsync(CreateRoleRequest createRoleRequest)
        {
            try
            {
                _logger.LogInformation("Starting role creation process for company {CompanyId} with role name {RoleName}", createRoleRequest.CompanyId, createRoleRequest.RoleName);

                // Check if the company Id exists; return an error if not
                if (!await _companyService.CompanyIdExist(createRoleRequest.CompanyId))
                {
                    _logger.LogWarning("Invalid Company Id: {CompanyId}", createRoleRequest.CompanyId);
                    return ServiceResponse<RoleCreationResult>.ErrorResponse("Invalid Company Id.");
                }

                // Check if the role name already exists within the company; return an error if it does
                if (await RoleExist(createRoleRequest.RoleName, createRoleRequest.CompanyId))
                {
                    _logger.LogWarning("Role name '{RoleName}' already exists for company {CompanyId}", createRoleRequest.RoleName, createRoleRequest.CompanyId);
                    return ServiceResponse<RoleCreationResult>.ErrorResponse("Role name must be unique within the company.");
                }

                // Validate module Ids in the role-module relationships; return an error if any module Id is invalid
                foreach (var relationship in createRoleRequest.ModuleRoleRelationships)
                {
                    try
                    {
                        if (!await _moduleService.ModuleExists(relationship.ModuleId))
                        {
                            _logger.LogWarning("Invalid Module Id: {ModuleId}", relationship.ModuleId);
                            return ServiceResponse<RoleCreationResult>.ErrorResponse($"Invalid Module Id: {relationship.ModuleId}.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error checking existence of Module Id: {ModuleId}", relationship.ModuleId);
                        return ServiceResponse<RoleCreationResult>.ErrorResponse($"Invalid Module Id: {relationship.ModuleId}.");
                    }
                }

                // Check for duplicate Module-Access level combinations
                var duplicateModuleChecks = createRoleRequest.ModuleRoleRelationships
                    .GroupBy(r => r.ModuleId)
                    .Where(g => g.Select(r => r.AccessLevel).Distinct().Count() > 1);

                if (duplicateModuleChecks.Any())
                {
                    _logger.LogWarning("Duplicate access level assignments detected for modules.");
                    return ServiceResponse<RoleCreationResult>.ErrorResponse("Same module cannot be assigned with different access levels for the same role.");
                }

                // Fetch existing relationships for this role and company from the database
                var existingRelationships = await _roleRepository
                    .GetRoleModuleRelationshipsAsync(createRoleRequest.RoleName, createRoleRequest.CompanyId);

                // Check if any existing module assignments conflict with the new request
                foreach (var relationship in createRoleRequest.ModuleRoleRelationships)
                {
                    var existingRelationship = existingRelationships
                        .FirstOrDefault(er => er.module_id == relationship.ModuleId);

                    if (existingRelationship != null && existingRelationship.access_level != (AccessLevel)relationship.AccessLevel)
                    {
                        _logger.LogWarning("Module {ModuleId} already has a different access level assigned.", relationship.ModuleId);
                        return ServiceResponse<RoleCreationResult>.ErrorResponse($"Module {relationship.ModuleId} already has a different access level assigned for this role in the company.");
                    }
                }

                // Create a new Role object
                Role role = new Role
                {
                    role_name = createRoleRequest.RoleName,
                    company_id = createRoleRequest.CompanyId
                };

                // Create a list of RoleModuleRelationship objects from the request
                var roleModuleRelationships = createRoleRequest.ModuleRoleRelationships.Select(relationship => new RoleModuleRelationship
                {
                    module_id = relationship.ModuleId,
                    access_level = (AccessLevel)relationship.AccessLevel
                }).ToList();

                // Add the role and its relationships to the repository asynchronously
                await _roleRepository.AddRoleWithRelationshipsAsync(role, roleModuleRelationships);

                _logger.LogInformation("Role {RoleId} created successfully for company {CompanyId}.", role.role_id, createRoleRequest.CompanyId);

                return ServiceResponse<RoleCreationResult>.SuccessResponse(new RoleCreationResult
                {
                    Success = true,
                    RoleId = role.role_id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the role {RoleName} for company {CompanyId}", createRoleRequest.RoleName, createRoleRequest.CompanyId);
                // Return an error result if an exception occurs during role creation
                return ServiceResponse<RoleCreationResult>.ErrorResponse("An error occurred while creating the role. Please try again.");
            }
        }

        /// <summary>
        /// Retrieves a list of roles associated with a specified company asynchronously.
        /// </summary>
        /// <param name="companyId">The Id of the company for which to retrieve roles.</param>
        /// <returns>An enumerable collection of <see cref="RoleDto"/> representing the roles of the company.</returns>
        public async Task<ServiceResponse<IEnumerable<RoleDto>>> GetRolesByCompanyIdAsync(int companyId)
        {
            _logger.LogInformation("Retrieving roles for CompanyId: {CompanyId}", companyId);

            try
            {
                // Validate that the Company Id exists
                var companyExists = await _companyRepository.GetByIdAsync(companyId);
                if (companyExists is null)
                {
                    _logger.LogWarning("Company with Id {CompanyId} not found", companyId);
                    return ServiceResponse<IEnumerable<RoleDto>>.ErrorResponse("Company not found.");
                }

                // Retrieve roles for the specified Company Id
                var roles = await _roleRepository.GetRolesByCompanyIdAsync(companyId);

                // Map the roles to RoleDto
                var roleDtos = roles.Select(role => new RoleDto
                {
                    RoleName = role.role_name
                });

                _logger.LogInformation("Retrieved {RoleCount} roles for CompanyId: {CompanyId}", roles.Count(), companyId);
                // Return the list of mapped RoleDto objects
                return ServiceResponse<IEnumerable<RoleDto>>.SuccessResponse(roleDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles for CompanyId: {CompanyId}", companyId);
                return ServiceResponse<IEnumerable<RoleDto>>.ErrorResponse("An error occurred while retrieving roles.");
            }
        }

        /// <summary>
        /// Checks if a role with the specified name exists within a given company.
        /// </summary>
        /// <param name="roleName">The name of the role to check.</param>
        /// <param name="companyId">The Id of the company to check within.</param>
        /// <returns>True if the role exists; otherwise, false.</returns>
        public async Task<bool> RoleExist(string roleName, int companyId)
        {
            _logger.LogInformation("Checking if role {RoleName} exists in CompanyId: {CompanyId}", roleName, companyId);
            return await _roleRepository.RoleExistsAsync(roleName, companyId);
        }

        public async Task<ServiceResponse<RoleCreationResult>> CreateSuperAdminRoleAsync(int companyId)
        {
            _logger.LogInformation("Creating SuperAdmin role for CompanyId: {CompanyId}", companyId);

            try
            {
                // Check if the company ID exists before proceeding
                if (!await _companyService.CompanyIdExist(companyId))
                {
                    _logger.LogWarning("Invalid CompanyId: {CompanyId}", companyId);
                    return ServiceResponse<RoleCreationResult>.ErrorResponse("Invalid Company Id.");
                }

                // Create the role entity
                var role = new Role
                {
                    role_name = "superadmin",
                    company_id = companyId
                };

                // Add the role to the role repository
                await _roleRepository.AddAsync(role);

                // Get all available modules
                var modules = await _moduleRepository.GetAllAsync();

                // Assign level 3 (READ & WRITE) access for each module to the Superadmin role
                foreach (var module in modules)
                {
                    var roleModuleRelationship = new RoleModuleRelationship
                    {
                        module_id = module.module_id,
                        role_id = role.role_id,
                        access_level = AccessLevel.read_only
                    };

                    await _roleModuleRelationshipRepository.AddAsync(roleModuleRelationship);
                }

                _logger.LogInformation("SuperAdmin role created successfully for CompanyId: {CompanyId}", companyId);
                // Return successful ServiceResponse with Role Id
                return new ServiceResponse<RoleCreationResult>
                {
                    Success = true,
                    Message = "SuperAdmin role created successfully.",
                    Data = new RoleCreationResult
                    {
                        Success = true,
                        RoleId = role.role_id
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating SuperAdmin role for CompanyId: {CompanyId}", companyId);
                // Return an error result if an exception occurs during role creation
                return ServiceResponse<RoleCreationResult>.ErrorResponse("An error occurred while creating the role. Please try again.");
            }
        }

        public async Task<ServiceResponse<IEnumerable<RoleDto>>> GetRolesAsync(int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching roles for companyId: {CompanyId}", companyId);

                // // Validate the role_cat parameter
                // if (roleCat != null && !new[] { "Custom", "Standard" }.Contains(roleCat, StringComparer.OrdinalIgnoreCase))
                //     return ServiceResponse<IEnumerable<RoleDto>>.ErrorResponse("Invalid role category specified.");

                // Fetch roles based on filters
                var roles = await _roleRepository.GetRolesAsync(companyId);

                // Map roles to RoleDto
                var roleDtos = roles.Select(role => new RoleDto
                {
                    RoleId = role.role_id,
                    RoleName = role.role_name,
                    RoleCategory = role.role_cat.ToString(),
                    CompanyId = role.company_id
                }).ToList();

                _logger.LogInformation("Successfully retrieved {Count} roles for companyId: {CompanyId}", roleDtos.Count, companyId);

                return ServiceResponse<IEnumerable<RoleDto>>.SuccessResponse(roleDtos, "Roles retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving roles for companyId: {CompanyId}", companyId);
                return ServiceResponse<IEnumerable<RoleDto>>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<string>> DeleteRoleAsync(int roleId, int companyId, int userRoleId)
        {
            try
            {
                _logger.LogInformation("Attempting to delete roleId: {RoleId} for companyId: {CompanyId}", roleId, companyId);

                // Fetch role details
                var role = await _roleRepository.GetRoleByIdAsync(roleId);
                if (role == null || (role.company_id != companyId && role.company_id != null))
                {
                    _logger.LogWarning("Role not found or not authorized. RoleId: {RoleId}, CompanyId: {CompanyId}", roleId, companyId);
                    return ServiceResponse<string>.ErrorResponse("Role not found or you don't have permission to delete it.", 404);
                }

                // Validate role category
                if (role.role_cat == RoleCategory.standard)
                {
                    _logger.LogWarning("Attempt to delete a standard role. RoleId: {RoleId}", roleId);
                    return ServiceResponse<string>.ErrorResponse("Deleting a Standard Role is not allowed.", 400);
                }

                // Check if the user has access to the "Setting" module and "Role" component
                var hasAccess = await _roleRepository.HasAccessToDeleteRoleAsync(userRoleId);
                if (!hasAccess)
                {
                    _logger.LogWarning("Insufficient permission. UserRoleId: {UserRoleId}, RoleId: {RoleId}", userRoleId, roleId);
                    return ServiceResponse<string>.ErrorResponse("You do not have permission to access this resource", 401);
                }

                // Delete role and related data
                await _roleRepository.DeleteRoleAsync(roleId);
                _logger.LogInformation("Successfully deleted RoleId: {RoleId}", roleId);

                return ServiceResponse<string>.SuccessResponse(null, "Role deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting RoleId: {RoleId}", roleId);
                return ServiceResponse<string>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }
    }
}