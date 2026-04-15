using System.Text.Json.Serialization;

namespace MAFStudio.Application.DTOs;

/// <summary>
/// 工作流节点定义
/// </summary>
public class WorkflowNodeDto
{
    /// <summary>
    /// 节点ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 节点类型：start, agent, aggregator, condition, loop, review
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 节点名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Agent角色（仅agent/review类型节点）
    /// </summary>
    public string? AgentRole { get; set; }

    /// <summary>
    /// 具体的Agent ID（仅agent/review类型节点）
    /// </summary>
    public string? AgentId { get; set; }

    /// <summary>
    /// 任务描述模板（仅agent类型节点）
    /// </summary>
    public string? InputTemplate { get; set; }

    /// <summary>
    /// 条件表达式（仅condition类型节点）
    /// </summary>
    public string? Condition { get; set; }

    /// <summary>
    /// 审核通过关键词（仅review类型节点，默认 [APPROVED]）
    /// </summary>
    public string? ApprovalKeyword { get; set; }

    /// <summary>
    /// 审核不通过时打回的目标节点ID（仅review类型节点）
    /// </summary>
    public string? RejectTargetNode { get; set; }

    /// <summary>
    /// 最大重试次数（仅review类型节点，默认3次）
    /// </summary>
    public int? MaxRetries { get; set; }

    /// <summary>
    /// 审核标准/提示词（仅review类型节点，指导审核者如何审核）
    /// </summary>
    public string? ReviewCriteria { get; set; }

    /// <summary>
    /// 参数配置
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }
}

/// <summary>
/// 工作流边定义
/// </summary>
public class WorkflowEdgeDto
{
    /// <summary>
    /// 边类型：sequential, fan-out, fan-in, conditional, loop, approved, rejected
    /// </summary>
    public string Type { get; set; } = "sequential";

    /// <summary>
    /// 源节点ID
    /// </summary>
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// 目标节点ID（fan-out和fan-in时为数组）
    /// </summary>
    public object To { get; set; } = string.Empty;

    /// <summary>
    /// 条件表达式（仅conditional类型边）
    /// </summary>
    public string? Condition { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// 工作流定义
/// </summary>
public class WorkflowDefinitionDto
{
    /// <summary>
    /// 节点列表
    /// </summary>
    public List<WorkflowNodeDto> Nodes { get; set; } = new();

    /// <summary>
    /// 边列表
    /// </summary>
    public List<WorkflowEdgeDto> Edges { get; set; } = new();
}

/// <summary>
/// 参数定义
/// </summary>
public class ParameterDefinitionDto
{
    /// <summary>
    /// 参数类型：string, number, boolean, array
    /// </summary>
    public string Type { get; set; } = "string";

    /// <summary>
    /// 参数描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public object? Default { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool Required { get; set; } = false;
}

/// <summary>
/// 工作流模板DTO
/// </summary>
public class WorkflowTemplateDto
{
    /// <summary>
    /// 模板ID
    /// </summary>
    public long Id { get; set; }

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
    /// 标签
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 工作流定义
    /// </summary>
    public WorkflowDefinitionDto Workflow { get; set; } = new();

    /// <summary>
    /// 参数定义
    /// </summary>
    public Dictionary<string, ParameterDefinitionDto>? Parameters { get; set; }

    /// <summary>
    /// 创建者ID
    /// </summary>
    public long? CreatedBy { get; set; }

    /// <summary>
    /// 是否公开
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// 使用次数
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// 来源
    /// </summary>
    public string Source { get; set; } = "manual";

    /// <summary>
    /// 原始任务描述
    /// </summary>
    public string? OriginalTask { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 创建工作流模板请求
/// </summary>
public class CreateWorkflowTemplateRequest
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
    /// 标签
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// 工作流定义
    /// </summary>
    public WorkflowDefinitionDto Workflow { get; set; } = new();

    /// <summary>
    /// 参数定义
    /// </summary>
    public Dictionary<string, ParameterDefinitionDto>? Parameters { get; set; }

    /// <summary>
    /// 是否公开
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// 来源
    /// </summary>
    public string Source { get; set; } = "manual";

    /// <summary>
    /// 原始任务描述
    /// </summary>
    public string? OriginalTask { get; set; }
}

/// <summary>
/// 更新工作流模板请求
/// </summary>
public class UpdateWorkflowTemplateRequest
{
    /// <summary>
    /// 模板名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 模板描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 模板分类
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// 标签
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// 工作流定义
    /// </summary>
    public WorkflowDefinitionDto? Workflow { get; set; }

    /// <summary>
    /// 参数定义
    /// </summary>
    public Dictionary<string, ParameterDefinitionDto>? Parameters { get; set; }

    /// <summary>
    /// 是否公开
    /// </summary>
    public bool? IsPublic { get; set; }
}

/// <summary>
/// 执行工作流模板请求
/// </summary>
public class ExecuteTemplateRequest
{
    /// <summary>
    /// 协作ID
    /// </summary>
    public long CollaborationId { get; set; }

    /// <summary>
    /// 输入内容
    /// </summary>
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// 参数值
    /// </summary>
    public Dictionary<string, object>? ParameterValues { get; set; }
}

/// <summary>
/// Magentic生成计划请求
/// </summary>
public class GenerateMagenticPlanRequest
{
    /// <summary>
    /// 协作ID
    /// </summary>
    public long CollaborationId { get; set; }

    /// <summary>
    /// 任务描述
    /// </summary>
    public string Task { get; set; } = string.Empty;
}

/// <summary>
/// Magentic生成计划响应
/// </summary>
public class GenerateMagenticPlanResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 工作流定义
    /// </summary>
    public WorkflowDefinitionDto? Workflow { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// 保存Magentic计划请求
/// </summary>
public class SaveMagenticPlanRequest
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
    /// 标签
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// 工作流定义
    /// </summary>
    public WorkflowDefinitionDto Workflow { get; set; } = new();

    /// <summary>
    /// 参数定义
    /// </summary>
    public Dictionary<string, ParameterDefinitionDto>? Parameters { get; set; }

    /// <summary>
    /// 是否公开
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// 是否让Magentic学习
    /// </summary>
    public bool EnableLearning { get; set; } = false;

    /// <summary>
    /// 原始任务描述
    /// </summary>
    public string? OriginalTask { get; set; }
}
