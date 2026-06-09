using LicensingAPI.Data;
using LicensingAPI.Models;
using LicensingAPI.Models.AppSettings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LicensingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppsettingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AppsettingController> _logger;

        public AppsettingController(ApplicationDbContext context, ILogger<AppsettingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // =========================
        // 📋 LIST ALL APP SETTINGS
        // =========================
        [HttpGet]
        public async Task<ActionResult<APIResponse<IEnumerable<AppSettingDTO>>>> GetAppSettings()
        {
            var response = new APIResponse<IEnumerable<AppSettingDTO>>();
            try
            {
                var settings = await _context.AppSettings
                    .OrderBy(s => s.Key)
                    .ToListAsync();

                response.IsSuccess = true;
                response.Message = "App settings fetched successfully";
                response.Data = settings.Select(s => new AppSettingDTO
                {
                    Id = s.Id,
                    Key = s.Key,
                    Value = s.Value,
                    Description = s.Description,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                });
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error occurred while fetching app settings");
                response.IsSuccess = false;
                response.Message = "An error occurred while fetching app settings.";
                response.Details = ex.Message;
                return StatusCode(500, response);
            }
        }

        // =========================
        // 🔍 GET APP SETTING BY ID
        // =========================
        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse<AppSettingDTO>>> GetAppSetting(int id)
        {
            var response = new APIResponse<AppSettingDTO>();
            try
            {
                var setting = await _context.AppSettings.FindAsync(id);
                if (setting == null)
                {
                    response.IsSuccess = false;
                    response.Message = "App setting not found";
                    return NotFound(response);
                }

                response.IsSuccess = true;
                response.Message = "App setting retrieved successfully";
                response.Data = new AppSettingDTO
                {
                    Id = setting.Id,
                    Key = setting.Key,
                    Value = setting.Value,
                    Description = setting.Description,
                    CreatedAt = setting.CreatedAt,
                    UpdatedAt = setting.UpdatedAt
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error fetching app setting {Id}", id);
                response.IsSuccess = false;
                response.Message = "An error occurred while fetching the app setting.";
                return StatusCode(500, response);
            }
        }

        // =========================
        // ➕ CREATE APP SETTING
        // =========================
        [HttpPost]
        public async Task<ActionResult<APIResponse<AppSettingDTO>>> CreateAppSetting([FromBody] AppSettingRequest request)
        {
            var response = new APIResponse<AppSettingDTO>();
            try
            {
                if (string.IsNullOrWhiteSpace(request.Key))
                {
                    response.IsSuccess = false;
                    response.Message = "Setting key is required";
                    return BadRequest(response);
                }

                var exists = await _context.AppSettings.AnyAsync(s => s.Key == request.Key);
                if (exists)
                {
                    response.IsSuccess = false;
                    response.Message = $"App setting with key '{request.Key}' already exists";
                    return BadRequest(response);
                }

                var setting = new AppSetting
                {
                    Key = request.Key,
                    Value = request.Value,
                    Description = request.Description,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AppSettings.Add(setting);
                await _context.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "App setting created successfully";
                response.Data = new AppSettingDTO
                {
                    Id = setting.Id,
                    Key = setting.Key,
                    Value = setting.Value,
                    Description = setting.Description,
                    CreatedAt = setting.CreatedAt
                };

                return CreatedAtAction(nameof(GetAppSetting), new { id = setting.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error occurred while creating app setting {Key}", request.Key);
                response.IsSuccess = false;
                response.Message = "An error occurred while creating the app setting.";
                return StatusCode(500, response);
            }
        }

        // =========================
        // ✏️ UPDATE APP SETTING
        // =========================
        [HttpPut("{id}")]
        public async Task<ActionResult<APIResponse<AppSettingDTO>>> UpdateAppSetting(int id, [FromBody] AppSettingRequest request)
        {
            var response = new APIResponse<AppSettingDTO>();
            try
            {
                var setting = await _context.AppSettings.FindAsync(id);
                if (setting == null)
                {
                    response.IsSuccess = false;
                    response.Message = "App setting not found";
                    return NotFound(response);
                }

                // Check if key is being changed and if new key already exists
                if (setting.Key != request.Key)
                {
                    var exists = await _context.AppSettings.AnyAsync(s => s.Key == request.Key && s.Id != id);
                    if (exists)
                    {
                        response.IsSuccess = false;
                        response.Message = $"Another app setting with key '{request.Key}' already exists";
                        return BadRequest(response);
                    }
                }

                setting.Key = request.Key;
                setting.Value = request.Value;
                setting.Description = request.Description;
                setting.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "App setting updated successfully";
                response.Data = new AppSettingDTO
                {
                    Id = setting.Id,
                    Key = setting.Key,
                    Value = setting.Value,
                    Description = setting.Description,
                    CreatedAt = setting.CreatedAt,
                    UpdatedAt = setting.UpdatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error occurred while updating app setting {Id}", id);
                response.IsSuccess = false;
                response.Message = "An error occurred while updating the app setting.";
                return StatusCode(500, response);
            }
        }

        // =========================
        // 🗑️ DELETE APP SETTING
        // =========================
        [HttpDelete("{id}")]
        public async Task<ActionResult<APIResponse<object>>> DeleteAppSetting(int id)
        {
            var response = new APIResponse<object>();
            try
            {
                var setting = await _context.AppSettings.FindAsync(id);
                if (setting == null)
                {
                    response.IsSuccess = false;
                    response.Message = "App setting not found";
                    return NotFound(response);
                }

                _context.AppSettings.Remove(setting);
                await _context.SaveChangesAsync();

                response.IsSuccess = true;
                response.Message = "App setting deleted successfully";
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error occurred while deleting app setting {Id}", id);
                response.IsSuccess = false;
                response.Message = "An error occurred while deleting the app setting.";
                return StatusCode(500, response);
            }
        }

        // =========================
        // 🔑 GET VALUE BY KEY
        // =========================
        [HttpGet("key/{key}")]
        public async Task<ActionResult<APIResponse<string>>> GetValueByKey(string key)
        {
            var response = new APIResponse<string>();
            try
            {
                var setting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
                if (setting == null)
                {
                    response.IsSuccess = false;
                    response.Message = $"App setting with key '{key}' not found";
                    return NotFound(response);
                }

                response.IsSuccess = true;
                response.Message = "App setting value retrieved successfully";
                response.Data = setting.Value;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error fetching value for key {Key}", key);
                response.IsSuccess = false;
                response.Message = "An error occurred while fetching the app setting value.";
                return StatusCode(500, response);
            }
        }
    }
}
