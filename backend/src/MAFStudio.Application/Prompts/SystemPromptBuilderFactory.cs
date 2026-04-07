using MAFStudio.Application.DTOs;

namespace MAFStudio.Application.Prompts;

public interface ISystemPromptBuilderFactory
{
    ISystemPromptBuilder Create(GroupChatOrchestrationMode mode);
}

public class SystemPromptBuilderFactory : ISystemPromptBuilderFactory
{
    public ISystemPromptBuilder Create(GroupChatOrchestrationMode mode)
    {
        return mode switch
        {
            GroupChatOrchestrationMode.RoundRobin => new RoundRobinPromptBuilder(),
            GroupChatOrchestrationMode.Manager => new ManagerPromptBuilder(),
            GroupChatOrchestrationMode.Intelligent => new IntelligentPromptBuilder(),
            _ => throw new ArgumentException($"不支持的群聊模式: {mode}")
        };
    }
}
