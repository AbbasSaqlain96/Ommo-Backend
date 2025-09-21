using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Models
{
    public class TruckLocation
    {
        [Key]
        public int location_id { get; set; }

        [Required]
        public int truck_id { get; set; }

        [Required]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90 degrees.")]
        public double latitude { get; set; }

        [Required]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180 degrees.")]
        public double longitude { get; set; }

        [Required]
        public USState location_state { get; set; }

        public string location_city { get; set; }

        [Required]
        public DateTime last_updated_location { get; set; }
    }
}