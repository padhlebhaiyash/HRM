using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMSystem.Models
{
    public class Designation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int Level { get; set; } // Hierarchy Level: 1 = Admin (highest), 2 = Manager, 3 = Team Lead, 4 = Executive (lowest)

        public int? ParentDesignationId { get; set; }

        [ForeignKey("ParentDesignationId")]
        public virtual Designation? ParentDesignation { get; set; }
    }
}

