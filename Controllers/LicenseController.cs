using LicensingAPI.Data;
using LicensingAPI.Models;
using LicensingAPI.Models.Licenses;
using LicensingAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace LicensingAPI.Controllers
{
    [ApiController]
    [Route("api/license")]
    public class LicenseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LicenseController> _logger;
        private readonly KeygenService _keygenService;
        private readonly DataService _dataService;

        public LicenseController(ApplicationDbContext context, ILogger<LicenseController> logger,
            KeygenService keygenService, DataService dataService)
        {
            _context = context;
            _logger = logger;
            _keygenService = keygenService;
            _dataService = dataService;
        }

        //// =========================
        //// 📋 LIST ALL LICENSES (GetLicense)
        //// =========================
        [HttpGet]
        public async Task<ActionResult<APIResponse<IEnumerable<License>>>> GetLicenses()
        {
            var response = new APIResponse<IEnumerable<License>>();
            try
            {
                var licenses = await _context.Licenses.ToListAsync();
                response.IsSuccess = true;
                response.Message = "Licenses fetched successfully";
                response.Data = licenses;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error occurred while fetching license list");
                response.IsSuccess = false;
                response.Message = "An error occurred while fetching licenses.";
                return StatusCode(500, response);
            }
        }

        //// =========================
        //// 🔍 GET LICENSE BY ID (GetLicenseById)
        //// =========================
        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse<KeygenLicenseResult>>> GetLicenseById(string id)
        {
            var response = new APIResponse<KeygenLicenseResult>();
            try
            {
                KeygenLicenseResult licenseResult = await _keygenService.GetLicenseByIdAsync(id);
                if (licenseResult == null || !licenseResult.Success)
                {
                    response.IsSuccess = false;
                    response.Message = "License not found in Keygen";
                    return NotFound(response);
                }
                response.IsSuccess = true;
                response.Message = "License retrieved successfully from Keygen";
                response.Data = licenseResult;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching license {LicenseId} from Keygen", id);
                response.IsSuccess = false;
                response.Message = "Error fetching license: " + ex.Message;
                return StatusCode(500, response);
            }
        }

        [HttpGet("company/{companyId}")]
        public async Task<ActionResult<APIResponse<IEnumerable<KeygenLicenseDto>>>> GetLicensesByCompany(int companyId)
        {
            var response = new APIResponse<IEnumerable<KeygenLicenseDto>>();
            var LicenseList = new List<KeygenLicenseDto>();
            try
            {
                var licenses = await _context.Licenses.Where(l => l.CompanyId == companyId).ToListAsync();
                if (licenses.Any())
                {
                    foreach(var license in licenses)
                    {
                        KeygenLicenseDto licenseData = new KeygenLicenseDto();
                        licenseData.Id = license.ProviderLicenseId;
                        licenseData.Name = license.Name;
                        licenseData.Key = license.LicenseKey;
                        licenseData.Expiry = license.ExpiryDate;
                        licenseData.Status = license.Status;
                        licenseData.CreatedAt = license.CreatedAt;
                        licenseData.ProductId = license.ProductId;
                        //licenseData.ProductName = license.Name;
                        licenseData.PolicyId = license.PolicyId;
                       // licenseData.PolicyName = license.Name;
                        //licenseData.UserEmail = license.Name;
                        LicenseList.Add(licenseData);
                    }
                }

                response.IsSuccess = true;
                response.Message = "Licenses fetched successfully";
                response.Data = LicenseList;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching licenses for company {CompanyId}", companyId);
                response.IsSuccess = false;
                response.Message = "Error fetching licenses";
                return StatusCode(500, response);
            }
        }

        //// =========================
        //// ➕ ADD LICENSE (CreateLicense)
        //// =========================
        [HttpPost("create")]
        public async Task<ActionResult<APIResponse<License>>> CreateLicense([FromBody] CreateLicenseRequest request)
        {
            var response = new APIResponse<License>();
            try
            {
                var company = await _context.Companies.FindAsync(request.Company);
                if (company == null)
                {
                    response.IsSuccess = false;
                    response.Message = $"Company with ID {request.Company} not found.";
                    return BadRequest(response);
                }

                KeygenLicenseRequest licenseRequest = new KeygenLicenseRequest();
                licenseRequest.PolicyId = request.Policy;
                licenseRequest.createLicense = request;
                request.Name = company.CompanyName;

                // Resolve local product and policy to set foreign key IDs
                var localProduct = await _context.Products.FirstOrDefaultAsync(p => p.ProviderProductId == request.Product);
                var localPolicy = await _context.Policies.FirstOrDefaultAsync(p => p.ProviderPolicyId == request.Policy);

                // 1. Call Keygen API
                var keygenResult = await _keygenService.CreateLicenseAsync(licenseRequest);

                if (!keygenResult.Success || keygenResult.Data == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Failed to create license in Keygen";
                    return StatusCode(502, response);
                }
                
                // 2. Create local license record mapping all relevant fields
                var license = new License
                {
                    ProviderLicenseId = keygenResult.Data.Id,
                    ProviderPolicyId = request.Policy,
                    ProviderProductId = request.Product,
                    ProductId = localProduct?.ProductId,
                    PolicyId = localPolicy?.PolicyId,
                    CompanyId = request.Company,
                    LicenseKey = keygenResult.Data.Attributes.Key,
                    Status = keygenResult.Data.Attributes.Status ?? "ACTIVE",
                    ExpiryDate = keygenResult.Data.Attributes.Expiry ?? request.Expiry,
                    Owner = company.UserEmail ?? "Unknown User",
                    MaxMachines = keygenResult.Data.Attributes.MaxMachines ?? 0,
                    Uses = keygenResult.Data.Attributes.Uses ?? 0,
                    MaxUsers = keygenResult.Data.Attributes.MaxUsers ?? 0,
                    IsTrial = keygenResult.Data.Attributes.IsTrial,
                    IsValid = true,
                    CreatedAt = keygenResult.Data.Attributes.CreatedAt != default ? keygenResult.Data.Attributes.CreatedAt : DateTime.UtcNow,
                    UpdatedAt = keygenResult.Data.Attributes.UpdatedAt != default ? keygenResult.Data.Attributes.UpdatedAt : DateTime.UtcNow,
                };

                // 3. Save to database
                _context.Licenses.Add(license);
                var res = await _context.SaveChangesAsync();
                //// Update the license quantity purchased by the company
                if (res > 0)
                {
                    company.LicenseQuantity = request.MaxUsers;
                    _context.Companies.Update(company);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("License created successfully for {UserEmail}. Keygen ID: {KeygenId}",company.UserEmail, license.ProviderLicenseId);

                response.IsSuccess = true;
                response.Message = "License created successfully";
                response.Data = license;

                return Created($"/api/license/{license.LicenseId}", response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error occurred while creating license for UserEmail");
                response.IsSuccess = false;
                response.Message = $"An error occurred while creating the license: {ex.Message}";
                return StatusCode(500, response);
            }
        }


        //// =========================
        //// ✏️ EDIT LICENSE (UpdateLicense)
        //// =========================
        [HttpPut("{id}")]
        public async Task<ActionResult<APIResponse<object>>> UpdateLicense(string id, [FromBody] UpdateLicenseRequest request)
        {
            var response = new APIResponse<object>();
            try
            {
                var license = await _context.Licenses.Where(x=>x.ProviderLicenseId == id).FirstOrDefaultAsync();
                if (license == null)
                {
                    response.IsSuccess = false;
                    response.Message = "License not found";
                    return NotFound(response);
                }

                // Retrieve policy ID (from local record or Keygen fallback)
                string? policyId = license.ProviderPolicyId;
                if (string.IsNullOrEmpty(policyId))
                {
                    var keygenLicense = await _keygenService.GetLicenseByIdAsync(id);
                    if (keygenLicense != null && keygenLicense.Success && keygenLicense.Data?.Relationships?.Policy?.Data != null)
                    {
                        policyId = keygenLicense.Data.Relationships.Policy.Data.Id;
                        license.ProviderPolicyId = policyId;
                    }
                }

                if (string.IsNullOrEmpty(policyId))
                {
                    response.IsSuccess = false;
                    response.Message = "License does not have a valid Policy ID.";
                    return BadRequest(response);
                }

                // Perform validation against the policy
                var validationResult = await ValidateUpdateRequest(request, policyId);
                if (!validationResult.isValid)
                {
                    response.IsSuccess = false;
                    response.Message = validationResult.error ?? "Validation failed.";
                    return BadRequest(response);
                }

                bool hasUpdates = request.Expiry.HasValue || request.MaxUsers > 0 || request.MaxMachines > 0;
                if (hasUpdates)
                {
                    if (string.IsNullOrEmpty(license.ProviderLicenseId))
                    {
                        response.IsSuccess = false;
                        response.Message = "License does not have a valid Keygen ID.";
                        return BadRequest(response);
                    }

                    var updateKeygenResult = await _keygenService.UpdateLicenseExpiryAsync(id, request);
                    if (!updateKeygenResult)
                    {
                        response.IsSuccess = false;
                        response.Message = "Failed to update license in Keygen.";
                        return StatusCode(500, response);
                    }
                    if (request.Expiry.HasValue) license.ExpiryDate = request.Expiry.Value;
                    if (request.MaxUsers > 0) license.MaxUsers = request.MaxUsers;
                    if (request.MaxMachines > 0) license.MaxMachines = request.MaxMachines;
                }

                // if (request.LicenseKey != null) license.LicenseKey = request.LicenseKey;
                // 3. Save to database
                _context.Licenses.Update(license);
                await _context.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "License updated successfully";
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating license {Id}", id);
                response.IsSuccess = false;
                response.Message = "Error updating license";
                return StatusCode(500, response);
            }
        }


        private async Task<(bool isValid, string? error)> ValidateUpdateRequest(
            UpdateLicenseRequest request, string policyId)
        {
            // fetch the policy first
            var policy = await _keygenService.GetPolicyByIdAsync(policyId);
            if (policy == null || !policy.Success || policy.Data == null)
            {
                return (false, "Could not fetch licensing policy from Keygen service.");
            }

            var policyAttrs = policy.Data.Attributes;

            // ❌ maxMachines > 1 requires floating
            if (request.MaxMachines > 1 && !(policyAttrs?.Floating ?? false))
                return (false, "MaxMachines cannot exceed 1 for non-floating policy.");

            // ⚠️ warn if exceeding policy maxUsers
            if (policyAttrs?.MaxUsers.HasValue == true &&
                request.MaxUsers > policyAttrs.MaxUsers.Value)
                return (false, $"MaxUsers cannot exceed policy limit of {policyAttrs.MaxUsers}.");

            return (true, null);
        }

        [HttpPut("{id}/revoke")]
        public async Task<ActionResult<APIResponse<object>>> RevokeLicense(int id, [FromQuery] string accountId)
        {
            var response = new APIResponse<object>();
            try
            {
                var license = await _context.Licenses.FindAsync(id);
                if (license == null)
                {
                    response.IsSuccess = false;
                    response.Message = "License not found";
                    return NotFound(response);
                }

                if (string.IsNullOrEmpty(license.LicenseKey))
                {
                    response.IsSuccess = false;
                    response.Message = "License key not found";
                    return BadRequest(response);
                }

                var keygenRevokeResult = await _keygenService.RevokeLicenseAsync(accountId, license.LicenseKey);
                if (!keygenRevokeResult)
                {
                    response.IsSuccess = false;
                    response.Message = "Failed to revoke license in Keygen";
                    return StatusCode(500, response);
                }

                license.Status = "SUSPENDED";
                license.IsValid = false;
                await _context.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "License revoked successfully";
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking license {Id}", id);
                response.IsSuccess = false;
                response.Message = "Error revoking license";
                return StatusCode(500, response);
            }
        }

        //// =========================
        //// 🗑️ DELETE LICENSE (DeleteLicense)
        //// =========================
        [HttpDelete("{id}")]
        public async Task<ActionResult<APIResponse<object>>> DeleteLicense(string id, [FromBody] DeleteLicenseRequest request)
        {
            var response = new APIResponse<object>();
            try
            {

                var license = await _context.Licenses.Where(x => x.ProviderLicenseId == id).FirstOrDefaultAsync();
                if (license == null)
                {
                    response.IsSuccess = false;
                    response.Message = "License not found";
                    return NotFound(response);
                }

                if (!string.IsNullOrEmpty(license.LicenseKey))
                {
                    var keygenDeleteResult = await _keygenService.DeleteLicenseAsync(request.AccountId, license.LicenseKey);
                    if (keygenDeleteResult)
                    {
                        // ✅ Only soft delete when Keygen deletion is successful
                        license.IsValid = false;
                        //license.DeletedAt = DateTime.UtcNow;

                        _context.Licenses.Update(license);
                        await _context.SaveChangesAsync();

                        response.IsSuccess = true;
                        response.Message = "License deleted successfully";
                        return Ok(response);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Failed to delete license key {LicenseKey} in Keygen. DB update skipped.",
                            license.LicenseKey);

                        response.IsSuccess = false;
                        response.Message = "Failed to delete license in Keygen";
                        return StatusCode(500, response);
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "No LicenseKey found for license {Id}. DB update skipped.",
                        id);

                    response.IsSuccess = false;
                    response.Message = "License key not found";
                    return NotFound(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting license {Id}", id);
                response.IsSuccess = false;
                response.Message = "Error deleting license";
                return StatusCode(500, response);
            }
        }

        #region  GET LICENSE
        //// =========================
        //// 📋 LIST ALL LICENSES FROM KEYGEN
        //// =========================
        //[HttpGet("keygen")] ////OLD Method
        //public async Task<ActionResult<APIResponse<IEnumerable<KeygenLicenseDto>>>> GetKeygenLicenses([FromQuery] string accountId,[FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        //{
        //    var response = new APIResponse<IEnumerable<KeygenLicenseDto>>();
        //    try
        //    {
        //        var (licenses, meta) = await _keygenService.GetLicensesAsync(accountId,pageNumber, pageSize);
        //        response.IsSuccess = true;
        //        response.Message = "Keygen licenses fetched successfully";
        //        response.Data = licenses;
                
        //        if (meta != null)
        //        {
        //            response.Pagination = new PaginationMetadata
        //            {
        //                TotalCount = meta.Page?.Total ?? meta.Count,
        //                PageSize = pageSize,
        //                CurrentPage = pageNumber,
        //                TotalPages = meta.Pages ?? (meta.Page != null ? (int)Math.Ceiling((double)meta.Page.Total / pageSize) : (int)Math.Ceiling((double)meta.Count / pageSize))
        //            };
        //        }

        //        return Ok(response);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Exception: Error occurred while fetching Keygen licenses list");
        //        response.IsSuccess = false;
        //        response.Message = "An error occurred while fetching Keygen licenses.";
        //        return StatusCode(500, response);
        //    }
        //}

        [HttpGet("keygen")]
        public async Task<ActionResult<APIResponse<PagedResult<IEnumerable<KeygenLicenseDto>>>>> GetKeygenLicenses(
            [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, string? searchKey = null)
        {
            var response = new APIResponse<PagedResult<IEnumerable<KeygenLicenseDto>>>();
            try
            {
                var (licenses, meta) = await _keygenService.GetLicensesAsync(
                    pageNumber, pageSize, searchKey);

                if (licenses.Any())
                {
                    response.IsSuccess = true;
                    response.Message = "Licenses fetched from Keygen";
                    response.Data = new PagedResult<IEnumerable<KeygenLicenseDto>>
                    {
                        Data = licenses,
                        Meta = meta
                    };
                    return Ok(response);
                }

                // 2. Fallback to database
                var dbResult = await _dataService.GetLicensesAsync(pageNumber, pageSize, searchKey);

                response.IsSuccess = true;
                response.Message = "Licenses fetched from database";
                response.Data = new PagedResult<IEnumerable<KeygenLicenseDto>>
                {
                    Data = dbResult.Licenses,
                    Meta = new KeygenMeta
                    {
                        Total = dbResult.TotalCount,
                        Count = dbResult.Licenses.Count(),
                        Number = pageNumber,
                        //Pages = pageSize,
                        Size = pageSize,
                    }
                };

                //response.Data = dbResult.Licenses;
                //response.Pagination = new PaginationMetadata
                //{
                //    TotalCount = dbResult.TotalCount,
                //    PageSize = pageSize,
                //    CurrentPage = pageNumber,
                //    TotalPages = (int)Math.Ceiling((double)dbResult.TotalCount / pageSize)
                //};

                return response;


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error occurred while fetching Keygen licenses list");
                response.IsSuccess = false;
                response.Message = "An error occurred while fetching Keygen licenses.";
                return StatusCode(500, response);
            }
        }


        [HttpGet("validate")]
        public async Task<ActionResult<APIResponse<object>>> ValidateLicense([FromQuery] string licenseKey)
        {
            var response = new APIResponse<License>();
            try
            {
                var res = await _keygenService.ValidateLicenseAsync(licenseKey);
                //var license = await _context.Licenses.FindAsync(id);
                //if (license == null)
                //{
                //    response.IsSuccess = false;
                //    response.Message = "License not found";
                //    return NotFound(response);
                //}

                response.IsSuccess = true;
                response.Message = "License retrieved successfully";
                //response.Data = license;
                return Ok(response);
            }
            catch (Exception ex)
            {
              //  _logger.LogError(ex, "Error fetching license {LicenseId}", id);
                response.IsSuccess = false;
                response.Message = "Error fetching license";
                return StatusCode(500, response);
            }
        }

        [HttpPost("validate")]
        public async Task<ActionResult<APIResponse<LicenseValidationResult>>> ValidateLicense([FromBody] LicenseValidationRequest request)
        {
            var response = new APIResponse<LicenseValidationResult>();
            var result = new LicenseValidationResult();
            
            try
            {
                // 1. Validate with Keygen
                var keygenResult = await _keygenService.ValidateLicenseDetailsAsync(request.LicenseKey);
                
                if (!keygenResult.Success)
                {
                    result.IsValid = false;
                    result.Message = "License key is invalid or validation failed.";
                    result.Status = "INVALID";
                    
                    if (keygenResult.Data?.Attributes != null)
                    {
                        result.Status = keygenResult.Data.Attributes.Status;
                        result.ExpiryDate = keygenResult.Data.Attributes.Expiry;
                        result.IsTrial = keygenResult.Data.Attributes.IsTrial;
                    }

                    response.IsSuccess = true; // Request succeeded, but license is invalid
                    response.Data = result;
                    return Ok(response);
                }

                // 2. Verify Email Ownership
                // Check if license exists locally and belongs to this email
                //var localLicense = await _context.Licenses
                //    .Include(l => l.UserLicenses)
                //    .ThenInclude(ul => ul.User)
                //    .FirstOrDefaultAsync(l => l.LicenseKey == request.LicenseKey || l.ProviderLicenseId == keygenResult.Data.Id);

                bool isOwner = false;
                //if (localLicense != null)
                //{
                //    // Check direct Owner field
                //    if (string.Equals(localLicense.Owner, request.Email, StringComparison.OrdinalIgnoreCase))
                //    {
                //        isOwner = true;
                //    }
                //    // Check UserLicense mappings
                //    else if (localLicense.UserLicenses.Any(ul => string.Equals(ul.User.Email, request.Email, StringComparison.OrdinalIgnoreCase)))
                //    {
                //        isOwner = true;
                //    }
                //}

                if (!isOwner)
                {
                    result.IsValid = false;
                    result.Message = "License key is valid, but it is not registered to this email address.";
                    result.Status = "UNAUTHORIZED";
                    response.IsSuccess = true;
                    response.Data = result;
                    return Ok(response);
                }

                // 3. Success
                result.IsValid = true;
                result.Status = keygenResult.Data.Attributes.Status;
                result.ExpiryDate = keygenResult.Data.Attributes.Expiry;
                result.IsTrial = keygenResult.Data.Attributes.IsTrial;
                result.Message = "License validated successfully.";

                response.IsSuccess = true;
                response.Data = result;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during license validation for email {Email}", request.Email);
                response.IsSuccess = false;
                response.Message = "An error occurred during validation.";
                return StatusCode(500, response);
            }
        }

        [HttpPost("request")]
        public async Task<ActionResult<APIResponse<License>>> RequestLicense([FromBody] LicenseRequestModel request)
        {
            var response = new APIResponse<License>();
            try
            {
                // 1. Basic Validation
                if (!await IsValidClientSecret(request.ClientSecret))
                {
                    response.IsSuccess = false;
                    response.Message = "Unauthorized request.";
                    return Unauthorized(response);
                }

                // 2. Find Product
                var product = await _context.Products
                    .Include(p => p.Policies)
                    .FirstOrDefaultAsync(p => p.Code == request.ProductCode);

                if (product == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Product not found.";
                    return NotFound(response);
                }

                // 3. Find Policy (prefer one named 'Trial' or just take the first)
                var policy = product.Policies.FirstOrDefault(p => p.PolicyName?.Contains("Trial", StringComparison.OrdinalIgnoreCase) == true)
                             ?? product.Policies.FirstOrDefault();

                if (policy == null)
                {
                    response.IsSuccess = false;
                    response.Message = "No active licensing policy found for this product.";
                    return BadRequest(response);
                }

                // 4. Check if user already has a license for this product
                var existingLicense = await _context.Licenses
                    .FirstOrDefaultAsync(l => l.Owner == request.Email && l.ProviderProductId == product.ProviderProductId);

                if (existingLicense != null)
                {
                    response.IsSuccess = true;
                    response.Message = "User already has a license for this product.";
                    response.Data = existingLicense;
                    return Ok(response);
                }

                // 5. Create License via Keygen
                var accountId = await _context.AppSettings
                    .Where(a => a.Key == "ACCOUNT_ID")
                    .Select(a => a.Value)
                    .FirstOrDefaultAsync() ?? string.Empty;

                var keygenReq = new KeygenLicenseRequest
                {
                    AccountId = accountId,
                    UserEmail = request.Email,
                    PolicyId = policy.ProviderPolicyId
                };

                var keygenResult = await _keygenService.CreateLicenseAsync(keygenReq);

                if (!keygenResult.Success || keygenResult.Data == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Failed to generate license on server.";
                    return StatusCode(502, response);
                }

                // 6. Save Local Record
                var license = new License
                {
                    ProviderLicenseId = keygenResult.Data.Id,
                    LicenseKey = keygenResult.Data.Attributes.Key,
                    Status = keygenResult.Data.Attributes.Status ?? "ACTIVE",
                    Name = keygenResult.Data.Attributes.Name ?? request.FullName,
                    ExpiryDate = keygenResult.Data.Attributes.Expiry,
                    Owner = request.Email,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsValid = true,
                    ProductId = product.ProductId,
                    PolicyId = policy.PolicyId,
                    ProviderProductId = product.ProviderProductId,
                    ProviderPolicyId = policy.ProviderPolicyId
                };

                _context.Licenses.Add(license);
                await _context.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "License requested and generated successfully.";
                response.Data = license;
                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing license request for {Email}", request.Email);
                response.IsSuccess = false;
                response.Message = "An error occurred while processing your request.";
                return StatusCode(500, response);
            }
        }

        private async Task<bool> IsValidClientSecret(string? secret)
        {
            if (string.IsNullOrEmpty(secret)) return false;

            var configuredSecret = await _context.AppSettings
                .Where(a => a.Key == "CLIENT_SECRET")
                .Select(a => a.Value)
                .FirstOrDefaultAsync();

            return secret == configuredSecret;
        }

        // Extracted helper
        //private static PaginationMetadata BuildPagination(KeygenMeta? meta, int pageNumber, int pageSize)
        //{
        //    var total = meta?.Page?.Total ?? meta?.Count ?? 0;
        //    return new PaginationMetadata
        //    {
        //        TotalCount = total,
        //        PageSize = pageSize,
        //        CurrentPage = pageNumber,
        //        TotalPages = meta?.Pages ?? (int)Math.Ceiling((double)total / pageSize)
        //    };
        //}


        //public async Task<(List<KeygenLicenseDto> Licenses, int TotalCount)> GetLicensesAsync(int pageNumber, int pageSize, string? searchKey)
        //{
        //    using var conn = new SqlConnection(_connectionString);

        //    using var multi = await conn.QueryMultipleAsync(
        //        "sp_GetLicenses",
        //        new { PageNumber = pageNumber, PageSize = pageSize, SearchKey = searchKey },
        //        commandType: CommandType.StoredProcedure);

        //    var totalCount = await multi.ReadFirstAsync<int>();
        //    var licenses = (await multi.ReadAsync<KeygenLicenseDto>()).ToList();

        //    return (licenses, totalCount);
        //}
        #endregion GET LICENSE
    }
}
