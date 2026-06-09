using LicensingAPI.Models.Users;
using Microsoft.AspNetCore.Identity;
using LicensingAPI.Models.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using LicensingAPI.Models;
using LicensingAPI.Data;
using Microsoft.EntityFrameworkCore;


[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly ApplicationDbContext _context;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _logger = logger;
        _context = context;
    }

    // =========================
    // 🔐 REGISTER
    // =========================
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<APIResponse<string>>> Register([FromBody] RegisterRequest model)
    {
        var response = new APIResponse<string>();
        try
        {
            if (!ModelState.IsValid)
            {
                response.IsSuccess = false;
                response.Message = "Validation failed";
                response.Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => new ValidationError { Error = e.ErrorMessage }).ToList();
                return BadRequest(response);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                response.IsSuccess = false;
                response.Message = "User already exists";
                return BadRequest(response);
            }

            var user = new ApplicationUser
            {
                FullName = model.FullName,
                UserName = model.Email,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                response.IsSuccess = false;
                response.Message = "Registration failed";
                response.Errors = result.Errors.Select(e => new ValidationError { Error = e.Description }).ToList();
                return BadRequest(response);
            }

            _logger.LogInformation("User {Email} registered successfully", model.Email);

            response.IsSuccess = true;
            response.Message = "User registered successfully";
            response.Data = "Success";
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception: Error occurred during user registration for {Email}", model.Email);
            response.IsSuccess = false;
            response.Message = "An error occurred while processing your request.";
            return StatusCode(500, response);
        }
    }

    // =========================
    // 🔐 LOGIN
    // =========================
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<APIResponse<object>>> Login([FromBody] LoginRequest model)
    {
        var response = new APIResponse<object>();
        try
        {
            if (!ModelState.IsValid)
            {
                response.IsSuccess = false;
                response.Message = "Validation failed";
                response.Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => new ValidationError { Error = e.ErrorMessage }).ToList();
                return BadRequest(response);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                response.IsSuccess = false;
                response.Message = "Invalid email or password";
                return Unauthorized(response);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                false,
                false);

            if (!result.Succeeded)
            {
                response.IsSuccess = false;
                response.Message = "Invalid email or password";
                return Unauthorized(response);
            }

            var token = await GenerateJwtToken(user);

            if (token == null)
            {
                response.IsSuccess = false;
                response.Message = "An error occurred while generating authentication token.";
                return StatusCode(500, response);
            }

            _logger.LogInformation("User {Email} logged in successfully", model.Email);

            response.IsSuccess = true;
            response.Message = "Login successful";
            response.Data = new LoginResponseDto
            {
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    //KeygenId = user.SerialKey,
                    //Roles = user.roles
                }
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception: Error occurred during login for {Email}", model.Email);
            response.IsSuccess = false;
            response.Message = "An error occurred while processing your request.";
            return StatusCode(500, response);
        }
    }


    // =========================
    // 🔐 INFO
    // =========================
    [Authorize]
    [HttpGet("info")]
    public ActionResult<APIResponse<UserInfo>> GetCurrentUser()
    {
        var response = new APIResponse<UserInfo>();
        try
        {
            // 🔐 Get user ID from JWT token
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                response.IsSuccess = false;
                response.Message = "Unauthorized";
                return Unauthorized(response);
            }

            // 📦 Return DTO
            var result = new UserInfo
            {
                FullName = "UserName",
                Email = "UserEmail"
            };

            response.IsSuccess = true;
            response.Message = "User info fetched successfully";
            response.Data = result;
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception: Error occurred while getting current user info");
            response.IsSuccess = false;
            response.Message = "An error occurred while processing your request.";
            return StatusCode(500, response);
        }
    }

    // =========================
    // 🔐 GENERATE JWT TOKEN
    // =========================
    private async Task<string> GenerateJwtToken(ApplicationUser user)
    {
        try
        {
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email!),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Query company details
            //var companyUser = await _context.CompanyUser.AsNoTracking().FirstOrDefaultAsync(cu => cu.UserId == user.Id);
            //if (companyUser != null)
            //{
            //    authClaims.Add(new Claim("CompanyId", companyUser.CompanyId.ToString()));
            //    authClaims.Add(new Claim("CompanyRole", companyUser.Role));
            //}

            // Add roles and their claims to authClaims
            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var roleName in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, roleName));
                
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    var roleClaims = await _roleManager.GetClaimsAsync(role);
                    foreach (var claim in roleClaims)
                    {
                        authClaims.Add(claim);
                    }
                }
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
            );

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.Now.AddMinutes(
                    Convert.ToDouble(_configuration["Jwt:DurationInMinutes"])
                ),
                claims: authClaims,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception: Error generating JWT token for {Email}", user.Email);
            return null;
        }
    }
}