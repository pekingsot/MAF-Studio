namespace MAFStudio.Application.Prompts;

public class ManagerPromptBuilder : BaseSystemPromptBuilder
{
    protected override string BuildModeInstruction()
    {
        return "";
    }

    public override string BuildPrompt(SystemPromptContext context)
    {
        var prompt = BuildModeInstruction();
        
        if (!string.IsNullOrEmpty(context.AgentPrompt))
        {
            prompt += context.AgentPrompt;
        }
        
        return ReplaceVariables(prompt, context);
    }
}
