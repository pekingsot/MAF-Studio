using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Backend.Services;
using MAFStudio.Backend.Abstractions;
using MAFStudio.Backend.Models.Requests;
using System.Security.Claims;

namespace MAFStudio.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var (success, message, user, token) = await _authService.RegisterAsync(
                request.Username, 
                request.Email, 
                request.Password
            );

            if (!success)
            {
                return BadRequest(new { message });
            }

            return Ok(new { 
                message,
                user = new { user!.Id, user.Username, user.Email, user.Avatar, user.Role },
                token
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var (success, message, user, token) = await _authService.LoginAsync(
                request.Username, 
                request.Password
            );

            if (!success)
            {
                return Unauthorized(new { message });
            }

            return Ok(new { 
                message,
                user = new { user!.Id, user.Username, user.Email, user.Avatar, user.Role },
                token
            });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "未授权" });
            }

            var user = await _authService.GetUserByIdAsync(userId);
            
            if (user == null)
            {
                return NotFound(new { message = "用户不存在" });
            }

            return Ok(new { 
                user.Id, 
                user.Username, 
                user.Email, 
                user.Avatar, 
                user.Role,
                user.CreatedAt
            });
        }
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
