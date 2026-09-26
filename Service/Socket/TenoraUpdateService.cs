using Microsoft.AspNetCore.SignalR;
using XeniaRentalBackend.Repositories.ManageMaintenance;
using XeniaTenoraBackend.Hubs;

namespace XeniaTenoraBackend.Service.Socket
{
    public class TenoraUpdateService : ITenoraUpdateService
    {
        private readonly IMaintenanceRepository _repository;
        private readonly IHubContext<TenoraHub> _hubContext;

        public TenoraUpdateService(
            IMaintenanceRepository repository,
            IHubContext<TenoraHub> hubContext)
        {
            _repository = repository;
            _hubContext = hubContext;
        }

        public async Task SendTenoraUpdate(
            int companyId,
            int? tenantId = null,
            int? employeeId = null,
            int? pageNumber = null,
            int? pageSize = null,
            string? search = null,
            string? connectionId = null
        )
        {
            try
            {
                Console.WriteLine("📡 SERVICE: Fetching data...");

                var maintenances = await _repository.GetMaintenance(
                    companyId,
                    tenantId,
                    employeeId,
                    search
                );

                var data = new
                {
                    Maintenance = maintenances
                };

                if (employeeId.HasValue)
                {
                    Console.WriteLine("📡 Sending to employee group");

                    await _hubContext.Clients
                        .Group($"company-{companyId}-employee-{employeeId.Value}")
                        .SendAsync("ReceiveMaintenanceUpdate", data);
                }
                else
                {
                    Console.WriteLine("📡 Sending to company group");

                    await _hubContext.Clients
                        .Group($"company-{companyId}")
                        .SendAsync("ReceiveMaintenanceUpdate", data);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 SERVICE ERROR: " + ex.ToString());
                throw;
            }
        }
    }
}