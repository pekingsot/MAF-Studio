namespace MAFStudio.Application.DTOs;

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
}
