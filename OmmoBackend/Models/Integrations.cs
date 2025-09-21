using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OmmoBackend.Models
{
    public class Integrations
    {
        [Key]
        public int integration_id { get; set; }
        public int default_integration_id { get; set; }
        public int company_id { get; set; }
        public string integration_status { get; set; }

        [Column(TypeName = "jsonb")]
        public JsonDocument credentials { get; set; }

        [Column(TypeName = "jsonb")]
        public JsonDocument extra_config { get; set; } = JsonDocument.Parse("{}");
        public DateTime last_updated { get; set; }

        public string? requested_by_email { get; set; }
    }
}
