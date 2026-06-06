using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Dtos
{
    public sealed class CreateCheckoutSessionRequest
    {
        [Required]
        public int PlanId { get; set; }
    }
}
