using LicensingAPI.Controllers;
using LicensingAPI.Data;
using LicensingAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LicensingAPI.Services
{
    public class DataService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LicenseController> _logger;

        public DataService(ApplicationDbContext context, ILogger<LicenseController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(IEnumerable<KeygenLicenseDto> Licenses, int TotalCount)> GetLicensesAsync(
        int pageNumber, int pageSize, string? searchKey = null)
        {
            var query = _context.Licenses.AsNoTracking();

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchKey))
                query = query.Where(x => x.LicenseKey.Contains(searchKey)
                                       || x.Name.Contains(searchKey));

            var totalCount = await query.CountAsync();

            var licenses = await query
                .OrderBy(x => x.LicenseId)                        // stable ordering is required
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new KeygenLicenseDto          // map to your DTO
                {
                    Id = x.ProviderLicenseId,
                    Name = x.Name,
                    Key = x.LicenseKey,
                    Expiry = x.ExpiryDate,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            return (licenses, totalCount);
        }

        public async Task<(IEnumerable<KeygenProductDto> Products, int TotalCount)> GetProductsAsync(
       int pageNumber, int pageSize, string? searchKey = null)
        {
            var query = _context.Products.AsNoTracking();

            var totalCount = await query.CountAsync();

            var products = await query
                .OrderBy(x => x.ProductId)                        // stable ordering is required
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new KeygenProductDto          // map to your DTO
                {
                    Id = d.ProviderProductId,
                    Name = d.ProductName,
                    Code = d.Code,
                    Url = d.Url,
                    Description = d.Description
                })
                .ToListAsync();

            return (products, totalCount);
        }

        public async Task<(IEnumerable<KeygenPolicyDto> Policies, int TotalCount)> GetPoliciesAsync(
      int pageNumber, int pageSize, string? searchKey = null)
        {
            var query = _context.Policies.AsNoTracking();

            var totalCount = await query.CountAsync();

            var policies = await query
                .OrderBy(x => x.PolicyId)                        // stable ordering is required
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new KeygenPolicyDto          // map to your DTO
                {
                    Id = d.ProviderPolicyId,
                    Name = d.PolicyName,
                   // Duration = d.Duration,
                    Created = d.CreatedAt
                })
                .ToListAsync();

            return (policies, totalCount);
        }
    }
}
