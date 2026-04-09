namespace MAFStudio.Application.DTOs;

/// <summary>
/// 群聊协调模式
/// </summary>
public enum GroupChatOrchestrationMode
{
    /// <summary>
    /// 轮询模式：所有 Agent 轮流发言
    /// </summary>
    RoundRobin = 0,
    
    /// <summary>
    /// 主Agent协调模式：Manager 引导 Worker 发言
    /// </summary>
    Manager = 1,
    
    /// <summary>
    /// AI智能选择模式：使用 LLM 智能选择下一个发言者
    /// </summary>
    Intelligent = 2
}

/// <summary>
/// 群聊工作流参数
/// </summary>
public class GroupChatParameters
{
    /// <summary>
    /// 协调模式（默认 Manager）
    /// </summary>
    public GroupChatOrchestrationMode OrchestrationMode { get; set; } = GroupChatOrchestrationMode.Manager;
    
    /// <summary>
    /// 最大迭代次数（默认10次）
    /// </summary>
    public int MaxIterations { get; set; } = 10;
}

public class CollaborationResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public List<ChatMessageDto> Messages { get; set; } = new();
    public string? Error { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class ChatMessageDto
{
    public string Sender { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Role { get; set; } = "assistant";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// 审阅迭代工作流参数
/// </summary>
public class ReviewIterativeParameters
{
    /// <summary>
    /// 最大迭代次数（默认10次）
    /// </summary>
    public int? MaxIterations { get; set; } = 10;

    /// <summary>
    /// 审阅标准（可选）
    /// </summary>
    public string? ReviewCriteria { get; set; }

    /// <summary>
    /// 通过关键词（默认 [APPROVED]）
    /// </summary>
    public string ApprovalKeyword { get; set; } = "[APPROVED]";

    /// <summary>
    /// 是否在每次迭代后保存版本（默认true）
    /// </summary>
    public bool SaveVersions { get; set; } = true;

    /// <summary>
    /// 最大尝试次数（循环工作流）
    /// </summary>
    public int? MaxAttempts { get; set; }

    /// <summary>
    /// 阈值标准（多维度评分）
    /// 例如：{ "quality": 85, "accuracy": 90, "completeness": 80 }
    /// </summary>
    public Dictionary<string, double>? Thresholds { get; set; }

    /// <summary>
    /// 任务账本配置（Magentic双环规划 - 外环）
    /// </summary>
    public TaskLedgerConfig? TaskLedger { get; set; }

    /// <summary>
    /// 进度账本配置（Magentic双环规划 - 内环）
    /// </summary>
    public ProgressLedgerConfig? ProgressLedger { get; set; }
}
