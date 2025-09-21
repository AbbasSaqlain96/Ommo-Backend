using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Models
{
    public class IssueTicket
    {
        [Key]
        public int ticket_id { get; set; }
        public DateTime? next_schedule_date { get; set; }
        public DateTime? schedule_date { get; set; }
        public DateTime? completed_date { get; set; }
        public int? vehicle_id { get; set; }
        public Priority priority { get; set; }
        public IssueTicketStatus status { get; set; }
        public int? assigned_user { get; set; }
        public bool ismanaged_recurringly { get; set; } = false;
        [Required]
        public int? carrier_id { get; set; }
        public RecurrentType? recurrent_type { get; set; }
        public int? time_interval
        {
            get => ismanaged_recurringly && recurrent_type == RecurrentType.mileage ? null : _timeInterval;
            set => _timeInterval = value;
        }
        private int? _timeInterval;

        public int? mileage_interval
        {
            get => ismanaged_recurringly && recurrent_type == RecurrentType.time ? null : _mileageInterval;
            set => _mileageInterval = value;
        }
        private int? _mileageInterval;

        public int? current_mileage
        {
            get => ismanaged_recurringly && recurrent_type == RecurrentType.time ? null : _currentMileage;
            set => _currentMileage = value;
        }
        private int? _currentMileage; 
        
        public int? next_mileage { get; set; }
        public int? category_id { get; set; }
        public int created_by { get; set; }
        public DateTime updated_at { get; set; }
        public int company_id { get; set; }
    }
}