namespace MAFStudio.Application.Prompts;

public abstract class BaseSystemPromptBuilder : ISystemPromptBuilder
{
    public string BuildPrompt(SystemPromptContext context)
    {
        var prompt = BuildModeInstruction();
        
        if (!string.IsNullOrEmpty(context.AgentPrompt))
        {
            prompt += context.AgentPrompt;
        }
        
        if (!string.IsNullOrEmpty(context.TaskDescription))
        {
            prompt += "\n\n任务描述：\n" + context.TaskDescription;
        }
        
        if (!string.IsNullOrEmpty(context.TaskPrompt))
        {
            prompt += "\n\n任务要求：\n" + context.TaskPrompt;
        }
        
        return ReplaceVariables(prompt, context);
    }
    
    protected abstract string BuildModeInstruction();
    
    private string ReplaceVariables(string prompt, SystemPromptContext context)
    {
        return prompt
            .Replace("{{agent_name}}", context.AgentName)
            .Replace("{{agent_role}}", context.AgentRole)
            .Replace("{{agent_type}}", context.AgentTypeName)
            .Replace("{{members}}", context.MembersInfo)
            .Replace("{{taskDescription}}", context.TaskDescription ?? "")
            .Replace("{{taskPrompt}}", context.TaskPrompt ?? "");
    }
}
