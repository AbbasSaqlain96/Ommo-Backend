using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Models
{
    public class RoleModuleRelationship
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int role_module_relationship_id { get; set; }

        [Required(ErrorMessage = "Module Id is required.")]
        public int module_id { get; set; }

        [Required(ErrorMessage = "Role Id is required.")]
        public int role_id { get; set; }

        [Required]
        public AccessLevel access_level { get; set; }
    }
}