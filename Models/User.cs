using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMSystem.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        public int? EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public ICollection<UserRoleMapping>? UserRoles { get; set; } = new List<UserRoleMapping>();

        [Display(Name = "Must Change Password")]
        public bool MustChangePassword { get; set; } = true;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Last Login")]
        public DateTime? LastLoginAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
