using OmmoBackend.Helpers.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmmoBackend.Models
{
    public class CompanyPlanChangeRequest
    {
        [Key]
        public int company_plan_change_request_id { get; set; }

        [Required]
        public int company_id { get; set; }

        [Required]
        public int request_plan { get; set; }

        [Required]
        public PlanChangeRequestStatus status { get; set; }

        [Required]
        public DateTimeOffset current_cycle_end_date { get; set; }

        [Required]
        public DateTimeOffset created_at { get; set; } = DateTimeOffset.UtcNow;

        [Required]
        public DateTimeOffset updated_at { get; set; } = DateTimeOffset.UtcNow;
    }
}
