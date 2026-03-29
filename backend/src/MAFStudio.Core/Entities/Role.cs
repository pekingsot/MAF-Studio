namespace MAFStudio.Core.Entities;

/// <summary>
/// 角色实体
/// </summary>
public class Role : BaseEntity
{
    /// <summary>
    /// 角色名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 角色代码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 角色描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否系统角色
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }
}
