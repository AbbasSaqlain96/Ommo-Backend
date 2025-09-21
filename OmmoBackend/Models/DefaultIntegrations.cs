using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class DefaultIntegrations
    {
        [Key]
        public int default_integration_id { get; set; }
        public string integration_name { get; set; }
        public string integration_cat { get; set; }
        public string integration_description { get; set; }
        public string logo_path { get; set; }
    }
}
