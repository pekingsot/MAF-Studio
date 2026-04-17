using MAFStudio.Application.DTOs;
using MAFStudio.Application.Prompts;
using MAFStudio.Application.Workflows;
using MAFStudio.Application.Workflows.Selection;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Core.Exceptions;
using MAFStudio.Core.Interfaces.Repositories;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Services;

public partial class CollaborationWorkflowService
{
    public async IAsyncEnumerable<ChatMessageDto> ExecuteGroupChatAsync(
        long collaborationId,
        string input,
        GroupChatParameters? parameters = null,
        long? taskId = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        parameters ??= new GroupChatParameters();
        TaskConfig? taskConfig = await LoadTaskConfigAsync(taskId);

        if (taskConfig != null)
        {
            parameters = taskConfig.ToGroupChatParameters();
            _logger.LogInformation("从任务配置加载参数: Mode={Mode}, MaxIterations={MaxIterations}",
                parameters.OrchestrationMode, parameters.MaxIterations);
        }

        var collaboration = await _collaborationRepository.GetByIdAsync(collaborationId)
            ?? throw new NotFoundException($"协作 {collaborationId} 不存在");

        var members = await LoadMembersAsync(collaborationId, taskId, taskConfig);
        if (members.Count == 0)
        {
            throw new InvalidOperationException("没有可用的Agent");
        }

        var agentIds = members.Select(m => m.AgentId).ToList();
        await SetAgentStatusesAsync(agentIds, AgentStatus.Busy);

        try
        {
            var agentEntities = await LoadAgentEntitiesAsync(members);
            var session = await CreateSessionAsync(collaborationId, taskId, input, agentEntities, parameters);

            var (mafAgents, managerAgent, agentIdToNameMap, agentIdMap) =
                await BuildAgentsAsync(members, agentEntities, parameters, taskConfig, taskId);

            var thinkingQueue = new System.Collections.Concurrent.ConcurrentQueue<ManagerThinkingEventArgs>();

            var workflow = BuildGroupChatWorkflow(mafAgents, managerAgent, parameters, thinkingQueue);

            _logger.LogInformation("开始执行GroupChat工作流，模式: {Mode}", parameters.OrchestrationMode);

            await using var run = await InProcessExecution.RunStreamingAsync<List<ChatMessage>>(workflow,
                [new ChatMessage(ChatRole.User, input)]);
            await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

            var processingContext = new WorkflowProcessingContext
            {
                CollaborationId = collaborationId,
                TaskId = taskId,
                SessionId = session.Id,
                AgentIdToNameMap = agentIdToNameMap,
                AgentIdMap = agentIdMap,
                Members = members,
                ThinkingQueue = thinkingQueue
            };

            await foreach (var msg in _eventProcessor.ProcessStreamAsync(
                CastEvents(run.WatchStreamAsync()), processingContext, cancellationToken))
            {
                yield return msg;
            }

            await _workflowSessionRepository.EndSessionAsync(session.Id, conclusion: "工作流完成");
            _logger.LogInformation("工作流会话结束: {SessionId}", session.Id);
        }
        finally
        {
            await SetAgentStatusesAsync(agentIds, AgentStatus.Active);
            _logger.LogInformation("已将 {Count} 个智能体状态恢复为Active", agentIds.Count);
        }
    }

    private async Task<TaskConfig?> LoadTaskConfigAsync(long? taskId)
    {
        if (!taskId.HasValue || taskId.Value <= 0) return null;

        var task = await _taskRepository.GetByIdAsync(taskId.Value);
        if (task == null) return null;

        _taskContextService.SetCurrentTask(task);

        return string.IsNullOrEmpty(task.Config) ? null : TaskConfig.FromJson(task.Config);
    }

    private async Task<List<CollaborationAgent>> LoadMembersAsync(
        long collaborationId, long? taskId, TaskConfig? taskConfig)
    {
        var allMembers = await _collaborationAgentRepository.GetByCollaborationIdAsync(collaborationId);

        if (taskId.HasValue && taskId.Value > 0 && taskConfig != null)
        {
            var selectedIds = new List<long>();
            if (taskConfig.ManagerAgentId.HasValue)
                selectedIds.Add(taskConfig.ManagerAgentId.Value);
            if (taskConfig.WorkerAgents?.Count > 0)
                selectedIds.AddRange(taskConfig.WorkerAgents.Select(w => w.AgentId));

            var filtered = allMembers.Where(ca => selectedIds.Contains(ca.AgentId)).ToList();
            _logger.LogInformation("从任务配置获取Agent: TaskId={TaskId}, Count={Count}",
                taskId.Value, filtered.Count);
            return filtered;
        }

        _logger.LogInformation("从团队表获取Agent: CollaborationId={Id}, Count={Count}",
            collaborationId, allMembers.Count);
        return allMembers;
    }

    private async Task<List<(CollaborationAgent Member, Agent Entity)>> LoadAgentEntitiesAsync(
        List<CollaborationAgent> members)
    {
        var result = new List<(CollaborationAgent, Agent)>();
        foreach (var member in members)
        {
            var entity = await _agentRepository.GetByIdAsync(member.AgentId);
            if (entity != null)
                result.Add((member, entity));
        }
        return result;
    }

    private async Task<WorkflowSession> CreateSessionAsync(
        long collaborationId, long? taskId, string input,
        List<(CollaborationAgent Member, Agent Entity)> agentEntities,
        GroupChatParameters parameters)
    {
        var managerNames = agentEntities
            .Where(x => x.Member.Role?.Equals("Manager", StringComparison.OrdinalIgnoreCase) == true)
            .Select(x => x.Entity.Name).ToList();
        var workerNames = agentEntities
            .Where(x => x.Member.Role?.Equals("Worker", StringComparison.OrdinalIgnoreCase) == true ||
                        string.IsNullOrEmpty(x.Member.Role))
            .Select(x => x.Entity.Name).ToList();

        var metadata = System.Text.Json.JsonSerializer.Serialize(new
        {
            workflowMode = "群聊",
            orchestrationMode = parameters.OrchestrationMode.ToString(),
            maxIterations = parameters.MaxIterations,
            managerNames,
            workerNames,
            totalAgents = agentEntities.Count
        });

        var session = await _workflowSessionRepository.CreateAsync(new WorkflowSession
        {
            CollaborationId = collaborationId,
            TaskId = taskId,
            WorkflowType = "GroupChat",
            OrchestrationMode = parameters.OrchestrationMode.ToString(),
            Status = "running",
            Topic = input.Length > 200 ? input[..200] + "..." : input,
            Metadata = metadata,
            StartedAt = DateTime.UtcNow
        });

        _logger.LogInformation("创建工作流会话: {SessionId}", session.Id);
        return session;
    }

    private async Task<(List<ChatClientAgent> Agents, ChatClientAgent? Manager,
        Dictionary<string, string> IdToNameMap, Dictionary<string, long> IdMap)>
        BuildAgentsAsync(
            List<CollaborationAgent> members,
            List<(CollaborationAgent Member, Agent Entity)> agentEntities,
            GroupChatParameters parameters,
            TaskConfig? taskConfig,
            long? taskId)
    {
        var mafAgents = new List<ChatClientAgent>();
        var agentIdToNameMap = new Dictionary<string, string>();
        var agentIdMap = new Dictionary<string, long>();
        ChatClientAgent? managerAgent = null;

        var membersInfo = BuildMembersInfo(agentEntities);
        var (taskDescription, taskPrompt) = await LoadTaskPromptsAsync(taskId);

        foreach (var (member, agentEntity) in agentEntities)
        {
            var isManager = member.Role?.Equals("Manager", StringComparison.OrdinalIgnoreCase) == true;

            var chatClient = isManager
                ? await _agentFactory.CreateAgentWithoutCapabilitiesAsync(member.AgentId)
                : await _agentFactory.CreateAgentAsync(member.AgentId);

            var basePrompt = ResolveBasePrompt(isManager, member, agentEntity, taskConfig);
            var systemPrompt = _promptBuilderFactory.Create(parameters.OrchestrationMode)
                .BuildPrompt(new SystemPromptContext
                {
                    AgentName = agentEntity.Name,
                    AgentRole = member.Role ?? "Worker",
                    AgentTypeName = agentEntity.TypeName ?? "",
                    MembersInfo = membersInfo,
                    TaskDescription = taskDescription,
                    TaskPrompt = taskPrompt,
                    AgentPrompt = basePrompt
                });

            var agent = new ChatClientAgent(
                chatClient,
                systemPrompt,
                agentEntity.Name,
                $"专业{agentEntity.TypeName ?? "专家"}");

            agentIdToNameMap[agent.Id] = agentEntity.Name;
            agentIdMap[agent.Id] = member.AgentId;

            if (isManager)
            {
                managerAgent = agent;
                _logger.LogInformation("识别主Agent: Name={Name}", agentEntity.Name);
            }

            mafAgents.Add(agent);
        }

        return (mafAgents, managerAgent, agentIdToNameMap, agentIdMap);
    }

    private Workflow BuildGroupChatWorkflow(
        List<ChatClientAgent> mafAgents,
        ChatClientAgent? managerAgent,
        GroupChatParameters parameters,
        System.Collections.Concurrent.ConcurrentQueue<ManagerThinkingEventArgs> thinkingQueue)
    {
        return AgentWorkflowBuilder
            .CreateGroupChatBuilderWith(agents =>
            {
                var allAgents = new List<AIAgent>(agents);
                if (managerAgent != null && !allAgents.Any(a => a.Name == managerAgent.Name))
                {
                    allAgents.Insert(0, managerAgent);
                }

                return CreateGroupChatManager(allAgents, managerAgent, parameters, thinkingQueue);
            })
            .AddParticipants(mafAgents.ToArray())
            .Build();
    }

    private GroupChatManager CreateGroupChatManager(
        IReadOnlyList<AIAgent> allAgents,
        ChatClientAgent? managerAgent,
        GroupChatParameters parameters,
        System.Collections.Concurrent.ConcurrentQueue<ManagerThinkingEventArgs> thinkingQueue)
    {
        if (parameters.OrchestrationMode == GroupChatOrchestrationMode.RoundRobin)
        {
            _logger.LogInformation("使用RoundRobinGroupChatManager");
            return new RoundRobinGroupChatManager(allAgents)
            {
                MaximumIterationCount = parameters.MaxIterations
            };
        }

        if (managerAgent == null)
        {
            _logger.LogWarning("无Manager，回退到RoundRobin");
            return new RoundRobinGroupChatManager(allAgents)
            {
                MaximumIterationCount = parameters.MaxIterations
            };
        }

        _logger.LogInformation("使用OrchestratedGroupChatManager，协调者: {Name}", managerAgent.Name);

        var workerAgents = allAgents.Where(a => a.Name != managerAgent.Name).ToList();
        var strategy = AgentSelectionStrategyFactory.CreateOrchestratedStrategy(
            _loggerFactory.CreateLogger<CompositeSelectionStrategy>());

        var manager = new OrchestratedGroupChatManager(
            managerAgent,
            workerAgents,
            strategy,
            parameters.MaxIterations,
            _loggerFactory.CreateLogger<OrchestratedGroupChatManager>());

        manager.ManagerThinking += (_, args) =>
        {
            thinkingQueue.Enqueue(args);
            _logger.LogInformation("[ManagerThinking] {Name}: {Thinking}", args.ManagerName, args.Thinking);
        };

        return manager;
    }

    private string ResolveBasePrompt(
        bool isManager,
        CollaborationAgent member,
        Agent agentEntity,
        TaskConfig? taskConfig)
    {
        if (isManager && taskConfig?.ManagerCustomPrompt != null)
        {
            _logger.LogInformation("使用自定义协调者提示词");
            return taskConfig.ManagerCustomPrompt;
        }

        return member.CustomPrompt ?? agentEntity.SystemPrompt ?? "You are a helpful assistant.";
    }

    private async Task<(string? Description, string? Prompt)> LoadTaskPromptsAsync(long? taskId)
    {
        if (!taskId.HasValue || taskId.Value <= 0)
            return (null, null);

        var task = await _taskRepository.GetByIdAsync(taskId.Value);
        return task == null ? (null, null) : (task.Description, task.Prompt);
    }

    private async Task SetAgentStatusesAsync(List<long> agentIds, AgentStatus status)
    {
        foreach (var agentId in agentIds)
        {
            await _agentRepository.UpdateStatusAsync(agentId, status);
        }
    }

    private string BuildMembersInfo(List<(CollaborationAgent Member, Agent Entity)> agentEntities)
    {
        var members = agentEntities
            .Where(a => a.Member.Role?.Equals("Manager", StringComparison.OrdinalIgnoreCase) != true)
            .Select(a => $"- {a.Entity.Name}：负责{a.Entity.TypeName ?? "成员"}");

        return string.Join("\n", members);
    }

    private static async IAsyncEnumerable<object> CastEvents<T>(IAsyncEnumerable<T> source)
    {
        await foreach (var item in source)
        {
            yield return item!;
        }
    }
}
