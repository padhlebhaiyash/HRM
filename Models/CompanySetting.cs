using System.ComponentModel.DataAnnotations;

namespace HRMSystem.Models
{
    public class CompanySetting
    {
        [Key]
        public int Id { get; set; }

        [StringLength(100)]
        [Display(Name = "Company Name / Logo Text")]
        public string? CompanyName { get; set; }

        [StringLength(255)]
        [Display(Name = "Logo Image Path")]
        public string? LogoImagePath { get; set; }

        [StringLength(255)]
        [Display(Name = "Portal Base URL")]
        public string? AppBaseUrl { get; set; }

        [StringLength(150)]
        [Display(Name = "Sender From Email")]
        public string? FromEmail { get; set; }

        [StringLength(100)]
        [Display(Name = "Sender From Name")]
        public string? FromName { get; set; }
    }
}
