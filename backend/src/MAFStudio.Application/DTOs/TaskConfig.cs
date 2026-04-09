using System.Text.Json;
using System.Text.Json.Serialization;

namespace MAFStudio.Application.DTOs;

/// <summary>
/// 任务配置
/// </summary>
public class TaskConfig
{
    /// <summary>
    /// 工作流类型（默认 GroupChat）
    /// GroupChat: 群聊协作模式（简单协调）
    /// Magentic: 智能工作流模式（基于图的工作流）
    /// </summary>
    [JsonPropertyName("workflowType")]
    public string WorkflowType { get; set; } = "GroupChat";

    /// <summary>
    /// 群聊协调模式（默认 RoundRobin）
    /// 仅当 WorkflowType = GroupChat 时有效
    /// </summary>
    [JsonPropertyName("orchestrationMode")]
    public string OrchestrationMode { get; set; } = "RoundRobin";

    /// <summary>
    /// 协调者 Agent ID（Manager 模式需要）
    /// 仅当 WorkflowType = GroupChat 且 OrchestrationMode = Manager 时有效
    /// </summary>
    [JsonPropertyName("managerAgentId")]
    public long? ManagerAgentId { get; set; }

    /// <summary>
    /// 协调者自定义提示词
    /// 如果指定，将覆盖协调者Agent的默认提示词
    /// 用于自定义协调者的行为和策略
    /// </summary>
    [JsonPropertyName("managerCustomPrompt")]
    public string? ManagerCustomPrompt { get; set; }

    /// <summary>
    /// Worker Agent列表
    /// 包含任务中选择的Worker Agents
    /// </summary>
    [JsonPropertyName("workerAgents")]
    public List<WorkerAgentConfig>? WorkerAgents { get; set; }

    /// <summary>
    /// 最大迭代次数（默认 10）
    /// </summary>
    [JsonPropertyName("maxIterations")]
    public int MaxIterations { get; set; } = 10;

    /// <summary>
    /// 工作流计划ID
    /// 仅当 WorkflowType = Magentic 时有效
    /// 如果指定，则使用已有的工作流计划
    /// </summary>
    [JsonPropertyName("workflowPlanId")]
    public long? WorkflowPlanId { get; set; }

    /// <summary>
    /// 工作流定义（动态生成）
    /// 仅当 WorkflowType = Magentic 且未指定 WorkflowPlanId 时有效
    /// </summary>
    [JsonPropertyName("workflowDefinition")]
    public WorkflowDefinitionDto? WorkflowDefinition { get; set; }

    /// <summary>
    /// 阈值标准（多维度评分）
    /// 仅当 WorkflowType = Magentic 时有效
    /// 例如：{ "quality": 85, "accuracy": 90, "completeness": 80 }
    /// </summary>
    [JsonPropertyName("thresholds")]
    public Dictionary<string, double>? Thresholds { get; set; }

    /// <summary>
    /// 最大尝试次数（循环工作流）
    /// 仅当 WorkflowType = Magentic 时有效
    /// </summary>
    [JsonPropertyName("maxAttempts")]
    public int? MaxAttempts { get; set; }

    /// <summary>
    /// 条件判断字段
    /// 仅当 WorkflowType = Magentic 时有效
    /// 用于条件路由工作流
    /// </summary>
    [JsonPropertyName("conditionField")]
    public string? ConditionField { get; set; }

    /// <summary>
    /// 任务账本配置（Magentic双环规划）
    /// 仅当 WorkflowType = Magentic 时有效
    /// </summary>
    [JsonPropertyName("taskLedger")]
    public TaskLedgerConfig? TaskLedger { get; set; }

    /// <summary>
    /// 进度账本配置（Magentic双环规划）
    /// 仅当 WorkflowType = Magentic 时有效
    /// </summary>
    [JsonPropertyName("progressLedger")]
    public ProgressLedgerConfig? ProgressLedger { get; set; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 从 JSON 字符串解析配置
    /// </summary>
    public static TaskConfig? FromJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new TaskConfig();
        
        try
        {
            return JsonSerializer.Deserialize<TaskConfig>(json, JsonOptions);
        }
        catch
        {
            return new TaskConfig();
        }
    }

    /// <summary>
    /// 转换为 GroupChatOrchestrationMode 枚举
    /// </summary>
    public GroupChatOrchestrationMode GetOrchestrationMode()
    {
        return OrchestrationMode?.ToLowerInvariant() switch
        {
            "manager" => GroupChatOrchestrationMode.Manager,
            "intelligent" => GroupChatOrchestrationMode.Intelligent,
            _ => GroupChatOrchestrationMode.RoundRobin
        };
    }

    /// <summary>
    /// 转换为 GroupChatParameters
    /// </summary>
    public GroupChatParameters ToGroupChatParameters()
    {
        return new GroupChatParameters
        {
            OrchestrationMode = GetOrchestrationMode(),
            MaxIterations = MaxIterations
        };
    }

    /// <summary>
    /// 转换为 ReviewIterativeParameters
    /// </summary>
    public ReviewIterativeParameters ToReviewIterativeParameters()
    {
        return new ReviewIterativeParameters
        {
            MaxIterations = MaxIterations,
            MaxAttempts = MaxAttempts,
            Thresholds = Thresholds,
            TaskLedger = TaskLedger,
            ProgressLedger = ProgressLedger
        };
    }
}

/// <summary>
/// 任务账本配置（Magentic双环规划 - 外环）
/// </summary>
public class TaskLedgerConfig
{
    /// <summary>
    /// 全局目标
    /// </summary>
    [JsonPropertyName("globalGoal")]
    public string? GlobalGoal { get; set; }

    /// <summary>
    /// 已知事实
    /// </summary>
    [JsonPropertyName("knownFacts")]
    public List<string>? KnownFacts { get; set; }

    /// <summary>
    /// 总体规划
    /// </summary>
    [JsonPropertyName("overallPlan")]
    public string? OverallPlan { get; set; }

    /// <summary>
    /// 任务边界
    /// </summary>
    [JsonPropertyName("boundaries")]
    public List<string>? Boundaries { get; set; }
}

/// <summary>
/// 进度账本配置（Magentic双环规划 - 内环）
/// </summary>
public class ProgressLedgerConfig
{
    /// <summary>
    /// 当前步骤目标
    /// </summary>
    [JsonPropertyName("currentStepGoal")]
    public string? CurrentStepGoal { get; set; }

    /// <summary>
    /// 已完成步骤
    /// </summary>
    [JsonPropertyName("completedSteps")]
    public List<string>? CompletedSteps { get; set; }

    /// <summary>
    /// 反思结果
    /// </summary>
    [JsonPropertyName("reflectionResult")]
    public string? ReflectionResult { get; set; }

    /// <summary>
    /// 下一步行动
    /// </summary>
    [JsonPropertyName("nextAction")]
    public string? NextAction { get; set; }
}

/// <summary>
/// Worker Agent配置
/// </summary>
public class WorkerAgentConfig
{
    /// <summary>
    /// Agent ID
    /// </summary>
    [JsonPropertyName("agentId")]
    public long AgentId { get; set; }
}
