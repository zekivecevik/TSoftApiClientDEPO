using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSoftApiClient.Models;
using TSoftApiClient.Services;

namespace TSoftApiClient.Controllers
{
    /// <summary>
    /// Authentication Controller - Login, Logout, User Management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly AuditLogService _auditLogService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AuthService authService,
            AuditLogService auditLogService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        /// <summary>
        /// Login endpoint
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers["User-Agent"].ToString();

            var result = _authService.Login(request, ipAddress, userAgent);

            if (result.Success)
            {
                _auditLogService.LogLogin(request.Username, true, ipAddress, userAgent);
                return Ok(result);
            }

            _auditLogService.LogLogin(request.Username, false, ipAddress, userAgent);
            return Unauthorized(result);
        }

        /// <summary>
        /// Get current user info
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var user = _authService.GetUserFromToken(token);

            if (user == null)
                return Unauthorized(new { message = "Invalid token" });

            return Ok(new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                ApiKey = user.ApiKey
            });
        }

        /// <summary>
        /// Create new user (Admin only)
        /// </summary>
        [HttpPost("users")]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateUser([FromBody] CreateUserRequest request)
        {
            var (success, message, user) = _authService.CreateUser(
                request.Username,
                request.Email,
                request.Password,
                request.FullName,
                request.Role
            );

            if (!success)
                return BadRequest(new { message });

            return Ok(new
            {
                message,
                user = new UserInfo
                {
                    Id = user!.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    ApiKey = user.ApiKey
                }
            });
        }

        /// <summary>
        /// Get all users (Admin only)
        /// </summary>
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetAllUsers()
        {
            var users = _authService.GetAllUsers();
            return Ok(users);
        }

        /// <summary>
        /// Regenerate API Key
        /// </summary>
        [HttpPost("regenerate-apikey")]
        [Authorize]
        public IActionResult RegenerateApiKey()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var user = _authService.GetUserFromToken(token);

            if (user == null)
                return Unauthorized(new { message = "Invalid token" });

            var newApiKey = _authService.RegenerateApiKey(user.Id);

            return Ok(new { success = true, apiKey = newApiKey });
        }
    }

    /// <summary>
    /// Create user request DTO
    /// </summary>
    public class CreateUserRequest
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string FullName { get; set; }
        public required string Role { get; set; }
    }
}