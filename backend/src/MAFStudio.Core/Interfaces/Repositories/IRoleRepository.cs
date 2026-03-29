using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

/// <summary>
/// 角色仓储接口
/// </summary>
public interface IRoleRepository
{
    /// <summary>
    /// 根据ID获取角色
    /// </summary>
    Task<Role?> GetByIdAsync(long id);

    /// <summary>
    /// 根据代码获取角色
    /// </summary>
    Task<Role?> GetByCodeAsync(string code);

    /// <summary>
    /// 获取所有角色
    /// </summary>
    Task<List<Role>> GetAllAsync();

    /// <summary>
    /// 获取用户的角色列表
    /// </summary>
    Task<List<Role>> GetUserRolesAsync(long userId);

    /// <summary>
    /// 创建角色
    /// </summary>
    Task<Role> CreateAsync(Role role);

    /// <summary>
    /// 更新角色
    /// </summary>
    Task<Role> UpdateAsync(Role role);

    /// <summary>
    /// 删除角色
    /// </summary>
    Task DeleteAsync(long id);

    /// <summary>
    /// 为用户分配角色
    /// </summary>
    Task<bool> AssignRoleToUserAsync(long userId, long roleId);

    /// <summary>
    /// 移除用户的角色
    /// </summary>
    Task<bool> RemoveRoleFromUserAsync(long userId, long roleId);
}
