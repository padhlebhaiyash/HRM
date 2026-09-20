using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMSystem.Models
{
    public class PayrollRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Employee")]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [Required]
        public int Month { get; set; }

        [Required]
        public int Year { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Basic Salary")]
        public decimal BasicSalary { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Allowances { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Deductions { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Net Salary")]
        public decimal NetSalary { get; set; }

        public PayrollStatus Status { get; set; } = PayrollStatus.Draft;

        [Display(Name = "Generated At")]
        public DateTime GeneratedAt { get; set; } = DateTime.Now;

        [Display(Name = "Paid At")]
        public DateTime? PaidAt { get; set; }
    }
}
