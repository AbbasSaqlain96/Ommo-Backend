using OmmoBackend.Helpers.Enums;
using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class MaintenanceCategory
    {
        [Key]
        public int category_id { get; set; }
        public string category_description { get; set; }
        public string category_name { get; set; }
        public DateTime created_at { get; set; }
        public MaintenanceCategoryType cat_type { get; set; }
        public int? carrier_id { get; set; }
    }
}