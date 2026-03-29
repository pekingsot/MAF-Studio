using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Entities;

namespace MAFStudio.Api.Controllers;

/// <summary>
/// 权限管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(
        IPermissionRepository permissionRepository,
        ILogger<PermissionsController> logger)
    {
        _permissionRepository = permissionRepository;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有权限列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetAllPermissions()
    {
        var permissions = await _permissionRepository.GetAllAsync();
        
        var result = permissions.Select(p => new
        {
            id = p.Id,
            name = p.Name,
            code = p.Code,
            description = p.Description,
            resource = p.Resource,
            action = p.Action,
            isEnabled = p.IsEnabled,
            createdAt = p.CreatedAt
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// 获取权限详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetPermissionById(long id)
    {
        var permission = await _permissionRepository.GetByIdAsync(id);
        if (permission == null)
        {
            return NotFound(new { message = "权限不存在" });
        }

        return Ok(new
        {
            id = permission.Id,
            name = permission.Name,
            code = permission.Code,
            description = permission.Description,
            resource = permission.Resource,
            action = permission.Action,
            isEnabled = permission.IsEnabled,
            createdAt = permission.CreatedAt
        });
    }

    /// <summary>
    /// 创建权限
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> CreatePermission([FromBody] CreatePermissionRequest request)
    {
        var existingPermission = await _permissionRepository.GetByCodeAsync(request.Code);
        if (existingPermission != null)
        {
            return BadRequest(new { message = "权限代码已存在" });
        }

        var permission = new Permission
        {
            Id = GenerateId(),
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            Resource = request.Resource,
            Action = request.Action,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow
        };

        var createdPermission = await _permissionRepository.CreateAsync(permission);
        _logger.LogInformation($"创建权限: {createdPermission.Name}");

        return Ok(new
        {
            id = createdPermission.Id,
            name = createdPermission.Name,
            code = createdPermission.Code,
            description = createdPermission.Description,
            resource = createdPermission.Resource,
            action = createdPermission.Action,
            isEnabled = createdPermission.IsEnabled
        });
    }

    /// <summary>
    /// 更新权限
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdatePermission(long id, [FromBody] UpdatePermissionRequest request)
    {
        var permission = await _permissionRepository.GetByIdAsync(id);
        if (permission == null)
        {
            return NotFound(new { message = "权限不存在" });
        }

        permission.Name = request.Name;
        permission.Description = request.Description;
        permission.Resource = request.Resource;
        permission.Action = request.Action;
        permission.IsEnabled = request.IsEnabled;

        var updatedPermission = await _permissionRepository.UpdateAsync(permission);
        _logger.LogInformation($"更新权限: {updatedPermission.Name}");

        return Ok(new
        {
            id = updatedPermission.Id,
            name = updatedPermission.Name,
            code = updatedPermission.Code,
            description = updatedPermission.Description,
            resource = updatedPermission.Resource,
            action = updatedPermission.Action,
            isEnabled = updatedPermission.IsEnabled
        });
    }

    /// <summary>
    /// 删除权限
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeletePermission(long id)
    {
        var permission = await _permissionRepository.GetByIdAsync(id);
        if (permission == null)
        {
            return NotFound(new { message = "权限不存在" });
        }

        await _permissionRepository.DeleteAsync(id);
        _logger.LogInformation($"删除权限: {permission.Name}");

        return Ok(new { message = "权限删除成功" });
    }

    /// <summary>
    /// 根据资源获取权限列表
    /// </summary>
    [HttpGet("resource/{resource}")]
    public async Task<ActionResult> GetPermissionsByResource(string resource)
    {
        var permissions = await _permissionRepository.GetByResourceAsync(resource);
        return Ok(permissions.Select(p => new { id = p.Id, name = p.Name, code = p.Code, action = p.Action }));
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
/// 创建权限请求
/// </summary>
public class CreatePermissionRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}

/// <summary>
/// 更新权限请求
/// </summary>
public class UpdatePermissionRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}
