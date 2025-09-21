using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record CreateRoleRequest
    {
        [Required(ErrorMessage = "Role Name is required.")]
        public string RoleName { get; init; }

        [Required(ErrorMessage = "Company Id is required.")]
        public int CompanyId { get; init; }

        [Required(ErrorMessage = "Module Role Relationship is required.")]
        public List<ModuleRoleRelationshipRequest> ModuleRoleRelationships { get; init; }
    }

    public record CreateRoleDto
    {

        [Required(ErrorMessage = "Role Name is required.")]
        public string RoleName { get; init; }

        //[Required(ErrorMessage = "Company Id is required.")]
        //public int CompanyId { get; init; }

        // [Required(ErrorMessage = "Access Level.")]
        // public Dictionary<int, int> AccessLevel { get; set; }

        [Required]
        public List<ModuleAccessDto> Modules { get; set; }
    }

    public class ModuleAccessDto
    {
        [Required]
        public int ModuleId { get; set; }

        [Required]
        public int AccessLevel { get; set; }

        [Required]
        public List<ComponentAccessDto> Components { get; set; }
    }

    public class ComponentAccessDto
    {
        [Required]
        public int ComponentId { get; set; }

        [Required]
        public int AccessLevel { get; set; }
    }

}