using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class IncidentDoc
    {
        [Key]
        public int incident_doc_id { get; set; }

        [Required]
        public int doc_type_id { get; set; }

        [Required]
        public string doc_number { get; set; }

        [Required]
        public int incident_id { get; set; }

        [Required]
        public string file_path { get; set; }

        [Required]
        public string status { get; set; }
    }
}
