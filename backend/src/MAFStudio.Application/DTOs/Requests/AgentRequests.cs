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
    /// 大模型选择配置（包含主模型和副模型，带验证状态）
    /// </summary>
    public string? LlmConfigs { get; set; }
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
    /// 大模型选择配置（包含主模型和副模型，带验证状态）
    /// </summary>
    public string? LlmConfigs { get; set; }
}

public class UpdateAgentStatusRequest
{
    public Core.Enums.AgentStatus Status { get; set; }
}
