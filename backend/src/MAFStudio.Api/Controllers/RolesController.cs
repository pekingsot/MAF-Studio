using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Entities;
using MAFStudio.Application.Services;

namespace MAFStudio.Api.Controllers;

/// <summary>
/// 角色管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly ILogger<RolesController> _logger;

    public RolesController(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        ILogger<RolesController> logger)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有角色列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetAllRoles()
    {
        var roles = await _roleRepository.GetAllAsync();
        
        var result = roles.Select(r => new
        {
            id = r.Id,
            name = r.Name,
            code = r.Code,
            description = r.Description,
            isSystem = r.IsSystem,
            isEnabled = r.IsEnabled,
            createdAt = r.CreatedAt,
            updatedAt = r.UpdatedAt
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// 获取角色详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetRoleById(long id)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
        {
            return NotFound(new { message = "角色不存在" });
        }

        var permissions = await _permissionRepository.GetRolePermissionsAsync(id);

        return Ok(new
        {
            id = role.Id,
            name = role.Name,
            code = role.Code,
            description = role.Description,
            isSystem = role.IsSystem,
            isEnabled = role.IsEnabled,
            permissions = permissions.Select(p => new { id = p.Id, name = p.Name, code = p.Code }).ToList(),
            createdAt = role.CreatedAt,
            updatedAt = role.UpdatedAt
        });
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var existingRole = await _roleRepository.GetByCodeAsync(request.Code);
        if (existingRole != null)
        {
            return BadRequest(new { message = "角色代码已存在" });
        }

        var role = new Role
        {
            Id = GenerateId(),
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            IsSystem = false,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdRole = await _roleRepository.CreateAsync(role);
        _logger.LogInformation($"创建角色: {createdRole.Name}");

        return Ok(new
        {
            id = createdRole.Id,
            name = createdRole.Name,
            code = createdRole.Code,
            description = createdRole.Description,
            isSystem = createdRole.IsSystem,
            isEnabled = createdRole.IsEnabled
        });
    }

    /// <summary>
    /// 更新角色
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateRole(long id, [FromBody] UpdateRoleRequest request)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
        {
            return NotFound(new { message = "角色不存在" });
        }

        if (role.IsSystem)
        {
            return BadRequest(new { message = "系统角色不能修改" });
        }

        role.Name = request.Name;
        role.Description = request.Description;
        role.IsEnabled = request.IsEnabled;
        role.UpdatedAt = DateTime.UtcNow;

        var updatedRole = await _roleRepository.UpdateAsync(role);
        _logger.LogInformation($"更新角色: {updatedRole.Name}");

        return Ok(new
        {
            id = updatedRole.Id,
            name = updatedRole.Name,
            code = updatedRole.Code,
            description = updatedRole.Description,
            isEnabled = updatedRole.IsEnabled
        });
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteRole(long id)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
        {
            return NotFound(new { message = "角色不存在" });
        }

        if (role.IsSystem)
        {
            return BadRequest(new { message = "系统角色不能删除" });
        }

        await _roleRepository.DeleteAsync(id);
        _logger.LogInformation($"删除角色: {role.Name}");

        return Ok(new { message = "角色删除成功" });
    }

    /// <summary>
    /// 获取角色的权限列表
    /// </summary>
    [HttpGet("{id}/permissions")]
    public async Task<ActionResult> GetRolePermissions(long id)
    {
        var permissions = await _permissionRepository.GetRolePermissionsAsync(id);
        return Ok(permissions.Select(p => new { id = p.Id, name = p.Name, code = p.Code, description = p.Description }));
    }

    /// <summary>
    /// 为角色分配权限
    /// </summary>
    [HttpPost("{id}/permissions/{permissionId}")]
    public async Task<ActionResult> AssignPermission(long id, long permissionId)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
        {
            return NotFound(new { message = "角色不存在" });
        }

        var permission = await _permissionRepository.GetByIdAsync(permissionId);
        if (permission == null)
        {
            return NotFound(new { message = "权限不存在" });
        }

        await _permissionRepository.AssignPermissionToRoleAsync(id, permissionId);
        _logger.LogInformation($"为角色 {role.Name} 分配权限 {permission.Name}");

        return Ok(new { message = "权限分配成功" });
    }

    /// <summary>
    /// 移除角色的权限
    /// </summary>
    [HttpDelete("{id}/permissions/{permissionId}")]
    public async Task<ActionResult> RemovePermission(long id, long permissionId)
    {
        await _permissionRepository.RemovePermissionFromRoleAsync(id, permissionId);
        _logger.LogInformation($"移除角色 {id} 的权限 {permissionId}");

        return Ok(new { message = "权限移除成功" });
    }

    /// <summary>
    /// 生成雪花ID
    /// </summary>
    private long GenerateId()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() << 22;
    }
}

/// <summary>
/// 创建角色请求
/// </summary>
public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// 更新角色请求
/// </summary>
public class UpdateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
}
