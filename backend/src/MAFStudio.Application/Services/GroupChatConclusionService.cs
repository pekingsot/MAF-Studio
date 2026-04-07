using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text;

namespace MAFStudio.Application.Services;

public interface IGroupChatConclusionService
{
    Task<string?> GenerateAndCommitConclusionAsync(
        long taskId,
        long collaborationId,
        string topic,
        List<Message> messages,
        long managerAgentId,
        string managerAgentName,
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
        long managerAgentId,
        string managerAgentName,
        IChatClient chatClient,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("========== 开始让Agent执行任务后续操作 ==========");
        _logger.LogInformation("任务ID: {TaskId}, 协作ID: {CollaborationId}, 协调者Agent: {AgentName} (ID: {ManagerAgentId})", 
            taskId, collaborationId, managerAgentName, managerAgentId);

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

        var workDir = _workspaceService.GetAgentDirById(collaborationId, taskId, managerAgentId);
        var repoDir = _workspaceService.GetAgentRepoDirById(collaborationId, taskId, managerAgentId);
        _workspaceService.EnsureDirectoryExists(workDir);

        _logger.LogInformation("工作目录: {WorkDir}", workDir);
        _logger.LogInformation("仓库目录: {RepoDir}", repoDir);

        var participants = messages
            .Select(m => m.FromAgentName)
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct()
            .ToList();

        var conversationContent = BuildConversationContent(messages);

        var systemPrompt = $@"你是一个任务执行专家。请根据以下信息完成任务后续操作。

## 群聊讨论信息
- **讨论主题**: {topic}
- **参与人员**: {string.Join(", ", participants)}
- **消息总数**: {messages.Count}

## 群聊讨论内容
{conversationContent}

## 工作目录
- **你的工作目录**: {workDir}
- **仓库克隆目录**: {repoDir}

## 任务提示词（用户定义的任务要求）
{taskPrompt}

## 执行要求
1. 仔细阅读任务提示词，理解用户的要求
2. 根据任务提示词中的指令执行相应操作（如：生成文档、Git提交、发送邮件等）
3. 如果任务提示词中包含Git配置，请按以下步骤操作：
   - 克隆仓库（如果提供了访问令牌，请在URL中嵌入令牌）
   - 创建文档或修改文件
   - 添加文件到暂存区 (AddFiles)
   - 提交更改 (Commit)
   - 推送到远程仓库 (Push)
4. 所有文件路径使用绝对路径
5. Git提交时使用你的名称 ({managerAgentName}) 作为提交者

请开始执行任务。";

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
}
