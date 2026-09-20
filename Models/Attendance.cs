using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMSystem.Models
{
    public class Attendance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        public DateTime? ClockInTime { get; set; }
        public DateTime? ClockOutTime { get; set; }

        public double? ClockInLatitude { get; set; }
        public double? ClockInLongitude { get; set; }

        public double? ClockOutLatitude { get; set; }
        public double? ClockOutLongitude { get; set; }

        [StringLength(200)]
        public string? ClockInLocationName { get; set; }

        [StringLength(200)]
        public string? ClockOutLocationName { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Present"; // Present, Late, Clocked In, Completed, HalfDay

        public bool IsLocationVerified { get; set; } = false;

        public double? WorkDurationMinutes { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string DisplayWorkDuration
        {
            get
            {
                if (!WorkDurationMinutes.HasValue || WorkDurationMinutes.Value <= 0) return "--";
                var ts = TimeSpan.FromMinutes(WorkDurationMinutes.Value);
                return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            }
        }
    }
}
