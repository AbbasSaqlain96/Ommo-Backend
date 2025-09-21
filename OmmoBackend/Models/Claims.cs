using OmmoBackend.Helpers.Enums;
using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class Claims
    {
        [Key]
        public int claim_id { get; set; }
        public int event_id { get; set; }
        public ClaimType claim_type { get; set; }
        public int claim_amount { get; set; }
        public ClaimStatus status { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string claim_description { get; set; }
    }
}
