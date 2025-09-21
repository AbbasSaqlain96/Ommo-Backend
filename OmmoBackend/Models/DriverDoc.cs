using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Models
{
    public class DriverDoc
    {
        [Key]
        public int doc_id { get; set; }

        [Required]
        public int doc_type_id { get; set; }

        [Required]
        public int driver_id { get; set; }

        [Required]
        public string file_path { get; set; }

        [Required]
        public DriverDocStatus status { get; set; }

        [Required]
        public DateTime start_date { get; set; }

        [Required]
        public DateTime end_date { get; set; }

        [Required]
        public DateTime updated_at { get; set; }
    }
}