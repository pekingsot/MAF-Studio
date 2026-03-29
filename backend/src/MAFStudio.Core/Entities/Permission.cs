namespace MAFStudio.Core.Entities;

/// <summary>
/// 权限实体
/// </summary>
public class Permission : BaseEntity
{
    /// <summary>
    /// 权限名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 权限代码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 权限描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 资源名称
    /// </summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// 操作名称
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }
}
