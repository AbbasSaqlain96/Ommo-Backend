using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Models
{
    public class RequestModule
    {
        [Key]
        public int request_module_id { get; set; }

        [Required]
        public int subscription_request_id { get; set; }

        [Required]
        public int module_id { get; set; }
    }
}