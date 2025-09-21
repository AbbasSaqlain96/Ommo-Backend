using Microsoft.Extensions.Hosting;
using OmmoBackend.Helpers.Enums;
using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class VehicleDocument
    {
        [Key]
        public int doc_id { get; set; }
        public int doc_type_id { get; set; }
        public int vehicle_id { get; set; }
        public USState state_code { get; set; }
        public DateTime start_date { get; set; }
        public DateTime end_date { get; set; }
        public string path { get; set; }
        public DateTime updated_at { get; set; }
        public VehicleDocumentStatus status { get; set; }
    }
}