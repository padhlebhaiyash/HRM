using System;
using System.ComponentModel.DataAnnotations;

namespace HRMSystem.Models
{
    public class Holiday
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [StringLength(50)]
        public string Color { get; set; } = "#fffde7"; // default light yellow background color
    }
}
