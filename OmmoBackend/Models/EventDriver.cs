using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Models
{
    public class EventDriver
    {
        [Key]
        public int event_id { get; set; }

        [Required]
        public string event_type { get; set; }

        [Required]
        public int driver_id { get; set; }

        [Required]
        public DateTime scheduled_date { get; set; }

        [Required]
        public DateTime deadline { get; set; }

        [Required]
        public EventDriverStatus status { get; set; }
    }
}