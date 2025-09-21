using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Models
{
    public class Unit
    {
        [Key]
        public int unit_id { get; set; }

        [Required]
        public int carrier_id { get; set; }

        [Required]
        public int driver_id { get; set; }

        [Required]
        public int dispatcher_id { get; set; } // Foreign key to Users (Dispatcher)

        [Required]
        public DateTime created_at { get; set; }

        [Required]
        public UnitStatus status { get; set; }        

        [Required]
        public DateTime last_updated_status { get; set; }

        [Required]
        public int truck_id { get; set; }

        [Required]
        public int trailer_id { get; set; }
    }
}