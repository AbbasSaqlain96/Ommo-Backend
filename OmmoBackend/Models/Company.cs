using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Models
{
    public class Company
    {
        [Key]
        public int company_id { get; set; }

        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(100, ErrorMessage = "Company name cannot be longer than 100 characters.")]
        public string name { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(200, ErrorMessage = "Address cannot be longer than 200 characters.")]
        public string address { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^\+?[1-9]\d{1,14}$", ErrorMessage = "Invalid phone number format.")]
        public string phone { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email address format.")]
        public string email { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "User count must be a non-negative number.")]
        public int user_count { get; set; }

        [Required(ErrorMessage = "Company type is required.")]
        [Range(1, 2, ErrorMessage = "Invalid company type. Must be 1 (Carrier) or 2 (Dispatch Service).")]
        public int company_type { get; set; }

        public int? parent_id { get; set; }

        public int category_type { get; set; } = 1;
        public CompanyStatus status { get; set; } = CompanyStatus.active;
        public DateTime created_at { get; set; } = DateTime.Now;

        [RegularExpression(@"^\+?[1-9]\d{1,14}$", ErrorMessage = "Invalid twillo number format.")]
        public string twilio_number { get; set; }
        
        public string? tax_id { get; set; }
        public string? dot_number { get; set; }
        public string? logo { get; set; }
        public int? fleet_size { get; set; }
        public string? eld { get; set; }
    }
}