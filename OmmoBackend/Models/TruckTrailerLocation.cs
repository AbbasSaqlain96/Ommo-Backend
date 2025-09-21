using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Models
{
    public class TruckTrailerLocation
    {
        [Key]
        public int location_id { get; set; }
        public string vehicle_type { get; set; }
        public int vehicle_id { get; set; }
        public decimal latitude { get; set; }
        public decimal longitude { get; set; }
        public string location_state { get; set; }
        public string location_city { get; set; }
        public DateTime last_updated_location { get; set; }
    }
}