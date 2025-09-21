using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record CreateMaintenanceIssueRequest
    {
        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Issue Type is required.")]
        [RegularExpression("recurring|one-time", ErrorMessage = "Issue type must be either 'recurring' or 'one-time'")]
        public string IssueType { get; set; }

        public int? CompanyId { get; set; }

        public DateTime? ScheduleInterval { get; set; }
        
    }
}