using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Models
{
    public class Otp
    {
        [Key]
        public int otp_id { get; set; }

        [Required]
        public int otp_code { get; set; }
    
        [Required]
        public string receiver { get; set; }

        [Required]
        public DateTime generate_time { get; set; } = DateTime.Now;

        [Required]
        public OtpSubject subject { get; set; }

        public int? company_id { get; set; }
    }
}