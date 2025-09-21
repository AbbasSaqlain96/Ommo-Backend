using OmmoBackend.Validators;
using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Dtos
{
    public record CreateUserSignupRequest
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; }

        [EmailOrPhoneRequired("Phone", ErrorMessage = "Either Email or Phone must be provided.")]
        [EmailValidation(ErrorMessage = "Invalid email address format.")]
        public string? Email { get; set; }


        [EmailOrPhoneRequired("Email", ErrorMessage = "Either Email or Phone must be provided.")]
        [PhoneValidation(ErrorMessage = "Invalid phone format.")]
        public string? Phone { get; set; }



        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Company Id is required")]
        public int CompanyId { get; set; }

        [Required(ErrorMessage = "Role Id is required")]
        public int RoleId { get; set; }

        public string? Status { get; set; }
    }
}
