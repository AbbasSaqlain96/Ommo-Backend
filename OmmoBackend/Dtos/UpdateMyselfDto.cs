using OmmoBackend.Validators;

namespace OmmoBackend.Dtos
{
    public record UpdateMyselfDto
    {
        public string? Username { get; init; }

        [EmailValidation(ErrorMessage = "Invalid email address format.")]
        public string? Email { get; init; }

        [PhoneValidation(ErrorMessage = "Invalid phone format.")]
        public string? Phone { get; init; }

        public int? Role { get; init; }

        public bool DeletePicture { get; init; } = false;
    }
}
