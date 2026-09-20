using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMSystem.Models
{
    public class BankDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [Display(Name = "Account Holder Name")]
        public string? AccountHolderName { get; set; }

        [Display(Name = "Account Number")]
        public string? AccountNumber { get; set; }

        [Display(Name = "Bank Name")]
        public string? BankName { get; set; }

        public string? BSB { get; set; }

        [Display(Name = "Tax Payer Id")]
        public string? TaxPayerId { get; set; }

        [Display(Name = "Bank Identifier Code")]
        public string? BankIdentifierCode { get; set; }

        [Display(Name = "Branch Location")]
        public string? BranchLocation { get; set; }
    }
}
