using XeniaRentalBackend.Dtos;
using XeniaRentalBackend.Models;
using XeniaTenoraBackend.Dtos;

namespace XeniaRentalBackend.Repositories.ManageMaintenance
{
    public interface IMaintenanceRepository
    {

        Task<List<MaintenanceStatusGroupDto>> GetMaintenance(int companyId, int? tenantId, int? employeeId, string? search);
        Task<MaintenanceResponseDto> CreateMaintenance(MaintenanceDto dto);
        Task<bool> UpdateMaintenance(int maintainceId, int? employeeId, string status);
        Task<MaintenanceDetailsDto> GetMaintenanceDetails(int maintenanceId, int companyId);
        Task<MaintenanceDashboardDto> GetMaintenanceDashboard(int companyId);



 
        Task<PagedResultDto<XRS_MaintenanceCategory>> GetMaintenanceCategoryByCompanyId(int companyId, string? search = null, int pageNumber = 1, int pageSize = 10);
        Task<IEnumerable<XRS_MaintenanceCategory>> GetMaintenanceCategories(int companyId);
        Task<IEnumerable<XRS_MaintenanceCategory>> GetMaintenanceCategoryById(int categoryId);
        Task<XRS_MaintenanceCategory> CreateMaintenanceCategory(MaintenanceCategoryDto dtoCategory);
        Task<bool> UpdateMaintenanceCategory(int id, MaintenanceCategoryDto category);
        Task<bool> DeleteMaintenanceCategory(int id);
    }
}
