using OmmoBackend.Helpers.Enums;
using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Dtos
{
    public class AssetDetailIssueTicketDto
    {
        public int TicketId { get; init; }
        public int IssueId { get; init; }
        public DateTime? NextScheduleDate { get; set; }
        public DateTime? ScheduleDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public int? VehicleId { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public int? AssignedUser { get; set; }
        public bool IsmanagedRecurringly { get; set; }
        [Required]
        public int? CarrierId { get; set; }
        public string RecurrentType { get; set; }
        public int? TimeInterval { get; set; }
        public int? MileageInterval { get; set; }
        public int? CurrentMileage { get; set; }
        public int? NextMileage { get; set; }
    }
}