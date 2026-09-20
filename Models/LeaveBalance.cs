using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMSystem.Models
{
    public class LeaveBalance
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

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Total Allowed")]
        public decimal Allocated { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Used { get; set; }

        [NotMapped]
        public decimal Balance => Allocated - Used;
    }
}
