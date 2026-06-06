using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Dtos
{
    public class SupportRequestDto
    {
        public string Subject { get; set; }

        public string Message { get; set; }

        [EmailAddress]
        public string ContactEmail { get; set; }

        public bool IsOmmoExistingCustomer { get; set; } = false;
    }
}
    