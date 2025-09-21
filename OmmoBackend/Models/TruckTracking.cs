using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Models
{
    public class TruckTracking
    {
        [Key]
        public int tracking_id { get; set; }

        [Required]
        public int truck_id { get; set; }

        [Required]
        public int odometer { get; set; }

        [Required]
        public DateTime last_update_odometer { get; set; }

        [Required]
        public int speed { get; set; }

        [Required]
        public DateTime last_update_speed { get; set; }

        [Required]
        public int mileage { get; set; }

        [Required]
        public DateTime last_updated_mileage { get; set; }
    }
}