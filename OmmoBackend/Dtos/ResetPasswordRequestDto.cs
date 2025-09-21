using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Dtos
{
    public class ResetPasswordRequestDto
    {
        [Required, EmailAddress]
        public string EmailAddress { get; init; }
    }
}
