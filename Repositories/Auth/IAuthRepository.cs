
using Microsoft.AspNetCore.Mvc;
using XeniaRentalBackend.DTOs;
using XeniaRentalBackend.Models;
using XeniaTenoraBackend.DTOs;

namespace XeniaRentalBackend.Repositories.Auth
{
    public interface IAuthRepository
    {
        Task<XRS_Users?> AuthenticateAdminUser(LoginRequest request);
        
        Task<IActionResult> GenerateLoginOTPAsync(LoginOTPDTO request);
        Task<XRS_Tenant?> AuthenticateUser(string username, int companyId, string otp, string? deviceToken);

        Task<IActionResult> GenerateForgotPasswordOTP(ForgetPasswordOTPDTO request);

         Task<bool> ResetUserPassword(ForegtPasswordDTO request);

        string GenerateJwtAdminToken(XRS_Users user);

        string GenerateJwtCustomerToken(XRS_Tenant user);


        Task<bool> DisableTenantAsync(int tenantId);
        
        Task<XRS_Employee?> AuthenticateEmployee(EmployeeLoginRequest request);
        string GenerateJwtEmployeeToken(XRS_Employee employee);

        Task<AppVersionCheckResponseDto?> CheckAppVersionAsync(string platform, string currentVersion);
        Task<List<XRS_AppVersion>> GetAppVersionsAsync(string? platform = null);
        Task<XRS_AppVersion> SaveAppVersionAsync(SaveAppVersionDto request);
    }
}
