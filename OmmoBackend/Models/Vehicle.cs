using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Models
{
    public class Vehicle
    {
        [Key]
        public int vehicle_id { get; set; }

        [Required]
        public string plate_number { get; set; }

        [Required]
        public LicensePlateState license_plate_state { get; set; }

        public int? carrier_id { get; set; }

        [Required]
        public string vin_number { get; set; }

        [Required]
        public VehicleType vehicle_type { get; set; }

        [Range(1, 5)]
        public int? rating { get; set; }

        public bool is_assigned { get; set; }

        [Required]
        [Range(1900, 2100)]
        public int year { get; set; }

        public DateTime created_at { get; set; } = DateTime.Now;

        public string vehicle_trademark { get; set; }

        [Required]
        public VehicleStatus status { get; set; }
    }
}