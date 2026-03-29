namespace MAFStudio.Core.Entities;

/// <summary>
/// 用户角色关联实体
/// </summary>
public class UserRole : BaseEntity
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }
}
