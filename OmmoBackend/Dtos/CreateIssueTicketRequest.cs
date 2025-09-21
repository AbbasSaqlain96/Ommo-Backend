namespace OmmoBackend.Dtos
{
    public class CreateIssueTicketRequest
    {
        public int CatagoryId { get; set; }
        public int VehicleId { get; set; }
        public DateTime ScheduleDate { get; set; }
        public string Priority { get; set; }
        public int AssignedUser { get; set; }
        public bool IsManagedRecurringly { get; set; }
        public string? RecurrentType { get; set; }
        //public int? TimeInterval { get; set; }
        public int? TimeInterval
        {
            get => IsManagedRecurringly && RecurrentType == "mileage" ? null : _timeInterval;
            set => _timeInterval = value;
        }
        private int? _timeInterval;

        public int? MileageInterval
        {
            get => IsManagedRecurringly && RecurrentType == "time" ? null : _mileageInterval;
            set => _mileageInterval = value;
        }
        private int? _mileageInterval;

        public int? CurrentMileage 
        {
            get => IsManagedRecurringly && RecurrentType == "time" ? null : _currentMileage;
            set => _currentMileage = value;
        }
        private int? _currentMileage;

        public List<IFormFile>? Image { get; set; }
    }
}
