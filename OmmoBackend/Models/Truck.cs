using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Models
{
    public class Truck
    {
        [Key]
        public int truck_id { get; set; }

        [Required]
        public string brand { get; set; }

        [Required]
        public int vehicle_id { get; set; }

        [Required]
        public string model { get; set; }

        [Required]
        public TruckFuelType fuel_type { get; set; }

        [Required]
        public string color { get; set; }

        public TruckStatus truck_status { get; set; }

        public int? unit_id { get; set; }
    }
}