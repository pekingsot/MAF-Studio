using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

/// <summary>
/// 权限仓储接口
/// </summary>
public interface IPermissionRepository
{
    /// <summary>
    /// 根据ID获取权限
    /// </summary>
    Task<Permission?> GetByIdAsync(long id);

    /// <summary>
    /// 根据代码获取权限
    /// </summary>
    Task<Permission?> GetByCodeAsync(string code);

    /// <summary>
    /// 获取所有权限
    /// </summary>
    Task<List<Permission>> GetAllAsync();

    /// <summary>
    /// 获取角色的权限列表
    /// </summary>
    Task<List<Permission>> GetRolePermissionsAsync(long roleId);

    /// <summary>
    /// 获取用户的权限列表
    /// </summary>
    Task<List<Permission>> GetUserPermissionsAsync(long userId);

    /// <summary>
    /// 创建权限
    /// </summary>
    Task<Permission> CreateAsync(Permission permission);

    /// <summary>
    /// 更新权限
    /// </summary>
    Task<Permission> UpdateAsync(Permission permission);

    /// <summary>
    /// 删除权限
    /// </summary>
    Task DeleteAsync(long id);

    /// <summary>
    /// 为角色分配权限
    /// </summary>
    Task AssignPermissionToRoleAsync(long roleId, long permissionId);

    /// <summary>
    /// 移除角色的权限
    /// </summary>
    Task RemovePermissionFromRoleAsync(long roleId, long permissionId);

    /// <summary>
    /// 根据资源获取权限列表
    /// </summary>
    Task<List<Permission>> GetByResourceAsync(string resource);
}
