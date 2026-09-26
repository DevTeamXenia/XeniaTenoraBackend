using System;
using System.Collections.Generic;
using XeniaRentalBackend.Models;

namespace XeniaRentalBackend.Dtos
{
    public class MaintenanceResponseDto
    {
        public int MaintenanceId { get; set; }
        public int CompanyId { get; set; }
        
        public int? TenantId { get; set; }
        public string? TenantName { get; set; }
        
        public string ComplaintNo { get; set; } = string.Empty;
        
        public int PropertyId { get; set; }
        public string? PropertyName { get; set; }
        
        public int UnitId { get; set; }
        public string? UnitName { get; set; }
        
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        
        public string Complaint { get; set; } = string.Empty;
        public string PreferredVisitTime { get; set; } = string.Empty;
        
        public string Status { get; set; } = "Pending";
        public bool IsOverdue { get; set; }
        public int? AssignedEmployeeId { get; set; }
        
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<XRS_MaintenancePhotos> Photos { get; set; } = new();
    }
}
