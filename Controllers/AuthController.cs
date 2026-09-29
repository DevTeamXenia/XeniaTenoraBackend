using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XeniaRentalBackend.DTOs;
using XeniaRentalBackend.Repositories.Auth;
using XeniaRentalBackend.Service.Common;
using XeniaTenoraBackend.DTOs;


namespace XeniaRentalBackend.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepository;
        private readonly JwtHelperService _jwtHelperService;

        public AuthController(IAuthRepository authRepository, JwtHelperService jwtHelperService)
        {
            _authRepository = authRepository;
            _jwtHelperService = jwtHelperService;
        }

 

        [HttpPost("admin/login")]
        public async Task<IActionResult> AdminLogin([FromBody] LoginRequest request)
        {
            try
            {
                var user = await _authRepository.AuthenticateAdminUser(request);

                if (user == null)
                {
                    return NotFound(new
                    {
                        Status = "Error",
                        Message = "User does not exist."
                    });
                }

                var token = _authRepository.GenerateJwtAdminToken(user);

                return Ok(new
                {
                    Status = "Success",
                    Message = "Login successful.",
                    Token = token
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    Status = "Error",
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Status = "Error",
                    Message = "An unexpected error occurred.",
                    Details = ex.Message
                });
            }
        }
        [HttpPost("employee/login")]
        public async Task<IActionResult> EmployeeLogin([FromBody] EmployeeLoginRequest request)
        {
            try
            {
                var employee = await _authRepository.AuthenticateEmployee(request);

                if (employee == null)
                {
                    return NotFound(new
                    {
                        Status = "Error",
                        Message = "Employee does not exist."
                    });
                }

                var token = _authRepository.GenerateJwtEmployeeToken(employee);

                return Ok(new
                {
                    Status = "Success",
                    Message = "Login successful.",
                    Token = token
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    Status = "Error",
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Status = "Error",
                    Message = "An unexpected error occurred.",
                    Details = ex.Message
                });
            }
        }

        [HttpPost]
        [Route("OTP/login")]
        public async Task<IActionResult> GenerateLoginOTP([FromBody] LoginOTPDTO request)
        {
            return await _authRepository.GenerateLoginOTPAsync(request);
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login(string userName, string password, int companyId, string otp, string? deviceToken)
        {
            try
            {
                var user = await _authRepository.AuthenticateUser(userName, companyId, otp, deviceToken);

                if (user == null)
                {
                    return NotFound(new { Status = "Error", Message = "User does not exist." });
                }

                var token = _authRepository.GenerateJwtCustomerToken(user);
                return Ok(new { Status = "Success", Token = token });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Status = "Error", Message = ex.Message });
            }
        }


        [HttpPost]
        [Route("OTP/forgotPassword")]
        public async Task<IActionResult> GenerateForgotPasswordOTP([FromBody] ForgetPasswordOTPDTO request)
        {
            return await _authRepository.GenerateForgotPasswordOTP(request);
        }


        [HttpPost("forgetPassword")]
        public async Task<IActionResult> ForgetPassword([FromBody] ForegtPasswordDTO request)
        {
            try
            {
                var result = await _authRepository.ResetUserPassword(request);
                if (!result)
                {
                    return Unauthorized(new { Status = "Error", Message = "Password reset faild." });
                }

                return Ok(new { Status = "Success", Message = "Password has been reset successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Status = "Error", Message = ex.Message });
            }
        }


        [HttpPut("disable")]
        public async Task<IActionResult> DisableTenant( [FromQuery] int tenantId)
        {
            var result = await _authRepository.DisableTenantAsync(tenantId);

            if (!result)
                return NotFound(new
                {
                    message = "Tenant not found or already disabled"
                });

            return Ok(new
            {
                message = "Tenant account disabled successfully"
            });
        }

        #region APP VERSION CHECK ENDPOINTS

        [HttpPost("version/check")]
        public async Task<IActionResult> CheckAppVersionPost([FromBody] AppVersionCheckRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { Status = "Error", Message = "Invalid request payload.", Errors = ModelState });
            }

            var result = await _authRepository.CheckAppVersionAsync(request.Platform, request.AppVersion);

            if (result == null)
            {
                return NotFound(new
                {
                    Status = "Error",
                    Message = $"No active version information found for platform '{request.Platform}'."
                });
            }

            return Ok(new
            {
                Status = "Success",
                Data = result
            });
        }

        [HttpGet("version/check")]
        public async Task<IActionResult> CheckAppVersionGet([FromQuery] string platform, [FromQuery] string appVersion)
        {
            if (string.IsNullOrWhiteSpace(platform) || string.IsNullOrWhiteSpace(appVersion))
            {
                return BadRequest(new
                {
                    Status = "Error",
                    Message = "Both 'platform' and 'appVersion' query parameters are required."
                });
            }

            var result = await _authRepository.CheckAppVersionAsync(platform, appVersion);

            if (result == null)
            {
                return NotFound(new
                {
                    Status = "Error",
                    Message = $"No active version information found for platform '{platform}'."
                });
            }

            return Ok(new
            {
                Status = "Success",
                Data = result
            });
        }

        [HttpGet("version/all")]
        public async Task<IActionResult> GetAllAppVersions([FromQuery] string? platform = null)
        {
            var versions = await _authRepository.GetAppVersionsAsync(platform);
            return Ok(new
            {
                Status = "Success",
                Data = versions
            });
        }

        [HttpPost("version/save")]
        public async Task<IActionResult> SaveAppVersion([FromBody] SaveAppVersionDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { Status = "Error", Message = "Invalid payload.", Errors = ModelState });
            }

            var result = await _authRepository.SaveAppVersionAsync(request);

            return Ok(new
            {
                Status = "Success",
                Message = "App version details saved successfully.",
                Data = result
            });
        }

        #endregion

    }
}

