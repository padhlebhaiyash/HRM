using System.ComponentModel.DataAnnotations;

namespace HRMSystem.Models
{
    public class Notice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Content { get; set; } = string.Empty;

        public DateTime Date { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;
    }
}
