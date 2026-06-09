using LicensingAPI.Models;
using LicensingAPI.Models.Activations;
using LicensingAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LicensingAPI.Controllers
{
    [ApiController]
    [Route("api/activation")]
    public class ActivationController : ControllerBase
    {
        private readonly KeygenService _keygenService;
        private readonly ILogger<ActivationController> _logger;
        private readonly IConfiguration _configuration;

        public ActivationController(KeygenService keygenService, ILogger<ActivationController> logger, IConfiguration configuration)
        {
            _keygenService = keygenService;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("activate")]
        public async Task<ActionResult<APIResponse<object>>> Activate([FromBody] ActivateRequest request, [FromQuery] string accountId)
        {
            var response = new APIResponse<object>();
            try
            {
                // In Keygen, we first validate the license, then activate the machine.
                // For simplicity, we assume the machine fingerprint is enough to activate if the license is valid.
                
                // We need the internal Keygen ID of the license to activate a machine.
                // This would usually require a lookup if the user only provides the LicenseKey.
                // For this implementation, we'll assume the KeygenService handles finding the license or we look it up here.
                
                // Simplified flow: Use KeygenService to activate machine.
                // Note: We might need to fetch the license ID from the database first.
                
                var (success, machineId, message) = await _keygenService.ActivateMachineAsync(accountId, request.LicenseKey, request.MachineFingerprint, request.MachineName, request.MachinePlatform);

                if (!success)
                {
                    response.IsSuccess = false;
                    response.Message = message ?? "Activation failed";
                    return BadRequest(response);
                }

                response.IsSuccess = true;
                response.Message = "Machine activated successfully";
                response.Data = new { MachineId = machineId };
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during activation");
                response.IsSuccess = false;
                response.Message = "An error occurred during activation";
                return StatusCode(500, response);
            }
        }

        [HttpPost("heartbeat")]
        public async Task<ActionResult<APIResponse<object>>> Heartbeat([FromBody] HeartbeatRequest request)
        {
            var response = new APIResponse<object>();
            // Keygen heartbeat usually involves sending a request to the machine endpoint or a specific ping endpoint.
            // For now, we'll return success as a placeholder if the request is valid.
            response.IsSuccess = true;
            response.Message = "Heartbeat received";
            return Ok(response);
        }

        [HttpPost("deactivate")]
        public async Task<ActionResult<APIResponse<object>>> Deactivate([FromBody] DeactivateRequest request, [FromQuery] string accountId)
        {
            var response = new APIResponse<object>();
            try
            {
                var success = await _keygenService.DeactivateMachineAsync(request.MachineId);
                if (!success)
                {
                    response.IsSuccess = false;
                    response.Message = "Deactivation failed";
                    return BadRequest(response);
                }

                response.IsSuccess = true;
                response.Message = "Machine deactivated successfully";
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during deactivation");
                response.IsSuccess = false;
                response.Message = "An error occurred during deactivation";
                return StatusCode(500, response);
            }
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse<IEnumerable<MachineDto>>>> GetActivations([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchKey = null)
        {
            var response = new APIResponse<IEnumerable<MachineDto>>();
            try
            {
                var (data, meta) = await _keygenService.GetMachinesAsync(pageNumber, pageSize, searchKey);
                response.IsSuccess = true;
                response.Data = data;
                if (meta != null)
                {
                    response.Pagination = new PaginationMetadata
                    {
                        CurrentPage = pageNumber,
                        PageSize = pageSize,
                        TotalCount = meta.Count,
                        TotalPages = meta.Pages ?? 0
                    };
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting machines");
                response.IsSuccess = false;
                response.Message = "An error occurred while fetching activations";
                return StatusCode(500, response);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse<MachineDto>>> GetMachine(string id)
        {
            var response = new APIResponse<MachineDto>();
            try
            {
                var machine = await _keygenService.GetMachineAsync(id);
                if (machine == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Activation not found";
                    return NotFound(response);
                }

                response.IsSuccess = true;
                response.Data = machine;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting machine details");
                response.IsSuccess = false;
                response.Message = "An error occurred while fetching activation details";
                return StatusCode(500, response);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<APIResponse<object>>> DeleteMachine(string id, [FromQuery] string accountId)
        {
            // Note: in keygen, deleting a machine deactivates it.
            // If they are synonymous we can use DeactivateMachineAsync.
            var response = new APIResponse<object>();
            try
            {
                var success = await _keygenService.DeactivateMachineAsync(id);
                if (!success)
                {
                    response.IsSuccess = false;
                    response.Message = "Deletion failed";
                    return BadRequest(response);
                }

                response.IsSuccess = true;
                response.Message = "Machine deleted successfully";
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during deletion");
                response.IsSuccess = false;
                response.Message = "An error occurred during deletion";
                return StatusCode(500, response);
            }
        }
    }
}
