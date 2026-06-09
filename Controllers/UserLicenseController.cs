using LicensingAPI.Data;
using LicensingAPI.Models;
using LicensingAPI.Models.Licenses;
using LicensingAPI.Models.Users;
using LicensingAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicensingAPI.Controllers
{
    [ApiController]
    [Route("api/user-licenses")]
    public class UserLicenseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserLicenseController> _logger;
        private readonly KeygenService _keygenService;

        public UserLicenseController(ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            ILogger<UserLicenseController> logger,
            KeygenService keygenService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _keygenService = keygenService;
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse<IEnumerable<UserLicenseDTO>>>> GetUserLicenses()
        {
            var response = new APIResponse<IEnumerable<UserLicenseDTO>>();
            try
            {
                //var mappings = await _context.UserLicenses
                //    .Include(ul => ul.User)
                //    .Include(ul => ul.Product)
                //    .Select(ul => new UserLicenseDTO
                //    {
                //        RecordId = ul.UserLicenseId,
                //        UserId = ul.UserId ?? string.Empty,
                //        UserEmail = ul.User != null ? ul.User.Email : string.Empty,
                //        LicenseId = ul.UserLicenseId,
                //        LicenseKey = ul.LicenseKey,
                //        ProductName = ul.Product != null ? ul.Product.ProductName : "N/A",
                //        CreatedAt = ul.CreatedAt
                //    })
                //    .ToListAsync();

                response.IsSuccess = true;
                response.Message = "User-License mappings fetched successfully";
               // response.Data = mappings;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user-license mappings");
                response.IsSuccess = false;
                response.Message = "An error occurred while fetching mappings";
                return StatusCode(500, response);
            }
        }

        [HttpGet("company/{companyId}")]
        public async Task<ActionResult<APIResponse<IEnumerable<UserLicenseDTO>>>> GetCompanyUserLicenses(int companyId)
        {
            var response = new APIResponse<IEnumerable<UserLicenseDTO>>();
            try
            {
                //var mappings = await _context.UserLicenses
                //    .Include(ul => ul.User)
                //    .Include(ul => ul.Product)
                //    .Where(ul => ul.CompanyId == companyId)
                //    .Select(ul => new UserLicenseDTO
                //    {
                //        RecordId = ul.UserLicenseId,
                //        UserId = ul.UserId ?? string.Empty,
                //        UserEmail = ul.User != null ? ul.User.Email : string.Empty,
                //        LicenseId = ul.UserLicenseId,
                //        LicenseKey = ul.LicenseKey,
                //        ProductName = ul.Product != null ? ul.Product.ProductName : "N/A",
                //        CreatedAt = ul.CreatedAt
                //    })
                //    .ToListAsync();

                response.IsSuccess = true;
                response.Message = "Company user-license mappings fetched successfully";
               // response.Data = mappings;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user-license mappings for company {CompanyId}", companyId);
                response.IsSuccess = false;
                response.Message = "An error occurred while fetching mappings";
                return StatusCode(500, response);
            }
        }

        [HttpPost("map")]
        public async Task<ActionResult<APIResponse<UserLicense>>> MapUserLicense([FromBody] MapUserLicenseRequest request)
        {
            var response = new APIResponse<UserLicense>();
            //try
            //{
            //    // 1. Validate User
            //    var user = await _userManager.FindByIdAsync(request.UserId);
            //    if (user == null)
            //    {
            //        response.IsSuccess = false;
            //        response.Message = "User not found";
            //        return NotFound(response);
            //    }

            //    //// 2. Validate License
            //    //var userLicense = await _context.UserLicenses
            //    //    .Include(ul => ul.Company)
            //    //    .FirstOrDefaultAsync(ul => ul.UserLicenseId == request.LicenseId);

            //    //if (userLicense == null)
            //    //{
            //    //    response.IsSuccess = false;
            //    //    response.Message = "Allocated license slot not found";
            //    //    return NotFound(response);
            //    //}

            //    //if (!string.IsNullOrEmpty(userLicense.UserId) && userLicense.UserId != request.UserId)
            //    //{
            //    //    response.IsSuccess = false;
            //    //    response.Message = "This license is already mapped to another user";
            //    //    return BadRequest(response);
            //    //}

            //    // 4. Keygen Sync
            //    var accountId = await _context.AppSettings
            //        .Where(a => a.Key == "ACCOUNT_ID")
            //        .Select(a => a.Value)
            //        .FirstOrDefaultAsync() ?? string.Empty;

            //    //if (string.IsNullOrEmpty(user.KeygenUserId))
            //    //{
            //    //    var keygenUserId = await _keygenService.GetOrCreateUserAsync(accountId, user.Email, user.FullName);
            //    //    if (!string.IsNullOrEmpty(keygenUserId))
            //    //    {
            //    //        user.KeygenUserId = keygenUserId;
            //    //        await _userManager.UpdateAsync(user);
            //    //    }
            //    //}

            //    if (!string.IsNullOrEmpty(user.KeygenUserId) && !string.IsNullOrEmpty(userLicense.KeygenLicenseId))
            //    {
            //        var synced = await _keygenService.AttachUserToLicenseAsync(accountId, userLicense.KeygenLicenseId, user.KeygenUserId);
            //        if (!synced)
            //        {
            //            _logger.LogWarning("Local mapping updated but failed to sync with Keygen for User {UserId} and License {LicenseId}", user.Id, userLicense.UserLicenseId);
            //        }
            //    }

            //    // 5. Update mapping
            //    userLicense.UserId = request.UserId;
            //    userLicense.Status = "assigned";
            //    userLicense.UpdatedAt = DateTime.UtcNow;

            //    _context.UserLicenses.Update(userLicense);
            //    await _context.SaveChangesAsync();

            //    response.IsSuccess = true;
            //    response.Message = "User successfully mapped to license" + (string.IsNullOrEmpty(user.KeygenUserId) ? " (Local only)" : " and synced with Keygen");
            //    response.Data = userLicense;
            //    return StatusCode(200, response);
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Error mapping user {UserId} to license {LicenseId}", request.UserId, request.LicenseId);
            //    response.IsSuccess = false;
            //    response.Message = "An error occurred during mapping";
                return StatusCode(500, response);
            //}
        }

        [HttpDelete("unmap")]
        public async Task<ActionResult<APIResponse<object>>> UnmapUserLicense([FromQuery] string userId, [FromQuery] int licenseId)
        {
            var response = new APIResponse<object>();
            //try
            //{
            //    var userLicense = await _context.UserLicenses
            //        .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.UserLicenseId == licenseId);

            //    if (userLicense == null)
            //    {
            //        response.IsSuccess = false;
            //        response.Message = "Mapping not found";
            //        return NotFound(response);
            //    }

            //    // Keygen Sync
            //    var user = await _userManager.FindByIdAsync(userId);
            //    var accountId = await _context.AppSettings
            //        .Where(a => a.Key == "ACCOUNT_ID")
            //        .Select(a => a.Value)
            //        .FirstOrDefaultAsync() ?? string.Empty;

            //    if (user != null && !string.IsNullOrEmpty(user.KeygenUserId) && !string.IsNullOrEmpty(userLicense.KeygenLicenseId))
            //    {
            //        await _keygenService.DetachUserFromLicenseAsync(accountId, userLicense.KeygenLicenseId, user.KeygenUserId);
            //    }

            //    // Clear assignment instead of removing row
            //    userLicense.UserId = null;
            //    userLicense.Status = "unassigned";
            //    userLicense.UpdatedAt = DateTime.UtcNow;

            //    _context.UserLicenses.Update(userLicense);
            //    await _context.SaveChangesAsync();

            //    response.IsSuccess = true;
            //    response.Message = "User successfully unmapped from license";
            //    return Ok(response);
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Error unmapping user {UserId} from license {LicenseId}", userId, licenseId);
            //    response.IsSuccess = false;
            //    response.Message = "An error occurred during unmapping";
                return StatusCode(500, response);
            //}
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<APIResponse<IEnumerable<License>>>> GetLicensesForUser(string userId)
        {
            var response = new APIResponse<IEnumerable<License>>();
            try
            {
                //var licenses = await _context.UserLicenses
                //    .Where(ul => ul.UserId == userId)
                //    .Include(ul => ul.Events)
                //    .Select(ul => ul.Events)
                //    .ToListAsync();

                response.IsSuccess = true;
                response.Message = "Licenses fetched for user";
               // response.Data = licenses;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching licenses for user {UserId}", userId);
                response.IsSuccess = false;
                response.Message = "An error occurred while fetching licenses";
                return StatusCode(500, response);
            }
        }

        [HttpGet("license/{licenseId}")]
        public async Task<ActionResult<APIResponse<IEnumerable<ApplicationUser>>>> GetUsersForLicense(int licenseId)
        {
            var response = new APIResponse<IEnumerable<ApplicationUser>>();
            try
            {
                //var users = await _context.UserLicenses
                //    .Where(ul => ul.UserLicenseId == licenseId)
                //    .Include(ul => ul.User)
                //    .Select(ul => ul.User)
                //    .ToListAsync();

                response.IsSuccess = true;
                response.Message = "Users fetched for license";
               // response.Data = users;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users for license {LicenseId}", licenseId);
                response.IsSuccess = false;
                response.Message = "An error occurred while fetching users";
                return StatusCode(500, response);
            }
        }
    }
}
