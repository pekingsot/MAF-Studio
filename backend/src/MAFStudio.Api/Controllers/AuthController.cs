using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Application.DTOs.Requests;
using MAFStudio.Api.Extensions;

namespace MAFStudio.Api.Controllers;

/// <summary>
/// 认证控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _authService.ValidateUserAsync(request.Username, request.Password);
        if (user == null)
        {
            return Unauthorized(new { message = "用户名或密码错误" });
        }

        // 获取用户的实际角色和权限
        var (roles, permissions) = await _authService.GetUserRolesAndPermissionsAsync(user.Id);

        // 如果没有角色，根据用户表的 Role 字段设置默认角色
        if (roles.Count == 0)
        {
            roles = new List<string> { user.Role == "admin" ? "ADMIN" : "USER" };
        }

        // 生成 JWT Token（包含角色和权限）
        var token = _authService.GenerateJwtToken(user, roles, permissions);

        return Ok(new
        {
            token,
            user = new
            {
                id = user.Id.ToString(),
                username = user.Username,
                email = user.Email,
                roles,
                permissions
            }
        });
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var user = await _authService.RegisterAsync(request.Username, request.Email, request.Password);

            // 新用户默认角色和权限
            var roles = new List<string> { "USER" };
            var permissions = new List<string>();

            // 生成 JWT Token（包含角色和权限）
            var token = _authService.GenerateJwtToken(user, roles, permissions);

            return Ok(new
            {
                token,
                user = new
                {
                    id = user.Id.ToString(),
                    username = user.Username,
                    email = user.Email,
                    roles,
                    permissions
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
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

        // 暂时返回默认角色和权限
        var roles = new List<string> { user.Role == "admin" ? "ADMIN" : "USER" };
        var permissions = new List<string>();

        return Ok(new
        {
            id = user.Id.ToString(),
            username = user.Username,
            email = user.Email,
            avatar = user.Avatar,
            roles,
            permissions
        });
    }
}
