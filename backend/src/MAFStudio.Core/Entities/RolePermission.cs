namespace MAFStudio.Core.Entities;

/// <summary>
/// 角色权限关联实体
/// </summary>
public class RolePermission : BaseEntity
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 权限ID
    /// </summary>
    public long PermissionId { get; set; }
}
