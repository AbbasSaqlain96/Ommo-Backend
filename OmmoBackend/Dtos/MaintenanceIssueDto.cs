using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Dtos
{
    public record MaintenanceIssueDto
    {
        public int IssueId { get; init; }
        public string IssueDescription { get; init; }
        public IssueType IssueType { get; init; }
        public IssueCat IssueCategory { get; init; }
        public DateTime CreatedAt { get; init; }
        public int? CompanyId { get; init; }
        public DateTime? ScheduleInterval { get; init; }
    }
}