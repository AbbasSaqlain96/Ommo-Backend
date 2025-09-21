using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Dtos
{
    public class ResetPasswordConfirmDto
    {
        [Required] public string Token { get; init; }
        [Required] public string Password { get; init; }
        [Required] public string ConfirmPassword { get; init; }

    }
}
