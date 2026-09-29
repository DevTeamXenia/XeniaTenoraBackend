using XeniaRentalBackend.Dtos;
using XeniaRentalBackend.Models;
using XeniaTenoraBackend.DTOs;

namespace XeniaRentalBackend.Repositories.EmployeeMaster
{
    public interface IEmployeeRepository
    {
        // Listing Page + Search
        Task<PagedResultDto<XRS_Employee>> GetEmployeesByCompanyId(
            int companyId, string? search = null, int pageNumber = 1, int pageSize = 10);

        // Create New Employee
        Task<XRS_Employee> CreateEmployee(EmployeeMasterDto dto);
        // IEmployeeMasterRepository.cs-ൽ Add ചെയ്യുക
        Task<XRS_Employee?> GetEmployeeById(int employeeId);
        Task<bool> UpdateEmployee(int id, EmployeeMasterDto dto);
        Task<ResponseDto> ValidationByMobileNo(int companyId, string? mobileNumber);
        Task<IEnumerable<PropertyServiceCategoryDto>> GetEmployeesByPropertyId(int propertyId);
    }
}