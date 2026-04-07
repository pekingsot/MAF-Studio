namespace MAFStudio.Application.Prompts;

public interface ISystemPromptBuilder
{
    string BuildPrompt(SystemPromptContext context);
}

public class SystemPromptContext
{
    public string AgentName { get; set; } = string.Empty;
    public string AgentRole { get; set; } = string.Empty;
    public string AgentType { get; set; } = string.Empty;
    public string MembersInfo { get; set; } = string.Empty;
    public string? TaskPrompt { get; set; }
    public string? AgentPrompt { get; set; }
}
