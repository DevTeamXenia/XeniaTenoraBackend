namespace XeniaRentalBackend.Service.Common
{
    public class JwtHelperService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public JwtHelperService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int GetCompanyId()
        {
            var companyIdClaim = _httpContextAccessor.HttpContext?.User.Claims
                .FirstOrDefault(c => c.Type == "CompanyId");

            if (companyIdClaim != null && int.TryParse(companyIdClaim.Value, out int companyId))
            {
                return companyId;
            }

            return 0;
        }

        public int GetUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.Claims
                .FirstOrDefault(c => c.Type == "UserId");

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }

            return 0;
        }


        public int GetCustomerId()
        {
            var customerIdClaim = _httpContextAccessor.HttpContext?.User.Claims
                .FirstOrDefault(c => c.Type == "CustomerId");

            if (customerIdClaim != null && int.TryParse(customerIdClaim.Value, out int customerId))
            {
                return customerId;
            }

            return 0;
        }


        public int GetTenantIdId()
        {
            var customerIdClaim = _httpContextAccessor.HttpContext?.User.Claims
                .FirstOrDefault(c => c.Type == "TenantId");

            if (customerIdClaim != null && int.TryParse(customerIdClaim.Value, out int customerId))
            {
                return customerId;
            }

            return 0;
        }

        public int GetEmployeeId()
        {
            var employeeIdClaim = _httpContextAccessor.HttpContext?.User.Claims
                .FirstOrDefault(c => c.Type == "EmployeeId");

            if (employeeIdClaim != null && int.TryParse(employeeIdClaim.Value, out int employeeId))
            {
                return employeeId;
            }

            return 0;
        }
    }
}
