using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XeniaRentalBackend.Dtos;
using XeniaRentalBackend.Repositories.ManageMaintenance;
using XeniaRentalBackend.Models;
using XeniaTenoraBackend.Service.Socket;

namespace XeniaRentalBackend.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class MaintenanceController : ControllerBase
    {
        private readonly IMaintenanceRepository _manageMaintenanceRepository;
        private readonly ITenoraUpdateService _tenoraUpdateService;

        public MaintenanceController(
            IMaintenanceRepository manageMaintenanceRepository,
            ITenoraUpdateService tenoraUpdateService)
        {
            _manageMaintenanceRepository = manageMaintenanceRepository;
            _tenoraUpdateService = tenoraUpdateService;
        }

        #region CATEGORY

        [HttpGet("category/all/{companyId}")]
        public async Task<ActionResult<IEnumerable<XRS_MaintenanceCategory>>> GetCategories(int companyId)
        {
            var categories = await _manageMaintenanceRepository.GetMaintenanceCategories(companyId);
            if (categories == null || !categories.Any())
            {
                return NotFound(new { Status = "Error", Message = "No maintenance categories found." });
            }
            return Ok(new { Status = "Success", Data = categories });
        }

        [HttpGet("category/company/{companyId}")]
        public async Task<ActionResult<PagedResultDto<XRS_MaintenanceCategory>>> GetMaintenanceCategoryByCompanyId(int companyId, string? search = null, int pageNumber = 1, int pageSize = 10)
        {
            var categories = await _manageMaintenanceRepository.GetMaintenanceCategoryByCompanyId(companyId, search, pageNumber, pageSize);
            if (categories == null)
            {
                return NotFound(new { Status = "Error", Message = "No maintenance categories found for the given Company ID." });
            }
            return Ok(new { Status = "Success", Data = categories });
        }

        [HttpGet("category/{id}")]
        public async Task<ActionResult<IEnumerable<XRS_MaintenanceCategory>>> GetMaintenanceCategory(int id)
        {
            var category = await _manageMaintenanceRepository.GetMaintenanceCategoryById(id);
            if (category == null || !category.Any())
            {
                return NotFound(new { Status = "Error", Message = "Maintenance category not found." });
            }
            return Ok(new { Status = "Success", Data = category });
        }

        [HttpPost("category")]
        public async Task<IActionResult> CreateMaintenanceCategory([FromBody] MaintenanceCategoryDto category)
        {
            if (category == null)
            {
                return BadRequest(new { Status = "Error", Message = "Invalid maintenance category." });
            }

            var createdCategory = await _manageMaintenanceRepository.CreateMaintenanceCategory(category);
            return Ok(new { Status = "Success", Data = createdCategory });
        }

        [HttpPut("category/{id}")]
        public async Task<IActionResult> UpdateMaintenanceCategory(int id, [FromBody] MaintenanceCategoryDto category)
        {
            if (category == null)
            {
                return BadRequest(new { Status = "Error", Message = "Invalid maintenance category data." });
            }

            var updated = await _manageMaintenanceRepository.UpdateMaintenanceCategory(id, category);
            if (!updated)
            {
                return NotFound(new { Status = "Error", Message = "Maintenance category not found or update failed." });
            }

            return Ok(new { Status = "Success", Message = "Maintenance category updated successfully." });
        }

        [HttpDelete("category/{id}")]
        public async Task<IActionResult> DeleteMaintenanceCategory(int id)
        {
            var deleted = await _manageMaintenanceRepository.DeleteMaintenanceCategory(id);
            if (!deleted)
            {
                return NotFound(new { Status = "Error", Message = "Maintenance category not found or delete failed." });
            }

            return Ok(new { Status = "Success", Message = "Maintenance category deleted successfully." });
        }


        #endregion

        [HttpGet("list/{companyId}")]
        public async Task<IActionResult> GetMaintenance(int companyId, int? tenantId = null, int? employeeId = null, string? search = null)
        {
            var result = await _manageMaintenanceRepository.GetMaintenance(companyId, tenantId, employeeId, search);
            return Ok(new
            {
                Status = "Success",
                Data = result
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateMaintenance([FromBody] MaintenanceDto dto)
        {
            if (dto == null)
                return BadRequest(new { Status = "Error", Message = "Invalid data." });

            var result = await _manageMaintenanceRepository.CreateMaintenance(dto);

            try
            {
                if (result.AssignedEmployeeId.HasValue)
                {
                    await _tenoraUpdateService.SendTenoraUpdate(
                        result.CompanyId,
                        result.TenantId,
                        result.AssignedEmployeeId
                    );
                }

                await _tenoraUpdateService.SendTenoraUpdate(
                    result.CompanyId,
                    result.TenantId,
                    employeeId: null
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine("Socket broadcast error: " + ex.Message);
            }

            return Ok(new
            {
                Status = "Success",
                Message = "Maintenance request created successfully",
                Data = result
            });
        }

        [HttpGet("details/{maintenanceId}/{companyId}")]
        public async Task<IActionResult> GetDetails(int maintenanceId, int companyId)
        {
            var data = await _manageMaintenanceRepository.GetMaintenanceDetails(maintenanceId, companyId);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpPut("update/{maintenanceId}")]
        public async Task<IActionResult> UpdateMaintenance(int maintenanceId, int? employeeId, string status)
        {
            if (string.IsNullOrEmpty(status))
                return BadRequest("Status is required");

            var maintenanceDetails = await _manageMaintenanceRepository.GetMaintenanceDetails(maintenanceId, 0);

            var result = await _manageMaintenanceRepository.UpdateMaintenance(
                maintenanceId,
                employeeId,
                status
            );

            if (!result)
                return NotFound("Maintenance not found or update failed");

            try
            {
                int companyId = maintenanceDetails?.Current?.CompanyId ?? 0;
                int? tenantId = maintenanceDetails?.Current?.TenantId;
                int? targetEmpId = employeeId ?? maintenanceDetails?.Current?.AssignedEmployeeId;

                if (companyId > 0)
                {
                    if (targetEmpId.HasValue)
                    {
                        await _tenoraUpdateService.SendTenoraUpdate(companyId, tenantId, targetEmpId.Value);
                    }
                    await _tenoraUpdateService.SendTenoraUpdate(companyId, tenantId, employeeId: null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Socket broadcast error on update: " + ex.Message);
            }

            return Ok(new
            {
                Message = "Maintenance updated successfully"
            });
        }


        [HttpGet("dashboard/{companyId}")]
        public async Task<IActionResult> GetDashboard(int companyId)
        {
            var result = await _manageMaintenanceRepository.GetMaintenanceDashboard(companyId);
            return Ok(new
            {
                Status = "Success",
                Data = result
            });
        }
    }
}
































































