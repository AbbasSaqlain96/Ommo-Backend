using OmmoBackend.Helpers.Enums;
using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class DocInspection
    {
        [Key]
        public int doc_inspection_id { get; set; }

        [Required]
        public DocInspectionStatus status { get; set; }

        [Required]
        public int inspection_level { get; set; }

        [Required]
        public CitationStatus citation { get; set; }

        [Required]
        public int event_id { get; set; }
    }
}
