using LicensingAPI.Models;
using LicensingAPI.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LicensingAPI.Controllers
{
    //[Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<RolesController> _logger;

        public RolesController(RoleManager<IdentityRole> roleManager, ILogger<RolesController> logger)
        {
            _roleManager = roleManager;
            _logger = logger;
        }

        // =========================
        // 📋 LIST ALL ROLES
        // =========================
        [HttpGet]
        public async Task<ActionResult<APIResponse<IEnumerable<RoleDTO>>>> GetRoleslist()
        {
            var response = new APIResponse<IEnumerable<RoleDTO>>();
            try
            {
                var roles = await _roleManager.Roles.ToListAsync();
                response.IsSuccess = true;
                response.Message = "Roles fetched successfully";
                response.Data = roles.Select(r => new RoleDTO { Id = r.Id, Name = r.Name! });
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error occurred while fetching roles list");
                response.IsSuccess = false;
                response.Message = "An error occurred while fetching roles.";
                return StatusCode(500, response);
            }
        }

        // =========================
        // 🔍 GET ROLE BY ID
        // =========================
        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse<RoleDTO>>> GetRolesById(string id)
        {
            var response = new APIResponse<RoleDTO>();
            try
            {
                var role = await _roleManager.FindByIdAsync(id);
                if (role == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Role not found";
                    return NotFound(response);
                }

                response.IsSuccess = true;
                response.Message = "Role retrieved successfully";
                response.Data = new RoleDTO { Id = role.Id, Name = role.Name! };
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error fetching role {Id}", id);
                response.IsSuccess = false;
                response.Message = "An error occurred while fetching the role.";
                return StatusCode(500, response);
            }
        }

        // =========================
        // ➕ CREATE ROLE
        // =========================
        [HttpPost]
        public async Task<ActionResult<APIResponse<RoleDTO>>> PostRoles([FromBody] RoleRequest request)
        {
            var response = new APIResponse<RoleDTO>();
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    response.IsSuccess = false;
                    response.Message = "Role name is required";
                    return BadRequest(response);
                }

                var exists = await _roleManager.RoleExistsAsync(request.Name);
                if (exists)
                {
                    response.IsSuccess = false;
                    response.Message = "Role already exists";
                    return BadRequest(response);
                }

                var result = await _roleManager.CreateAsync(new IdentityRole(request.Name));
                if (!result.Succeeded)
                {
                    response.IsSuccess = false;
                    response.Message = "Failed to create role";
                    response.Errors = result.Errors.Select(e => new ValidationError { Error = e.Description }).ToList();
                    return BadRequest(response);
                }

                var role = await _roleManager.FindByNameAsync(request.Name);
                response.IsSuccess = true;
                response.Message = "Role created successfully";
                response.Data = new RoleDTO { Id = role!.Id, Name = role.Name! };
                _logger.LogInformation("Role {RoleName} created successfully", request.Name);
                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error occurred while creating role {RoleName}", request.Name);
                response.IsSuccess = false;
                response.Message = "An error occurred while creating the role.";
                return StatusCode(500, response);
            }
        }

        // =========================
        // ✏️ UPDATE ROLE
        // =========================
        [HttpPut("{id}")]
        public async Task<ActionResult<APIResponse<RoleDTO>>> PutRoles(string id, [FromBody] RoleRequest request)
        {
            var response = new APIResponse<RoleDTO>();
            try
            {
                var role = await _roleManager.FindByIdAsync(id);
                if (role == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Role not found";
                    return NotFound(response);
                }

                role.Name = request.Name;
                var result = await _roleManager.UpdateAsync(role);
                if (!result.Succeeded)
                {
                    response.IsSuccess = false;
                    response.Message = "Failed to update role";
                    response.Errors = result.Errors.Select(e => new ValidationError { Error = e.Description }).ToList();
                    return BadRequest(response);
                }

                response.IsSuccess = true;
                response.Message = "Role updated successfully";
                response.Data = new RoleDTO { Id = role.Id, Name = role.Name! };
                _logger.LogInformation("Role {Id} updated to {RoleName} successfully", id, request.Name);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error occurred while updating role {Id}", id);
                response.IsSuccess = false;
                response.Message = "An error occurred while updating the role.";
                return StatusCode(500, response);
            }
        }

        // =========================
        // 🗑️ DELETE ROLE
        // =========================
        [HttpDelete("{id}")]
        public async Task<ActionResult<APIResponse<object>>> DeleteRoles(string id)
        {
            var response = new APIResponse<object>();
            try
            {
                var role = await _roleManager.FindByIdAsync(id);
                if (role == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Role not found";
                    return NotFound(response);
                }

                var result = await _roleManager.DeleteAsync(role);
                if (!result.Succeeded)
                {
                    response.IsSuccess = false;
                    response.Message = "Failed to delete role";
                    response.Errors = result.Errors.Select(e => new ValidationError { Error = e.Description }).ToList();
                    return BadRequest(response);
                }

                response.IsSuccess = true;
                response.Message = "Role deleted successfully";
                _logger.LogInformation("Role {Id} deleted successfully", id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error occurred while deleting role {Id}", id);
                response.IsSuccess = false;
                response.Message = "An error occurred while deleting the role.";
                return StatusCode(500, response);
            }
        }
        // =========================
        // 🔒 GET ROLE PERMISSIONS
        // =========================
        [HttpGet("permissions/{id}")]
        public async Task<ActionResult<APIResponse<RolePermissionsDTO>>> GetPermissions(string id)
        {
            var response = new APIResponse<RolePermissionsDTO>();
            try
            {
                var role = await _roleManager.FindByIdAsync(id);
                if (role == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Role not found";
                    return NotFound(response);
                }

                var claims = await _roleManager.GetClaimsAsync(role);
                var permissions = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList();

                response.IsSuccess = true;
                response.Message = "Permissions fetched successfully";
                response.Data = new RolePermissionsDTO { RoleId = id, Permissions = permissions };
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error fetching permissions for role {Id}", id);
                response.IsSuccess = false;
                response.Message = "An error occurred while fetching permissions.";
                return StatusCode(500, response);
            }
        }

        // =========================
        // 🔒 UPDATE ROLE PERMISSIONS
        // =========================
        [HttpPost("permissions")]
        public async Task<ActionResult<APIResponse<object>>> UpdatePermissions([FromBody] UpdateRolePermissionsRequest request)
        {
            var response = new APIResponse<object>();
            try
            {
                var role = await _roleManager.FindByIdAsync(request.RoleId);
                if (role == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Role not found";
                    return NotFound(response);
                }

                var currentClaims = await _roleManager.GetClaimsAsync(role);
                var permissionClaims = currentClaims.Where(c => c.Type == "Permission").ToList();

                foreach (var claim in permissionClaims)
                {
                    await _roleManager.RemoveClaimAsync(role, claim);
                }

                foreach (var permission in request.Permissions)
                {
                    await _roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("Permission", permission));
                }

                response.IsSuccess = true;
                response.Message = "Permissions updated successfully";
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error updating permissions for role {Id}", request.RoleId);
                response.IsSuccess = false;
                response.Message = "An error occurred while updating permissions.";
                return StatusCode(500, response);
            }
        }
    }
}
