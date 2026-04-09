namespace MAFStudio.Application.Prompts;

public abstract class BaseSystemPromptBuilder : ISystemPromptBuilder
{
    public string BuildPrompt(SystemPromptContext context)
    {
        var prompt = BuildModeInstruction();
        
        if (!string.IsNullOrEmpty(context.AgentPrompt))
        {
            var agentPrompt = ReplaceVariables(context.AgentPrompt, context);
            prompt += agentPrompt;
        }
        
        return prompt;
    }
    
    protected abstract string BuildModeInstruction();
    
    private string ReplaceVariables(string prompt, SystemPromptContext context)
    {
        return prompt
            .Replace("{{agent_name}}", context.AgentName)
            .Replace("{{agent_role}}", context.AgentRole)
            .Replace("{{agent_type}}", context.AgentTypeName)
            .Replace("{{members}}", context.MembersInfo);
    }
}
