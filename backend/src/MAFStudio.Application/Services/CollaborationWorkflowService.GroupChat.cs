using MAFStudio.Application.DTOs;
using MAFStudio.Application.Prompts;
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

            string? taskPrompt = null;
            if (taskId.HasValue && taskId.Value > 0)
            {
                var task = await _taskRepository.GetByIdAsync(taskId.Value);
                if (task != null)
                {
                    taskPrompt = task.Prompt;
                    _logger.LogInformation("任务提示词: HasPrompt={HasPrompt}", !string.IsNullOrEmpty(taskPrompt));
                }
            }

            var mafAgents = new List<ChatClientAgent>();
            var agentIdToNameMap = new Dictionary<string, string>();
            ChatClientAgent? managerAgent = null;
            IChatClient? orchestratorChatClient = null;
            long managerAgentId = 0;

            foreach (var (member, agentEntity) in agentEntities)
            {
                var chatClient = await _agentFactory.CreateAgentAsync(member.AgentId);
                
                var basePrompt = member.CustomPrompt ?? agentEntity.SystemPrompt ?? "You are a helpful assistant.";
                
                var promptBuilder = _promptBuilderFactory.Create(parameters.OrchestrationMode);
                var systemPrompt = promptBuilder.BuildPrompt(new SystemPromptContext
                {
                    AgentName = agentEntity.Name,
                    AgentRole = member.Role ?? "Worker",
                    AgentType = agentEntity.TypeName ?? "",
                    MembersInfo = membersInfo,
                    TaskPrompt = taskPrompt,
                    AgentPrompt = basePrompt
                });
                
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
                    managerAgentId = member.AgentId;
                    orchestratorChatClient = chatClient;
                    _logger.LogInformation("识别主Agent: Id={Id}, Name={Name}, DBId={DBId}", agent.Id, agentEntity.Name, member.AgentId);
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

            if (taskId.HasValue && taskId.Value > 0 && session != null && managerAgentId > 0 && orchestratorChatClient != null)
            {
                _logger.LogInformation("开始让协调者Agent生成群聊总结文档...");
                
                yield return new ChatMessageDto
                {
                    Sender = "System",
                    Content = "📝 正在让协调者Agent生成讨论总结文档...",
                    Timestamp = DateTime.UtcNow,
                    Role = "system"
                };

                string? conclusionResult = null;
                string? errorMessage = null;
                
                try
                {
                    var sessionMessages = await _messageRepository.GetBySessionIdAsync(session.Id);
                    
                    conclusionResult = await _conclusionService.GenerateAndCommitConclusionAsync(
                        taskId.Value,
                        collaborationId,
                        input,
                        sessionMessages.ToList(),
                        managerAgentId,
                        managerAgent?.Name ?? "协调者",
                        orchestratorChatClient,
                        cancellationToken);
                        
                    _logger.LogInformation("总结文档结果: {Result}", conclusionResult);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "生成总结文档失败");
                    errorMessage = ex.Message;
                }

                if (!string.IsNullOrEmpty(conclusionResult))
                {
                    yield return new ChatMessageDto
                    {
                        Sender = "System",
                        Content = $"📄 {conclusionResult}",
                        Timestamp = DateTime.UtcNow,
                        Role = "system"
                    };
                }
                else if (!string.IsNullOrEmpty(errorMessage))
                {
                    yield return new ChatMessageDto
                    {
                        Sender = "System",
                        Content = $"⚠️ 总结文档生成失败: {errorMessage}",
                        Timestamp = DateTime.UtcNow,
                        Role = "system"
                    };
                }
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
}
