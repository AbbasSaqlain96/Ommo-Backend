using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class GlobalIntegrationCredentials
    {
        [Key]
        public int global_integration_id { get; set; }

        public int default_integration_id { get; set; }

        public string credential_name { get; set; } = string.Empty;

        public string credential_value { get; set; } = string.Empty;

        public DateTime created_at { get; set; }

        public DateTime updated_at { get; set; }
    }
}
