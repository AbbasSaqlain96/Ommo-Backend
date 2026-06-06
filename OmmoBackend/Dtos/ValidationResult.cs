using OmmoBackend.Models;

namespace OmmoBackend.Dtos
{
    public class ValidationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public PackagePlan? Plan { get; set; }
    }
}
