using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class Accident
    {
        [Key]
        public int accident_id { get; set; }
        
        public bool has_casuality { get; set; }
        
        public bool driver_drug_test { get; set; }

        [Required]
        public bool driver_fault { get; set; }

        [Required]
        public bool alcohol_test { get; set; }

        public DateTime? drug_test_date_time { get; set; }

        public DateTime? alcohol_test_date_time { get; set; }

        public int? ticket_id { get; set; }
        
        public int event_id { get; set; }
    }
}