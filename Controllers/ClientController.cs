using LicensingAPI.Data;
using LicensingAPI.Models;
using LicensingAPI.Models.Activations;
using LicensingAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LicensingAPI.Controllers
{
    [ApiController]
    [Route("api/client")]
    public class ClientController : ControllerBase
    {
        private readonly KeygenService _keygenService;
        private readonly ILogger<ClientController> _logger;
        private readonly ApplicationDbContext _context;
        public ClientController(KeygenService keygenService, ILogger<ClientController> logger, ApplicationDbContext context)
        {
            _keygenService = keygenService;
            _logger = logger;
            _context = context;
        }

        [HttpPost("license/activate")]
        public async Task<ActionResult<APIResponse<ClientLicenseResponse>>> ActivateLicense([FromBody] ActivateRequest request)
        {
            var response = new APIResponse<ClientLicenseResponse>();
            try
            {
                _logger.LogInformation("Client activation request received for key/username: {Key}", request.LicenseKey);

                bool isEmail = StringTypeDetector.IsEmail(request.LicenseKey);

                if (isEmail)
                {
                    var userLicense = _context.UserLicenses
                        .Where(x => x.UserEmail == request.LicenseKey && !x.IsDeleted)
                        .FirstOrDefault();

                    if (userLicense == null)
                        return BadRequest(new { message = "No license found for this email." });

                    var license = _context.Licenses
                        .Where(l => l.ProviderLicenseId == userLicense.ProviderLicenseId)
                        .FirstOrDefault();

                    if (license == null)
                        return BadRequest(new { message = "License record not found." });

                    request.LicenseKey = license.LicenseKey;
                }


                string actualLicenseKey = request.LicenseKey;

                // 1. Validate the key first to get the License ID
                var licenseInfo = await _keygenService.ValidateLicenseDetailsAsync(actualLicenseKey);
                if (!licenseInfo.Success || string.IsNullOrEmpty(licenseInfo.LicenseId))
                {
                    _logger.LogWarning("Activation failed: Invalid license key {Key}", request.LicenseKey);
                    response.IsSuccess = false;
                    response.Message = "Invalid license key.";
                    return BadRequest(response);
                }
               
                // 2. Activate the machine
                var (success, machineId, message) = await _keygenService.ActivateMachineAsync(
                    licenseInfo.LicenseId, 
                    request.MachineFingerprint, 
                    request.MachineName, 
                    request.MachinePlatform ?? "windows"
                );

                if (!success)
                {
                    _logger.LogWarning("Activation failed for key {Key}: {Message}", request.LicenseKey, message);
                    response.IsSuccess = false;
                    response.Message = message ?? "Machine activation failed.";
                    return BadRequest(response);
                }

                // 3. Return success with machine ID as token
                response.IsSuccess = true;
                response.Message = "License activated successfully.";
                response.Data = new ClientLicenseResponse
                {
                    IsValid = true,
                    Status = "Activated",
                    Message = "License activated successfully.",
                    Token = machineId,
                    ExpiryDate = licenseInfo.Expiry
                };

                try
                {
                    ////Saving to DB :Save the values to DB activation table
                    Activation activation = new Activation();
                    var license = await _context.Licenses.Where(x=>x.LicenseKey == actualLicenseKey).FirstOrDefaultAsync();
                    var userlicense = await _context.UserLicenses.Where(x => x.ProviderLicenseId == license.ProviderLicenseId).FirstOrDefaultAsync();
                    activation.UserLicenseId = userlicense.UserLicenseId;
                    activation.LicenseId = license?.LicenseId ?? 0;
                    activation.UserEmail = userlicense.UserEmail;
                    activation.ProviderMachineId = machineId;
                    activation.MachineFingerprint = request.MachineFingerprint;
                    activation.MachineName = request.MachineName;
                    activation.Status = response.Data.Status;
                    activation.IsValid = response.Data.IsValid;
                    activation.IsDeleted = false;
                    activation.ActivatedAt = DateTime.UtcNow;
                    // 3. Save to database
                    _context.Activations.Add(activation);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error while saving Activation {ex.Message}");
                }
               

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating license for {Key}", request.LicenseKey);
                response.IsSuccess = false;
                response.Message = "An internal error occurred.";
                return StatusCode(500, response);
            }
        }

        [HttpPost("license/validate")]
        public async Task<ActionResult<APIResponse<ClientLicenseResponse>>> ValidateLicense([FromBody] ValidateLicenseRequest request)
        {
            var response = new APIResponse<ClientLicenseResponse>();
            try
            {
                _logger.LogInformation("Client validation request received for key/username: {Key}", request.LicenseKey);
                //// Resolve email to license key
                bool isEmail = StringTypeDetector.IsEmail(request.LicenseKey);
                if (isEmail)
                {
                    var userLicense = _context.UserLicenses
                        .Where(x => x.UserEmail == request.LicenseKey && !x.IsDeleted)
                        .FirstOrDefault();

                    if (userLicense == null)
                        return BadRequest(new { message = "No license found for this email." });

                    var license = _context.Licenses
                        .Where(l => l.ProviderLicenseId == userLicense.ProviderLicenseId)
                        .FirstOrDefault();

                    if (license == null)
                        return BadRequest(new { message = "License record not found." });

                    request.LicenseKey = license.LicenseKey;
                }
                else
                {
                    //// Check key is assigned
                    var isAssigned = _context.UserLicenses
                        .Any(x => x.License.LicenseKey == request.LicenseKey && !x.IsDeleted);

                    if (!isAssigned)
                        return BadRequest(new { message = "This license key is not assigned to any user." });
                }

                //// Detect offline or online
                bool isOffline = request.LicenseKey.StartsWith("key/");

                if (isOffline)
                {
                    var setting = await _keygenService.GetSettingsAsync();
                    var result = new OfflineLicenseValidator(setting.PublicKey)
                        .Validate(request.LicenseKey);

                    if (!result.IsValid)
                    {
                        response.IsSuccess = false;
                        response.Message = result.Message;
                        return BadRequest(response);
                    }

                    response.IsSuccess = true;
                    response.Message = "License is valid.";
                    response.Data = new ClientLicenseResponse
                    {
                        IsValid = true,
                        Status = "Valid",
                        Message = "License is valid.",
                        ExpiryDate = result.Payload?.Expiry
                    };
                }
                else
                {
                    var result = await _keygenService.ValidateLicenseDetailsAsync(
                        request.LicenseKey, request.MachineFingerprint);

                    if (result.HasErrors)
                    {
                        response.IsSuccess = false;
                        response.Message = ExtractErrorMessage(result.RawResponse);
                        return BadRequest(response);
                    }

                    response.IsSuccess = true;
                    response.Message = "License is valid.";
                    response.Data = new ClientLicenseResponse
                    {
                        IsValid = true,
                        Status = "Valid",
                        Message = "License is valid.",
                        ExpiryDate = result.Data?.Attributes?.Expiry
                    };
                }

                //var result = await _keygenService.ValidateLicenseDetailsAsync(request.LicenseKey, request.MachineFingerprint);

                //// ✅ HasErrors → extract detail from RawResponse
                //if (result.HasErrors)
                //{
                //    response.IsSuccess = false;
                //    response.Message = ExtractErrorMessage(result.RawResponse);
                //    return BadRequest(response);
                //}

                //response.IsSuccess = true;
                //response.Message = "License is valid.";
                //response.Data = new ClientLicenseResponse
                //{
                //    IsValid = true,
                //    Status = "Valid",
                //    Message = "License is valid.",
                //    ExpiryDate = result.Data?.Attributes?.Expiry
                //};

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating license for {Key}", request.LicenseKey);
                response.IsSuccess = false;
                response.Message = "An internal error occurred.";
                return StatusCode(500, response);
            }
        }

        private string ExtractErrorMessage(string jsonResponse)
        {
            try
            {
                var responseObject = JsonDocument.Parse(jsonResponse).RootElement;

                if (responseObject.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0)
                    return errors[0].GetProperty("detail").GetString() ?? "An unexpected error occurred.";
            }
            catch (JsonException) { }

            return "An unexpected error occurred.";
        }

        [HttpGet("license/server-time")]
        public async Task<ActionResult<APIResponse<DateTime>>> GetServerTime()
        {
            return Ok(new APIResponse<DateTime>
            {
                IsSuccess = true,
                Data = DateTime.UtcNow
            });
        }
    }
}
