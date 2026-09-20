using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMSystem.Models
{
    public class ProfileChangeRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [StringLength(500)]
        [Display(Name = "Local Address")]
        public string? LocalAddress { get; set; }

        [StringLength(500)]
        [Display(Name = "Permanent Address")]
        public string? PermanentAddress { get; set; }

        [StringLength(255)]
        [Display(Name = "Photo Path")]
        public string? PhotoPath { get; set; }

        [StringLength(100)]
        [Display(Name = "Father's Name")]
        public string? FathersName { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Gender")]
        public Gender? Gender { get; set; }

        [StringLength(15)]
        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        public bool IsPending { get; set; } = true;
        public bool IsApproved { get; set; } = false;

        public DateTime RequestedAt { get; set; } = DateTime.Now;
        public DateTime? ReviewedAt { get; set; }
    }
}
