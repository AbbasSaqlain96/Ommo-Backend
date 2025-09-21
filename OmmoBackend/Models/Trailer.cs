using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Models
{
    public class Trailer
    {
        [Key]
        public int trailer_id { get; set; }
        [Required]
        public TrailerType trailer_type { get; set; }
        public int vehicle_id { get; set; }
    }
}