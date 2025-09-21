using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class Agent
    {
        [Key]
        public int agent_id { get; set; }
        public int company_id { get; set; }
        public string agent_type { get; set; }
    }
}
