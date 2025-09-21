using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Models
{
    public class Module
    {
        [Key]
        public int module_id {get;set;}
        
        [Required(ErrorMessage = "Module name is required.")]
        public string module_name {get;set;}
    
        public string tab_name {get;set;}

        [Required(ErrorMessage = "Company type access is required.")]
        public CompanyType company_type_access {get;set;}
    }
}