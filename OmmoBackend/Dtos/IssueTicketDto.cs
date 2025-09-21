using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record IssueTicketDto
    {
        public int TicketId { get; init; }
        public int IssueId { get; init; }
        public string IssueDescription { get; init; }
        public string AssetType { get; init; }
        public int AssetId { get; init; }
        public DateTime ScheduleDate { get; init; }
        public DateTime? NextScheduleDate { get; init; }
        public int? AssignedUserId { get; init; }
        public string Status { get; init; }
        public string Priority { get; init; }
    }
}