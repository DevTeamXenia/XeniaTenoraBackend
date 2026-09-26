using Microsoft.EntityFrameworkCore;
using XeniaRentalBackend.Dtos;
using XeniaRentalBackend.Models;
using XeniaRentalBackend.Service.Common;
using XeniaTenoraBackend.Dtos;


namespace XeniaRentalBackend.Repositories.Properties
{
    public class PropertiesRepository : IPropertiesRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtHelperService _jwtHelperService;
        public PropertiesRepository(ApplicationDbContext context, JwtHelperService jwtHelperService)
        {
            _context = context;
            _jwtHelperService = jwtHelperService;

        }

        public async Task<IEnumerable<PropertyListDto>> GetProperties(int companyId, int userId)
        {
            List<int>? userPropertyIds = null;
           
            userPropertyIds = await _context.UserMapping
                .Where(m => m.UserID == userId && m.IsActive)
                .Select(m => m.PropID)
                .ToListAsync();
            

            return await (
                from p in _context.Properties
                join a in _context.Areas
                    on p.propertyAreaId equals a.AreaId into areaGroup
                from area in areaGroup.DefaultIfEmpty()
                where p.CompanyId == companyId &&
                      (userPropertyIds == null || userPropertyIds.Contains(p.PropID)) 
                select new PropertyListDto
                {
                    PropID = p.PropID,
                    propertyName = p.propertyName,
                    propertyType = p.propertyType,
                    propertyPrefix = p.propertyPrefix,
                    propertyAreaId = p.propertyAreaId,
                    AreaName = area != null ? area.AreaName : null,
                    IsActive = p.IsActive,
                    CompanyId = p.CompanyId
                }
            ).ToListAsync();
        }

        public async Task<IEnumerable<PropertyListDto>> GetUserMapProperties(int companyId)
        {
       
            return await (
                from p in _context.Properties
                join a in _context.Areas
                    on p.propertyAreaId equals a.AreaId into areaGroup
                from area in areaGroup.DefaultIfEmpty()
                where p.CompanyId == companyId
                select new PropertyListDto
                {
                    PropID = p.PropID,
                    propertyName = p.propertyName,
                    propertyType = p.propertyType,
                    propertyPrefix = p.propertyPrefix,
                    propertyAreaId = p.propertyAreaId,
                    AreaName = area != null ? area.AreaName : null,
                    IsActive = p.IsActive,
                    CompanyId = p.CompanyId
                }
            ).ToListAsync();
        }

        public async Task<PagedResultDto<PropertyListDto>> GetPropertiesByCompanyId(int companyId, int userId, string? search = null, int pageNumber = 1, int pageSize = 10)
        {
            List<int>? userPropertyIds = null;
           
            userPropertyIds = await _context.UserMapping
                .Where(m => m.UserID == userId && m.IsActive)
                .Select(m => m.PropID)
                .ToListAsync();
            

            var query =
                from p in _context.Properties
                join a in _context.Areas
                    on p.propertyAreaId equals a.AreaId into areaGroup
                from area in areaGroup.DefaultIfEmpty()
                where p.CompanyId == companyId &&
                      (userPropertyIds == null || userPropertyIds.Contains(p.PropID))  
                select new PropertyListDto
                {
                    PropID = p.PropID,
                    propertyName = p.propertyName,
                    propertyType = p.propertyType,
                    propertyPrefix = p.propertyPrefix,
                    propertyAreaId = p.propertyAreaId,
                    AreaName = area != null ? area.AreaName : null,
                    IsActive = p.IsActive,
                    CompanyId = p.CompanyId
                };

            if (!string.IsNullOrWhiteSpace(search))
            {
                string lowerSearch = search.ToLower();

                query = query.Where(u =>
                    u.propertyName.ToLower().Contains(lowerSearch) ||
                    u.propertyType.ToLower().Contains(lowerSearch) ||
                    (u.AreaName != null &&
                     u.AreaName.ToLower().Contains(lowerSearch)));
            }

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderBy(u => u.propertyName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResultDto<PropertyListDto>
            {
                Data = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords
            };
        }

        public async Task<List<XRS_Properties>> GetPropertiesForApp()
        {
            int customerId = _jwtHelperService.GetCustomerId();

            if (customerId == 0)
                return new List<XRS_Properties>();

            var propertyIds = await _context.UserMapping
                .Where(x => x.UserID == customerId && x.IsActive == true)
                .Select(x => x.PropID)
                .Distinct()
                .ToListAsync();

            if (propertyIds.Count == 0)
                return new List<XRS_Properties>();

            var properties = await _context.Properties
                .Where(p => propertyIds.Contains(p.PropID))
                .Select(p => new XRS_Properties
                {
                    PropID = p.PropID,
                    propertyName = p.propertyName,
                    propertyType = p.propertyType,
                    propertyPrefix = p.propertyPrefix,
                    IsActive = p.IsActive,
                    CompanyId = p.CompanyId
                })
                .ToListAsync();

            return properties;
        }

        public async Task<IEnumerable<PropertyListDto>> GetPrpoertiesbyId(int propertyId)
        {
            var result =
                from p in _context.Properties
                join a in _context.Areas
                    on p.propertyAreaId equals a.AreaId into areaGroup
                from area in areaGroup.DefaultIfEmpty()
                where p.PropID == propertyId
                select new PropertyListDto
                {
                    PropID = p.PropID,
                    propertyName = p.propertyName,
                    propertyType = p.propertyType,
                    propertyPrefix = p.propertyPrefix,
                    propertyAreaId = p.propertyAreaId,
                    AreaName = area != null
                        ? area.AreaName
                        : null,
                    IsActive = p.IsActive,
                    CompanyId = p.CompanyId
                };

            return await result.ToListAsync();
        }

        public async Task<XRS_Properties> CreateProperties(XRS_Properties dtoProperties)
        {
            bool exists = await _context.Properties.AnyAsync(p =>
                p.CompanyId == dtoProperties.CompanyId &&
                p.propertyName.ToLower() == dtoProperties.propertyName.ToLower() &&
                p.propertyPrefix.ToLower() == dtoProperties.propertyPrefix.ToLower()
            );

            if (exists)
            {
                throw new Exception("Property with same name and prefix already exists.");
            }

            var properties = new XRS_Properties
            {
                propertyName = dtoProperties.propertyName,
                propertyType = dtoProperties.propertyType,
                propertyPrefix = dtoProperties.propertyPrefix,
                propertyAreaId = dtoProperties.propertyAreaId,
                CompanyId = dtoProperties.CompanyId,
                IsActive = dtoProperties.IsActive
            };

            await _context.Properties.AddAsync(properties);
            await _context.SaveChangesAsync();

            return properties;
        }

        public async Task<IEnumerable<PropertyWithUnitsDto>> GetPropertyForApp()
        {
            int tenantId = _jwtHelperService.GetTenantIdId();


            var assignedUnitIds = await _context.TenantAssignemnts
                .Where(t => t.tenantID == tenantId && t.isActive == true)
                .Select(t => t.unitID)
                .ToListAsync();

            if (!assignedUnitIds.Any())
                return new List<PropertyWithUnitsDto>();

            var result = await (
                from p in _context.Properties
                join u in _context.Units on p.PropID equals u.PropID
                where assignedUnitIds.Contains(u.UnitId)
                group u by new { p.PropID, p.propertyName } into g
                select new PropertyWithUnitsDto
                {
                    PropID = g.Key.PropID,
                    PropertyName = g.Key.propertyName,
                    Units = g.Select(u => new UnitPropertyDto
                    {
                        UnitID = u.UnitId,
                        UnitName = u.UnitName
                    }).ToList()
                }
            ).ToListAsync();

            return result;
        }

        public async Task<IEnumerable<PropertyWithUnitsDto>> GetPropertyForEmployeeApp()
        {
            int employeeId = _jwtHelperService.GetEmployeeId();

            if (employeeId == 0)
                return new List<PropertyWithUnitsDto>();

            var employeeAreaIds = await _context.EmployeeArea
                .Where(ea => ea.EmployeeId == employeeId)
                .Select(ea => ea.AreaId)
                .ToListAsync();

            if (!employeeAreaIds.Any())
                return new List<PropertyWithUnitsDto>();

            var result = await (
                from p in _context.Properties
                join u in _context.Units on p.PropID equals u.PropID
                where p.propertyAreaId != null && employeeAreaIds.Contains(p.propertyAreaId.Value)
                group u by new { p.PropID, p.propertyName } into g
                select new PropertyWithUnitsDto
                {
                    PropID = g.Key.PropID,
                    PropertyName = g.Key.propertyName,
                    Units = g.Select(u => new UnitPropertyDto
                    {
                        UnitID = u.UnitId,
                        UnitName = u.UnitName
                    }).ToList()
                }
            ).ToListAsync();

            return result;
        }

        public async Task<bool> UpDateProperties(int id, XRS_Properties properties)
        {
            var updateProperties = await _context.Properties.FirstOrDefaultAsync(u => u.PropID == id);
            if (updateProperties == null) return false;

            updateProperties.propertyName = properties.propertyName;
            updateProperties.propertyType = properties.propertyType;
            updateProperties.propertyPrefix = properties.propertyPrefix;    
            updateProperties.propertyAreaId = properties.propertyAreaId;    
            updateProperties.CompanyId = properties.CompanyId;
            updateProperties.IsActive = properties.IsActive;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteProperty(int id)
        {
            var bedspacesettings = await _context.Properties.FirstOrDefaultAsync(u => u.PropID == id);
            if (bedspacesettings == null) return false;
            bedspacesettings.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

    }
}
