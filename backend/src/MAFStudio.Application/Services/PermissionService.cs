using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Application.Services;

/// <summary>
/// 权限管理服务接口
/// </summary>
public interface IPermissionService
{
    Task<List<Role>> GetUserRolesAsync(long userId);
    Task<List<Permission>> GetUserPermissionsAsync(long userId);
    Task<bool> AssignRoleToUserAsync(long userId, long roleId);
    Task<bool> RemoveRoleFromUserAsync(long userId, long roleId);
    Task<List<Role>> GetAllRolesAsync();
    Task<List<Permission>> GetAllPermissionsAsync();
}

/// <summary>
/// 权限管理服务实现
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;

    public PermissionService(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
    }

    public async Task<List<Role>> GetUserRolesAsync(long userId)
    {
        return await _roleRepository.GetUserRolesAsync(userId);
    }

    public async Task<List<Permission>> GetUserPermissionsAsync(long userId)
    {
        return await _permissionRepository.GetUserPermissionsAsync(userId);
    }

    public async Task<bool> AssignRoleToUserAsync(long userId, long roleId)
    {
        return await _roleRepository.AssignRoleToUserAsync(userId, roleId);
    }

    public async Task<bool> RemoveRoleFromUserAsync(long userId, long roleId)
    {
        return await _roleRepository.RemoveRoleFromUserAsync(userId, roleId);
    }

    public async Task<List<Role>> GetAllRolesAsync()
    {
        return await _roleRepository.GetAllAsync();
    }

    public async Task<List<Permission>> GetAllPermissionsAsync()
    {
        return await _permissionRepository.GetAllAsync();
    }
}
