namespace MAFStudio.Core.Entities;

/// <summary>
/// 带有更新时间追踪的基础实体类
/// 继承此类的实体在更新时会自动设置 UpdatedAt 字段
/// </summary>
public abstract class BaseEntityWithUpdate
{
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 标记实体已更新，自动设置 UpdatedAt 为当前时间
    /// </summary>
    public void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
