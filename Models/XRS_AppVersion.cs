using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XeniaRentalBackend.Models
{
    [Table("XRS_AppVersion")]
    public class XRS_AppVersion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VersionId { get; set; }

        [Required]
        [StringLength(20)]
        public string Platform { get; set; } = string.Empty; // "Android" or "iOS"

        [Required]
        [StringLength(20)]
        public string AppVersion { get; set; } = string.Empty; // Latest version, e.g. "1.0.1"

        [Required]
        [StringLength(20)]
        public string MinVersion { get; set; } = string.Empty; // Minimum required version, e.g. "1.0.0"

        public bool ForceUpdate { get; set; } = false;

        [StringLength(500)]
        public string? UpdateUrl { get; set; }

        [StringLength(1000)]
        public string? ReleaseNotes { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
