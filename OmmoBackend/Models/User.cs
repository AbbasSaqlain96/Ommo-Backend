using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Helpers.Enums;
namespace OmmoBackend.Models
{
    public class User
    {
        [Key]
        public int user_id { get; set; }

        [Required]
        public int company_id { get; set; }

        [Required]
        public int role_id { get; set; }

        // Email Validation with optional empty string handling
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email address format.")]
        public string? user_email { get; set; }

        [Required]
        public string user_name { get; set; }

        // Phone number Validation
        [RegularExpression(@"^\+?[1-9]\d{1,14}$", ErrorMessage = "Invalid phone number format.")]
        public string? phone { get; set; }

        public string? profile_image_url { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        public byte[] password_hash { get; set; }

        public byte[] password_salt { get; set; }

        public UserStatus status { get; set; }

        // Custom logic to handle empty string values
        public void EnsureValidEmailAndPhone()
        {
            if (string.IsNullOrEmpty(user_email))
                user_email = null; // treat empty string as null
            if (string.IsNullOrEmpty(phone))
                phone = null; // treat empty string as null
        }
    }
}
