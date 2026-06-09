using LicensingAPI.Data;
using LicensingAPI.Models;
using LicensingAPI.Models.Policies;
using LicensingAPI.Models.Products;
using LicensingAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LicensingAPI.Controllers
{
    [ApiController]
    [Route("api/policies")]
    public class PoliciesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly KeygenService _keygenService;
        private readonly ILogger<PoliciesController> _logger;
        private readonly DataService _dataService;
        public PoliciesController(ApplicationDbContext context, ILogger<PoliciesController> logger,
            KeygenService keygenService, DataService dataService)
        {
            _context = context;
            _logger = logger;
            _keygenService = keygenService;
            _dataService = dataService;
        }

        //// =========================
        //// 📋 LIST ALL POLICIES
        //// =========================
        [HttpGet]
        public async Task<ActionResult<APIResponse<IEnumerable<PolicyDTO>>>> GetPolicies()
        {
            var response = new APIResponse<IEnumerable<PolicyDTO>>();
            try
            {
                var keygenpolicies = await _keygenService.GetPoliciesAsync();
                var localPolicies = await _context.Policies.Include(p => p.Product).ToListAsync();

                var policiesList = keygenpolicies?.Any() == true
                    ? keygenpolicies.Select(p => {
                        var local = localPolicies.FirstOrDefault(lp => lp.ProviderPolicyId == p.Id);
                        return new PolicyDTO 
                        { 
                            Id = p.Id, 
                            LocalId = local?.PolicyId ?? 0, 
                            Name = p.Name ?? string.Empty,
                            Duration = p.Duration ?? 0,
                            ProductId = local?.ProductId ?? 0,
                            ProductName = local?.Product?.ProductName ?? string.Empty
                        };
                    }).ToList()
                    : localPolicies
                        .Select(p => new PolicyDTO 
                        { 
                            Id = p.ProviderPolicyId, 
                            LocalId = p.PolicyId, 
                            Name = p.PolicyName ?? string.Empty,
                            Duration = p.Duration.HasValue ? (int)p.Duration.Value : 0,
                            ProductId = p.ProductId ?? 0,
                            ProductName = p.Product?.ProductName ?? string.Empty
                        })
                        .ToList();

                response.IsSuccess = true;
                response.Message = "Policies fetched successfully";
                response.Data = policiesList;
                return Ok(response);     
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error occurred while fetching policies list");
                response.IsSuccess = false;
                response.Message = "An error occurred while fetching policies.";
                return StatusCode(500, response);  
            }
        }

        //// =========================
        //// 🔍 GET POLICY BY ID
        //// =========================
        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse<Policy>>> GetPolicy(int id)
        {
            var response = new APIResponse<Policy>();

            try
            {
                var policy = await _context.Policies.FindAsync(id);

                if (policy == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Policy not found";
                    return NotFound(response);
                }

                response.IsSuccess = true;
                response.Message = "Policy retrieved successfully";
                response.Data = policy;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching policy {PolicyId}", id);
                response.IsSuccess = false;
                response.Message = "Error fetching policy";
                return StatusCode(500, response);
            }
        }

        //// =========================
        //// ➕ ADD POLICY
        //// =========================
        [HttpPost("create")]
        public async Task<ActionResult<APIResponse<Policy>>> CreatePolicy([FromBody] CreatePolicyRequest request)
        {
            var response = new APIResponse<Policy>();

            try
            {
                // 0. Resolve Product
                var product = await _context.Products.Where(p => p.ProviderProductId == request.ProductId).FirstOrDefaultAsync();
                if (product == null)
                {
                    response.IsSuccess = false;
                    response.Message = $"Product with ID {request.ProductId} not found.";
                    return NotFound(response);
                }

                if (string.IsNullOrEmpty(product.ProviderProductId))
                {
                    response.IsSuccess = false;
                    response.Message = "Selected product does not have a valid Keygen Product ID.";
                    return BadRequest(response);
                }

                // 1. Call Keygen API
                var keygenPolicyId = await _keygenService.CreatePolicyAsync(request, product.ProviderProductId);
                if (string.IsNullOrEmpty(keygenPolicyId))
                {
                    response.IsSuccess = false;
                    response.Message = "Failed to create policy in Keygen API";
                    return StatusCode(502, response);
                }

                // 2. Create local policy record
                var policy = new Policy
                {
                    ProviderPolicyId = keygenPolicyId,
                    PolicyName = request.Name,
                    Duration = request.Duration,
                    ProductId = product.ProductId,
                    ProviderProductId = product.ProviderProductId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Policies.Add(policy);
                await _context.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "Policy created successfully";
                response.Data = policy;

                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical Exception: Error occurred while creating policy");
                response.IsSuccess = false;
                response.Message = $"An unexpected error occurred: {ex.Message}";
                return StatusCode(500, response);
            }
        }

        //// =========================
        //// ✏️ EDIT POLICY
        //// =========================
        [HttpPut("{id}")]
        public async Task<ActionResult<APIResponse<object>>> UpdatePolicy(int id, [FromBody] UpdatePolicyRequest request)
        {
            var response = new APIResponse<object>();

            try
            {
                var policy = await _context.Policies.FindAsync(id);
                if (policy == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Policy not found";
                    return NotFound(response);
                }

                // 1. Update in Keygen
                if (!string.IsNullOrEmpty(policy.ProviderPolicyId))
                {
                    var keygenSuccess = await _keygenService.UpdatePolicyAsync(policy.ProviderPolicyId, request.Name, request.Duration);
                    if (!keygenSuccess)
                    {
                        response.IsSuccess = false;
                        response.Message = "Failed to update policy in Keygen API";
                        return StatusCode(502, response);
                    }
                }

                // 2. Update locally
                policy.PolicyName = request.Name;
                policy.Duration = request.Duration;
                policy.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "Policy updated successfully";
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical Exception: Error updating policy {Id}", id);
                response.IsSuccess = false;
                response.Message = $"An unexpected error occurred: {ex.Message}";
                return StatusCode(500, response);
            }
        }

        //// =========================
        //// 🗑️ DELETE POLICY
        //// =========================
        [HttpDelete("{id}")]
        public async Task<ActionResult<APIResponse<object>>> DeletePolicy(int id)
        {
            var response = new APIResponse<object>();

            try
            {
                var policy = await _context.Policies.FindAsync(id);
                if (policy == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Policy not found";
                    return NotFound(response);
                }

                // 1. Delete from Keygen
                if (!string.IsNullOrEmpty(policy.ProviderPolicyId))
                {
                    var keygenSuccess = await _keygenService.DeletePolicyAsync(policy.ProviderPolicyId);
                    if (!keygenSuccess)
                    {
                        _logger.LogWarning("Failed to delete policy {KeygenPolicyId} in Keygen. Proceeding locally.", policy.ProviderPolicyId);
                    }
                }

                // 2. Delete locally
                _context.Policies.Remove(policy);
                await _context.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "Policy deleted successfully";
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical Exception: Error deleting policy {Id}", id);
                response.IsSuccess = false;
                response.Message = $"An unexpected error occurred: {ex.Message}";
                return StatusCode(500, response);
            }
        }

        //// =========================
        //// 📋 LIST ALL POLICIES FROM KEYGEN
        //// =========================
        [HttpGet("keygen")]
        public async Task<ActionResult<APIResponse<PagedResult<IEnumerable<KeygenPolicyDto>>>>> GetKeygenPolicies([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, string? searchKey = null)
        {
            var response = new APIResponse<PagedResult<IEnumerable<KeygenPolicyDto>>>();
            try
            {
                //if (string.IsNullOrEmpty(accountId))
                //    return BadRequest("Account ID is required");

                var (policies, meta) = await _keygenService.GetPoliciesAsync(
                    pageNumber, pageSize, searchKey);

                if (policies.Any())
                {
                    response.IsSuccess = true;
                    response.Message = "Policies fetched from Keygen";
                    response.Data = new PagedResult<IEnumerable<KeygenPolicyDto>>
                    {
                        Data = policies,
                        Meta = meta
                    };
                    return Ok(response);
                }

                //// 2. Fallback to database
                var dbResult = await _dataService.GetPoliciesAsync(pageNumber, pageSize, searchKey);

                response.IsSuccess = true;
                response.Message = "Licenses fetched from database";
                response.Data = new PagedResult<IEnumerable<KeygenPolicyDto>>
                {
                    Data = dbResult.Policies,
                    Meta = new KeygenMeta
                    {
                        Total = dbResult.TotalCount,
                        Count = dbResult.Policies.Count(),
                        Number = pageNumber,
                        //Pages = pageSize,
                        Size = pageSize,
                    }
                };

                //var dbResult = await _licenseRepository.GetLicensesAsync(pageNumber, pageSize, searchKey);

                //response.IsSuccess = true;
                //response.Message = "Licenses fetched from database";
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
                _logger.LogError(ex, "Exception: Error occurred while fetching Keygen policies list");
                response.IsSuccess = false;
                response.Message = "An error occurred while fetching Keygen policies.";
                return StatusCode(500, response);
            }
        }

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
    }
}
