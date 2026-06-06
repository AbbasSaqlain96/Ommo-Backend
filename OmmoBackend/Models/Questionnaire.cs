using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmmoBackend.Models
{
    public class Questionnaire
    {
        [Key]
        public int questionnaire_id { get; set; }

        [Required]
        public int company_id { get; set; }

        [Required]
        public int questionnaire_number { get; set; }

        [Required]
        public string answer { get; set; } = string.Empty;

        [Required]
        public DateTime created_at { get; set; }

        [Required]
        public DateTime updated_at { get; set; }

    }
}
