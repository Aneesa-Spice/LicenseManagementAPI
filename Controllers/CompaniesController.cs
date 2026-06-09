using LicensingAPI.Data;
using LicensingAPI.Models;
using LicensingAPI.Models.Companies;
using LicensingAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicensingAPI.Controllers
{
    [ApiController]
    [Route("api/companies")]
    [Route("companies")]
    public class CompaniesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CompaniesController> _logger;
        private readonly KeygenService _keygenService;
        public CompaniesController(ApplicationDbContext context, ILogger<CompaniesController> logger, KeygenService keygenService)
        {
            _context = context;
            _logger = logger;
            _keygenService = keygenService;
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse<IEnumerable<CompanyDTO>>>> GetCompanies(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchKey = null)
        {
            var response = new APIResponse<IEnumerable<CompanyDTO>>();
            try
            {
                pageNumber = Math.Max(pageNumber, 1);
                pageSize = Math.Clamp(pageSize, 1, 100);

                var query = _context.Companies.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(searchKey))
                {
                    var searchTerm = searchKey.Trim().ToLower();
                    query = query.Where(c =>
                        c.CompanyName.ToLower().Contains(searchTerm) ||
                        (c.Address != null && c.Address.ToLower().Contains(searchTerm)) ||
                       // (c.ContactEmail != null && c.ContactEmail.ToLower().Contains(searchTerm)) ||
                        (c.ContactPhone != null && c.ContactPhone.ToLower().Contains(searchTerm)));
                      //  ||(c.Status != null && c.Status.ToLower().Contains(searchTerm)));
                }

                var totalCount = await query.CountAsync();
                var companies = await query
                    .OrderBy(c => c.CompanyName)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => MapCompanyToDto(c))
                    .ToListAsync();

                response.IsSuccess = true;
                response.Message = "Companies fetched successfully";
                response.Data = companies;
                response.Pagination = new PaginationMetadata
                {
                    TotalCount = totalCount,
                    PageSize = pageSize,
                    CurrentPage = pageNumber,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching companies");
                response.IsSuccess = false;
                response.Message = "Error fetching companies";
                return StatusCode(500, response);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse<CompanyDTO>>> GetCompany(int id)
        {
            var response = new APIResponse<CompanyDTO>();
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Company not found";
                    return NotFound(response);
                }

                response.IsSuccess = true;
                response.Message = "Company fetched successfully";
                response.Data = MapCompanyToDto(company);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching company {Id}", id);
                response.IsSuccess = false;
                response.Message = "Error fetching company";
                return StatusCode(500, response);
            }
        }

        [HttpPost]
        public async Task<ActionResult<APIResponse<CompanyDTO>>> CreateCompany([FromBody] CreateCompanyRequest request)
        {
            var response = new APIResponse<CompanyDTO>();
            try
            {
                var user = await _context.Users.FindAsync(request.UserId)
               ?? throw new InvalidOperationException($"User {request.UserId} not found.");
                var userEmail = user.Email;
                var company = new Company
                {
                    CompanyName = request.Name,
                    Address = request.Address,
                    ContactEmail = userEmail,
                    ContactPhone = request.ContactPhone,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LicenseQuantity = request.LicenseQuantity,
                    Status = true,
                    UserId = request.UserId,
                    UserEmail = userEmail
                };

                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "Company created successfully";
                response.Data = MapCompanyToDto(company);
                return CreatedAtAction(nameof(GetCompany), new { id = company.CompanyId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company");
                response.IsSuccess = false;
                response.Message = "Error creating company";
                return StatusCode(500, response);
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<APIResponse<CompanyDTO>>> UpdateCompany(int id, [FromBody] UpdateCompanyRequest request)
        {
            var response = new APIResponse<CompanyDTO>();
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Company not found";
                    return NotFound(response);
                }

                var user = await _context.Users.FindAsync(request.UserId)
                    ?? throw new InvalidOperationException($"User {request.UserId} not found.");
                var userEmail = user.Email;

                company.CompanyName = request.Name;
                company.Address = request.Address;
                company.ContactEmail = userEmail;
                company.ContactPhone = request.ContactPhone;
                company.LicenseQuantity = request.LicenseQuantity;
                company.UserId = request.UserId;
                company.UserEmail = userEmail;
                company.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "Company updated successfully";
                response.Data = MapCompanyToDto(company);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company {Id}", id);
                response.IsSuccess = false;
                response.Message = "Error updating company";
                return StatusCode(500, response);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<APIResponse<object>>> DeleteCompany(int id)
        {
            var response = new APIResponse<object>();
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Company not found";
                    return NotFound(response);
                }

                _context.Companies.Remove(company);
                await _context.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "Company deleted successfully";
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company {Id}", id);
                response.IsSuccess = false;
                response.Message = "Error deleting company";
                return StatusCode(500, response);
            }
        }

        [HttpGet("{id}/users")]
        public async Task<ActionResult<APIResponse<IEnumerable<CompanyUserDTO>>>> GetCompanyUsers(int id)
        {
            var response = new APIResponse<IEnumerable<CompanyUserDTO>>();
            try
            {
                var companyExists = await _context.Companies.AnyAsync(c => c.CompanyId == id);
                if (!companyExists)
                {
                    response.IsSuccess = false;
                    response.Message = "Company not found";
                    return NotFound(response);
                }

                var users = await _context.UserLicenses
                    .AsNoTracking()
                    .Where(cu => cu.CompanyId == id)
                    .OrderBy(cu => cu.CreatedAt)
                    .ThenBy(cu => cu.UserEmail)
                    .Select(cu => new CompanyUserDTO
                    {
                        UserLicenseId = cu.UserLicenseId,
                        CompanyId = cu.CompanyId,
                        LicenseId = cu.LicenseId,
                        Email = cu.UserEmail,
                        Status = cu.Status,
                        CreatedAt = cu.CreatedAt
                    })
                    .ToListAsync();

                response.IsSuccess = true;
                response.Message = "Company users fetched successfully";
                response.Data = users;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users for company {Id}", id);
                response.IsSuccess = false;
                response.Message = "Error fetching company users";
                return StatusCode(500, response);
            }
        }

        [HttpPost("{id}/users")]
        public async Task<ActionResult<APIResponse<CompanyUserDTO>>> AddCompanyUser(int id, [FromBody] AddCompanyUserRequest request)
        {
            var response = new APIResponse<CompanyUserDTO>();
            try
            {
                var licenseExists = await _context.Licenses.Where(c => c.CompanyId == id && c.IsValid && !c.IsDeleted).FirstOrDefaultAsync();
                if (licenseExists is null)
                {
                    response.IsSuccess = false;
                    response.Message = "No valid license found, Purchase a license first";
                    return NotFound(response);
                }

                var alreadyAddedUser = await _context.UserLicenses
                    .AnyAsync(cu => cu.CompanyId == id && cu.UserEmail == request.UserEmail);
                if (alreadyAddedUser)
                {
                    response.IsSuccess = false;
                    response.Message = "User is already assigned to this company";
                    return Conflict(response);
                }

                var userLicense = new UserLicense
                {
                    CompanyId = request.CompanyId,
                    UserEmail = request.UserEmail,
                    LicenseId = licenseExists.LicenseId,
                    ProviderLicenseId = licenseExists.ProviderLicenseId,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.UserLicenses.Add(userLicense);
                await _context.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "User added to company successfully";
               // response.Data = userLicense;
                return CreatedAtAction(nameof(GetCompanyUsers), new { id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user {UserEmail} to company {Id}", request.UserEmail, id);
                response.IsSuccess = false;
                response.Message = "Error adding user to company";
                return StatusCode(500, response);
            }
        }

        [HttpDelete("{id}/users/{userId}")]
        public async Task<ActionResult<APIResponse<object>>> RemoveCompanyUser(int id, string user)
        {
            var response = new APIResponse<object>();
            try
            {
                var companyUser = await _context.UserLicenses
                    .FirstOrDefaultAsync(cu => cu.UserLicenseId == id && cu.UserEmail == user);
                if (companyUser is null)
                {
                    response.IsSuccess = false;
                    response.Message = "User not found";
                    return NotFound(response);
                }
                
                companyUser.IsDeleted = true;
                companyUser.Status = "Removed";
                companyUser.UpdatedAt = DateTime.UtcNow;
                _context.UserLicenses.Update(companyUser);
                int result = await _context.SaveChangesAsync();

                if(result > 0)
                {
                    var activation = await _context.Activations.Where(x => x.UserLicenseId == id).FirstAsync();
                    if(activation is not null)
                    {
                        activation.IsDeleted = true;
                        activation.Status = "Revoked";
                        activation.IsValid = false;
                        activation.RevokedAt = DateTime.UtcNow;
                        _context.Activations.Update(activation);
                        _context.SaveChanges();
                        //// Revoked the machine
                        await _keygenService.DeactivateMachineAsync(activation.ProviderMachineId);
                    }
                }

                response.IsSuccess = true;
                response.Message = "User removed from company successfully";
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user {UserId} from company {Id}", user, id);
                response.IsSuccess = false;
                response.Message = "Error removing user from company";
                return StatusCode(500, response);
            }
        }

        [HttpPost("{id}/allocate-license")]
        public async Task<ActionResult<APIResponse<object>>> AllocateLicense(int id, [FromBody] AllocateLicenseRequest request)
        {
            var response = new APIResponse<object>();
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Company not found";
                    return NotFound(response);
                }

                var license = await _context.Licenses.FindAsync(request.LicenseId);
                if (license == null)
                {
                    response.IsSuccess = false;
                    response.Message = "License not found";
                    return NotFound(response);
                }

                if (license.CompanyId != null)
                {
                    response.IsSuccess = false;
                    response.Message = "License is already allocated to a company";
                    return BadRequest(response);
                }

                // Associate License with Company
               // license.CompanyId = id;
                license.UpdatedAt = DateTime.UtcNow;

                // Create UserLicense record (assignment slot) for the company
                var userLicense = new UserLicense
                {
                   // CompanyId = id,
                   // ProductId = license.ProductId,
                  //  PolicyId = license.PolicyId ?? 0,
                    //LicenseKey = license.LicenseKey,
                    //KeygenLicenseId = license.ProviderLicenseId,
                    Status = "unassigned",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

               // _context.UserLicenses.Add(userLicense);
                _context.Licenses.Update(license);
                await _context.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "License allocated to company successfully";
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error allocating license {LicenseId} to company {CompanyId}", request.LicenseId, id);
                response.IsSuccess = false;
                response.Message = "Error allocating license to company: " + ex.Message;
                return StatusCode(500, response);
            }
        }

        public class AllocateLicenseRequest
        {
            public int LicenseId { get; set; }
        }

        private static CompanyDTO MapCompanyToDto(Company company)
        {
            return new CompanyDTO
            {
                CompanyId = company.CompanyId,
                CompanyName = company.CompanyName,
                Address = company.Address,
                ContactEmail = company.ContactEmail,
                ContactPhone = company.ContactPhone,
                LicenseQuantity = company.LicenseQuantity,
                Status = company.Status,
                CreatedAt = company.CreatedAt,
                UpdatedAt = company.UpdatedAt,
                UserId = company.UserId,
                UserEmail = company.UserEmail
            };
        }
    }
}
