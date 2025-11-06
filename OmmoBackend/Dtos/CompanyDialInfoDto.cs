using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Dtos
{
    public class CompanyDialInfoDto
    {
        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(100, ErrorMessage = "Company name cannot be longer than 100 characters.")]
        public string name { get; set; } = string.Empty;

        [RegularExpression(@"^\+?[1-9]\d{1,14}$", ErrorMessage = "Invalid twillo number format.")]
        public string? twillo_number { get; set; }

        public CompanyDialInfoDto(string companyName, string twilioNumber)
        {
            name = companyName;
            twillo_number = twilioNumber;
        }
    }
}
