using OmmoBackend.Validators;
using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Dtos
{
    public class SignupCompanyRequest
    {
        [Required(ErrorMessage = "Company Name is required")]
        public string CompanyName { get; set; }

        [EmailOrPhoneRequired("Phone", ErrorMessage = "Either Email or Phone must be provided.")]
        [EmailValidation(ErrorMessage = "Invalid email address format.")]
        public string? Email { get; set; }
        public string Address { get; set; }

        [EmailOrPhoneRequired("Email", ErrorMessage = "Either Email or Phone must be provided.")]
        [PhoneValidation(ErrorMessage = "Invalid phone format.")]
        public string? Phone { get; set; }
        public string? MCNumber { get; set; }
        public IFormFile CompanyLogo { get; set; }
        public int CompanyType { get; set; }
        public string? DOTNumber { get; set; }
        public int FleetSize { get; set; }
        public string? ELD { get; set; }

        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Role Id is required")]
        public int RoleID { get; set; }
        public IFormFile UserProfilePicture { get; set; }
    }
}
