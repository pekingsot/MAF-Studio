using System.Security.Claims;

namespace MAFStudio.Api.Extensions;

/// <summary>
/// ClaimsPrincipal 扩展方法
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// 获取用户ID
    /// </summary>
    public static long GetUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new InvalidOperationException("用户ID声明不存在");
        }
        return long.Parse(userIdClaim);
    }

    /// <summary>
    /// 获取用户名
    /// </summary>
    public static string GetUsername(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Name)?.Value ?? "";
    }

    /// <summary>
    /// 获取用户邮箱
    /// </summary>
    public static string GetEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value ?? "";
    }

    /// <summary>
    /// 获取用户角色列表
    /// </summary>
    public static List<string> GetRoles(this ClaimsPrincipal user)
    {
        var rolesClaim = user.FindFirst("roles")?.Value;
        return string.IsNullOrEmpty(rolesClaim) 
            ? new List<string>() 
            : rolesClaim.Split(',').ToList();
    }

    /// <summary>
    /// 获取用户权限列表
    /// </summary>
    public static List<string> GetPermissions(this ClaimsPrincipal user)
    {
        var permissionsClaim = user.FindFirst("permissions")?.Value;
        return string.IsNullOrEmpty(permissionsClaim) 
            ? new List<string>() 
            : permissionsClaim.Split(',').ToList();
    }

    /// <summary>
    /// 检查用户是否拥有指定权限
    /// </summary>
    public static bool HasPermission(this ClaimsPrincipal user, string permission)
    {
        return user.GetPermissions().Contains(permission);
    }

    /// <summary>
    /// 检查用户是否拥有任一指定权限
    /// </summary>
    public static bool HasAnyPermission(this ClaimsPrincipal user, params string[] permissions)
    {
        var userPermissions = user.GetPermissions();
        return permissions.Any(p => userPermissions.Contains(p));
    }

    /// <summary>
    /// 检查用户是否拥有所有指定权限
    /// </summary>
    public static bool HasAllPermissions(this ClaimsPrincipal user, params string[] permissions)
    {
        var userPermissions = user.GetPermissions();
        return permissions.All(p => userPermissions.Contains(p));
    }

    /// <summary>
    /// 检查用户是否属于任一指定角色
    /// </summary>
    public static bool IsInAnyRole(this ClaimsPrincipal user, params string[] roles)
    {
        var userRoles = user.GetRoles();
        return roles.Any(r => userRoles.Contains(r));
    }

    /// <summary>
    /// 检查用户是否属于所有指定角色
    /// </summary>
    public static bool IsInAllRoles(this ClaimsPrincipal user, params string[] roles)
    {
        var userRoles = user.GetRoles();
        return roles.All(r => userRoles.Contains(r));
    }

    /// <summary>
    /// 检查用户是否为超级管理员
    /// </summary>
    public static bool IsSuperAdmin(this ClaimsPrincipal user)
    {
        return user.IsInAnyRole("SUPER_ADMIN");
    }

    /// <summary>
    /// 检查用户是否为管理员（包括超级管理员）
    /// </summary>
    public static bool IsAdmin(this ClaimsPrincipal user)
    {
        return user.IsInAnyRole("SUPER_ADMIN", "ADMIN");
    }
}
