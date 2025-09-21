using OmmoBackend.Helpers.Enums;
using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Dtos
{
    public class IntegrationRequestDto
    {
        public string? ServiceEmailDAT { get; set; }

        [Required]
        public LoadboardType Loadboard { get; set; }

        [Required]
        public bool IsNew { get; set; }

        public string? ExistingEmail { get; set; }
    }
}
