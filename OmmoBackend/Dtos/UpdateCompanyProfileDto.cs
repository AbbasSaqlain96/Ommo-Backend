using OmmoBackend.Validators;

namespace OmmoBackend.Dtos
{
    public record UpdateCompanyProfileDto
    {
        public string? Name { get; set; }
        public string? Address { get; set; }

        [PhoneValidation(ErrorMessage = "Invalid phone format.")]
        public string? Phone { get; set; }

        [EmailValidation(ErrorMessage = "Invalid email address format.")]
        public string? Email { get; set; }

        public string? TaxID { get; set; }

        public string? DOTNumber { get; set; }

        public string? MCNumber { get; set; }

        public IFormFile? Logo { get; set; }
    }
}
