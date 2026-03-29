using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Application.Services;
using System.Security.Claims;

namespace MAFStudio.Api.Controllers;

/// <summary>
/// 用户管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionService permissionService,
        ILogger<UsersController> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionService = permissionService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有用户列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetAllUsers()
    {
        var users = await _userRepository.GetAllAsync();
        
        var result = users.Select(u => new
        {
            id = u.Id,
            username = u.Username,
            email = u.Email,
            avatar = u.Avatar,
            role = u.Role,
            createdAt = u.CreatedAt,
            updatedAt = u.UpdatedAt
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// 获取用户详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetUserById(long id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "用户不存在" });
        }

        var roles = await _permissionService.GetUserRolesAsync(id);
        var permissions = await _permissionService.GetUserPermissionsAsync(id);

        return Ok(new
        {
            id = user.Id,
            username = user.Username,
            email = user.Email,
            avatar = user.Avatar,
            role = user.Role,
            roles = roles.Select(r => new { id = r.Id, name = r.Name, code = r.Code }).ToList(),
            permissions = permissions.Select(p => new { id = p.Id, name = p.Name, code = p.Code }).ToList(),
            createdAt = user.CreatedAt,
            updatedAt = user.UpdatedAt
        });
    }

    /// <summary>
    /// 获取用户的角色列表
    /// </summary>
    [HttpGet("{id}/roles")]
    public async Task<ActionResult> GetUserRoles(long id)
    {
        var roles = await _permissionService.GetUserRolesAsync(id);
        return Ok(roles.Select(r => new { id = r.Id, name = r.Name, code = r.Code, description = r.Description }));
    }

    /// <summary>
    /// 为用户分配角色
    /// </summary>
    [HttpPost("{id}/roles/{roleId}")]
    public async Task<ActionResult> AssignRole(long id, long roleId)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "用户不存在" });
        }

        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role == null)
        {
            return NotFound(new { message = "角色不存在" });
        }

        var success = await _permissionService.AssignRoleToUserAsync(id, roleId);
        if (success)
        {
            _logger.LogInformation($"为用户 {user.Username} 分配角色 {role.Name}");
            return Ok(new { message = "角色分配成功" });
        }

        return BadRequest(new { message = "角色分配失败" });
    }

    /// <summary>
    /// 移除用户的角色
    /// </summary>
    [HttpDelete("{id}/roles/{roleId}")]
    public async Task<ActionResult> RemoveRole(long id, long roleId)
    {
        var success = await _permissionService.RemoveRoleFromUserAsync(id, roleId);
        if (success)
        {
            _logger.LogInformation($"移除用户 {id} 的角色 {roleId}");
            return Ok(new { message = "角色移除成功" });
        }

        return BadRequest(new { message = "角色移除失败" });
    }
}
