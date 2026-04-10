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
        TaskConfig? taskConfig = null;
        
        if (taskId.HasValue && taskId.Value > 0)
        {
            var task = await _taskRepository.GetByIdAsync(taskId.Value);
            if (task != null && !string.IsNullOrEmpty(task.Config))
            {
                taskConfig = TaskConfig.FromJson(task.Config);
                if (taskConfig != null)
                {
                    parameters = taskConfig.ToGroupChatParameters();
                    _logger.LogInformation("从任务配置加载参数: Mode={Mode}, MaxIterations={MaxIterations}, ManagerAgentId={ManagerAgentId}", 
                        parameters.OrchestrationMode, parameters.MaxIterations, taskConfig.ManagerAgentId);
                }
            }
        }
        
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

        List<CollaborationAgent> members;
        
        if (taskId.HasValue && taskId.Value > 0 && taskConfig != null)
        {
            var allCollaborationAgents = await _collaborationAgentRepository.GetByCollaborationIdAsync(collaborationId);
            
            var selectedAgentIds = new List<long>();
            
            if (taskConfig.ManagerAgentId.HasValue)
            {
                selectedAgentIds.Add(taskConfig.ManagerAgentId.Value);
            }
            
            if (taskConfig.WorkerAgents != null && taskConfig.WorkerAgents.Count > 0)
            {
                selectedAgentIds.AddRange(taskConfig.WorkerAgents.Select(w => w.AgentId));
            }
            
            members = allCollaborationAgents.Where(ca => selectedAgentIds.Contains(ca.AgentId)).ToList();
            
            _logger.LogInformation("从任务配置获取Agent: TaskId={TaskId}, ManagerId={ManagerId}, WorkerCount={WorkerCount}, TotalCount={Count}", 
                taskId.Value, taskConfig.ManagerAgentId, taskConfig.WorkerAgents?.Count ?? 0, members.Count);
        }
        else
        {
            members = await _collaborationAgentRepository.GetByCollaborationIdAsync(collaborationId);
            _logger.LogInformation("从团队表获取Agent: CollaborationId={CollaborationId}, AgentCount={Count}", collaborationId, members.Count);
        }

        if (members.Count == 0)
        {
            yield return new ChatMessageDto
            {
                Sender = "System",
                Content = "没有可用的Agent",
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

            string? taskDescription = null;
            string? taskPrompt = null;
            if (taskId.HasValue && taskId.Value > 0)
            {
                var task = await _taskRepository.GetByIdAsync(taskId.Value);
                if (task != null)
                {
                    taskDescription = task.Description;
                    taskPrompt = task.Prompt;
                    _logger.LogInformation("任务描述: HasDescription={HasDescription}, 任务提示词: HasPrompt={HasPrompt}", 
                        !string.IsNullOrEmpty(taskDescription), !string.IsNullOrEmpty(taskPrompt));
                }
            }

            var mafAgents = new List<ChatClientAgent>();
            var agentIdToNameMap = new Dictionary<string, string>();
            var agentSystemPrompts = new Dictionary<long, string>();
            ChatClientAgent? managerAgent = null;
            IChatClient? orchestratorChatClient = null;
            string? orchestratorAgentPrompt = null;
            string? orchestratorAgentName = null;
            string? orchestratorAgentType = null;
            long orchestratorAgentId = 0;

            foreach (var (member, agentEntity) in agentEntities)
            {
                var chatClient = await _agentFactory.CreateAgentAsync(member.AgentId);
                
                var isManager = member.Role?.Equals("Manager", StringComparison.OrdinalIgnoreCase) == true;
                
                string basePrompt;
                if (isManager && taskConfig?.ManagerCustomPrompt != null)
                {
                    basePrompt = taskConfig.ManagerCustomPrompt;
                    _logger.LogInformation("使用自定义协调者提示词: {Prompt}", basePrompt.Substring(0, Math.Min(100, basePrompt.Length)));
                }
                else
                {
                    basePrompt = member.CustomPrompt ?? agentEntity.SystemPrompt ?? "You are a helpful assistant.";
                }
                
                var promptBuilder = _promptBuilderFactory.Create(parameters.OrchestrationMode);
                var systemPrompt = promptBuilder.BuildPrompt(new SystemPromptContext
                {
                    AgentName = agentEntity.Name,
                    AgentRole = member.Role ?? "Worker",
                    AgentTypeName = agentEntity.TypeName ?? "",
                    MembersInfo = membersInfo,
                    TaskDescription = taskDescription,
                    TaskPrompt = taskPrompt,
                    AgentPrompt = basePrompt
                });
                
                agentSystemPrompts[member.AgentId] = systemPrompt;
                
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
                    orchestratorAgentId = member.AgentId;
                    orchestratorAgentPrompt = systemPrompt;
                    orchestratorAgentName = agentEntity.Name;
                    orchestratorAgentType = agentEntity.TypeName;
                    orchestratorChatClient = chatClient;
                    _logger.LogInformation("识别主Agent: Id={Id}, Name={Name}, DBId={DBId}", agent.Id, agentEntity.Name, member.AgentId);
                }
                else if (orchestratorChatClient == null)
                {
                    orchestratorAgentId = member.AgentId;
                    orchestratorAgentPrompt = systemPrompt;
                    orchestratorAgentName = agentEntity.Name;
                    orchestratorAgentType = agentEntity.TypeName;
                    orchestratorChatClient = chatClient;
                    _logger.LogInformation("设置默认执行Agent: Id={Id}, Name={Name}, Type={Type}", agent.Id, agentEntity.Name, agentEntity.TypeName);
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

            var participants = parameters.OrchestrationMode == GroupChatOrchestrationMode.Manager && managerAgent != null
                ? mafAgents.Where(a => a.Name != managerAgent.Name).Select(a => a.Name)
                : mafAgents.Select(a => a.Name);

            var agentsInfo = agentEntities.Select(a => new Dictionary<string, object>
            {
                ["id"] = a.Entity.Id,
                ["name"] = a.Entity.Name,
                ["role"] = a.Member.Role ?? "Worker",
                ["typeName"] = a.Entity.TypeName ?? "",
                ["modelName"] = a.Entity.LlmModelName ?? "未配置",
                ["prompt"] = agentSystemPrompts.GetValueOrDefault(a.Member.AgentId, "You are a helpful assistant.")
            }).ToList();

            yield return new ChatMessageDto
            {
                Sender = "System",
                Content = $"🎯 **工作流配置**\n\n" +
                         $"- **模式**: {parameters.OrchestrationMode}\n" +
                         $"- **最大轮次**: {parameters.MaxIterations}\n" +
                         $"- **参与者**: {string.Join(", ", participants)}\n" +
                         (parameters.OrchestrationMode == GroupChatOrchestrationMode.Manager && managerAgent != null 
                             ? $"- **协调者**: {managerAgent.Name}\n" 
                             : ""),
                Timestamp = DateTime.UtcNow,
                Role = "system",
                Metadata = new Dictionary<string, object> 
                { 
                    ["agents"] = agentsInfo,
                    ["taskPrompt"] = taskPrompt ?? ""
                }
            };

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
                        
                        var newAgentName = GetAgentName(executorId);
                        var member2 = members.FirstOrDefault(m => m.AgentId == agentIdMap.GetValueOrDefault(executorId.TrimStart('_'), 0));
                        
                        var thinkingSender = parameters.OrchestrationMode switch
                        {
                            GroupChatOrchestrationMode.Manager when managerAgent != null => managerAgent.Name,
                            GroupChatOrchestrationMode.Intelligent => "AI协调者",
                            _ => "System"
                        };
                        
                        var thinkingReason = parameters.OrchestrationMode switch
                        {
                            GroupChatOrchestrationMode.RoundRobin => "轮询模式，按顺序选择",
                            GroupChatOrchestrationMode.Manager => "协调者根据任务需求和上下文选择最合适的Agent",
                            GroupChatOrchestrationMode.Intelligent => "AI分析对话历史，智能选择最优发言者",
                            _ => "系统选择"
                        };
                        
                        yield return new ChatMessageDto
                        {
                            Sender = thinkingSender,
                            Content = $"🤔 **选择思考**\n\n" +
                                     $"- **当前轮次**: {roundNumber + 1}\n" +
                                     $"- **选择Agent**: {newAgentName}\n" +
                                     $"- **角色**: {member2?.Role ?? "Worker"}\n" +
                                     $"- **原因**: {thinkingReason}",
                            Timestamp = DateTime.UtcNow,
                            Role = "system"
                        };
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

            if (taskId.HasValue && taskId.Value > 0 && session != null && orchestratorAgentId > 0 && orchestratorChatClient != null)
            {
                _logger.LogInformation("开始让协调者Agent生成群聊总结文档...");
                
                yield return new ChatMessageDto
                {
                    Sender = "System",
                    Content = "📝 正在让Agent生成讨论总结文档...",
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
                        orchestratorAgentId,
                        orchestratorAgentName ?? "执行者",
                        orchestratorAgentType,
                        orchestratorAgentPrompt,
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
        var members = agentEntities
            .Where(a => a.Member.Role?.Equals("Manager", StringComparison.OrdinalIgnoreCase) != true)
            .Select(a => 
            {
                var name = a.Entity.Name;
                var typeName = a.Entity.TypeName ?? "成员";
                return $"- {name}：负责{typeName}";
            });
        
        var result = string.Join("\n", members);
        _logger.LogInformation("团队成员信息（Worker）: {Members}", result);
        return result;
    }
}
