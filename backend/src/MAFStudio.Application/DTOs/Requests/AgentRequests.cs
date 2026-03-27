namespace MAFStudio.Application.DTOs.Requests;

public class CreateAgentRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "Assistant";
    public string? SystemPrompt { get; set; }
    public string? Avatar { get; set; }
    public long? LlmConfigId { get; set; }
    public long? LlmModelConfigId { get; set; }
    
    /// <summary>
    /// 副模型配置列表（用于故障转移）
    /// </summary>
    public List<FallbackModelRequest>? FallbackModels { get; set; }
}

public class UpdateAgentRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SystemPrompt { get; set; }
    public string? Avatar { get; set; }
    public long? LlmConfigId { get; set; }
    public long? LlmModelConfigId { get; set; }
    
    /// <summary>
    /// 副模型配置列表（用于故障转移）
    /// </summary>
    public List<FallbackModelRequest>? FallbackModels { get; set; }
}

/// <summary>
/// 副模型配置请求
/// </summary>
public class FallbackModelRequest
{
    public long LlmConfigId { get; set; }
    public long? LlmModelConfigId { get; set; }
    public int Priority { get; set; }
}

public class UpdateAgentStatusRequest
{
    public Core.Enums.AgentStatus Status { get; set; }
}
