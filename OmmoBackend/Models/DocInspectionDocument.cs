using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class DocInspectionDocument
    {
        [Key]
        public int inspection_doc_id { get; set; }

        [Required]
        public int doc_type_id { get; set; }

        [Required]
        public int doc_inspection_id { get; set; }

        public string? doc_number { get; set; }

        public string? path { get; set; }

        public string? status { get; set; }
    }
}
