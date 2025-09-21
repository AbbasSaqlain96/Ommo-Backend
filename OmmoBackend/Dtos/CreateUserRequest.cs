using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Validators;

namespace OmmoBackend.Dtos
{
    public record CreateUserRequest
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailValidation(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; }

        [PhoneValidation(ErrorMessage = "Invalid phone format.")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Role Id is required")]
        public int RoleId { get; set; }
    }
}