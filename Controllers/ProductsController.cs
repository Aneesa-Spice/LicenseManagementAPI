using LicensingAPI.Data;
using LicensingAPI.Models;
using LicensingAPI.Models.Auth;
using LicensingAPI.Models.Policies;
using LicensingAPI.Models.Products;
using LicensingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace LicensingAPI.Controllers
{
    // [Authorize] // Enable this once testing is complete
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly KeygenService _keygenService;
        private readonly ILogger<ProductsController> _logger;
        private readonly DataService _dataService;
        public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger,
            KeygenService keygenService, DataService dataService)
        {
            _context = context;
            _logger = logger;
            _keygenService = keygenService;
            _dataService = dataService;
        }


        //// =========================
        //// 📋 LIST ALL PRODUCTS
        //// =========================
        [HttpGet]
        public async Task<ActionResult<APIResponse<IEnumerable<ProductDTO>>>> GetProducts()
        {
            var response = new APIResponse<IEnumerable<ProductDTO>>();
            try
            {
                //var products = await _context.Products.ToListAsync();
                //response.IsSuccess = true;
                //response.Message = "Products fetched successfully";
                //response.Data = products;
                //return Ok(response);
                var keygenproducts = await _keygenService.GetProductsAsync();
                var localProducts = await _context.Products.ToListAsync();

                var productList = keygenproducts?.Any() == true
                    ? keygenproducts.Select(p => {
                        var local = localProducts.FirstOrDefault(lp => lp.ProviderProductId == p.Id);
                        return new ProductDTO 
                        { 
                            Id = local?.ProductId ?? 0, 
                            KeygenId = p.Id, 
                            Name = p.Name ?? string.Empty,
                            Description = p.Description ?? string.Empty,
                            ProductCode = p.Code ?? string.Empty
                        };
                    }).ToList()
                    : localProducts
                        .Select(p => new ProductDTO 
                        { 
                            Id = p.ProductId, 
                            KeygenId = p.ProviderProductId, 
                            Name = p.ProductName,
                            Description = p.Description ?? string.Empty,
                            ProductCode = p.Code
                        })
                        .ToList();

                response.IsSuccess = true;
                response.Message = "Products fetched successfully";
                response.Data = productList;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error occurred while fetching products list");
                response.IsSuccess = false;
                response.Message = "An error occurred while fetching products.";
                return StatusCode(500, response);
            }
        }

        //// =========================
        //// 🔍 GET PRODUCT BY ID
        //// =========================
        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse<Product>>> GetProduct(int id)
        {
            var response = new APIResponse<Product>();

            try
            {
                var product = await _context.Products.FindAsync(id);

                if (product == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Product not found";
                    return NotFound(response);
                }

                response.IsSuccess = true;
                response.Message = "Product retrieved successfully";
                response.Data = product;

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching product {ProductId}", id);
                response.IsSuccess = false;
                response.Message = "Error fetching product";
                return StatusCode(500, response);
            }
        }

        //// =========================
        //// 🔍 GET PRODUCT BY CODE
        //// =========================
        [HttpGet("code/{code}")]
        public async Task<ActionResult<APIResponse<Product>>> GetProductByCode(string code)
        {
            var response = new APIResponse<Product>();

            try
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Code == code);

                if (product == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Product not found";
                    return NotFound(response);
                }

                response.IsSuccess = true;
                response.Message = "Product retrieved successfully";
                response.Data = product;

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching product by code {ProductCode}", code);
                response.IsSuccess = false;
                response.Message = "Error fetching product";
                return StatusCode(500, response);
            }
        }

        //// =========================
        //// ➕ ADD PRODUCT
        //// =========================
        [HttpPost("create")]
        public async Task<ActionResult<APIResponse<Product>>> CreateProduct([FromBody] CreateProductRequest request)
        {
            var response = new APIResponse<Product>();

            try
            {
                // 1. Validate request
                if (!ModelState.IsValid)
                {
                    response.IsSuccess = false;
                    response.Message = "Validation failed";
                    response.Errors = ModelState.Keys
                        .SelectMany(key => ModelState[key]!.Errors
                            .Select(e => new ValidationError
                            {
                                Field = key,
                                Error = e.ErrorMessage
                            })).ToList();

                    _logger.LogWarning("Validation failed for product creation: {@Response}", response);
                    return BadRequest(response);
                }

                // 2. Check for duplicate code locally
                if (_context.Products.Any(p => p.Code == request.Code))
                {
                    response.IsSuccess = false;
                    response.Message = $"Product with code {request.Code} already exists";
                    _logger.LogWarning("Duplicate product code attempted: {@Response}", response);
                    return Conflict(response);
                }

                // 3. Create product in Keygen
                var keygenProductId = await _keygenService.CreateProductAsync(request.Name, request.Code, request.Description);
                if (string.IsNullOrEmpty(keygenProductId))
                {
                    response.IsSuccess = false;
                    response.Message = "Failed to create product in Keygen API";
                    return StatusCode(502, response);
                }

                // 4. Create product locally
                var product = new Product
                {
                    ProviderProductId = keygenProductId,
                    ProductName = request.Name,
                    Description = request.Description,
                    Code = request.Code,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "Product created successfully";
                response.Data = product;

                _logger.LogInformation("Product {ProductCode} created successfully with ID {ProductId} and KeygenID {KeygenProductId}", product.Code, product.ProductId, keygenProductId);

                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical Exception: Error occurred while creating product {ProductCode}", request.Code);
                response.IsSuccess = false;
                response.Message = $"An unexpected error occurred during product creation: {ex.Message}";
                return StatusCode(500, response);
            }
        }

        //// =========================
        //// ✏️ EDIT PRODUCT
        //// =========================
        [HttpPut("{id}")]
        public async Task<ActionResult<APIResponse<object>>> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            var response = new APIResponse<object>();

            try
            {
                var product = await _context.Products.FindAsync(id);

                if (product == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Product not found";
                    return NotFound(response);
                }

                // 1. Update in Keygen
                if (!string.IsNullOrEmpty(product.ProviderProductId))
                {
                    var keygenSuccess = await _keygenService.UpdateProductAsync(product.ProviderProductId, request.Name, request.Code, request.Description);
                    if (!keygenSuccess)
                    {
                        response.IsSuccess = false;
                        response.Message = "Failed to update product in Keygen API";
                        return StatusCode(502, response);
                    }
                }

                // 2. Update locally
                product.ProductName = request.Name;
                product.Description = request.Description;
                product.Code = request.Code;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "Product updated successfully";

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical Exception: Error updating product {Id}", id);
                response.IsSuccess = false;
                response.Message = $"An unexpected error occurred during product update: {ex.Message}";
                return StatusCode(500, response);
            }
        }

        //// =========================
        //// 🗑️ DELETE PRODUCT
        //// =========================
        [HttpDelete("{id}")]
        public async Task<ActionResult<APIResponse<object>>> DeleteProduct(int id)
        {
            var response = new APIResponse<object>();

            try
            {
                var product = await _context.Products.FindAsync(id);

                if (product == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Product not found";
                    return NotFound(response);
                }

                // 1. Delete from Keygen
                if (!string.IsNullOrEmpty(product.ProviderProductId))
                {
                    var keygenSuccess = await _keygenService.DeleteProductAsync(product.ProviderProductId);
                    if (!keygenSuccess)
                    {
                        _logger.LogWarning("Failed to delete product {KeygenProductId} in Keygen. Proceeding with local deletion.", product.ProviderProductId);
                    }
                }

                // 2. Delete locally
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "Product deleted successfully";

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical Exception: Error deleting product {Id}", id);
                response.IsSuccess = false;
                response.Message = $"An unexpected error occurred during product deletion: {ex.Message}";
                return StatusCode(500, response);
            }
        }

        //// =========================
        //// 📋 LIST ALL PRODUCTS FROM KEYGEN
        //// =========================
        [HttpGet("keygen")]
        public async Task<ActionResult<APIResponse<PagedResult<IEnumerable<KeygenProductDto>>>>> GetKeygenProducts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, string? searchKey = null)
        {
            var response = new APIResponse<PagedResult<IEnumerable<KeygenProductDto>>>();
            try
            {
                //if (string.IsNullOrEmpty(accountId))
                //    return BadRequest("Account ID is required");

                var (products, meta) = await _keygenService.GetProductsAsync(
                    pageNumber, pageSize, searchKey);

                if (products.Any())
                {
                    response.IsSuccess = true;
                    response.Message = "Products fetched from Keygen";
                    response.Data = new PagedResult<IEnumerable<KeygenProductDto>>
                    {
                        Data = products,
                        Meta = meta
                    };
                    return Ok(response);
                }

                //// 2. Fallback to database
                var dbResult = await _dataService.GetProductsAsync(pageNumber, pageSize, searchKey);

                response.IsSuccess = true;
                response.Message = "Licenses fetched from database";
                response.Data = new PagedResult<IEnumerable<KeygenProductDto>>
                {
                    Data = dbResult.Products,
                    Meta = new KeygenMeta
                    {
                        Total = dbResult.TotalCount,
                        Count = dbResult.Products.Count(),
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
                _logger.LogError(ex, "Exception: Error occurred while fetching Keygen products list");
                response.IsSuccess = false;
                response.Message = "An error occurred while fetching Keygen products.";
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
