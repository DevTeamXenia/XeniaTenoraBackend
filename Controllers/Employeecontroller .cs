using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XeniaRentalBackend.Dtos;
using XeniaRentalBackend.Models;
using XeniaRentalBackend.Repositories.EmployeeMaster;
using XeniaRentalBackend.Service.Common;

namespace XeniaRentalBackend.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ApplicationDbContext _context;
        private readonly JwtHelperService _jwtHelperService;

        public EmployeeController(
       IEmployeeRepository employeeRepository,
       ApplicationDbContext context, JwtHelperService jwtHelperService)
        {
            _employeeRepository = employeeRepository;
            _context = context;
            _jwtHelperService = jwtHelperService;
        }


        [HttpGet("company/{companyId}")]
        public async Task<ActionResult<PagedResultDto<XRS_Employee>>> GetByCompany(
            int companyId, string? search = null, int pageNumber = 1, int pageSize = 10)
        {
            var employees = await _employeeRepository.GetEmployeesByCompanyId(
                companyId, search, pageNumber, pageSize);

            if (employees == null)
                return NotFound(new { Status = "Error", Message = "No employees found." });

            return Ok(new { Status = "Success", Data = employees });
        }

     

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EmployeeMasterDto dto)
        {
            if (dto == null)
                return BadRequest(new { Status = "Error", Message = "Invalid employee data." });

            var created = await _employeeRepository.CreateEmployee(dto);
            return Ok(new { Status = "Success", Data = created });
        }



        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var employee = await _employeeRepository.GetEmployeeById(id);

            if (employee == null)
                return NotFound(new { Status = "Error", Message = "Employee not found." });

            return Ok(new { Status = "Success", Data = employee });
        }


        [HttpGet("")]
        public async Task<IActionResult> GetById()
        {
            int id = _jwtHelperService.GetEmployeeId();
            var employee = await _employeeRepository.GetEmployeeById(id);

            if (employee == null)
                return NotFound(new { Status = "Error", Message = "Employee not found." });

            return Ok(new { Status = "Success", Data = employee });
        }




        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] EmployeeMasterDto dto)
        {
            if (dto == null)
                return BadRequest(new { Status = "Error", Message = "Invalid employee data." });

            var updated = await _employeeRepository.UpdateEmployee(id, dto);

            if (!updated)
                return NotFound(new { Status = "Error", Message = "Employee not found." });

            return Ok(new { Status = "Success", Message = "Employee updated successfully." });
        }


        [HttpGet("mobilenumber/{mobilenumber}")]
        public async Task<ActionResult<PagedResultDto<XRS_Employee>>> MobileValidation(
          int companyId, string? mobilenumber, string? search = null, int pageNumber = 1, int pageSize = 10)
        {
            var msg = await _employeeRepository.ValidationByMobileNo(companyId,mobilenumber);

            if (msg == null)
                return NotFound(new { Status = "Error", Message = "No employees found." });

            return Ok(new { Status = "Success", Data = msg });
        }

  
        [HttpGet("service/{propertyId}")]
        public async Task<IActionResult> GetEmployeesByProperty(int propertyId)
        {
            var data = await _employeeRepository.GetEmployeesByPropertyId(propertyId);
            return Ok(new { Status = "Success", Data = data });
        }
    }
}
