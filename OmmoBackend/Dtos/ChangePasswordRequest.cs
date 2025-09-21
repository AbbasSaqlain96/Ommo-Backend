using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Dtos
{
    public class ChangePasswordRequest
    {
        [Required]
        public string Password { get; set; }

        [Required]
        public string ConfirmPassword { get; set; }
    }
}
