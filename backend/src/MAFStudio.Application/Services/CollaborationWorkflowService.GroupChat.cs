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
            if (task != null)
            {
                _taskContextService.SetCurrentTask(task);
                
                if (!string.IsNullOrEmpty(task.Config))
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
        }
        
        var collaboration = await _collaborationRepository.GetByIdAsync(collaborationId);
        if (collaboration == null)
        {
            throw new NotFoundException($"协作 {collaborationId} 不存在");
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
            throw new InvalidOperationException("没有可用的Agent");
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
                var isManager = member.Role?.Equals("Manager", StringComparison.OrdinalIgnoreCase) == true;
                
                IChatClient chatClient;
                if (isManager)
                {
                    chatClient = await _agentFactory.CreateAgentWithoutCapabilitiesAsync(member.AgentId);
                    _logger.LogInformation("协调者使用无工具ChatClient: AgentId={AgentId}, ChatClientType={Type}", member.AgentId, chatClient.GetType().Name);
                }
                else
                {
                    chatClient = await _agentFactory.CreateAgentAsync(member.AgentId);
                }
                
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
                
                if (isManager)
                {
                    _logger.LogInformation("协调者完整系统提示词 (长度: {Length}):\n{Prompt}", systemPrompt.Length, systemPrompt);
                }
                
                var agentDescription = $"专业{agentEntity.TypeName ?? "专家"}";
                
                var agent = new ChatClientAgent(
                    chatClient,
                    systemPrompt,
                    agentEntity.Name,
                    agentDescription
                );
                
                agentIdToNameMap[agent.Id] = agentEntity.Name;
                agentIdMap[agent.Id] = member.AgentId;
                
                mafAgents.Add(agent);
                
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
                else
                {
                    if (orchestratorChatClient == null)
                    {
                        orchestratorAgentId = member.AgentId;
                        orchestratorAgentPrompt = systemPrompt;
                        orchestratorAgentName = agentEntity.Name;
                        orchestratorAgentType = agentEntity.TypeName;
                        orchestratorChatClient = chatClient;
                        _logger.LogInformation("设置默认执行Agent: Id={Id}, Name={Name}, Type={Type}", agent.Id, agentEntity.Name, agentEntity.TypeName);
                    }
                }
            }

            var managerThinkingQueue = new System.Collections.Concurrent.ConcurrentQueue<ManagerThinkingEventArgs>();
            
            _logger.LogInformation("创建工作流: OrchestrationMode={Mode}, ManagerAgent={Manager}, OrchestratorChatClient={HasClient}", 
                parameters.OrchestrationMode, managerAgent?.Name ?? "null", orchestratorChatClient != null ? "有" : "无");
            
            var workflow = AgentWorkflowBuilder
                .CreateGroupChatBuilderWith(agents =>
                {
                    var allAgents = new List<AIAgent>(agents);
                    if (managerAgent != null && !allAgents.Contains(managerAgent))
                    {
                        if (!allAgents.Any(a => a.Name == managerAgent.Name))
                        {
                            allAgents.Insert(0, managerAgent);
                        }
                    }
                    
                    _logger.LogInformation("选择GroupChatManager: Mode={Mode}, ManagerAgent={Manager}, OrchestratorChatClient={HasClient}", 
                        parameters.OrchestrationMode, managerAgent?.Name ?? "null", orchestratorChatClient != null ? "有" : "无");
                    
                    return parameters.OrchestrationMode switch
                    {
                        GroupChatOrchestrationMode.Intelligent when managerAgent != null && orchestratorChatClient != null =>
                            CreateIntelligentManager(managerAgent, allAgents, orchestratorChatClient, parameters.MaxIterations, taskConfig?.ManagerCustomPrompt),
                        
                        GroupChatOrchestrationMode.Manager when managerAgent != null && orchestratorChatClient != null =>
                            CreateIntelligentManager(managerAgent, allAgents, orchestratorChatClient, parameters.MaxIterations, taskConfig?.ManagerCustomPrompt),
                        
                        GroupChatOrchestrationMode.RoundRobin =>
                            CreateRoundRobinManager(agents, parameters.MaxIterations),
                        
                        _ when managerAgent != null && orchestratorChatClient != null =>
                            CreateIntelligentManager(managerAgent, allAgents, orchestratorChatClient, parameters.MaxIterations, taskConfig?.ManagerCustomPrompt),
                        
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
                    var update = updateEvent.Update;
                    var updateText = update?.Text ?? "";
                    
                    _logger.LogInformation("收到AgentResponseUpdateEvent: ExecutorId={ExecutorId}, TextLength={TextLength}, Text={Text}", 
                        executorId, updateText.Length, updateText.Length > 0 ? updateText.Substring(0, Math.Min(100, updateText.Length)) : "(空)");
                    
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
                            
                            while (managerThinkingQueue.TryDequeue(out var thinkingArgs))
                            {
                                _logger.LogInformation("发送Manager思考过程: {Thinking}", thinkingArgs.Thinking);
                                
                                yield return new ChatMessageDto
                                {
                                    Sender = thinkingArgs.ManagerName,
                                    Content = thinkingArgs.Thinking,
                                    Timestamp = DateTime.UtcNow,
                                    Role = "system",
                                    Metadata = new Dictionary<string, object>
                                    {
                                        ["type"] = "manager_thinking",
                                        ["selectedAgent"] = thinkingArgs.SelectedAgent ?? "",
                                        ["iterationCount"] = thinkingArgs.IterationCount
                                    }
                                };
                            }
                        }
                        
                        currentAgentId = executorId;
                        currentAgentContent.Clear();
                        _logger.LogInformation("Agent切换: {ExecutorId}, 名称: {Name}", executorId, GetAgentName(executorId));
                    }

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
                else if (evt is ExecutorFailedEvent failedEvent)
                {
                    var failedExecutorId = failedEvent.ExecutorId ?? "Unknown";
                    var failedAgentName = GetAgentName(failedExecutorId);
                    var failedData = failedEvent.Data?.ToString() ?? "";
                    
                    _logger.LogWarning("执行器失败: ExecutorId={ExecutorId}, Agent={AgentName}, Data={Data}", 
                        failedExecutorId, failedAgentName, failedData);
                    
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
                    
                    var friendlyError = ParseModelErrorMessage(failedData);
                    
                    if (session != null && agentIdMap.TryGetValue(failedExecutorId.TrimStart('_'), out var failedAgentId))
                    {
                        await _messageRepository.CreateAsync(new Message
                        {
                            SessionId = session.Id,
                            CollaborationId = collaborationId,
                            TaskId = taskId,
                            MessageType = "error",
                            RoundNumber = roundNumber,
                            FromAgentId = failedAgentId,
                            FromAgentName = failedAgentName,
                            Content = friendlyError
                        });
                        await _workflowSessionRepository.IncrementMessageCountAsync(session.Id);
                    }
                    
                    yield return new ChatMessageDto
                    {
                        Sender = failedAgentName,
                        Content = friendlyError,
                        Timestamp = DateTime.UtcNow,
                        Role = "system",
                        Metadata = new Dictionary<string, object>
                        {
                            ["type"] = "model_error",
                            ["executorId"] = failedExecutorId
                        }
                    };
                }
                else if (evt is WorkflowErrorEvent errorEvent)
                {
                    var errorMsg = errorEvent.Exception?.Message ?? "未知错误";
                    var innerMsg = errorEvent.Exception?.InnerException?.Message ?? "";
                    
                    _logger.LogError("工作流执行错误: {Error}\n内部异常: {InnerError}", errorMsg, innerMsg);
                    
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
                    
                    if (session != null)
                    {
                        await _workflowSessionRepository.EndSessionAsync(session.Id, conclusion: $"工作流错误: {errorMsg}");
                    }
                    
                    var fullError = errorMsg + (string.IsNullOrEmpty(innerMsg) ? "" : $"\n内部异常: {innerMsg}");
                    var friendlyError = ParseModelErrorMessage(fullError);
                    
                    if (session != null)
                    {
                        await _messageRepository.CreateAsync(new Message
                        {
                            SessionId = session.Id,
                            CollaborationId = collaborationId,
                            TaskId = taskId,
                            MessageType = "error",
                            FromAgentName = "System",
                            Content = friendlyError
                        });
                        await _workflowSessionRepository.IncrementMessageCountAsync(session.Id);
                    }
                    
                    yield return new ChatMessageDto
                    {
                        Sender = "System",
                        Content = friendlyError,
                        Timestamp = DateTime.UtcNow,
                        Role = "system",
                        Metadata = new Dictionary<string, object>
                        {
                            ["type"] = "workflow_error"
                        }
                    };
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
        int maxIterations,
        string? managerCustomPrompt = null)
    {
        _logger.LogInformation("使用IntelligentGroupChatManager，主Agent: {Name}, 有协调者提示词: {HasPrompt}", 
            managerAgent.Name, !string.IsNullOrEmpty(managerCustomPrompt));
        var logger = _loggerFactory.CreateLogger<IntelligentGroupChatManager>();
        return new IntelligentGroupChatManager(managerAgent, agents, chatClient, maxIterations, managerCustomPrompt, logger);
    }

    private GroupChatManager CreateManagerMode(
        AIAgent managerAgent, 
        IReadOnlyList<AIAgent> agents,
        int maxIterations,
        IChatClient managerChatClient,
        System.Collections.Concurrent.ConcurrentQueue<ManagerThinkingEventArgs>? thinkingQueue = null)
    {
        _logger.LogInformation("使用PrecisionOrchestrator，主Agent: {Name}", managerAgent.Name);
        var managerLogger = _loggerFactory.CreateLogger<PrecisionOrchestrator>();
        var orchestrator = new PrecisionOrchestrator(managerAgent, agents, managerChatClient, maxIterations, managerLogger);
        
        if (thinkingQueue != null)
        {
            orchestrator.ManagerThinking += (sender, args) =>
            {
                thinkingQueue.Enqueue(args);
                _logger.LogInformation("[ManagerThinking] {ManagerName}: {Thinking}", args.ManagerName, args.Thinking);
            };
        }
        
        return orchestrator;
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

    private static string ParseModelErrorMessage(string errorText)
    {
        if (string.IsNullOrEmpty(errorText))
            return "❌ 工作流执行失败: 未知错误";

        if (errorText.Contains("ModelCallFailedException", StringComparison.OrdinalIgnoreCase))
        {
            return "⚠️ 模型调用失败\n\n所有配置的模型均无法响应，请检查：\n  1. 免费额度是否已耗尽\n  2. API Key 是否有效\n  3. 模型服务是否可用\n\n请在管理后台检查模型配置后重试。";
        }

        if (errorText.Contains("FreeTierOnly", StringComparison.OrdinalIgnoreCase) ||
            errorText.Contains("free tier", StringComparison.OrdinalIgnoreCase) ||
            errorText.Contains("免费额度", StringComparison.Ordinal))
        {
            return "⚠️ 模型调用失败：免费额度已耗尽\n\n请在管理后台关闭\"仅使用免费额度\"模式，或配置有付费额度的模型后重试。";
        }

        if (errorText.Contains("401", StringComparison.OrdinalIgnoreCase) ||
            errorText.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
        {
            return "⚠️ 模型调用失败：API Key 无效或已过期\n\n请在管理后台检查模型配置中的 API Key 是否正确。";
        }

        if (errorText.Contains("429", StringComparison.OrdinalIgnoreCase) ||
            errorText.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
        {
            return "⚠️ 模型调用失败：请求频率超限\n\n模型服务当前请求过多，请稍后重试。";
        }

        if (errorText.Contains("403", StringComparison.OrdinalIgnoreCase))
        {
            return "⚠️ 模型调用失败：访问被拒绝\n\n可能原因：额度不足或权限不够，请检查模型配置。";
        }

        if (errorText.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
            errorText.Contains("timed out", StringComparison.OrdinalIgnoreCase))
        {
            return "⚠️ 模型调用失败：请求超时\n\n模型服务响应过慢，请稍后重试或更换模型。";
        }

        if (errorText.Contains("connection", StringComparison.OrdinalIgnoreCase))
        {
            return "⚠️ 模型调用失败：网络连接失败\n\n无法连接到模型服务，请检查网络和模型配置。";
        }

        var shortError = errorText.Length > 200 ? errorText.Substring(0, 200) + "..." : errorText;
        return $"❌ 工作流执行失败: {shortError}";
    }
}
