using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Application.DTOs.Requests;

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

        var token = _authService.GenerateJwtToken(user);
        return Ok(new { token, user = new { id = user.Id, username = user.Username, email = user.Email, role = user.Role } });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var user = await _authService.RegisterAsync(request.Username, request.Email, request.Password);
            var token = _authService.GenerateJwtToken(user);
            return Ok(new { token, user = new { id = user.Id, username = user.Username, email = user.Email, role = user.Role } });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("me")]
    public async Task<ActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _authService.GetByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(new { id = user.Id, username = user.Username, email = user.Email, role = user.Role, avatar = user.Avatar });
    }
}
