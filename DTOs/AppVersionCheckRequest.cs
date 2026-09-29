using System.ComponentModel.DataAnnotations;

namespace XeniaTenoraBackend.DTOs
{
    public class AppVersionCheckRequest
    {
        [Required(ErrorMessage = "Platform is required (e.g., Android, iOS).")]
        public string Platform { get; set; } = string.Empty;

        [Required(ErrorMessage = "AppVersion is required (e.g., 1.0.0).")]
        public string AppVersion { get; set; } = string.Empty;
    }

    public class AppVersionCheckResponseDto
    {
        public string Platform { get; set; } = string.Empty;
        public string CurrentAppVersion { get; set; } = string.Empty;
        public string LatestVersion { get; set; } = string.Empty;
        public string MinVersion { get; set; } = string.Empty;
        public bool IsUpdateAvailable { get; set; }
        public bool IsForceUpdate { get; set; }
        public string? UpdateUrl { get; set; }
        public string? ReleaseNotes { get; set; }
    }

    public class SaveAppVersionDto
    {
        public int? VersionId { get; set; }

        [Required]
        public string Platform { get; set; } = string.Empty; // "Android" or "iOS"

        [Required]
        public string AppVersion { get; set; } = string.Empty;

        [Required]
        public string MinVersion { get; set; } = string.Empty;

        public bool ForceUpdate { get; set; } = false;

        public string? UpdateUrl { get; set; }

        public string? ReleaseNotes { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
