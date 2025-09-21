using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Models
{
    public class DispatchService
    {
        [Key]
        public int dispatch_service_id {get;set;}

        [Required(ErrorMessage = "Company Id is required.")]
        public int company_id {get;set;}
    }
}