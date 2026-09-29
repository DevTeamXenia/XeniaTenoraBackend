using System.Collections.Generic;

namespace XeniaTenoraBackend.DTOs
{
    public class PropertyServiceCategoryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int SLADays { get; set; }
        public int SLAHours { get; set; }
        public List<EmployeeBasicDto> Employees { get; set; } = new();
    }

    public class EmployeeBasicDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string WhatAppNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
