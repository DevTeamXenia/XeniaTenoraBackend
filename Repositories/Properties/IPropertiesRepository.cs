using XeniaRentalBackend.Dtos;
using XeniaRentalBackend.Models;
using XeniaTenoraBackend.Dtos;

namespace XeniaRentalBackend.Repositories.Properties
{
    public interface IPropertiesRepository
    {
        Task<IEnumerable<PropertyListDto>> GetProperties(int companyId, int userId);
        Task<IEnumerable<PropertyListDto>> GetUserMapProperties(int companyId);
        Task<PagedResultDto<PropertyListDto>> GetPropertiesByCompanyId(int companyId, int userId, string? search = null, int pageNumber = 1, int pageSize = 10);
        Task<IEnumerable<PropertyWithUnitsDto>> GetPropertyForApp();
        Task<IEnumerable<PropertyWithUnitsDto>> GetPropertyForEmployeeApp();
        Task<IEnumerable<PropertyListDto>> GetPrpoertiesbyId(int propertyId);
        Task<bool> UpDateProperties(int id, XRS_Properties properties);
        Task<XRS_Properties> CreateProperties(XRS_Properties property);
        Task<bool> DeleteProperty(int id);
    }
}
