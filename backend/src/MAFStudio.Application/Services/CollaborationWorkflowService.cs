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

            var tasks = agents.Select(async agent =>
            {
                _logger.LogInformation($"并行执行Agent工作流");
                
                var response = await agent.GetResponseAsync(
                    new[] { new ChatMessage(ChatRole.User, input) },
                    cancellationToken: cancellationToken);

                return new ChatMessageDto
                {
                    Sender = "Agent",
                    Content = response.Messages.LastOrDefault()?.Text ?? string.Empty,
                    Timestamp = DateTime.UtcNow
                };
            });

            var messages = (await Task.WhenAll(tasks)).ToList();

            return new CollaborationResult
            {
                Success = true,
                Output = string.Join("\n\n---\n\n", messages.Select(m => $"{m.Sender}: {m.Content}")),
                Messages = messages
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
        var agents = await GetAgentsAsync(collaborationId);
        
        if (agents.Count == 0)
        {
            yield return new ChatMessageDto
            {
                Sender = "System",
                Content = "协作中没有Agent",
                Role = "system"
            };
            yield break;
        }

        var currentInput = input;
        var maxRounds = 10;
        var round = 0;

        while (round < maxRounds)
        {
            foreach (var agent in agents)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                _logger.LogInformation($"群聊轮次 {round + 1}");

                await foreach (var update in agent.GetStreamingResponseAsync(
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

                var response = await agent.GetResponseAsync(
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
