using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Models
{
    public class Component
    {
        [Key]
        public int component_id { get; set; }

        [Required]
        public int module_id { get; set; }

        [Required]
        public string component_name { get; set; }
        public string component_tab_name { get; set; }
        public int? order_no { get; set; }
    }
}