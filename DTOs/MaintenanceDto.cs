using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;


namespace XeniaRentalBackend.Dtos
{

  public class MaintenanceDto
{
    [Required]
    public int CompanyId { get; set; }

    [Required]
    public int PropertyId { get; set; }

    [Required]
    public int UnitId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public string Complaint { get; set; } = string.Empty;

    [Required]
    public string PreferredVisitTime { get; set; } = string.Empty;

    [Required]
    public int TenantId { get; set; }

    public List<MaintancePhotoDto> Photos { get; set; } = new();
}
    public class  MaintancePhotoDto
    {
        [Required]
        public string PhotoUrl { get; set; } = string.Empty;
    }


 
}
