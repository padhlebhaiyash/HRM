using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMSystem.Models
{
    public class Employee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Employee Code")]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(15)]
        public string? Phone { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Father's Name")]
        [StringLength(100)]
        public string? FathersName { get; set; }

        public Gender Gender { get; set; }

        [StringLength(500)]
        [Display(Name = "Local Address")]
        public string? LocalAddress { get; set; }

        [StringLength(500)]
        [Display(Name = "Permanent Address")]
        public string? PermanentAddress { get; set; }

        [StringLength(255)]
        [Display(Name = "Photo Path")]
        public string? PhotoPath { get; set; }

        [Required]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public Department? Department { get; set; }

        [Required(ErrorMessage = "Designation is required.")]
        [Display(Name = "Designation")]
        public int DesignationId { get; set; }

        [ForeignKey("DesignationId")]
        public Designation? Designation { get; set; }

        [Required]
        [Display(Name = "Date of Joining")]
        [DataType(DataType.Date)]
        public DateTime DateOfJoining { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Salary")]
        public decimal Salary { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        [NotMapped]
        public string FullNameWithDesignation => Designation != null ? $"{FirstName} {LastName} ({Designation.Name})" : $"{FirstName} {LastName}";

        // Navigation
        public User? User { get; set; }
        public ICollection<LeaveRequest>? LeaveRequests { get; set; }
        public ICollection<LeaveBalance>? LeaveBalances { get; set; }
        public BankDetail? BankDetail { get; set; }
        public ICollection<PayrollRecord>? PayrollRecords { get; set; }

        [Display(Name = "Reporting Manager")]
        public int? ManagerId { get; set; }
        
        [ForeignKey("ManagerId")]
        public Employee? Manager { get; set; }
        
        public ICollection<Employee>? Subordinates { get; set; }
    }
}
