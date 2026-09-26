using Microsoft.EntityFrameworkCore;
using XeniaRentalBackend.Dtos;
using XeniaRentalBackend.Models;
using XeniaTenoraBackend.Dtos;

namespace XeniaRentalBackend.Repositories.ManageMaintenance
{
    public class MaintenanceRepository : IMaintenanceRepository
    {
        private readonly ApplicationDbContext _context;
      
        public MaintenanceRepository(ApplicationDbContext context)
        {
            _context = context;
          
        }

        public async Task<List<MaintenanceStatusGroupDto>> GetMaintenance(int companyId, int? tenantId, int? employeeId, string? search)
        {
            var now = DateTime.Now;

            var query = from m in _context.ManageMaintenance
                        join p in _context.Properties on m.PropertyId equals p.PropID into pp
                        from property in pp.DefaultIfEmpty()
                        join u in _context.Units on m.UnitId equals u.UnitId into uu
                        from unit in uu.DefaultIfEmpty()
                        join t in _context.Tenants on m.TenantId equals t.tenantID into tt
                        from tenant in tt.DefaultIfEmpty()
                        join c in _context.MaintenanceCategories on m.CategoryId equals c.CategoryId into cc
                        from category in cc.DefaultIfEmpty()
                        where m.CompanyId == companyId && m.IsActive
                        select new MaintenanceResponseDto
                        {
                            MaintenanceId = m.MaintenanceId,
                            CompanyId = m.CompanyId,
                            TenantId = m.TenantId,
                            TenantName = tenant != null ? tenant.tenantName : null,
                            ComplaintNo = m.ComplaintNo,
                            PropertyId = m.PropertyId,
                            PropertyName = property != null ? property.propertyName : null,
                            UnitId = m.UnitId,
                            UnitName = unit != null ? unit.UnitName : null,
                            CategoryId = m.CategoryId,
                            CategoryName = category != null ? category.CategoryName : null,
                            Complaint = m.Complaint,
                            Status = m.Status,
                            IsOverdue = category != null &&
                                        m.CreatedAt.AddDays(category.SLADays) < now &&
                                        (m.Status == "Pending" || m.Status == "InProgress"),
                            AssignedEmployeeId = m.AssignedEmployeeId,
                            PreferredVisitTime = m.PreferredVisitTime,
                            IsActive = m.IsActive,
                            CreatedAt = m.CreatedAt,
                            UpdatedAt = m.UpdatedAt,
                            Photos = m.Photos.ToList()
                        };

            if (tenantId.HasValue)
                query = query.Where(m => m.TenantId == tenantId.Value);

            if (employeeId.HasValue && employeeId.Value > 0)
            {
                var employee = await _context.Employee
                    .FirstOrDefaultAsync(e => e.EmployeeId == employeeId.Value);

                if (employee != null && string.Equals(employee.Department, "Administrator", StringComparison.OrdinalIgnoreCase))
                {
                    var employeeAreaIds = await _context.EmployeeArea
                        .Where(ea => ea.EmployeeId == employeeId.Value)
                        .Select(ea => ea.AreaId)
                        .ToListAsync();

                    var propertyIds = await _context.Properties
                        .Where(p => p.propertyAreaId != null && employeeAreaIds.Contains(p.propertyAreaId.Value))
                        .Select(p => p.PropID)
                        .ToListAsync();

                    query = query.Where(m => propertyIds.Contains(m.PropertyId));
                }
                else
                {
                    query = query.Where(m => m.AssignedEmployeeId == employeeId.Value);
                }
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(m =>
                    (m.ComplaintNo ?? "").Contains(search) ||
                    (m.PropertyName ?? "").Contains(search) ||
                    (m.UnitName ?? "").Contains(search) ||
                    (m.CategoryName ?? "").Contains(search) ||
                    (m.Complaint ?? "").Contains(search) ||
                    (m.Status ?? "").Contains(search)
                );
            }

            var list = await query
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

           
            var result = list
                .GroupBy(x => x.Status)
                .Select(g => new MaintenanceStatusGroupDto
                {
                    Status = g.Key,
                    Count = g.Count(),
                    Data = g.ToList()
                })
                .OrderBy(g => g.Status) 
                .ToList();

            return result;
        }

        public async Task<MaintenanceDetailsDto> GetMaintenanceDetails(int maintenanceId, int companyId)
        {
            var baseQuery = from m in _context.ManageMaintenance
                            join p in _context.Properties on m.PropertyId equals p.PropID into pp
                            from property in pp.DefaultIfEmpty()
                            join u in _context.Units on m.UnitId equals u.UnitId into uu
                            from unit in uu.DefaultIfEmpty()
                            join t in _context.Tenants on m.TenantId equals t.tenantID into tt
                            from tenant in tt.DefaultIfEmpty()
                            join c in _context.MaintenanceCategories on m.CategoryId equals c.CategoryId into cc
                            from category in cc.DefaultIfEmpty()
                            where (companyId == 0 || m.CompanyId == companyId) && m.IsActive
                            select new MaintenanceResponseDto
                            {
                                MaintenanceId = m.MaintenanceId,
                                CompanyId = m.CompanyId,
                                TenantId = m.TenantId,
                                TenantName = tenant != null ? tenant.tenantName : null,
                                ComplaintNo = m.ComplaintNo,
                                PropertyId = m.PropertyId,
                                PropertyName = property != null ? property.propertyName : null,
                                UnitId = m.UnitId,
                                UnitName = unit != null ? unit.UnitName : null,
                                CategoryId = m.CategoryId,
                                CategoryName = category != null ? category.CategoryName : null,
                                Complaint = m.Complaint,
                                PreferredVisitTime = m.PreferredVisitTime,
                                Status = m.Status,
                                IsOverdue = category != null &&
                                            m.CreatedAt.AddDays(category.SLADays) < DateTime.Now &&
                                            (m.Status == "Pending" || m.Status == "InProgress"),
                                AssignedEmployeeId = m.AssignedEmployeeId,
                                IsActive = m.IsActive,
                                CreatedAt = m.CreatedAt,
                                UpdatedAt = m.UpdatedAt,
                                Photos = m.Photos.ToList()
                            };

            var current = await baseQuery
                .FirstOrDefaultAsync(x => x.MaintenanceId == maintenanceId);

            if (current == null)
                return null;

        
            var history = await baseQuery
                .Where(x => x.UnitId == current.UnitId && x.MaintenanceId != maintenanceId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return new MaintenanceDetailsDto
            {
                Current = current,
                History = history
            };
        }

        public async Task<MaintenanceResponseDto> CreateMaintenance(MaintenanceDto dto)
        {
            var lastComplaint = await _context.ManageMaintenance
                .OrderByDescending(m => m.MaintenanceId)
                .FirstOrDefaultAsync();

            int nextNo = 500;

            if (lastComplaint != null && !string.IsNullOrEmpty(lastComplaint.ComplaintNo))
            {
                var numberPart = lastComplaint.ComplaintNo.Replace("CMP", "");
                if (int.TryParse(numberPart, out int last))
                {
                    nextNo = last + 1;
                }
            }

            var maintenance = new XRS_Maintenance
            {
                CompanyId = dto.CompanyId,
                TenantId = dto.TenantId,
                ComplaintNo = $"CMP{nextNo}",
                PropertyId = dto.PropertyId,
                UnitId = dto.UnitId,
                CategoryId = dto.CategoryId,
                Complaint = dto.Complaint,
                PreferredVisitTime = dto.PreferredVisitTime,
                Status = "Pending",
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _context.ManageMaintenance.AddAsync(maintenance);
            await _context.SaveChangesAsync();

          
            if (dto.Photos != null && dto.Photos.Any())
            {
                foreach (var photo in dto.Photos)
                {
                    var maintenancePhoto = new XRS_MaintenancePhotos
                    {
                        MaintenanceId = maintenance.MaintenanceId,
                        PhotoUrl = photo.PhotoUrl, 
                        CreatedAt = DateTime.Now
                    };

                    await _context.MaintenancePhotos.AddAsync(maintenancePhoto);
                }

                await _context.SaveChangesAsync();
            }

            var property = await _context.Properties.FindAsync(dto.PropertyId);
            var unit = await _context.Units.FindAsync(dto.UnitId);
            var tenant = await _context.Tenants.FindAsync(dto.TenantId);
            var category = await _context.MaintenanceCategories.FindAsync(dto.CategoryId);

            return new MaintenanceResponseDto
            {
                MaintenanceId = maintenance.MaintenanceId,
                CompanyId = maintenance.CompanyId,
                TenantId = maintenance.TenantId,
                TenantName = tenant?.tenantName,
                ComplaintNo = maintenance.ComplaintNo,
                PropertyId = maintenance.PropertyId,
                PropertyName = property?.propertyName,
                UnitId = maintenance.UnitId,
                UnitName = unit?.UnitName,
                CategoryId = maintenance.CategoryId,
                CategoryName = category?.CategoryName,
                Complaint = maintenance.Complaint,
                PreferredVisitTime = maintenance.PreferredVisitTime,
                Status = maintenance.Status,
                IsOverdue = category != null &&
                            category.SLADays > 0 &&
                            maintenance.CreatedAt.AddDays(category.SLADays) < DateTime.Now &&
                            (maintenance.Status == "Pending" || maintenance.Status == "InProgress"),
                AssignedEmployeeId = maintenance.AssignedEmployeeId,
                IsActive = maintenance.IsActive,
                CreatedAt = maintenance.CreatedAt,
                UpdatedAt = maintenance.UpdatedAt,
                Photos = await _context.MaintenancePhotos.Where(p => p.MaintenanceId == maintenance.MaintenanceId).ToListAsync()
            };
        }

        public async Task<bool> UpdateMaintenance(int maintainceId, int? employeeId, string status)
        {
            var maintenance = await _context.ManageMaintenance
                .FirstOrDefaultAsync(m => m.MaintenanceId == maintainceId);

            if (maintenance == null) return false;

            maintenance.Status = status;
            maintenance.UpdatedAt = DateTime.Now;

            if (employeeId != null)
                maintenance.AssignedEmployeeId = employeeId;

            await _context.SaveChangesAsync();
            return true;
        }
    
        public async Task<IEnumerable<XRS_MaintenanceCategory>> GetMaintenanceCategories(int companyId)
        {
            return await _context.MaintenanceCategories
                .Where(u => u.CompanyId == companyId && u.IsActive == true)
                .Select(u => new XRS_MaintenanceCategory
                {
                    CategoryId = u.CategoryId,
                    CompanyId = u.CompanyId,
                    CategoryName = u.CategoryName,
                    SLADays = u.SLADays,
                    SLAHours = u.SLAHours,
                    IsActive = u.IsActive
                }).ToListAsync();
        }

        public async Task<PagedResultDto<XRS_MaintenanceCategory>> GetMaintenanceCategoryByCompanyId(int companyId, string? search = null, int pageNumber = 1, int pageSize = 10)
        {
            var query = _context.MaintenanceCategories.AsQueryable();
            query = query.Where(u => u.CompanyId == companyId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.CategoryName.Contains(search));
            }

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderBy(u => u.CategoryName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new XRS_MaintenanceCategory
                {
                    CategoryId = u.CategoryId,
                    CompanyId = u.CompanyId,
                    CategoryName = u.CategoryName,
                    SLADays = u.SLADays,
                    SLAHours = u.SLAHours,
                    IsActive = u.IsActive
                }).ToListAsync();

            return new PagedResultDto<XRS_MaintenanceCategory>
            {
                Data = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords
            };
        }

        public async Task<IEnumerable<XRS_MaintenanceCategory>> GetMaintenanceCategoryById(int categoryId)
        {
            return await _context.MaintenanceCategories
                .Where(u => u.CategoryId == categoryId)
                .Select(u => new XRS_MaintenanceCategory
                {
                    CategoryId = u.CategoryId,
                    CompanyId = u.CompanyId,
                    CategoryName = u.CategoryName,
                    SLADays = u.SLADays,
                    SLAHours = u.SLAHours,
                    IsActive = u.IsActive
                }).ToListAsync();
        }

        public async Task<XRS_MaintenanceCategory> CreateMaintenanceCategory(MaintenanceCategoryDto dtoCategory)
        {
            var category = new XRS_MaintenanceCategory
            {
                CategoryName = dtoCategory.CategoryName,
                CompanyId = dtoCategory.CompanyId,
                SLADays = dtoCategory.SLADays,
                SLAHours = dtoCategory.SLAHours,
                IsActive = dtoCategory.IsActive
            };
            await _context.MaintenanceCategories.AddAsync(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<bool> UpdateMaintenanceCategory(int id, MaintenanceCategoryDto category)
        {
            var updateCategory = await _context.MaintenanceCategories.FirstOrDefaultAsync(u => u.CategoryId == id);
            if (updateCategory == null) return false;

            updateCategory.CategoryName = category.CategoryName;
            updateCategory.CompanyId = category.CompanyId;
            updateCategory.SLADays = category.SLADays;
            updateCategory.SLAHours = category.SLAHours;
            updateCategory.IsActive = category.IsActive;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteMaintenanceCategory(int id)
        {
            var category = await _context.MaintenanceCategories.FirstOrDefaultAsync(u => u.CategoryId == id);
            if (category == null) return false;

            category.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<MaintenanceDashboardDto> GetMaintenanceDashboard(int companyId)
        {
            var now = DateTime.Now;

            var maintenance = await _context.ManageMaintenance
                .Where(m => m.CompanyId == companyId && m.IsActive)
                .ToListAsync();

            var categories = await _context.MaintenanceCategories
                .Where(c => c.CompanyId == companyId && c.IsActive)
                .ToListAsync();

            var categoryDict = categories.ToDictionary(c => c.CategoryId, c => c.SLADays);


            bool IsOverdue(XRS_Maintenance m)
            {
                if (m.Status != "Pending" && m.Status != "InProgress")
                    return false;

                if (!categoryDict.TryGetValue(m.CategoryId, out int slaDays))
                    return false;

                if (slaDays <= 0)
                    return false;

                var dueDate = m.CreatedAt.AddDays(slaDays);

                return dueDate < now;
            }


            var overdueItems = maintenance.Where(IsOverdue).ToList();

            var pendingItems = maintenance
                .Where(m => m.Status == "Pending" && !IsOverdue(m))
                .ToList();

            var inProgressItems = maintenance
                .Where(m => m.Status == "InProgress" && !IsOverdue(m))
                .ToList();

            var closedItems = maintenance
                .Where(m => m.Status == "Closed")
                .ToList();

            var dashboard = new MaintenanceDashboardDto
            {
                NewComplaints = pendingItems.Count,
                InProgress = inProgressItems.Count,
                Closed = closedItems.Count,
                Overdue = overdueItems.Count
            };

            var propertyStats = (from m in maintenance
                                 join p in _context.Properties
                                 on m.PropertyId equals p.PropID
                                 select new { m, p })
                                .ToList()
                                .GroupBy(x => new { x.p.PropID, x.p.propertyName })
                                .Select(g => new PropertyComplaintStatsDto
                                {
                                    PropertyName = g.Key.propertyName,
                                    Complaints = g.Count(),
                                    Solved = g.Count(x => x.m.Status == "Closed")
                                })
                                .OrderByDescending(x => x.Complaints)
                                .ToList();

            dashboard.PropertyStats = propertyStats;

            return dashboard;
        }
    }
}

