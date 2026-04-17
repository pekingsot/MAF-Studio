using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Application.DTOs.Requests;
using MAFStudio.Api.Extensions;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _authService.ValidateUserAsync(request.Username, request.Password);
        if (user == null)
        {
            return Unauthorized(new { message = "用户名或密码错误" });
        }

        var roles = new List<string> { user.Role == "admin" ? "ADMIN" : "USER" };
        var token = _authService.GenerateJwtToken(user, roles, new List<string>());

        return Ok(new
        {
            token,
            user = new
            {
                id = user.Id.ToString(),
                username = user.Username,
                email = user.Email,
                roles
            }
        });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var user = await _authService.RegisterAsync(request.Username, request.Email, request.Password);
            var roles = new List<string> { "USER" };
            var token = _authService.GenerateJwtToken(user, roles, new List<string>());

            return Ok(new
            {
                token,
                user = new
                {
                    id = user.Id.ToString(),
                    username = user.Username,
                    email = user.Email,
                    roles
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult> GetCurrentUser()
    {
        var userId = User.GetUserId();
        var user = await _authService.GetByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var roles = new List<string> { user.Role == "admin" ? "ADMIN" : "USER" };

        return Ok(new
        {
            id = user.Id.ToString(),
            username = user.Username,
            email = user.Email,
            avatar = user.Avatar,
            roles
        });
    }
}
