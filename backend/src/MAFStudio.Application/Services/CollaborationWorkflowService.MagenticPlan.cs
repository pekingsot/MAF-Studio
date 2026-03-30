using System.Text.Json;
using MAFStudio.Application.DTOs;
using MAFStudio.Application.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Services;

/// <summary>
/// Magentic工作流服务 - 实现动态Agent编排
/// </summary>
public partial class CollaborationWorkflowService
{
    /// <summary>
    /// 生成Magentic计划
    /// </summary>
    public async Task<WorkflowDefinitionDto> GenerateMagenticPlanAsync(
        long collaborationId,
        string task,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var chatClients = await GetAgentsAsync(collaborationId);
            
            if (chatClients.Count < 2)
            {
                throw new InvalidOperationException("Magentic工作流至少需要2个Agent（Manager和至少一个Worker）");
            }

            var managerClient = chatClients[0];
            
            var workerDescriptions = new List<string>();
            for (int i = 1; i < chatClients.Count; i++)
            {
                workerDescriptions.Add($"- Agent_{i}: Worker Agent #{i}");
            }

            var managerPrompt = $@"你是一个智能工作流协调者（Magentic Manager），负责分析任务并制定执行计划。

任务：{task}

可用的Worker Agents：
{string.Join("\n", workerDescriptions)}

请分析任务，制定执行计划。输出格式为JSON：

{{
  ""nodes"": [
    {{
      ""id"": ""start"",
      ""type"": ""start"",
      ""name"": ""开始""
    }},
    {{
      ""id"": ""node-1"",
      ""type"": ""agent"",
      ""agentId"": ""agent-001"",
      ""name"": ""任务1"",
      ""inputTemplate"": ""任务描述""
    }}
  ],
  ""edges"": [
    {{
      ""type"": ""sequential"",
      ""from"": ""start"",
      ""to"": ""node-1""
    }}
  ]
}}

边类型说明：
- sequential: 顺序执行
- fan-out: 并发执行（从一个节点分发到多个节点）
- fan-in: 汇聚结果（多个节点的结果汇聚到一个节点）
- conditional: 条件分支
- loop: 循环执行

请输出JSON格式的执行计划：";

            var managerResponse = await managerClient.GetResponseAsync(
                new[] { new ChatMessage(ChatRole.User, managerPrompt) },
                cancellationToken: cancellationToken);

            var managerOutput = managerResponse.Messages.LastOrDefault()?.Text ?? string.Empty;

            _logger.LogInformation($"[Manager] 生成的计划:\n{managerOutput}");

            var jsonStart = managerOutput.IndexOf('{');
            var jsonEnd = managerOutput.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = managerOutput.Substring(jsonStart, jsonEnd - jsonStart + 1);
                
                var workflow = JsonSerializer.Deserialize<WorkflowDefinitionDto>(jsonContent);
                
                if (workflow != null)
                {
                    return workflow;
                }
            }

            return CreateDefaultWorkflow(task, chatClients.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"生成Magentic计划失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 执行自定义工作流
    /// </summary>
    public async Task<CollaborationResult> ExecuteCustomWorkflowAsync(
        long collaborationId,
        WorkflowDefinitionDto workflow,
        string input,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation($"开始执行自定义工作流，协作ID: {collaborationId}");

            var messages = new List<ChatMessageDto>();
            
            messages.Add(new ChatMessageDto
            {
                Sender = "System",
                Content = $"开始执行工作流，共 {workflow.Nodes.Count} 个节点",
                Timestamp = DateTime.UtcNow
            });

            var nodeMap = new Dictionary<string, WorkflowNodeDto>();
            foreach (var node in workflow.Nodes)
            {
                nodeMap[node.Id] = node;
            }

            var executedNodes = new HashSet<string>();
            var nodeResults = new Dictionary<string, string>();

            var startNode = workflow.Nodes.FirstOrDefault(n => n.Type == "start");
            if (startNode == null)
            {
                return new CollaborationResult
                {
                    Success = false,
                    Error = "工作流缺少开始节点"
                };
            }

            await ExecuteNodeAsync(
                startNode,
                workflow,
                nodeMap,
                executedNodes,
                nodeResults,
                input,
                collaborationId,
                messages,
                cancellationToken);

            return new CollaborationResult
            {
                Success = true,
                Output = nodeResults.Values.LastOrDefault() ?? "",
                Messages = messages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"执行自定义工作流失败: {ex.Message}");
            return new CollaborationResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// 执行节点
    /// </summary>
    private async Task ExecuteNodeAsync(
        WorkflowNodeDto node,
        WorkflowDefinitionDto workflow,
        Dictionary<string, WorkflowNodeDto> nodeMap,
        HashSet<string> executedNodes,
        Dictionary<string, string> nodeResults,
        string input,
        long collaborationId,
        List<ChatMessageDto> messages,
        CancellationToken cancellationToken)
    {
        if (executedNodes.Contains(node.Id))
        {
            return;
        }

        executedNodes.Add(node.Id);

        if (node.Type == "start")
        {
            var nextEdges = workflow.Edges.Where(e => e.From == node.Id).ToList();
            foreach (var edge in nextEdges)
            {
                var nextNodeIds = ParseEdgeTargets(edge.To);
                foreach (var nextNodeId in nextNodeIds)
                {
                    if (nodeMap.TryGetValue(nextNodeId, out var nextNode))
                    {
                        await ExecuteNodeAsync(
                            nextNode,
                            workflow,
                            nodeMap,
                            executedNodes,
                            nodeResults,
                            input,
                            collaborationId,
                            messages,
                            cancellationToken);
                    }
                }
            }
        }
        else if (node.Type == "agent")
        {
            var chatClients = await GetAgentsAsync(collaborationId);
            var agentIndex = int.Parse(node.AgentId?.Replace("agent-", "") ?? "0");
            var agent = chatClients.ElementAtOrDefault(agentIndex);

            if (agent == null)
            {
                _logger.LogWarning($"Agent {node.AgentId} 不存在");
                return;
            }

            var taskInput = node.InputTemplate ?? input;
            
            messages.Add(new ChatMessageDto
            {
                Sender = node.Name,
                Content = $"开始执行任务: {taskInput}",
                Timestamp = DateTime.UtcNow
            });

            var response = await agent.GetResponseAsync(
                new[] { new ChatMessage(ChatRole.User, taskInput) },
                cancellationToken: cancellationToken);

            var result = response.Messages.LastOrDefault()?.Text ?? "";
            
            nodeResults[node.Id] = result;

            messages.Add(new ChatMessageDto
            {
                Sender = node.Name,
                Content = result,
                Timestamp = DateTime.UtcNow
            });

            var nextEdges = workflow.Edges.Where(e => e.From == node.Id).ToList();
            foreach (var edge in nextEdges)
            {
                var nextNodeIds = ParseEdgeTargets(edge.To);
                foreach (var nextNodeId in nextNodeIds)
                {
                    if (nodeMap.TryGetValue(nextNodeId, out var nextNode))
                    {
                        await ExecuteNodeAsync(
                            nextNode,
                            workflow,
                            nodeMap,
                            executedNodes,
                            nodeResults,
                            result,
                            collaborationId,
                            messages,
                            cancellationToken);
                    }
                }
            }
        }
        else if (node.Type == "aggregator")
        {
            var prevEdges = workflow.Edges.Where(e => ParseEdgeTargets(e.To).Contains(node.Id)).ToList();
            var results = prevEdges.Select(e => nodeResults.GetValueOrDefault(e.From, "")).ToList();
            
            var aggregatedResult = string.Join("\n\n", results);
            nodeResults[node.Id] = aggregatedResult;

            messages.Add(new ChatMessageDto
            {
                Sender = node.Name,
                Content = $"汇聚结果: {aggregatedResult.Substring(0, Math.Min(100, aggregatedResult.Length))}...",
                Timestamp = DateTime.UtcNow
            });

            var nextEdges = workflow.Edges.Where(e => e.From == node.Id).ToList();
            foreach (var edge in nextEdges)
            {
                var nextNodeIds = ParseEdgeTargets(edge.To);
                foreach (var nextNodeId in nextNodeIds)
                {
                    if (nodeMap.TryGetValue(nextNodeId, out var nextNode))
                    {
                        await ExecuteNodeAsync(
                            nextNode,
                            workflow,
                            nodeMap,
                            executedNodes,
                            nodeResults,
                            aggregatedResult,
                            collaborationId,
                            messages,
                            cancellationToken);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 创建默认工作流
    /// </summary>
    private WorkflowDefinitionDto CreateDefaultWorkflow(string task, int agentCount)
    {
        var nodes = new List<WorkflowNodeDto>
        {
            new WorkflowNodeDto
            {
                Id = "start",
                Type = "start",
                Name = "开始"
            }
        };

        var edges = new List<WorkflowEdgeDto>();

        if (agentCount >= 2)
        {
            nodes.Add(new WorkflowNodeDto
            {
                Id = "node-1",
                Type = "agent",
                AgentId = "agent-001",
                Name = "Worker 1",
                InputTemplate = task
            });

            edges.Add(new WorkflowEdgeDto
            {
                Type = "sequential",
                From = "start",
                To = "node-1"
            });
        }

        return new WorkflowDefinitionDto
        {
            Nodes = nodes,
            Edges = edges
        };
    }

    /// <summary>
    /// 解析边的目标节点
    /// </summary>
    private List<string> ParseEdgeTargets(object to)
    {
        if (to is string str)
        {
            return new List<string> { str };
        }
        
        if (to is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.String)
            {
                return new List<string> { jsonElement.GetString() ?? "" };
            }
            else if (jsonElement.ValueKind == JsonValueKind.Array)
            {
                return jsonElement.EnumerateArray()
                    .Select(e => e.GetString() ?? "")
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }
        }

        return new List<string>();
    }
}
