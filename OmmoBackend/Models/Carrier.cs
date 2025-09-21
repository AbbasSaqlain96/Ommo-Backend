using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Models
{
    public class Carrier
    {
        [Key]
        public int carrier_id { get; set; }

        [Required(ErrorMessage = "Company Id is required.")]
        public int company_id { get; set; }

        [Required(ErrorMessage = "MC number is required.")]
        [StringLength(20, ErrorMessage = "MC number cannot be longer than 20 characters.")]
        public string mc_number { get; set; }
    }
}