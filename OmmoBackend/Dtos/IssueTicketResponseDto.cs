namespace OmmoBackend.Dtos
{
    public class IssueTicketResponseDto
    {
        public int TicketId { get; set; }
        public int CatagoryId { get; set; }
        public string CatagoryName { get; set; }
        public int? VehicleId { get; set; }
        public DateTime? ScheduleDate { get; set; }
        public DateTime? NextScheduleDate { get; set; }
        public DateTime? CompleteDate { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public int AssignedUserId { get; set; }
        public string AssignedUser { get; set; }
        public bool IsManagedRecurringly { get; set; }
        public string? RecurrentType { get; set; }
        public int? TimeInterval { get; set; }
        public int? MileageInterval { get; set; }
        public int? CurrentMileage { get; set; }
        public int? NextMileage { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int CreatedBy { get; set; }
        public List<TicketFileResponseDto> ImageFiles { get; set; }
    }

    public class TicketFileResponseDto
    {
        public string Filepath { get; set; }
    }
}
