using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Models
{
    public class RoleComponentRelationship
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int role_component_relationship_id { get; set; }

        [Required]
        public int component_id { get; set; }

        [Required]
        public int role_id { get; set; }

        [Required]
        public AccessLevel access_level { get; set; }
    }
}