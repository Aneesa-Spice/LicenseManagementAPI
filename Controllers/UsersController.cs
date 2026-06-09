using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LicensingAPI.Models.Users;
using LicensingAPI.Models.Auth;
using Microsoft.Extensions.Logging;
using LicensingAPI.Models;


namespace LicensingAPI.Controllers
{
   // [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UsersController> _logger;

        public UsersController(UserManager<ApplicationUser> userManager, ILogger<UsersController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        ///// Get the users list
        [HttpGet]
        public async Task<ActionResult<APIResponse<IEnumerable<UserInfo>>>> GetUsers()
        {
            var response = new APIResponse<IEnumerable<UserInfo>>();
            try
            {
                var users = await _userManager.Users.ToListAsync();
                var result = new List<UserInfo>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);

                    result.Add(new UserInfo
                    {
                        Id = user.Id,
                        FullName = user.FullName!,
                        Email = user.Email!,
                        IsActive = true, // later connect real field
                      //  KeygenId = user.SerialKey,
                        Role = roles.FirstOrDefault()
                    });
                }

                response.IsSuccess = true;
                response.Message = "Users fetched successfully";
                response.Data = result;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error occurred while fetching users list");
                response.IsSuccess = false;
                response.Message = "An error occurred while fetching users.";
                return StatusCode(500, response);
            }
        }

        ///// Get user by id
        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse<UserInfo>>> GetUserById(string id)
        {
            var response = new APIResponse<UserInfo>();
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    response.IsSuccess = false;
                    response.Message = "User not found";
                    return NotFound(response);
                }

                var roles = await _userManager.GetRolesAsync(user);

                response.IsSuccess = true;
                response.Message = "User fetched successfully";
                response.Data = new UserInfo
                {
                    Id = user.Id,
                    FullName = user.FullName!,
                    Email = user.Email!,
                    IsActive = true,
                   // KeygenId = user.SerialKey,
                    Role = roles.FirstOrDefault()
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error occurred while fetching user {Id}", id);
                response.IsSuccess = false;
                response.Message = "An error occurred while fetching user.";
                return StatusCode(500, response);
            }
        }

        ///// Create new user
        [HttpPost("create")]
        public async Task<ActionResult<APIResponse<UserInfo>>> CreateUser([FromBody] CreateUserRequest request)
        {
            var response = new APIResponse<UserInfo>();
            try
            {
                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FullName = request.FullName,
                   // SerialKey = request.SerialKey
                };

                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    response.IsSuccess = false;
                    response.Message = "Failed to create user";
                    response.Errors = result.Errors.Select(e => new ValidationError { Error = e.Description }).ToList();
                    return BadRequest(response);
                }

                if (!string.IsNullOrEmpty(request.Role))
                {
                    await _userManager.AddToRoleAsync(user, request.Role);
                }

                response.IsSuccess = true;
                response.Message = "User created successfully";
                response.Data = new UserInfo
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    IsActive = true,
                   // KeygenId = user.SerialKey,
                    Role = request.Role
                };

                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error occurred while creating user {Email}", request.Email);
                response.IsSuccess = false;
                response.Message = "An error occurred while creating user.";
                return StatusCode(500, response);
            }
        }

        ///// Update user
        [HttpPut("{id}")]
        public async Task<ActionResult<APIResponse<object>>> UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
            var response = new APIResponse<object>();
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    response.IsSuccess = false;
                    response.Message = "User not found";
                    return NotFound(response);
                }

                user.FullName = request.FullName;
                user.Email = request.Email;
                user.UserName = request.Email;
               // user.SerialKey = request.SerialKey;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    response.IsSuccess = false;
                    response.Message = "Failed to update user";
                    response.Errors = result.Errors.Select(e => new ValidationError { Error = e.Description }).ToList();
                    return BadRequest(response);
                }

                response.IsSuccess = true;
                response.Message = "User updated successfully";
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error occurred while updating user {Id}", id);
                response.IsSuccess = false;
                response.Message = "An error occurred while updating user.";
                return StatusCode(500, response);
            }
        }

        ///// Delete user
        [HttpDelete("{id}")]
        public async Task<ActionResult<APIResponse<object>>> DeleteUser(string id)
        {
            var response = new APIResponse<object>();
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    response.IsSuccess = false;
                    response.Message = "User not found";
                    return NotFound(response);
                }

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    response.IsSuccess = false;
                    response.Message = "Failed to delete user";
                    response.Errors = result.Errors.Select(e => new ValidationError { Error = e.Description }).ToList();
                    return BadRequest(response);
                }

                response.IsSuccess = true;
                response.Message = "User deleted successfully";
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error occurred while deleting user {Id}", id);
                response.IsSuccess = false;
                response.Message = "An error occurred while deleting user.";
                return StatusCode(500, response);
            }
        }

        ///// Assign role to user
        [HttpPost("assign-role")]
        public async Task<ActionResult<APIResponse<string>>> AssignRole(string userId, string role)
        {
            var response = new APIResponse<string>();
            try
            {
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    response.IsSuccess = false;
                    response.Message = "User not found";
                    return NotFound(response);
                }

                var currentRoles = await _userManager.GetRolesAsync(user);

                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    _logger.LogWarning("Failed to remove existing roles for user {UserId}: {Errors}", userId, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                }

                var addResult = await _userManager.AddToRoleAsync(user, role);
                if (!addResult.Succeeded)
                {
                    response.IsSuccess = false;
                    response.Message = "Failed to assign role";
                    response.Errors = addResult.Errors.Select(e => new ValidationError { Error = e.Description }).ToList();
                    return BadRequest(response);
                }

                _logger.LogInformation("Role {Role} assigned to user {UserId} successfully", role, userId);

                response.IsSuccess = true;
                response.Message = "Role updated successfully";
                response.Data = "Role updated";
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: Error occurred while assigning role {Role} to user {UserId}", role, userId);
                response.IsSuccess = false;
                response.Message = "An error occurred while updating the role.";
                return StatusCode(500, response);
            }
        }
    }

    public class AssignRoleRequest
    {
        public required string Role { get; set; }
    }
}
