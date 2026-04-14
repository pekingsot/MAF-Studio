namespace MAFStudio.Application.Prompts;

public abstract class BaseSystemPromptBuilder : ISystemPromptBuilder
{
    public virtual string BuildPrompt(SystemPromptContext context)
    {
        var prompt = BuildIdentitySection(context);
        prompt += BuildModeInstruction();
        
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
        
        if (!string.IsNullOrEmpty(context.MembersInfo))
        {
            prompt += "\n\n团队成员：\n" + context.MembersInfo;
        }
        
        return ReplaceVariables(prompt, context);
    }

    private static string BuildIdentitySection(SystemPromptContext context)
    {
        var section = $"\n你是 {context.AgentName}";
        if (!string.IsNullOrEmpty(context.AgentRole))
        {
            section += $"，角色为{context.AgentRole}";
        }
        if (!string.IsNullOrEmpty(context.AgentTypeName))
        {
            section += $"，负责{context.AgentTypeName}";
        }
        return section + "。\n";
    }
    
    protected abstract string BuildModeInstruction();
    
    protected string ReplaceVariables(string prompt, SystemPromptContext context)
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
