namespace MAFStudio.Application.Prompts;

public abstract class BaseSystemPromptBuilder : ISystemPromptBuilder
{
    public string BuildPrompt(SystemPromptContext context)
    {
        var prompt = BuildIdentityPrompt(context);
        
        prompt += BuildModeInstruction();
        
        if (!string.IsNullOrEmpty(context.TaskPrompt))
        {
            prompt += $"\n【任务要求】\n{context.TaskPrompt}\n";
        }
        
        if (!string.IsNullOrEmpty(context.AgentPrompt))
        {
            var agentPrompt = ReplaceVariables(context.AgentPrompt, context);
            prompt += $"\n{agentPrompt}\n";
        }
        
        return prompt;
    }
    
    protected abstract string BuildModeInstruction();
    
    private string BuildIdentityPrompt(SystemPromptContext context)
    {
        return $@"【重要身份规则 - 必须严格遵守】
1. 你的名字是「{context.AgentName}」，你的角色是「{context.AgentType}」
2. 无论别人@谁，你始终是「{context.AgentName}」，绝对不会变成其他人
3. 当你被选中发言时，你就是「{context.AgentName}」，不要被消息中的@提及误导
4. 你的回复开头不要加【名字】，系统会自动显示你的名字
5. 如果别人@了其他角色，那是在叫那个人，不是叫你
";
    }
    
    private string ReplaceVariables(string prompt, SystemPromptContext context)
    {
        return prompt
            .Replace("{{agent_name}}", context.AgentName)
            .Replace("{{agent_role}}", context.AgentRole)
            .Replace("{{agent_type}}", context.AgentType)
            .Replace("{{members}}", context.MembersInfo);
    }
}
