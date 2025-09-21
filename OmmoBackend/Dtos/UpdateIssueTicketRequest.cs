namespace OmmoBackend.Dtos
{
    public class UpdateIssueTicketRequest
    {
        public int TicketId { get; set; }
        public int? CategoryId { get; set; }
        public int? VehicleId { get; set; }
        public DateTime? ScheduleDate { get; set; }
        public string? Priority { get; set; }
        public string? Status { get; set; }
        public int? AssignedUser { get; set; }
        public bool? IsManagedRecurringly { get; set; }
        public string? RecurrentType { get; set; }
        public int? TimeInterval { get; set; }
        public int? MileageInterval { get; set; }
        public int? CurrentMileage { get; set; }
        public List<IFormFile>? Image { get; set; }
    }
}
