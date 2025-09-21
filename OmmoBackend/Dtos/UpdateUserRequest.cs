using OmmoBackend.Validators;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record UpdateUserRequest
    {
        [Required(ErrorMessage = "User Id is required")]
        public int UserId { get; init; }

        public string? Username { get; init; }

        [EmailValidation(ErrorMessage = "Invalid email address format.")]
        public string? Email { get; set; }

        [PhoneValidation(ErrorMessage = "Invalid phone format.")]
        public string? Phone { get; set; }

        public int? RoleId { get; init; }
    }
}