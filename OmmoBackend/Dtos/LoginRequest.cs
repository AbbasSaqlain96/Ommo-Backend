using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Dtos
{
    public record LoginRequest
    {
        [Required(ErrorMessage = "Email or phone is required")]
        public string EmailOrPhone { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }
    }
}