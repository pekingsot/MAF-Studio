using MAFStudio.Application.DTOs;
using MAFStudio.Application.Interfaces;
using MAFStudio.Core.Interfaces.Repositories;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Services;

public partial class CollaborationWorkflowService : ICollaborationWorkflowService
{
    private readonly ICollaborationRepository _collaborationRepository;
    private readonly ICollaborationAgentRepository _collaborationAgentRepository;
    private readonly IAgentFactoryService _agentFactory;
    private readonly ILogger<CollaborationWorkflowService> _logger;

    public CollaborationWorkflowService(
        ICollaborationRepository collaborationRepository,
        ICollaborationAgentRepository collaborationAgentRepository,
        IAgentFactoryService agentFactory,
        ILogger<CollaborationWorkflowService> logger)
    {
        _collaborationRepository = collaborationRepository;
        _collaborationAgentRepository = collaborationAgentRepository;
        _agentFactory = agentFactory;
        _logger = logger;
    }

    public async Task<CollaborationResult> ExecuteSequentialAsync(
        long collaborationId, 
        string input, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var agents = await GetAgentsAsync(collaborationId);
            
            if (agents.Count == 0)
            {
                return new CollaborationResult
                {
                    Success = false,
                    Error = "协作中没有Agent"
                };
            }

            var messages = new List<ChatMessageDto>();
            var currentInput = input;

            foreach (var agent in agents)
            {
                _logger.LogInformation($"执行Agent顺序工作流");
                
                var response = await agent.GetResponseAsync(
                    new[] { new ChatMessage(ChatRole.User, currentInput) },
                    cancellationToken: cancellationToken);

                var content = response.Messages.LastOrDefault()?.Text ?? string.Empty;
                
                messages.Add(new ChatMessageDto
                {
                    Sender = "Agent",
                    Content = content,
                    Timestamp = DateTime.UtcNow
                });

                currentInput = content;
            }

            return new CollaborationResult
            {
                Success = true,
                Output = currentInput,
                Messages = messages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"顺序工作流执行失败: {ex.Message}");
            return new CollaborationResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<CollaborationResult> ExecuteConcurrentAsync(
        long collaborationId, 
        string input,
        List<long>? executorAgentIds = null,
        long? aggregatorAgentId = null,
        string aggregationStrategy = "simple",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var allAgents = await GetAgentsAsync(collaborationId);
            
            if (allAgents.Count == 0)
            {
                return new CollaborationResult
                {
                    Success = false,
                    Error = "协作中没有Agent"
                };
            }

            // 确定参与并发执行的Agent
            var executorAgents = executorAgentIds != null && executorAgentIds.Count > 0
                ? allAgents.Where((a, index) => executorAgentIds.Contains(index)).ToList()
                : allAgents;

            if (executorAgents.Count == 0)
            {
                return new CollaborationResult
                {
                    Success = false,
                    Error = "没有可用的执行Agent"
                };
            }

            _logger.LogInformation($"开始并发执行，共 {executorAgents.Count} 个执行Agent，聚合策略: {aggregationStrategy}");

            // 并发执行所有Agent
            var tasks = executorAgents.Select(async (agent, index) =>
            {
                _logger.LogInformation($"并发执行Agent #{index + 1}");
                
                var response = await agent.GetResponseAsync(
                    new[] { new ChatMessage(ChatRole.User, input) },
                    cancellationToken: cancellationToken);

                var content = response.Messages.LastOrDefault()?.Text ?? string.Empty;
                
                _logger.LogInformation($"Agent #{index + 1} 执行完成，结果长度: {content.Length}");

                return new ChatMessageDto
                {
                    Sender = $"Agent #{index + 1}",
                    Content = content,
                    Timestamp = DateTime.UtcNow
                };
            });

            var messages = (await Task.WhenAll(tasks)).ToList();

            // 聚合结果
            string aggregatedOutput;
            
            if (aggregatorAgentId.HasValue && aggregationStrategy == "intelligent")
            {
                // 智能聚合：使用指定的Agent进行合并
                _logger.LogInformation($"使用Agent #{aggregatorAgentId} 进行智能聚合");
                
                var aggregatorIndex = (int)aggregatorAgentId.Value;
                var aggregatorAgent = allAgents.ElementAtOrDefault(aggregatorIndex);
                
                if (aggregatorAgent != null)
                {
                    var mergePrompt = $@"你是一个结果聚合专家，负责整合多个Agent的并发执行结果。

原始任务：{input}

以下是 {messages.Count} 个Agent的执行结果：

{string.Join("\n\n" + new string('=', 50) + "\n\n", messages.Select((m, i) => $"【{m.Sender}】\n{m.Content}"))}

请整合以上结果，提供一个统一、连贯、高质量的最终答案。
要求：
1. 去除重复内容
2. 保留关键信息
3. 保持逻辑连贯
4. 提供清晰的结论";

                    var aggregatorResponse = await aggregatorAgent.GetResponseAsync(
                        new[] { new ChatMessage(ChatRole.User, mergePrompt) },
                        cancellationToken: cancellationToken);

                    aggregatedOutput = aggregatorResponse.Messages.LastOrDefault()?.Text ?? "";
                    
                    messages.Add(new ChatMessageDto
                    {
                        Sender = "Aggregator",
                        Content = $"【智能聚合结果】\n{aggregatedOutput}",
                        Timestamp = DateTime.UtcNow,
                        Metadata = new Dictionary<string, object>
                        {
                            ["aggregatorAgentId"] = aggregatorAgentId.Value,
                            ["strategy"] = "intelligent"
                        }
                    });
                    
                    _logger.LogInformation($"智能聚合完成，结果长度: {aggregatedOutput.Length}");
                }
                else
                {
                    _logger.LogWarning($"聚合Agent #{aggregatorAgentId} 不存在，降级为简单聚合");
                    aggregatedOutput = string.Join("\n\n---\n\n", messages.Select(m => $"【{m.Sender}】\n{m.Content}"));
                }
            }
            else
            {
                // 简单聚合：字符串拼接
                _logger.LogInformation("使用简单聚合（字符串拼接）");
                aggregatedOutput = string.Join("\n\n---\n\n", messages.Select(m => $"【{m.Sender}】\n{m.Content}"));
            }

            return new CollaborationResult
            {
                Success = true,
                Output = aggregatedOutput,
                Messages = messages,
                Metadata = new Dictionary<string, object>
                {
                    ["executorCount"] = executorAgents.Count,
                    ["aggregatorAgentId"] = aggregatorAgentId,
                    ["aggregationStrategy"] = aggregationStrategy
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"并发工作流执行失败: {ex.Message}");
            return new CollaborationResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<CollaborationResult> ExecuteHandoffsAsync(
        long collaborationId, 
        string input, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var agents = await GetAgentsAsync(collaborationId);
            
            if (agents.Count == 0)
            {
                return new CollaborationResult
                {
                    Success = false,
                    Error = "协作中没有Agent"
                };
            }

            var messages = new List<ChatMessageDto>();
            var currentInput = input;
            var currentIndex = 0;

            while (currentIndex < agents.Count)
            {
                var agent = agents[currentIndex];
                _logger.LogInformation($"执行Agent任务移交工作流");
                
                var response = await agent.GetResponseAsync(
                    new[] { new ChatMessage(ChatRole.User, currentInput) },
                    cancellationToken: cancellationToken);

                var content = response.Messages.LastOrDefault()?.Text ?? string.Empty;
                
                messages.Add(new ChatMessageDto
                {
                    Sender = "Agent",
                    Content = content,
                    Timestamp = DateTime.UtcNow
                });

                if (content.Contains("[HANDOFF:", StringComparison.OrdinalIgnoreCase))
                {
                    var nextAgentName = ExtractHandoffAgent(content);
                    var nextAgentIndex = agents.FindIndex(a => 
                        a.GetHashCode().ToString().Equals(nextAgentName, StringComparison.OrdinalIgnoreCase));
                    
                    if (nextAgentIndex >= 0)
                    {
                        currentIndex = nextAgentIndex;
                        currentInput = content.Replace($"[HANDOFF:{nextAgentName}]", "").Trim();
                        continue;
                    }
                }

                currentIndex++;
                currentInput = content;
            }

            return new CollaborationResult
            {
                Success = true,
                Output = currentInput,
                Messages = messages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"任务移交工作流执行失败: {ex.Message}");
            return new CollaborationResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async IAsyncEnumerable<ChatMessageDto> ExecuteGroupChatAsync(
        long collaborationId, 
        string input,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
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

        // 找出Manager角色的agent作为主持人
        var moderatorMember = members.FirstOrDefault(m => m.Role == "Manager");
        var workerMembers = members.Where(m => m.Role != "Manager").ToList();

        IChatClient? moderator = null;
        var workers = new List<IChatClient>();

        // 创建主持人Agent
        if (moderatorMember != null)
        {
            try
            {
                moderator = await _agentFactory.CreateAgentAsync(moderatorMember.AgentId);
                _logger.LogInformation($"群聊主持人: {moderatorMember.AgentId}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"无法创建主持人Agent {moderatorMember.AgentId}: {ex.Message}");
            }
        }

        // 创建Worker Agents
        foreach (var member in workerMembers)
        {
            try
            {
                var agent = await _agentFactory.CreateAgentAsync(member.AgentId);
                workers.Add(agent);
                _logger.LogInformation($"群聊参与者: {member.AgentId}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"无法创建Agent {member.AgentId}: {ex.Message}");
            }
        }

        if (workers.Count == 0)
        {
            yield return new ChatMessageDto
            {
                Sender = "System",
                Content = "协作中没有Worker Agent",
                Role = "system"
            };
            yield break;
        }

        var currentInput = input;
        var maxRounds = 10;
        var round = 0;

        // 如果有主持人，由主持人控制对话流程
        if (moderator != null)
        {
            while (round < maxRounds)
            {
                _logger.LogInformation($"群聊轮次 {round + 1}");

                // 主持人发言，决定下一步
                var moderatorPrompt = $@"你是一个讨论主持人。当前讨论主题：{currentInput}

请根据以下情况做出决定：
1. 如果讨论已经充分，请回复'[END]'结束讨论
2. 如果需要继续讨论，请指定下一个发言的Agent编号（1-{workers.Count}），格式：'[NEXT:编号]'
3. 或者直接提出新的讨论问题

当前轮次：{round + 1}/{maxRounds}";

                var moderatorResponse = await moderator.GetResponseAsync(
                    new[] { new ChatMessage(ChatRole.User, moderatorPrompt) },
                    cancellationToken: cancellationToken);

                var moderatorMessage = moderatorResponse.Messages.LastOrDefault()?.Text ?? string.Empty;

                yield return new ChatMessageDto
                {
                    Sender = "主持人",
                    Content = moderatorMessage,
                    Timestamp = DateTime.UtcNow
                };

                // 检查是否结束
                if (moderatorMessage.Contains("[END]", StringComparison.OrdinalIgnoreCase))
                {
                    yield return new ChatMessageDto
                    {
                        Sender = "System",
                        Content = "讨论已结束",
                        Role = "system",
                        Timestamp = DateTime.UtcNow
                    };
                    yield break;
                }

                // 检查是否指定了下一个发言者
                var nextAgentIndex = ExtractNextAgent(moderatorMessage, workers.Count);
                if (nextAgentIndex >= 0 && nextAgentIndex < workers.Count)
                {
                    var worker = workers[nextAgentIndex];
                    
                    await foreach (var update in worker.GetStreamingResponseAsync(
                        new[] { new ChatMessage(ChatRole.User, currentInput) },
                        cancellationToken: cancellationToken))
                    {
                        yield return new ChatMessageDto
                        {
                            Sender = $"Agent {nextAgentIndex + 1}",
                            Content = update.Text ?? string.Empty,
                            Timestamp = DateTime.UtcNow
                        };
                    }

                    var workerResponse = await worker.GetResponseAsync(
                        new[] { new ChatMessage(ChatRole.User, currentInput) },
                        cancellationToken: cancellationToken);

                    currentInput = workerResponse.Messages.LastOrDefault()?.Text ?? string.Empty;
                }

                round++;
            }
        }
        else
        {
            // 没有主持人，按轮次发言
            while (round < maxRounds)
            {
                foreach (var worker in workers)
                {
                    if (cancellationToken.IsCancellationRequested)
                        yield break;

                    _logger.LogInformation($"群聊轮次 {round + 1}");

                    await foreach (var update in worker.GetStreamingResponseAsync(
                        new[] { new ChatMessage(ChatRole.User, currentInput) },
                        cancellationToken: cancellationToken))
                    {
                        yield return new ChatMessageDto
                        {
                            Sender = "Agent",
                            Content = update.Text ?? string.Empty,
                            Timestamp = DateTime.UtcNow
                        };
                    }

                    var response = await worker.GetResponseAsync(
                        new[] { new ChatMessage(ChatRole.User, currentInput) },
                        cancellationToken: cancellationToken);

                    currentInput = response.Messages.LastOrDefault()?.Text ?? string.Empty;

                    if (currentInput.Contains("[END]", StringComparison.OrdinalIgnoreCase))
                    {
                        yield break;
                    }
                }

                round++;
            }
        }
    }

    private int ExtractNextAgent(string content, int maxAgents)
    {
        var start = content.IndexOf("[NEXT:");
        if (start == -1) return -1;
        
        start += 6;
        var end = content.IndexOf("]", start);
        if (end == -1) return -1;
        
        var numberStr = content.Substring(start, end - start).Trim();
        if (int.TryParse(numberStr, out var number))
        {
            return number - 1; // 转换为0-based索引
        }
        
        return -1;
    }

    private async Task<List<IChatClient>> GetAgentsAsync(long collaborationId)
    {
        var collaboration = await _collaborationRepository.GetByIdAsync(collaborationId);
        if (collaboration == null)
        {
            throw new NotFoundException($"协作 {collaborationId} 不存在");
        }

        var members = await _collaborationAgentRepository.GetByCollaborationIdAsync(collaborationId);
        var agents = new List<IChatClient>();

        foreach (var member in members)
        {
            try
            {
                var agent = await _agentFactory.CreateAgentAsync(member.AgentId);
                agents.Add(agent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"无法创建Agent {member.AgentId}: {ex.Message}");
            }
        }

        return agents;
    }

    private string? ExtractHandoffAgent(string content)
    {
        var start = content.IndexOf("[HANDOFF:");
        if (start == -1) return null;
        
        start += 9;
        var end = content.IndexOf("]", start);
        if (end == -1) return null;
        
        return content.Substring(start, end - start).Trim();
    }
}
