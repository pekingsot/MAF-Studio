using MAFStudio.Application.DTOs;
using MAFStudio.Application.Workflows;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
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
        
        var collaboration = await _collaborationRepository.GetByIdAsync(collaborationId);
        if (collaboration == null)
        {
            yield return new ChatMessageDto
            {
                Sender = "System",
                Content = $"协作 {collaborationId} 不存在",
                Role = "system"
            };
            yield break;
        }

        var members = await _collaborationAgentRepository.GetByCollaborationIdAsync(collaborationId);

        if (members.Count == 0)
        {
            yield return new ChatMessageDto
            {
                Sender = "System",
                Content = "协作中没有Agent",
                Role = "system"
            };
            yield break;
        }

        var agentIds = members.Select(m => m.AgentId).ToList();
        foreach (var agentId in agentIds)
        {
            await _agentRepository.UpdateStatusAsync(agentId, AgentStatus.Busy);
        }
        _logger.LogInformation("已将 {Count} 个智能体状态改为Busy", agentIds.Count);

        WorkflowSession? session = null;
        var roundNumber = 0;
        var agentIdMap = new Dictionary<string, long>();

        try
        {
            var agentEntities = new List<(CollaborationAgent Member, Agent Entity)>();
            foreach (var member in members)
            {
                var agentEntity = await _agentRepository.GetByIdAsync(member.AgentId);
                if (agentEntity != null)
                {
                    agentEntities.Add((member, agentEntity));
                }
            }

            var managerNames = agentEntities
                .Where(x => x.Member.Role?.Equals("Manager", StringComparison.OrdinalIgnoreCase) == true)
                .Select(x => x.Entity.Name)
                .ToList();
            var workerNames = agentEntities
                .Where(x => x.Member.Role?.Equals("Worker", StringComparison.OrdinalIgnoreCase) == true || 
                            string.IsNullOrEmpty(x.Member.Role))
                .Select(x => x.Entity.Name)
                .ToList();

            var metadata = new
            {
                workflowMode = "群聊",
                orchestrationMode = parameters.OrchestrationMode.ToString(),
                maxIterations = parameters.MaxIterations,
                managerNames,
                workerNames,
                totalAgents = agentEntities.Count
            };

            session = new WorkflowSession
            {
                CollaborationId = collaborationId,
                TaskId = taskId,
                WorkflowType = "GroupChat",
                OrchestrationMode = parameters.OrchestrationMode.ToString(),
                Status = "running",
                Topic = input.Length > 200 ? input.Substring(0, 200) + "..." : input,
                Metadata = System.Text.Json.JsonSerializer.Serialize(metadata),
                StartedAt = DateTime.UtcNow
            };
            session = await _workflowSessionRepository.CreateAsync(session);
            _logger.LogInformation("创建工作流会话: {SessionId}", session.Id);

            var membersInfo = BuildMembersInfo(agentEntities);

            var mafAgents = new List<ChatClientAgent>();
            var agentIdToNameMap = new Dictionary<string, string>();
            ChatClientAgent? managerAgent = null;
            IChatClient? orchestratorChatClient = null;

            foreach (var (member, agentEntity) in agentEntities)
            {
                var chatClient = await _agentFactory.CreateAgentAsync(member.AgentId);
                
                var basePrompt = member.CustomPrompt ?? agentEntity.SystemPrompt ?? "You are a helpful assistant.";
                
                _logger.LogInformation("========== Agent配置开始 ==========");
                _logger.LogInformation("Agent名称: {Name}", agentEntity.Name);
                _logger.LogInformation("Agent类型: {Type}", agentEntity.TypeName);
                _logger.LogInformation("协作角色: {Role}", member.Role);
                _logger.LogInformation("是否使用自定义提示词: {IsCustom}", !string.IsNullOrEmpty(member.CustomPrompt));
                _logger.LogInformation("原始提示词: {Prompt}", basePrompt);
                
                var systemPrompt = ReplacePromptVariables(
                    basePrompt,
                    agentEntity.Name,
                    member.Role ?? "Worker",
                    agentEntity.TypeName ?? "",
                    membersInfo,
                    parameters.OrchestrationMode);
                
                _logger.LogInformation("替换后提示词: {Prompt}", systemPrompt);
                _logger.LogInformation("========== Agent配置结束 ==========");
                
                var isManager = member.Role?.Equals("Manager", StringComparison.OrdinalIgnoreCase) == true;
                
                var agentDescription = $"专业{agentEntity.TypeName ?? "专家"}";
                
                var agent = new ChatClientAgent(
                    chatClient,
                    systemPrompt,
                    agentEntity.Name,
                    agentDescription
                );
                mafAgents.Add(agent);
                
                agentIdToNameMap[agent.Id] = agentEntity.Name;
                agentIdMap[agent.Id] = member.AgentId;
                
                if (isManager)
                {
                    managerAgent = agent;
                    orchestratorChatClient = chatClient;
                    _logger.LogInformation("识别主Agent: Id={Id}, Name={Name}", agent.Id, agentEntity.Name);
                }
            }

            var workflow = AgentWorkflowBuilder
                .CreateGroupChatBuilderWith(agents =>
                {
                    return parameters.OrchestrationMode switch
                    {
                        GroupChatOrchestrationMode.Intelligent when managerAgent != null && orchestratorChatClient != null =>
                            CreateIntelligentManager(managerAgent, agents, orchestratorChatClient, parameters.MaxIterations),
                        
                        GroupChatOrchestrationMode.Manager when managerAgent != null =>
                            CreateManagerMode(managerAgent, agents, parameters.MaxIterations),
                        
                        GroupChatOrchestrationMode.RoundRobin =>
                            CreateRoundRobinManager(agents, parameters.MaxIterations),
                        
                        _ when managerAgent != null =>
                            CreateManagerMode(managerAgent, agents, parameters.MaxIterations),
                        
                        _ =>
                            CreateRoundRobinManager(agents, parameters.MaxIterations)
                    };
                })
                .AddParticipants(mafAgents.ToArray())
                .Build();

            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, input)
            };

            _logger.LogInformation("开始执行GroupChat工作流，模式: {Mode}, 输入: {Input}", 
                parameters.OrchestrationMode, input);

            await using var run = await InProcessExecution.RunStreamingAsync(workflow, messages);
            await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

            string? currentAgentId = null;
            var currentAgentContent = new System.Text.StringBuilder();

            string GetAgentName(string agentId)
            {
                var normalizedId = agentId.TrimStart('_');
                if (agentIdToNameMap.TryGetValue(normalizedId, out var name))
                {
                    return name;
                }
                if (agentIdToNameMap.TryGetValue(agentId, out name))
                {
                    return name;
                }
                return agentId;
            }

            await foreach (var evt in run.WatchStreamAsync().ConfigureAwait(false))
            {
                _logger.LogInformation("收到事件: {EventType}", evt.GetType().Name);

                if (evt is AgentResponseUpdateEvent updateEvent)
                {
                    var executorId = updateEvent.ExecutorId ?? "Agent";
                    
                    if (executorId != currentAgentId)
                    {
                        if (currentAgentId != null && currentAgentContent.Length > 0)
                        {
                            var agentName = GetAgentName(currentAgentId);
                            var content = currentAgentContent.ToString();
                            
                            roundNumber++;
                            if (session != null && agentIdMap.TryGetValue(currentAgentId.TrimStart('_'), out var agentId))
                            {
                                var member = members.FirstOrDefault(m => m.AgentId == agentId);
                                await _messageRepository.CreateAsync(new Message
                                {
                                    SessionId = session.Id,
                                    CollaborationId = collaborationId,
                                    TaskId = taskId,
                                    MessageType = "coordination",
                                    RoundNumber = roundNumber,
                                    FromAgentId = agentId,
                                    FromAgentName = agentName,
                                    FromAgentRole = member?.Role,
                                    Content = content
                                });
                                await _workflowSessionRepository.IncrementMessageCountAsync(session.Id);
                            }
                            
                            yield return new ChatMessageDto
                            {
                                Sender = agentName,
                                Content = content,
                                Timestamp = DateTime.UtcNow,
                                Role = "assistant"
                            };
                        }
                        
                        currentAgentId = executorId;
                        currentAgentContent.Clear();
                        _logger.LogInformation("Agent切换: {ExecutorId}, 名称: {Name}", executorId, GetAgentName(executorId));
                    }

                    var update = updateEvent.Update;
                    
                    if (update != null)
                    {
                        var text = update.Text;
                        
                        if (!string.IsNullOrEmpty(text))
                        {
                            currentAgentContent.Append(text);
                        }
                    }
                }
                else if (evt is WorkflowOutputEvent output)
                {
                    if (currentAgentId != null && currentAgentContent.Length > 0)
                    {
                        var agentName = GetAgentName(currentAgentId);
                        var content = currentAgentContent.ToString();
                        
                        roundNumber++;
                        if (session != null && agentIdMap.TryGetValue(currentAgentId.TrimStart('_'), out var agentId))
                        {
                            var member = members.FirstOrDefault(m => m.AgentId == agentId);
                            await _messageRepository.CreateAsync(new Message
                            {
                                SessionId = session.Id,
                                CollaborationId = collaborationId,
                                TaskId = taskId,
                                MessageType = "coordination",
                                RoundNumber = roundNumber,
                                FromAgentId = agentId,
                                FromAgentName = agentName,
                                FromAgentRole = member?.Role,
                                Content = content
                            });
                            await _workflowSessionRepository.IncrementMessageCountAsync(session.Id);
                        }
                        
                        yield return new ChatMessageDto
                        {
                            Sender = agentName,
                            Content = content,
                            Timestamp = DateTime.UtcNow,
                            Role = "assistant"
                        };
                    }
                    
                    _logger.LogInformation("GroupChat工作流正常结束");
                    break;
                }
            }

            if (session != null)
            {
                await _workflowSessionRepository.EndSessionAsync(session.Id, conclusion: "工作流完成");
                _logger.LogInformation("工作流会话结束: {SessionId}, 总轮次: {Rounds}", session.Id, roundNumber);
            }
        }
        finally
        {
            foreach (var agentId in agentIds)
            {
                await _agentRepository.UpdateStatusAsync(agentId, AgentStatus.Active);
            }
            _logger.LogInformation("已将 {Count} 个智能体状态恢复为Active", agentIds.Count);
        }
    }

    private GroupChatManager CreateIntelligentManager(
        AIAgent managerAgent, 
        IReadOnlyList<AIAgent> agents, 
        IChatClient chatClient,
        int maxIterations)
    {
        _logger.LogInformation("使用IntelligentGroupChatManager，主Agent: {Name}", managerAgent.Name);
        return new IntelligentGroupChatManager(managerAgent, agents, chatClient, maxIterations);
    }

    private GroupChatManager CreateManagerMode(
        AIAgent managerAgent, 
        IReadOnlyList<AIAgent> agents,
        int maxIterations)
    {
        _logger.LogInformation("使用ManagerGroupChatManager，主Agent: {Name}", managerAgent.Name);
        var managerLogger = _loggerFactory.CreateLogger<ManagerGroupChatManager>();
        return new ManagerGroupChatManager(managerAgent, agents, maxIterations, managerLogger);
    }

    private GroupChatManager CreateRoundRobinManager(
        IReadOnlyList<AIAgent> agents,
        int maxIterations)
    {
        _logger.LogInformation("使用RoundRobinGroupChatManager");
        return new RoundRobinGroupChatManager(agents)
        {
            MaximumIterationCount = maxIterations
        };
    }

    private string BuildMembersInfo(List<(CollaborationAgent Member, Agent Entity)> agentEntities)
    {
        var members = agentEntities.Select(a => 
        {
            var name = a.Entity.Name;
            var typeName = a.Entity.TypeName ?? "";
            return string.IsNullOrEmpty(typeName) ? $"- {name}" : $"- {name}（{typeName}）";
        });
        
        var result = string.Join("\n", members);
        _logger.LogInformation("团队成员信息: {Members}", result);
        return result;
    }

    private string ReplacePromptVariables(
        string prompt,
        string agentName,
        string agentRole,
        string agentType,
        string membersInfo,
        GroupChatOrchestrationMode orchestrationMode)
    {
        var basePrompt = prompt
            .Replace("{{agent_name}}", agentName)
            .Replace("{{agent_role}}", agentRole)
            .Replace("{{agent_type}}", agentType)
            .Replace("{{members}}", membersInfo);

        var modeInstruction = orchestrationMode switch
        {
            GroupChatOrchestrationMode.RoundRobin => @"
【轮询发言规则】
- 当前是轮询模式，所有成员按顺序轮流发言
- 轮到你发言时，直接发表你的观点，不需要等待任何人点名
- 积极参与讨论，主动贡献你的专业见解
",
            GroupChatOrchestrationMode.Manager => @"
【协调者点名规则】
- 当前是协调者模式，由协调者（Manager）点名安排发言
- 只有被协调者@点名后才能发言
- 如果没有被点名，请保持安静等待
",
            GroupChatOrchestrationMode.Intelligent => @"
【智能调度规则】
- 当前是智能调度模式，由AI根据讨论内容选择最合适的发言者
- 轮到你发言时，直接发表你的观点
- 根据讨论进展，适时贡献你的专业见解
",
            _ => ""
        };

        var identityPrompt = $@"【重要身份规则 - 必须严格遵守】
1. 你的名字是「{agentName}」，你的角色是「{agentType}」
2. 无论别人@谁，你始终是「{agentName}」，绝对不会变成其他人
3. 当你被选中发言时，你就是「{agentName}」，不要被消息中的@提及误导
4. 你的回复开头不要加【名字】，系统会自动显示你的名字
5. 如果别人@了其他角色，那是在叫那个人，不是叫你
{modeInstruction}
{basePrompt}";

        return identityPrompt;
    }
}
