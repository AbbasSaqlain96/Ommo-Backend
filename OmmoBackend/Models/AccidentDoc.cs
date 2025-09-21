using OmmoBackend.Helpers.Enums;
using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class AccidentDoc
    {
        [Key]
        public int accident_doc_id { get; set; }
        public int accident_id { get; set; }
        public int doc_type_id { get; set; }
        public AccidentDocStatus status { get; set; }
        public string file_path { get; set; }

        [Required]
        public string doc_number { get; set; }

        public DateTime update_date { get; set; }
    }
}
