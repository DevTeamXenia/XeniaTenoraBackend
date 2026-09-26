using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using XeniaRentalBackend.Dtos;
using XeniaRentalBackend.DTOs;
using XeniaRentalBackend.Models;
using XeniaRentalBackend.Service.Common;

namespace XeniaRentalBackend.Repositories.Tenant
{
    public class TenantRepository : ITenantRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly FtpSettings _ftp;
        private readonly JwtHelperService _jwtHelperService;
        public TenantRepository(ApplicationDbContext context, IOptions<FtpSettings> ftpOptions, JwtHelperService jwtHelperService)
        {
            _context = context;
            _ftp = ftpOptions.Value;
            _jwtHelperService = jwtHelperService;
        }

        public async Task<IEnumerable<XRS_Tenant>> GetTenants(int companyId)
        {
            int userId = _jwtHelperService.GetUserId();

            var unitIds = await _context.UserMapping
                .AsNoTracking()
                .Where(m => m.UserID == userId && m.IsActive)
                .Select(m => m.PropID)
                .ToListAsync();

   
            var query = from t in _context.Tenants.AsNoTracking()
                        where t.companyID == companyId
                              && (
                                  _context.TenantAssignemnts.Any(a => a.tenantID == t.tenantID && unitIds.Contains(a.unitID))
                                  || !_context.TenantAssignemnts.Any(a => a.tenantID == t.tenantID)
                              )
                        select new XRS_Tenant
                        {
                            tenantID = t.tenantID,
                            tenantName = t.tenantName,
                            companyID = t.companyID,
                            phoneNumber = t.phoneNumber,
                            email = t.email,
                            emergencyContactNo = t.emergencyContactNo,
                            concessionper = t.concessionper,
                            note = t.note,
                            address = t.address,
                            isActive = t.isActive
                        };

            return await query.ToListAsync();
        }

        public async Task<PagedResultDto<TenantGetDto>> GetTenantsByCompanyId( int companyId, bool? status = null, string? search = null, int pageNumber = 1, int pageSize = 10)
        {
            int userId = _jwtHelperService.GetUserId();

            var unitIds = await _context.UserMapping
                .AsNoTracking()
                .Where(m => m.UserID == userId && m.IsActive)
                .Select(m => m.PropID)
                .ToListAsync();

            var query = _context.Tenants
                .Include(t => t.TenantDocuments)
                    .ThenInclude(td => td.Documents)
                .Where(t => t.companyID == companyId)
                .AsNoTracking();

      
            query = query.Where(t =>
                _context.TenantAssignemnts.Any(a => a.tenantID == t.tenantID && unitIds.Contains(a.unitID))
                || !_context.TenantAssignemnts.Any(a => a.tenantID == t.tenantID)
            );

            if (status.HasValue)
            {
                query = query.Where(t => t.isActive == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string lowerSearch = search.ToLower();
                query = query.Where(t =>
                    t.tenantName.ToLower().Contains(lowerSearch) ||
                    t.phoneNumber.ToLower().Contains(lowerSearch) ||
                    t.email.ToLower().Contains(lowerSearch)
                );
            }

            var totalRecords = await query.CountAsync();

            var tenants = await query
                .OrderBy(t => t.tenantName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = tenants.Select(t => new TenantGetDto
            {
                TenantID = t.tenantID,
                TenantName = t.tenantName,
                CompanyID = t.companyID,
                Email = t.email,
                PhoneNumber = t.phoneNumber,
                EmergencyContactNo = t.emergencyContactNo,
                Address = t.address,
                Note = t.note,
                ConcessionPer = t.concessionper,
                IsActive = t.isActive,
                Documents = t.TenantDocuments?
                    .Select(td => new TenantDocumentDto
                    {
                        TenantID = td.TenantID,
                        DocTypeId = td.DocTypeId,
                        CompanyID = td.CompanyID,
                        DocumentsNo = td.DocumentsNo,
                        DocumentUrl = td.Documenturl,
                        IsActive = td.isActive,
                        DocumentName = td.Documents?.docName ?? td.DocumentName,
                        IsAlphaNumeric = td.Documents?.isAlphanumeric ?? td.IsAlphaNumeric ?? false,
                        IsMandatory = td.Documents?.isMandatory ?? td.IsMandatory ?? false,
                        IsExpiry = td.Documents?.isExpiry ?? td.IsExpiry ?? false,
                        DocPurpose = td.Documents?.docPurpose ?? td.DocPurpose,
                        ExpiryDate = td.documentExpiryDate
                    })
                    .ToList() ?? new List<TenantDocumentDto>()
            }).ToList();

            return new PagedResultDto<TenantGetDto>
            {
                Data = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords
            };
        }

        public async Task<TenantGetDto> GetTenantWithDocumentsById(int tenantId)
        {
            var tenant = await _context.Tenants      
                .Include(t => t.TenantDocuments)
                    .ThenInclude(td => td.Documents)
                .FirstOrDefaultAsync(t => t.tenantID == tenantId);

            if (tenant == null)
                return new TenantGetDto { Documents = new List<TenantDocumentDto>() };

            return new TenantGetDto
            {
                TenantID = tenant.tenantID,
                TenantName = tenant.tenantName,
                CompanyID = tenant.companyID,
                Email = tenant.email,
                PhoneNumber = tenant.phoneNumber,
                EmergencyContactNo = tenant.emergencyContactNo,
                Address = tenant.address,
                Note = tenant.note,
                ConcessionPer = tenant.concessionper,
                IsActive = tenant.isActive,
                Documents = tenant.TenantDocuments?
                    .Where(td => (td.Documents?.docPurpose ?? td.DocPurpose) == "Tenant")
                    .Select(td => new TenantDocumentDto
                    {
                        TenantID = td.TenantID,
                        DocTypeId = td.DocTypeId,
                        CompanyID = td.CompanyID,
                        DocumentsNo = td.DocumentsNo,
                        DocumentUrl = td.Documenturl,
                        IsActive = td.isActive,
                        DocumentName = td.Documents?.docName ?? td.DocumentName,
                        IsAlphaNumeric = td.Documents?.isAlphanumeric ?? td.IsAlphaNumeric ?? false,
                        IsMandatory = td.Documents?.isMandatory ?? td.IsMandatory ?? false,
                        IsExpiry = td.Documents?.isExpiry ?? td.IsExpiry ?? false,
                        DocPurpose = td.Documents?.docPurpose ?? td.DocPurpose,
                        ExpiryDate =  td.documentExpiryDate
                    })
                    .ToList() ?? new List<TenantDocumentDto>()
            };
        }

        public async Task<TenantProfileDto> GetProfileById()
        {
            int tenantId = _jwtHelperService.GetTenantIdId();

            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.tenantID == tenantId);

            if (tenant == null)
                return new TenantProfileDto();

            return new TenantProfileDto
            {
                TenantID = tenant.tenantID,
                TenantName = tenant.tenantName,
                CompanyID = tenant.companyID,
                Email = tenant.email,
                PhoneNumber = tenant.phoneNumber,
                EmergencyContactNo = tenant.emergencyContactNo,
                Address = tenant.address,
                Note = tenant.note
            };
        }

        public async Task<XRS_Tenant> CreateTenant(TenantCreateDto tenantDto)
        {
            var tenant = new XRS_Tenant
            {
                tenantName = tenantDto.tenantName,        
                companyID = tenantDto.companyID,
                phoneNumber = tenantDto.phoneNumber,
                email = tenantDto.email,
                emergencyContactNo = tenantDto.emergencyContactNo,
                concessionper = tenantDto.concessionper,
                note = tenantDto.note,
                address = tenantDto.address,
                isActive = tenantDto.isActive,
            };

            if (tenantDto.Documents != null && tenantDto.Documents.Any())
            {
                tenant.TenantDocuments = tenantDto.Documents.Select(td => new XRS_TenantDocuments
                {
                    DocTypeId = td.docTypeId,
                    DocumentsNo = td.documentsNo,
                    Documenturl = td.documenturl,
                    isActive = td.isActive,
                    CompanyID = tenantDto.companyID
                }).ToList();
            }

            await _context.Tenants.AddAsync(tenant);
            await _context.SaveChangesAsync();
            return tenant;
        }

        public async Task<bool> UpdateTenant(int tenantId, TenantCreateDto tenantDto)
        {
            var tenant = await _context.Tenants
                .Include(t => t.TenantDocuments)
                .FirstOrDefaultAsync(t => t.tenantID == tenantId);

            if (tenant == null) return false;

            tenant.tenantName = tenantDto.tenantName;
            tenant.companyID = tenantDto.companyID;
            tenant.phoneNumber = tenantDto.phoneNumber;
            tenant.email = tenantDto.email;
            tenant.emergencyContactNo = tenantDto.emergencyContactNo;
            tenant.concessionper = tenantDto.concessionper;
            tenant.note = tenantDto.note;
            tenant.address = tenantDto.address;
            tenant.isActive = tenantDto.isActive;

            if (tenantDto.Documents != null)
            {
                foreach (var tdDto in tenantDto.Documents)
                {
                    var existingDoc = tenant.TenantDocuments.FirstOrDefault(d => d.DocTypeId == tdDto.docTypeId);
                    if (existingDoc != null)
                    {
                        existingDoc.DocumentsNo = tdDto.documentsNo;
                        existingDoc.Documenturl = tdDto.documenturl;
                        existingDoc.isActive = tdDto.isActive;
                    }
                    else
                    {
                        tenant.TenantDocuments.Add(new XRS_TenantDocuments
                        {
                            DocTypeId = tdDto.docTypeId,
                            DocumentsNo = tdDto.documentsNo,
                            Documenturl = tdDto.documenturl,
                            isActive = tdDto.isActive,
                            CompanyID = tenantDto.companyID,
                            TenantID = tenantId
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Dictionary<string, string>> UploadFilesAsync(List<IFormFile> files)
        {
            var uploaded = new Dictionary<string, string>();
            string folderName = "images";
            string uploadFolderPath = $"{_ftp.BaseUrl}/{folderName}";
            try
            {
                var dirRequest = (FtpWebRequest)WebRequest.Create(uploadFolderPath);
                dirRequest.Method = WebRequestMethods.Ftp.MakeDirectory;
                dirRequest.Credentials = new NetworkCredential(_ftp.Username, _ftp.Password);
                using var dirResponse = (FtpWebResponse)await dirRequest.GetResponseAsync();
            }
            catch (WebException ex)
            {
                var response = (FtpWebResponse)ex.Response;
                if (response.StatusCode != FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    throw;
                }
            }

            foreach (var file in files)
            {
                string extension = Path.GetExtension(file.FileName);
                string uniqueFileName = $"{Guid.NewGuid()}{extension}";

                string uploadPath = $"{uploadFolderPath}/{uniqueFileName}";
                var request = (FtpWebRequest)WebRequest.Create(uploadPath);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(_ftp.Username, _ftp.Password);
                request.UseBinary = true;
                request.UsePassive = true;

                using var stream = await request.GetRequestStreamAsync();
                await file.CopyToAsync(stream);
                string publicUrl = $"{_ftp.PublicBaseUrl}/{uniqueFileName}";
                uploaded[file.FileName] = publicUrl;
            }

            return uploaded;
        }

        public async Task<(byte[] FileContent, string ContentType)?> GetImageFromFtpAsync(string fileName)
        {
            string folderName = "images";
            string ftpUrl = $"{_ftp.BaseUrl}/{folderName}/{fileName}";

            try
            {
                var request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.Credentials = new NetworkCredential(_ftp.Username, _ftp.Password);

                using var response = (FtpWebResponse)await request.GetResponseAsync();
                using var stream = response.GetResponseStream();
                using var memory = new MemoryStream();
                await stream.CopyToAsync(memory);

                string contentType = GetContentType(fileName);
                return (memory.ToArray(), contentType);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> DeleteTenant(int id)
        {
            var tenants = await _context.Tenants.FirstOrDefaultAsync(u => u.tenantID == id);
            if (tenants == null) return false;
            tenants.isActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        private static string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }
    }
}
