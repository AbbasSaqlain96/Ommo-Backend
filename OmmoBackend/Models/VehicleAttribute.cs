using Microsoft.AspNetCore.Http.HttpResults;
using OmmoBackend.Helpers.Enums;
using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class VehicleAttribute
    {
        [Key]
        public int attribute_id { get; set; }
        public int vehicle_id { get; set; }
        public bool is_headrake { get; set; }
        public bool have_flatbed { get; set; }
        public bool have_loadbar { get; set; }
        public bool have_van_straps { get; set; }
        public int weight { get; set; }
        public int axle_spacing { get; set; }
        public int num_of_axles { get; set; }
        public VehicleType vehicle_type { get; set; }
        public DateTime updated_at { get; set; }
    }
}