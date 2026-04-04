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

        CoordinationSession? session = null;
        var roundNumber = 0;
        var agentIdMap = new Dictionary<string, long>();

        try
        {
            session = new CoordinationSession
            {
                CollaborationId = collaborationId,
                OrchestrationMode = parameters.OrchestrationMode.ToString(),
                Status = "running",
                Topic = input.Length > 200 ? input.Substring(0, 200) + "..." : input,
                StartTime = DateTime.UtcNow
            };
            session = await _coordinationSessionRepository.CreateAsync(session);
            _logger.LogInformation("创建协调会话: {SessionId}", session.Id);

            var mafAgents = new List<ChatClientAgent>();
            var agentIdToNameMap = new Dictionary<string, string>();
            ChatClientAgent? managerAgent = null;
            IChatClient? orchestratorChatClient = null;

            foreach (var member in members)
            {
                var agentEntity = await _agentRepository.GetByIdAsync(member.AgentId);
                if (agentEntity == null)
                {
                    _logger.LogWarning("无法找到Agent {AgentId}", member.AgentId);
                    continue;
                }

                var chatClient = await _agentFactory.CreateAgentAsync(member.AgentId);
                
                var systemPrompt = member.CustomPrompt ?? agentEntity.SystemPrompt ?? "You are a helpful assistant.";
                
                var isManager = member.Role?.Equals("Manager", StringComparison.OrdinalIgnoreCase) == true;
                
                var agent = new ChatClientAgent(
                    chatClient,
                    systemPrompt,
                    agentEntity.Name,
                    member.Role ?? "Worker"
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

                var participant = new CoordinationParticipant
                {
                    SessionId = session.Id,
                    AgentId = member.AgentId,
                    AgentName = agentEntity.Name,
                    AgentRole = member.Role,
                    IsManager = isManager
                };
                await _coordinationParticipantRepository.CreateAsync(participant);
                _logger.LogInformation("添加协调参与者: {AgentName}, 角色: {Role}", agentEntity.Name, member.Role);
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
                                await _coordinationRoundRepository.CreateAsync(new CoordinationRound
                                {
                                    SessionId = session.Id,
                                    RoundNumber = roundNumber,
                                    SpeakerAgentId = agentId,
                                    SpeakerName = agentName,
                                    SpeakerRole = members.FirstOrDefault(m => m.AgentId == agentId)?.Role,
                                    MessageContent = content
                                });
                                await _coordinationParticipantRepository.IncrementSpeakCountAsync(session.Id, agentId);
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
                            await _coordinationRoundRepository.CreateAsync(new CoordinationRound
                            {
                                SessionId = session.Id,
                                RoundNumber = roundNumber,
                                SpeakerAgentId = agentId,
                                SpeakerName = agentName,
                                SpeakerRole = members.FirstOrDefault(m => m.AgentId == agentId)?.Role,
                                MessageContent = content
                            });
                            await _coordinationParticipantRepository.IncrementSpeakCountAsync(session.Id, agentId);
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
                session.Status = "completed";
                session.EndTime = DateTime.UtcNow;
                session.TotalRounds = roundNumber;
                await _coordinationSessionRepository.UpdateAsync(session);
                _logger.LogInformation("协调会话结束: {SessionId}, 总轮次: {Rounds}", session.Id, roundNumber);
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
        return new ManagerGroupChatManager(managerAgent, agents, maxIterations);
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
}
