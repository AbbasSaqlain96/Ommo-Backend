using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Dtos
{
    public record IssueTicketRequest
    {
        public int IssueId { get; init; }
        public string AssetType { get; init; }
        public int AssetId { get; init; }
        public int AssignedUserId { get; init; }
        public int CarrierId { get; init; }
        public bool IsManagedRecurringly { get; init; }
        public string Priority { get; init; }
        public DateTime ScheduleDate { get; init; }
    }
}