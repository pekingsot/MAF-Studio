using System.Text.Json.Serialization;

namespace MAFStudio.Core.Entities;

/// <summary>
/// 工作流模板实体
/// </summary>
public class WorkflowTemplate : BaseEntity
{
    /// <summary>
    /// 模板名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模板描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 模板分类
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// 标签（JSON数组）
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 工作流定义（JSON格式）
    /// </summary>
    public string WorkflowDefinition { get; set; } = string.Empty;

    /// <summary>
    /// 参数定义（JSON对象）
    /// </summary>
    public string? Parameters { get; set; }

    /// <summary>
    /// 创建者ID
    /// </summary>
    public long? CreatedBy { get; set; }

    /// <summary>
    /// 是否公开
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// 使用次数
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// 来源：manual（手动创建）、magentic（Magentic生成）、magentic_optimized（Magentic优化后）
    /// </summary>
    public string Source { get; set; } = "manual";

    /// <summary>
    /// 原始任务描述（如果是Magentic生成的）
    /// </summary>
    public string? OriginalTask { get; set; }
}
