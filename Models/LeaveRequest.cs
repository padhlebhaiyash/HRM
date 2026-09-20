using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMSystem.Models
{
    public class LeaveRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Employee")]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [Required]
        [Display(Name = "Leave Type")]
        public int LeaveTypeId { get; set; }

        [ForeignKey("LeaveTypeId")]
        [Display(Name = "Leave Type")]
        public LeaveType? LeaveType { get; set; }

        public HalfDayPeriod HalfDayPeriod { get; set; } = HalfDayPeriod.None;

        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        public LeaveStatus Status { get; set; } = LeaveStatus.Pending;

        [Display(Name = "Approved By")]
        public int? ApprovedById { get; set; }

        [ForeignKey("ApprovedById")]
        public User? ApprovedBy { get; set; }

        [Display(Name = "Applied At")]
        public DateTime AppliedAt { get; set; } = DateTime.Now;

        [Display(Name = "Action At")]
        public DateTime? ActionAt { get; set; }

        [StringLength(500)]
        [Display(Name = "Rejection Remark")]
        public string? RejectionRemark { get; set; }

        [NotMapped]
        public decimal TotalDays => HalfDayPeriod != HalfDayPeriod.None ? 0.5m : (EndDate - StartDate).Days + 1;

        [NotMapped]
        public string DisplayDates
        {
            get
            {
                var format = "dd MMM yyyy";
                if (HalfDayPeriod != HalfDayPeriod.None)
                {
                    var halfStr = HalfDayPeriod == HalfDayPeriod.FirstHalf ? "First Half" : "Second Half";
                    return $"{StartDate.ToString(format)} ({halfStr})";
                }
                if (StartDate.Date == EndDate.Date)
                {
                    return StartDate.ToString(format);
                }
                return $"{StartDate.ToString(format)} to {EndDate.ToString(format)}";
            }
        }
    }
}
