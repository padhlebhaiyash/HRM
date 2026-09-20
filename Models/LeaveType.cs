using System.ComponentModel.DataAnnotations;

namespace HRMSystem.Models
{
    public class LeaveType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Code { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
