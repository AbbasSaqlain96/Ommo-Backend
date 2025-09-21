using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Models
{
    public class DocumentType
    {
        [Key]
        public int doc_type_id { get; set; }

        [Required]
        public string doc_name { get; set; }

        [Required]
        public DocCategory doc_cat { get; set; }

        [Required]
        public DocType doc_type { get; set; }

        public int? company_id { get; set; }
    }
}