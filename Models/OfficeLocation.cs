using System.ComponentModel.DataAnnotations;

namespace HRMSystem.Models
{
    public class OfficeLocation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Location Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Address { get; set; }

        [Required]
        [Display(Name = "Latitude")]
        public double Latitude { get; set; }

        [Required]
        [Display(Name = "Longitude")]
        public double Longitude { get; set; }

        [Required]
        [Display(Name = "Allowed Radius (Meters)")]
        public double AllowedRadiusMeters { get; set; } = 300.0;

        [Display(Name = "Active Location")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Enforce Geofence")]
        public bool IsEnforced { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
