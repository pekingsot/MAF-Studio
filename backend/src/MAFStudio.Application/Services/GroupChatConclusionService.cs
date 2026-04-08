using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;

namespace MAFStudio.Application.Services;

public interface IGroupChatConclusionService
{
    Task<string?> GenerateAndCommitConclusionAsync(
        long taskId,
        long collaborationId,
        string topic,
        List<Message> messages,
        long agentId,
        string agentName,
        string? agentType,
        string? agentPrompt,
        IChatClient chatClient,
        CancellationToken cancellationToken = default);
}

public class GroupChatConclusionService : IGroupChatConclusionService
{
    private readonly ICollaborationTaskRepository _taskRepository;
    private readonly IWorkspaceService _workspaceService;
    private readonly ILogger<GroupChatConclusionService> _logger;

    public GroupChatConclusionService(
        ICollaborationTaskRepository taskRepository,
        IWorkspaceService workspaceService,
        ILogger<GroupChatConclusionService> logger)
    {
        _taskRepository = taskRepository;
        _workspaceService = workspaceService;
        _logger = logger;
    }

    public async Task<string?> GenerateAndCommitConclusionAsync(
        long taskId,
        long collaborationId,
        string topic,
        List<Message> messages,
        long agentId,
        string agentName,
        string? agentType,
        string? agentPrompt,
        IChatClient chatClient,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("========== 开始让Agent执行任务后续操作 ==========");
        _logger.LogInformation("任务ID: {TaskId}, 协作ID: {CollaborationId}, 执行Agent: {AgentName} ({AgentType}) ID: {AgentId}", 
            taskId, collaborationId, agentName, agentType ?? "未知", agentId);

        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null)
        {
            _logger.LogWarning("任务 {TaskId} 不存在", taskId);
            return null;
        }

        var taskPrompt = task.Prompt;
        if (string.IsNullOrEmpty(taskPrompt))
        {
            _logger.LogInformation("任务 {TaskId} 未配置任务提示词，跳过后续操作", taskId);
            return null;
        }

        _logger.LogInformation("任务提示词长度: {Length}", taskPrompt.Length);

        var workDir = _workspaceService.GetAgentDirById(collaborationId, taskId, agentId);
        var repoDir = _workspaceService.GetAgentRepoDirById(collaborationId, taskId, agentId);
        _workspaceService.EnsureDirectoryExists(workDir);

        _logger.LogInformation("工作目录: {WorkDir}", workDir);
        _logger.LogInformation("仓库目录: {RepoDir}", repoDir);

        var participants = messages
            .Select(m => m.FromAgentName)
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct()
            .ToList();

        var conversationContent = BuildConversationContent(messages);

        taskPrompt = ReplaceTemplateVariables(taskPrompt, new TemplateVariables
        {
            AgentName = agentName,
            AgentType = agentType ?? "专家",
            Members = string.Join(", ", participants)
        });

        var agentIdentity = string.IsNullOrEmpty(agentPrompt) 
            ? $"你是 {agentName}，一个任务执行专家。" 
            : agentPrompt;

        var systemPrompt = $@"{agentIdentity}

## 群聊讨论记录

**讨论主题**: {topic}
**参与人员**: {string.Join(", ", participants)}
**消息总数**: {messages.Count}

{conversationContent}

## 系统信息

**工作目录**: {workDir}
**仓库目录**: {repoDir}

## 用户任务要求

{taskPrompt}";

        try
        {
            _logger.LogInformation("开始执行Agent任务...");

            var response = await chatClient.GetResponseAsync(
                new List<ChatMessage> { new(ChatRole.User, systemPrompt) },
                new ChatOptions { Temperature = 0.3f, MaxOutputTokens = 8000 },
                cancellationToken);

            var result = response?.Messages?.LastOrDefault()?.Text ?? "任务执行完成，但无返回结果";
            
            _logger.LogInformation("Agent任务执行完成");
            _logger.LogInformation("执行结果: {Result}", result);

            _logger.LogInformation("========== Agent任务后续操作完成 ==========");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent执行任务后续操作失败");
            return $"任务执行失败: {ex.Message}";
        }
    }

    private string BuildConversationContent(List<Message> messages)
    {
        var sb = new StringBuilder();
        foreach (var msg in messages.OrderBy(m => m.RoundNumber))
        {
            var role = msg.FromAgentRole ?? "成员";
            sb.AppendLine($"**【{msg.FromAgentName}】({role})**:");
            sb.AppendLine(msg.Content);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private string ReplaceTemplateVariables(string template, TemplateVariables variables)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        var result = template;
        
        result = Regex.Replace(result, @"\{\{\s*agent_name\s*\}\}", variables.AgentName ?? "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*agent_type\s*\}\}", variables.AgentType ?? "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{\{\s*members\s*\}\}", variables.Members ?? "", RegexOptions.IgnoreCase);

        _logger.LogInformation("模板变量替换完成: agent_name={AgentName}, agent_type={AgentType}, members={Members}", 
            variables.AgentName, variables.AgentType, variables.Members);

        return result;
    }

    private class TemplateVariables
    {
        public string? AgentName { get; set; }
        public string? AgentType { get; set; }
        public string? Members { get; set; }
    }
}
