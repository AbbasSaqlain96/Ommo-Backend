using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Models
{
    public class Role
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int role_id { get; set; }
        public int? company_id { get; set; }

        [Required(ErrorMessage = "Role Name is required.")]
        public string role_name { get; set; }
        
        [Required(ErrorMessage = "Role category is required.")]
        public RoleCategory role_cat { get; set; }
    }
}