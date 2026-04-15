using MAFStudio.Application.DTOs;
using MAFStudio.Application.Interfaces;
using MAFStudio.Application.Prompts;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Services;

public partial class CollaborationWorkflowService : ICollaborationWorkflowService
{
    private readonly ICollaborationRepository _collaborationRepository;
    private readonly ICollaborationAgentRepository _collaborationAgentRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentFactoryService _agentFactory;
    private readonly IMessageService _messageService;
    private readonly IWorkflowPlanRepository _workflowPlanRepository;
    private readonly IWorkflowSessionRepository _workflowSessionRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ICollaborationTaskRepository _taskRepository;
    private readonly IGroupChatConclusionService _conclusionService;
    private readonly ISystemPromptBuilderFactory _promptBuilderFactory;
    private readonly ITaskContextService _taskContextService;
    private readonly IWorkflowEventProcessor _eventProcessor;
    private readonly ILogger<CollaborationWorkflowService> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public CollaborationWorkflowService(
        ICollaborationRepository collaborationRepository,
        ICollaborationAgentRepository collaborationAgentRepository,
        IAgentRepository agentRepository,
        IAgentFactoryService agentFactory,
        IMessageService messageService,
        IWorkflowPlanRepository workflowPlanRepository,
        IWorkflowSessionRepository workflowSessionRepository,
        IMessageRepository messageRepository,
        ICollaborationTaskRepository taskRepository,
        IGroupChatConclusionService conclusionService,
        ISystemPromptBuilderFactory promptBuilderFactory,
        ITaskContextService taskContextService,
        IWorkflowEventProcessor eventProcessor,
        ILogger<CollaborationWorkflowService> logger,
        ILoggerFactory loggerFactory)
    {
        _collaborationRepository = collaborationRepository;
        _collaborationAgentRepository = collaborationAgentRepository;
        _agentRepository = agentRepository;
        _agentFactory = agentFactory;
        _messageService = messageService;
        _workflowPlanRepository = workflowPlanRepository;
        _workflowSessionRepository = workflowSessionRepository;
        _messageRepository = messageRepository;
        _taskRepository = taskRepository;
        _conclusionService = conclusionService;
        _promptBuilderFactory = promptBuilderFactory;
        _taskContextService = taskContextService;
        _eventProcessor = eventProcessor;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public async Task<CollaborationResult> ExecuteConcurrentAsync(
        long collaborationId, 
        string input,
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

            var executorAgents = allAgents;

            if (executorAgents.Count == 0)
            {
                return new CollaborationResult
                {
                    Success = false,
                    Error = "没有可用的执行Agent"
                };
            }

            _logger.LogInformation("开始并发执行，共 {Count} 个执行Agent", executorAgents.Count);

            var tasks = executorAgents.Select(async (agent, index) =>
            {
                _logger.LogInformation("并发执行Agent #{Index}", index + 1);
                
                var response = await agent.GetResponseAsync(
                    new[] { new ChatMessage(ChatRole.User, input) },
                    cancellationToken: cancellationToken);

                var content = response.Messages.LastOrDefault()?.Text ?? string.Empty;
                
                _logger.LogInformation("Agent #{Index} 执行完成，结果长度: {Length}", index + 1, content.Length);

                return new ChatMessageDto
                {
                    Sender = $"Agent #{index + 1}",
                    Content = content,
                    Timestamp = DateTime.UtcNow
                };
            });

            var messages = (await Task.WhenAll(tasks)).ToList();

            string aggregatedOutput = string.Join("\n\n", messages.Select(m => m.Content));

            return new CollaborationResult
            {
                Success = true,
                Output = aggregatedOutput,
                Messages = messages,
                Metadata = new Dictionary<string, object>
                {
                    ["executorCount"] = executorAgents.Count
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "并发工作流执行失败: {Message}", ex.Message);
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

    public async Task<CollaborationResult> ExecuteCustomWorkflowAsync(
        long collaborationId,
        WorkflowDefinitionDto workflow,
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
            var nodeResults = new Dictionary<string, string>();
            var executedNodes = new HashSet<string>();
            var agentNodes = workflow.Nodes.Where(n => n.Type == "agent").ToList();
            var stepNumber = 0;

            foreach (var node in agentNodes)
            {
                stepNumber++;
                var agentIndex = stepNumber - 1;
                if (agentIndex >= agents.Count)
                {
                    agentIndex = agents.Count - 1;
                }

                var taskInput = ReplaceTemplateVariables(node.InputTemplate ?? input, nodeResults, input, input);
                var response = await agents[agentIndex].GetResponseAsync(
                    new[] { new ChatMessage(ChatRole.User, taskInput) },
                    cancellationToken: cancellationToken);

                var content = response.Messages.LastOrDefault()?.Text ?? string.Empty;
                nodeResults[node.Id] = content;

                messages.Add(new ChatMessageDto
                {
                    Sender = node.AgentRole ?? node.Name,
                    Content = content,
                    Timestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["type"] = "agent_response",
                        ["step"] = stepNumber,
                        ["nodeId"] = node.Id
                    }
                });
            }

            var aggregatedOutput = string.Join("\n\n---\n\n", nodeResults.Values);

            return new CollaborationResult
            {
                Success = true,
                Output = aggregatedOutput,
                Messages = messages,
                Metadata = new Dictionary<string, object>
                {
                    ["nodeCount"] = agentNodes.Count,
                    ["stepCount"] = stepNumber
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "自定义工作流执行失败: {Message}", ex.Message);
            return new CollaborationResult
            {
                Success = false,
                Error = ex.Message
            };
        }
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
