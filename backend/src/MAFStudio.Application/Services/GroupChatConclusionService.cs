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
        _logger.LogInformation("========== 开始让Agent执行写文档任务 ==========");
        _logger.LogInformation("任务ID: {TaskId}, 协作ID: {CollaborationId}, 协调者Agent: {AgentName} (ID: {ManagerAgentId})", 
            taskId, collaborationId, managerAgentName, managerAgentId);

        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null)
        {
            _logger.LogWarning("任务 {TaskId} 不存在", taskId);
            return null;
        }

        var gitUrl = task.GitUrl;
        var gitBranch = task.GitBranch ?? "main";
        var gitToken = task.GitCredentials;

        if (string.IsNullOrEmpty(gitUrl))
        {
            _logger.LogInformation("任务 {TaskId} 未配置Git URL，跳过文档提交", taskId);
            return null;
        }

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

        var taskPrompt = $@"你是一个文档生成专家。请完成以下任务：

## 任务目标
根据群聊讨论内容，生成一份结构化的总结文档，并提交到Git仓库。

## 讨论信息
- **讨论主题**: {topic}
- **参与人员**: {string.Join(", ", participants)}
- **消息总数**: {messages.Count}

## 讨论内容
{conversationContent}

## Git配置
- **仓库地址**: {gitUrl}
- **分支**: {gitBranch}
- **访问令牌**: {(string.IsNullOrEmpty(gitToken) ? "未配置" : gitToken)}

## 工作目录
- **你的工作目录**: {workDir}
- **仓库克隆目录**: {repoDir}

## 执行步骤
1. 使用Git工具克隆仓库到指定目录（如果访问令牌不为空，请在URL中嵌入令牌）
2. 在仓库的 `docs/discussions/` 目录下创建总结文档
3. 文档名称格式: `discussion_summary_{DateTime.Now:yyyyMMdd_HHmmss}.md`
4. 文档内容要求:
   - 使用Markdown格式
   - 包含：讨论概述、主要观点和结论、行动项和待办事项、决策记录、后续建议
   - 包含完整讨论记录
5. 使用Git工具添加文件到暂存区 (AddFiles)
6. 使用Git工具提交更改 (Commit)，提交信息格式: `docs: 添加群聊讨论总结文档 [任务{taskId}]`
7. 使用Git工具推送到远程仓库 (Push)

**重要**: 必须按顺序执行: AddFiles -> Commit -> Push，不能跳过Commit步骤！

## 注意事项
- 所有文件路径使用绝对路径
- Git提交时使用你的名称 ({managerAgentName}) 作为提交者
- 如果推送失败，请报告错误信息

请开始执行任务。";

        try
        {
            _logger.LogInformation("开始执行Agent任务...");

            var response = await chatClient.GetResponseAsync(
                new List<ChatMessage> { new(ChatRole.User, taskPrompt) },
                new ChatOptions { Temperature = 0.3f, MaxOutputTokens = 8000 },
                cancellationToken);

            var result = response?.Messages?.LastOrDefault()?.Text ?? "任务执行完成，但无返回结果";
            
            _logger.LogInformation("Agent任务执行完成");
            _logger.LogInformation("执行结果: {Result}", result);

            _logger.LogInformation("========== Agent写文档任务完成 ==========");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent执行写文档任务失败");
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
