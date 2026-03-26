using MAFStudio.Core.Entities;

namespace MAFStudio.Application.DTOs.Requests;

public class CreateAgentRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "Assistant";
    public string Configuration { get; set; } = "{}";
    public string? Avatar { get; set; }
    public Guid? LlmConfigId { get; set; }
    public Guid? LlmModelConfigId { get; set; }
}

public class UpdateAgentRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Configuration { get; set; }
    public string? Avatar { get; set; }
    public Guid? LlmConfigId { get; set; }
    public Guid? LlmModelConfigId { get; set; }
}

public class UpdateAgentStatusRequest
{
    public Core.Enums.AgentStatus Status { get; set; }
}
