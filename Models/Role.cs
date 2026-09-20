using System.ComponentModel.DataAnnotations;

namespace HRMSystem.Models
{
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public ICollection<UserRoleMapping>? UserRoles { get; set; }
    }
}
