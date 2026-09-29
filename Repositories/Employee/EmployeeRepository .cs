using Microsoft.EntityFrameworkCore;
using XeniaRentalBackend.Dtos;
using XeniaRentalBackend.Models;
using XeniaTenoraBackend.DTOs;
using XeniaTenoraBackend.Models;

namespace XeniaRentalBackend.Repositories.EmployeeMaster
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly ApplicationDbContext _context;

        public EmployeeRepository(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<IEnumerable<XRS_Employee>> GetAllEmployees(int companyId)
        {
            return await _context.Employee
                .Where(e => e.CompanyId == companyId && e.IsActive == true)
                .Select(e => new XRS_Employee
                {
                    EmployeeId = e.EmployeeId,
                    CompanyId = e.CompanyId,
                    EmployeeCode = e.EmployeeCode,
                    Name = e.Name,
                    Department = e.Department,
                    CategoryId = e.CategoryId,
                    WhatAppNumber = e.WhatAppNumber,
                    MobileNumber = e.MobileNumber,
                    IsActive = e.IsActive
       
                })
                .ToListAsync();
        }

     
        public async Task<PagedResultDto<XRS_Employee>> GetEmployeesByCompanyId(
            int companyId, string? search = null, int pageNumber = 1, int pageSize = 10)
        {
            var query = _context.Employee
                .Where(e => e.CompanyId == companyId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e =>
                    e.Name.Contains(search) ||
                    e.EmployeeCode.Contains(search) ||
                    e.Department.Contains(search)
                );
            }

            var totalRecords = await query.CountAsync();

            var items = await (
            from e in _context.Employee
            join c in _context.MaintenanceCategories
            on e.CategoryId equals c.CategoryId
            where e.CompanyId == companyId
            orderby e.Name
            select new XRS_Employee
            {
                EmployeeId = e.EmployeeId,
                CompanyId = e.CompanyId,
                EmployeeCode = e.EmployeeCode,
                Name = e.Name,
                Department = e.Department,
                CategoryId = e.CategoryId,
                CategoryName = c.CategoryName,
                WhatAppNumber = e.WhatAppNumber,
                MobileNumber = e.MobileNumber,
                Password = e.Password,
                IsActive = e.IsActive
            })
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

            return new PagedResultDto<XRS_Employee>
            {
                Data = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords
            };
        }


        public async Task<XRS_Employee> CreateEmployee(EmployeeMasterDto dto)
        {
            var employee = new XRS_Employee
            {
                CompanyId = dto.CompanyId,
                EmployeeCode = dto.EmployeeCode,
                Name = dto.Name,
                Department = dto.Department,
                CategoryId = dto.CategoryId,
                WhatAppNumber = dto.WhatAppNumber, 
                MobileNumber = dto.MobileNumber,
                Password = dto.Password,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _context.Employee.AddAsync(employee);
            await _context.SaveChangesAsync();

            int employeeId = employee.EmployeeId;
            if (dto.EmployeeAreas != null && dto.EmployeeAreas.Any())
            {
                var areaMappings = dto.EmployeeAreas.Select(x => new XRS_EmployeeArea
                {
                    EmployeeId = employeeId,
                    AreaId = x.AreaId,
                    IsPrimary = x.IsPrimary,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }).ToList();

                await _context.EmployeeArea.AddRangeAsync(areaMappings);
                await _context.SaveChangesAsync();
            }

            return employee;
        }



        public async Task<XRS_Employee?> GetEmployeeById(int employeeId)
        {
            return await _context.Employee
                .Where(e => e.EmployeeId == employeeId)
                .Select(e => new XRS_Employee
                {
                    EmployeeId = e.EmployeeId,
                    CompanyId = e.CompanyId,
                    EmployeeCode = e.EmployeeCode,
                    Name = e.Name,
                    Department = e.Department,
                    CategoryId = e.CategoryId,
                    WhatAppNumber = e.WhatAppNumber,
                    MobileNumber = e.MobileNumber,
                    Password = e.Password,
                    IsActive = e.IsActive ,
                    EmployeeAreas = e.EmployeeAreas
                .Select(a => new XRS_EmployeeArea
                {
                    EmployeeAreaId = a.EmployeeAreaId,
                    EmployeeId = a.EmployeeId,
                    AreaId = a.AreaId,
                    IsPrimary = a.IsPrimary,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                })
                .ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateEmployee(int id, EmployeeMasterDto dto)
        {
            var employee = await _context.Employee
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null) return false;

            employee.EmployeeCode = dto.EmployeeCode;
            employee.Name = dto.Name;
            employee.Department = dto.Department;
            employee.CategoryId = dto.CategoryId;
            employee.WhatAppNumber = dto.WhatAppNumber;
            employee.MobileNumber = dto.MobileNumber;
            employee.IsActive = dto.IsActive;
            employee.UpdatedAt = DateTime.Now;

            var existingAreas = await _context.EmployeeArea
                .Where(x => x.EmployeeId == employee.EmployeeId)
                .ToListAsync();

            if (existingAreas.Any())
            {
                _context.EmployeeArea.RemoveRange(existingAreas);
            }
       
            if (dto.EmployeeAreas != null && dto.EmployeeAreas.Any())
            {
                var newAreas = dto.EmployeeAreas.Select(x => new XRS_EmployeeArea
                {
                    EmployeeId = employee.EmployeeId,
                    AreaId = x.AreaId,
                    IsPrimary = x.IsPrimary,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });

                await _context.EmployeeArea.AddRangeAsync(newAreas);
            }

            if (!string.IsNullOrWhiteSpace(dto.Password))
                employee.Password = dto.Password;

            await _context.SaveChangesAsync();
            return true;
        }
   
        public async Task<ResponseDto> ValidationByMobileNo(int companyId, string? mobilenumber)
        {
            bool exists = await _context.Employee
                .AnyAsync(e => e.CompanyId == companyId &&
                               e.MobileNumber == mobilenumber);

            return new ResponseDto
            {
                Success = exists,
                Message = exists
                    ? "Mobile Number already exists."
                    : "Mobile Number does not exist."
            };
        }

        public async Task<IEnumerable<PropertyServiceCategoryDto>> GetEmployeesByPropertyId(int propertyId)
        {
            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.PropID == propertyId);

            if (property == null || property.propertyAreaId == null)
                return new List<PropertyServiceCategoryDto>();

            int areaId = property.propertyAreaId.Value;

            var employees = await (
                from ea in _context.EmployeeArea
                join e in _context.Employee on ea.EmployeeId equals e.EmployeeId
                join c in _context.MaintenanceCategories on e.CategoryId equals c.CategoryId into cc
                from category in cc.DefaultIfEmpty()
                where ea.AreaId == areaId && e.IsActive
                select new
                {
                    e.EmployeeId,
                    e.EmployeeCode,
                    e.Name,
                    e.Department,
                    e.MobileNumber,
                    e.WhatAppNumber,
                    e.IsActive,
                    CategoryId = e.CategoryId,
                    CategoryName = category != null ? category.CategoryName : "General",
                    SLADays = category != null ? category.SLADays : 0,
                    SLAHours = category != null ? category.SLAHours : 0
                }
            ).ToListAsync();

            var result = employees
                .GroupBy(x => new { x.CategoryId, x.CategoryName, x.SLADays, x.SLAHours })
                .Select(g => new PropertyServiceCategoryDto
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    SLADays = g.Key.SLADays,
                    SLAHours = g.Key.SLAHours,
                    Employees = g.Select(e => new EmployeeBasicDto
                    {
                        EmployeeId = e.EmployeeId,
                        EmployeeCode = e.EmployeeCode,
                        Name = e.Name,
                        Department = e.Department,
                        MobileNumber = e.MobileNumber,
                        WhatAppNumber = e.WhatAppNumber,
                        IsActive = e.IsActive
                    }).GroupBy(x => x.EmployeeId).Select(group => group.First()).ToList()
                })
                .ToList();

            return result;
        }
    }
}