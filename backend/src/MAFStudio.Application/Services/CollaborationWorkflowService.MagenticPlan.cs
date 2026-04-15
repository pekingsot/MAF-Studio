using System.Text.Json;
using MAFStudio.Application.DTOs;
using MAFStudio.Application.Interfaces;
using MAFStudio.Core.Entities;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Services;

public partial class CollaborationWorkflowService
{
    public async Task<WorkflowDefinitionDto> GenerateMagenticPlanAsync(
        long collaborationId,
        string task,
        CancellationToken cancellationToken = default)
    {
        var (managerClient, workerInfos) = await GetCollaborationAgentInfosAsync(collaborationId);

        if (workerInfos.Count == 0)
        {
            throw new InvalidOperationException("Magentic工作流至少需要1个Worker Agent");
        }

        var workerDescriptions = workerInfos.Select(w =>
            $"- {w.Key}: {w.Value.Description}").ToList();

        var agentIdMapping = workerInfos.Select((w, i) =>
            $"  \"{w.Key}\" → agentId: \"{w.Value.AgentId}\"").ToList();

        var managerPrompt = $@"你是一个智能工作流协调者（Magentic Manager），负责分析任务并制定执行计划。

任务：{task}

可用的Worker Agents：
{string.Join("\n", workerDescriptions)}

Agent ID映射：
{string.Join("\n", agentIdMapping)}

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
      ""agentId"": ""{workerInfos[0].Value.AgentId}"",
      ""agentRole"": ""{workerInfos[0].Key}"",
      ""name"": ""任务1描述"",
      ""inputTemplate"": ""具体的任务描述，包含上下文""
    }},
    {{
      ""id"": ""end"",
      ""type"": ""aggregator"",
      ""name"": ""汇总结果""
    }}
  ],
  ""edges"": [
    {{
      ""type"": ""sequential"",
      ""from"": ""start"",
      ""to"": ""node-1""
    }},
    {{
      ""type"": ""sequential"",
      ""from"": ""node-1"",
      ""to"": ""end""
    }}
  ]
}}

规则：
1. agentId 必须使用上面映射表中的真实 agentId 值
2. agentRole 必须使用上面映射表中的 Agent 名称
3. inputTemplate 要详细描述该节点需要执行的具体任务
4. 边类型说明：
   - sequential: 顺序执行
   - fan-out: 并发执行（从一个节点分发到多个节点）
   - fan-in: 汇聚结果（多个节点的结果汇聚到一个节点）
   - conditional: 条件分支
5. 最后一个节点应该是 aggregator 类型，用于汇总所有结果
6. 根据任务复杂度合理安排节点数量和执行顺序

请输出JSON格式的执行计划：";

        var managerResponse = await managerClient.GetResponseAsync(
            new[] { new ChatMessage(ChatRole.User, managerPrompt) },
            cancellationToken: cancellationToken);

        var managerOutput = managerResponse.Messages.LastOrDefault()?.Text ?? string.Empty;

        _logger.LogInformation("[Manager] 生成的计划:\n{Output}", managerOutput);

        var workflow = ParseWorkflowFromLlmOutput(managerOutput);
        if (workflow != null)
        {
            return workflow;
        }

        return CreateDefaultWorkflow(task, workerInfos);
    }

    public async IAsyncEnumerable<ChatMessageDto> ExecuteMagenticWorkflowStreamAsync(
        long collaborationId,
        WorkflowDefinitionDto workflow,
        string input,
        long? taskId = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var session = new WorkflowSession
        {
            CollaborationId = collaborationId,
            TaskId = taskId,
            WorkflowType = "MagenticWorkflow",
            Status = "running",
            Topic = input.Length > 200 ? input.Substring(0, 200) + "..." : input,
            StartedAt = DateTime.UtcNow
        };
        session = await _workflowSessionRepository.CreateAsync(session);
        _logger.LogInformation("创建Magentic工作流会话: {SessionId}", session.Id);

        var collaborationAgents = await _collaborationAgentRepository.GetByCollaborationIdAsync(collaborationId);
        var agentClientMap = new Dictionary<string, (IChatClient Client, string Name)>();

        foreach (var member in collaborationAgents)
        {
            var entity = await _agentRepository.GetByIdAsync(member.AgentId);
            if (entity == null) continue;

            try
            {
                var client = await _agentFactory.CreateAgentAsync(member.AgentId);
                agentClientMap[member.AgentId.ToString()] = (client, entity.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "无法创建Agent {AgentId} 的ChatClient", member.AgentId);
            }
        }

        yield return new ChatMessageDto
        {
            Sender = "System",
            Content = $"🚀 开始执行Magentic工作流，共 {workflow.Nodes.Count} 个节点，{workflow.Edges.Count} 条边",
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, object> { ["type"] = "system" }
        };

        var nodeMap = workflow.Nodes.ToDictionary(n => n.Id);
        var executedNodes = new Dictionary<string, int>();
        var nodeResults = new Dictionary<string, string>();
        var nodeRetryCount = new Dictionary<string, int>();
        var stepCounter = new StepCounter();

        var startNode = workflow.Nodes.FirstOrDefault(n => n.Type == "start");
        if (startNode == null)
        {
            yield return new ChatMessageDto
            {
                Sender = "System",
                Content = "❌ 工作流缺少开始节点",
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object> { ["type"] = "error" }
            };
            yield break;
        }

        await foreach (var msg in ExecuteNodeStreamAsync(
            startNode, workflow, nodeMap, executedNodes, nodeResults, nodeRetryCount,
            input, input, collaborationId, agentClientMap, session.Id, taskId,
            stepCounter, cancellationToken))
        {
            yield return msg;
        }

        await _workflowSessionRepository.EndSessionAsync(session.Id,
            conclusion: "Magentic工作流执行完成");

        yield return new ChatMessageDto
        {
            Sender = "System",
            Content = $"✅ Magentic工作流执行完成，共执行 {stepCounter.Value} 步",
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, object> { ["type"] = "system_complete" }
        };
    }

    private async IAsyncEnumerable<ChatMessageDto> ExecuteNodeStreamAsync(
        WorkflowNodeDto node,
        WorkflowDefinitionDto workflow,
        Dictionary<string, WorkflowNodeDto> nodeMap,
        Dictionary<string, int> executedNodes,
        Dictionary<string, string> nodeResults,
        Dictionary<string, int> nodeRetryCount,
        string upstreamResult,
        string originalInput,
        long collaborationId,
        Dictionary<string, (IChatClient Client, string Name)> agentClientMap,
        long sessionId,
        long? taskId,
        StepCounter stepCounter,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (executedNodes.ContainsKey(node.Id) && executedNodes[node.Id] > 0 && node.Type != "review")
        {
            yield break;
        }

        executedNodes[node.Id] = executedNodes.GetValueOrDefault(node.Id) + 1;

        if (node.Type == "start")
        {
            var nextEdges = workflow.Edges.Where(e => e.From == node.Id).ToList();
            var fanOutTargets = new List<string>();

            foreach (var edge in nextEdges)
            {
                var targets = ParseEdgeTargets(edge.To);
                if (edge.Type == "fan-out")
                {
                    fanOutTargets.AddRange(targets);
                }
                else
                {
                    foreach (var targetId in targets)
                    {
                        if (nodeMap.TryGetValue(targetId, out var nextNode))
                        {
                            await foreach (var msg in ExecuteNodeStreamAsync(
                                nextNode, workflow, nodeMap, executedNodes, nodeResults, nodeRetryCount,
                                upstreamResult, originalInput, collaborationId, agentClientMap, sessionId, taskId,
                                stepCounter, cancellationToken))
                            {
                                yield return msg;
                            }
                        }
                    }
                }
            }

            if (fanOutTargets.Count > 0)
            {
                var fanOutTasks = fanOutTargets.Select(async targetId =>
                {
                    if (!nodeMap.TryGetValue(targetId, out var targetNode))
                        return (targetId, new List<ChatMessageDto>());

                    var localMessages = new List<ChatMessageDto>();

                    await foreach (var msg in ExecuteNodeStreamAsync(
                        targetNode, workflow, nodeMap, executedNodes, nodeResults, nodeRetryCount,
                        upstreamResult, originalInput, collaborationId, agentClientMap, sessionId, taskId,
                        stepCounter, cancellationToken))
                    {
                        localMessages.Add(msg);
                    }

                    return (targetId, localMessages);
                }).ToList();

                var results = await Task.WhenAll(fanOutTasks);

                foreach (var (_, localMessages) in results)
                {
                    foreach (var msg in localMessages)
                    {
                        yield return msg;
                    }
                }
            }
        }
        else if (node.Type == "agent")
        {
            if (stepCounter.HasExceededMaxSteps)
            {
                yield return new ChatMessageDto
                {
                    Sender = "System",
                    Content = $"❌ 工作流已达到最大步骤限制({StepCounter.MaxSteps}步)，自动终止",
                    Timestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object> { ["type"] = "error" }
                };
                yield break;
            }

            stepCounter.Value++;
            var agentIdStr = node.AgentId ?? "";

            yield return new ChatMessageDto
            {
                Sender = node.AgentRole ?? node.Name,
                Content = $"📋 开始执行: {node.Name}",
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["type"] = "step_start",
                    ["step"] = stepCounter.Value,
                    ["nodeId"] = node.Id
                }
            };

            if (!agentClientMap.TryGetValue(agentIdStr, out var agentInfo))
            {
                var fallback = agentClientMap.FirstOrDefault();
                if (fallback.Value.Client != null)
                {
                    agentInfo = fallback.Value;
                }
                else
                {
                    yield return new ChatMessageDto
                    {
                        Sender = "System",
                        Content = $"⚠️ Agent {agentIdStr} 不存在，跳过此节点",
                        Timestamp = DateTime.UtcNow,
                        Metadata = new Dictionary<string, object> { ["type"] = "warning" }
                    };
                    yield break;
                }
            }

            var taskInput = ReplaceTemplateVariables(node.InputTemplate ?? upstreamResult, nodeResults, upstreamResult, originalInput);

            var response = await agentInfo.Client.GetResponseAsync(
                new[] { new ChatMessage(ChatRole.User, taskInput) },
                cancellationToken: cancellationToken);

            var result = response.Messages.LastOrDefault()?.Text ?? "";
            nodeResults[node.Id] = result;

            await _messageRepository.CreateAsync(new Message
            {
                SessionId = sessionId,
                CollaborationId = collaborationId,
                TaskId = taskId,
                MessageType = "workflow",
                StepNumber = stepCounter.Value,
                FromAgentId = long.TryParse(agentIdStr, out var aid) ? aid : 0,
                FromAgentName = agentInfo.Name,
                FromAgentRole = node.AgentRole ?? "Worker",
                Content = result
            });
            await _workflowSessionRepository.IncrementMessageCountAsync(sessionId);

            yield return new ChatMessageDto
            {
                Sender = agentInfo.Name,
                Content = result,
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["type"] = "agent_response",
                    ["step"] = stepCounter.Value,
                    ["nodeId"] = node.Id,
                    ["agentId"] = agentIdStr
                }
            };

            var nextEdges = workflow.Edges.Where(e => e.From == node.Id).ToList();
            var fanOutTargets = new List<string>();

            foreach (var edge in nextEdges)
            {
                var targets = ParseEdgeTargets(edge.To);
                if (edge.Type == "fan-out")
                {
                    fanOutTargets.AddRange(targets);
                }
                else
                {
                    foreach (var targetId in targets)
                    {
                        if (nodeMap.TryGetValue(targetId, out var nextNode))
                        {
                            await foreach (var msg in ExecuteNodeStreamAsync(
                                nextNode, workflow, nodeMap, executedNodes, nodeResults, nodeRetryCount,
                                result, originalInput, collaborationId, agentClientMap, sessionId, taskId,
                                stepCounter, cancellationToken))
                            {
                                yield return msg;
                            }
                        }
                    }
                }
            }

            if (fanOutTargets.Count > 0)
            {
                var fanOutTasks = fanOutTargets.Select(async targetId =>
                {
                    if (!nodeMap.TryGetValue(targetId, out var targetNode))
                        return (targetId, new List<ChatMessageDto>());

                    var localMessages = new List<ChatMessageDto>();

                    await foreach (var msg in ExecuteNodeStreamAsync(
                        targetNode, workflow, nodeMap, executedNodes, nodeResults, nodeRetryCount,
                        result, originalInput, collaborationId, agentClientMap, sessionId, taskId,
                        stepCounter, cancellationToken))
                    {
                        localMessages.Add(msg);
                    }

                    return (targetId, localMessages);
                }).ToList();

                var results = await Task.WhenAll(fanOutTasks);

                foreach (var (_, localMessages) in results)
                {
                    foreach (var msg in localMessages)
                    {
                        yield return msg;
                    }
                }
            }
        }
        else if (node.Type == "aggregator")
        {
            if (stepCounter.HasExceededMaxSteps)
            {
                yield return new ChatMessageDto
                {
                    Sender = "System",
                    Content = $"❌ 工作流已达到最大步骤限制({StepCounter.MaxSteps}步)，自动终止",
                    Timestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object> { ["type"] = "error" }
                };
                yield break;
            }

            stepCounter.Value++;
            var prevNodeIds = workflow.Edges
                .Where(e => ParseEdgeTargets(e.To).Contains(node.Id))
                .Select(e => e.From)
                .Distinct()
                .ToList();

            var prevResults = prevNodeIds
                .Where(id => nodeResults.ContainsKey(id))
                .Select(id => nodeResults[id])
                .ToList();

            var rawAggregated = prevResults.Count > 0
                ? string.Join("\n\n---\n\n", prevResults)
                : upstreamResult;

            var aggregationInstruction = !string.IsNullOrWhiteSpace(node.InputTemplate)
                ? node.InputTemplate
                : "请汇总以上所有结果，生成最终的综合报告。";

            var agentIdStr = node.AgentId ?? "";
            IChatClient? aggregatorClient = null;
            string aggregatorName = node.AgentRole ?? node.Name;
            long aggregatorAgentId = 0;

            if (!agentClientMap.TryGetValue(agentIdStr, out var agentInfo))
            {
                var fallback = agentClientMap.FirstOrDefault();
                if (fallback.Value.Client != null)
                {
                    aggregatorClient = fallback.Value.Client;
                    aggregatorName = fallback.Value.Name;
                    if (long.TryParse(fallback.Key, out var fAid))
                        aggregatorAgentId = fAid;
                }
            }
            else
            {
                aggregatorClient = agentInfo.Client;
                aggregatorName = agentInfo.Name;
                if (long.TryParse(agentIdStr, out var aid))
                    aggregatorAgentId = aid;
            }

            string aggregatedResult;

            if (aggregatorClient != null)
            {
                yield return new ChatMessageDto
                {
                    Sender = aggregatorName,
                    Content = $"📊 开始汇总 {prevResults.Count} 个节点的结果...",
                    Timestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["type"] = "aggregator_start",
                        ["step"] = stepCounter.Value,
                        ["nodeId"] = node.Id,
                        ["sourceCount"] = prevResults.Count
                    }
                };

                var aggregationPrompt = $"""
                    {aggregationInstruction}
                    
                    以下是需要汇总的各节点结果：
                    
                    {rawAggregated}
                    """;

                var response = await aggregatorClient.GetResponseAsync(
                    new[] { new ChatMessage(ChatRole.User, aggregationPrompt) },
                    cancellationToken: cancellationToken);

                aggregatedResult = response.Messages.LastOrDefault()?.Text ?? rawAggregated;

                await _messageRepository.CreateAsync(new Message
                {
                    SessionId = sessionId,
                    CollaborationId = collaborationId,
                    TaskId = taskId,
                    MessageType = "workflow",
                    StepNumber = stepCounter.Value,
                    FromAgentId = aggregatorAgentId,
                    FromAgentName = aggregatorName,
                    FromAgentRole = node.AgentRole ?? "Aggregator",
                    Content = aggregatedResult
                });
            }
            else
            {
                aggregatedResult = rawAggregated;

                await _messageRepository.CreateAsync(new Message
                {
                    SessionId = sessionId,
                    CollaborationId = collaborationId,
                    TaskId = taskId,
                    MessageType = "workflow",
                    StepNumber = stepCounter.Value,
                    FromAgentId = 0,
                    FromAgentName = "Aggregator",
                    FromAgentRole = "Aggregator",
                    Content = $"汇总 {prevResults.Count} 个节点的结果（无可用Agent，使用简单拼接）"
                });
            }

            nodeResults[node.Id] = aggregatedResult;

            yield return new ChatMessageDto
            {
                Sender = aggregatorName,
                Content = aggregatedResult,
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["type"] = "aggregator",
                    ["step"] = stepCounter.Value,
                    ["nodeId"] = node.Id,
                    ["sourceCount"] = prevResults.Count
                }
            };

            var nextEdges = workflow.Edges.Where(e => e.From == node.Id).ToList();
            foreach (var edge in nextEdges)
            {
                var targets = ParseEdgeTargets(edge.To);
                foreach (var targetId in targets)
                {
                    if (nodeMap.TryGetValue(targetId, out var nextNode))
                    {
                        await foreach (var msg in ExecuteNodeStreamAsync(
                            nextNode, workflow, nodeMap, executedNodes, nodeResults, nodeRetryCount,
                            aggregatedResult, originalInput, collaborationId, agentClientMap, sessionId, taskId,
                            stepCounter, cancellationToken))
                        {
                            yield return msg;
                        }
                    }
                }
            }
        }
        else if (node.Type == "condition")
        {
            if (stepCounter.HasExceededMaxSteps)
            {
                yield return new ChatMessageDto
                {
                    Sender = "System",
                    Content = $"❌ 工作流已达到最大步骤限制({StepCounter.MaxSteps}步)，自动终止",
                    Timestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object> { ["type"] = "error" }
                };
                yield break;
            }

            stepCounter.Value++;
            var condition = node.Condition ?? "true";
            var branchResult = EvaluateCondition(condition, nodeResults);

            yield return new ChatMessageDto
            {
                Sender = "Condition",
                Content = $"🔀 条件判断: {condition} → {(branchResult ? "True" : "False")}",
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["type"] = "condition",
                    ["step"] = stepCounter.Value,
                    ["result"] = branchResult
                }
            };

            var nextEdges = workflow.Edges.Where(e => e.From == node.Id).ToList();
            foreach (var edge in nextEdges)
            {
                var isTrueBranch = edge.Condition?.Equals("true", StringComparison.OrdinalIgnoreCase) == true
                    || edge.Type == "sequential";
                var isFalseBranch = edge.Condition?.Equals("false", StringComparison.OrdinalIgnoreCase) == true;

                if ((branchResult && isTrueBranch) || (!branchResult && isFalseBranch))
                {
                    var targets = ParseEdgeTargets(edge.To);
                    foreach (var targetId in targets)
                    {
                        if (nodeMap.TryGetValue(targetId, out var nextNode))
                        {
                            await foreach (var msg in ExecuteNodeStreamAsync(
                                nextNode, workflow, nodeMap, executedNodes, nodeResults, nodeRetryCount,
                                upstreamResult, originalInput, collaborationId, agentClientMap, sessionId, taskId,
                                stepCounter, cancellationToken))
                            {
                                yield return msg;
                            }
                        }
                    }
                }
            }
        }
        else if (node.Type == "review")
        {
            if (stepCounter.HasExceededMaxSteps)
            {
                yield return new ChatMessageDto
                {
                    Sender = "System",
                    Content = $"❌ 工作流已达到最大步骤限制({StepCounter.MaxSteps}步)，自动终止",
                    Timestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object> { ["type"] = "error" }
                };
                yield break;
            }

            stepCounter.Value++;
            var maxRetries = node.MaxRetries ?? 3;
            var approvalKeyword = node.ApprovalKeyword ?? "[APPROVED]";
            var rejectTargetNodeId = node.RejectTargetNode ?? "";
            var reviewCriteria = node.ReviewCriteria ?? "请审核以下内容是否满足要求";
            var agentIdStr = node.AgentId ?? "";

            var currentRetry = nodeRetryCount.GetValueOrDefault(node.Id, 0);

            yield return new ChatMessageDto
            {
                Sender = node.AgentRole ?? node.Name,
                Content = $"🔍 开始审核 (第{currentRetry + 1}次，最多{maxRetries}次): {node.Name}",
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["type"] = "review_start",
                    ["step"] = stepCounter.Value,
                    ["nodeId"] = node.Id,
                    ["retry"] = currentRetry + 1,
                    ["maxRetries"] = maxRetries
                }
            };

            if (!agentClientMap.TryGetValue(agentIdStr, out var agentInfo))
            {
                var fallback = agentClientMap.FirstOrDefault();
                if (fallback.Value.Client != null)
                {
                    agentInfo = fallback.Value;
                }
                else
                {
                    yield return new ChatMessageDto
                    {
                        Sender = "System",
                        Content = $"⚠️ 审核Agent {agentIdStr} 不存在，跳过审核",
                        Timestamp = DateTime.UtcNow,
                        Metadata = new Dictionary<string, object> { ["type"] = "warning" }
                    };

                    var skipEdges = workflow.Edges.Where(e => e.From == node.Id && e.Type == "approved").ToList();
                    foreach (var edge in skipEdges)
                    {
                        var targets = ParseEdgeTargets(edge.To);
                        foreach (var targetId in targets)
                        {
                            if (nodeMap.TryGetValue(targetId, out var nextNode))
                            {
                                await foreach (var msg in ExecuteNodeStreamAsync(
                                    nextNode, workflow, nodeMap, executedNodes, nodeResults, nodeRetryCount,
                                    upstreamResult, originalInput, collaborationId, agentClientMap, sessionId, taskId,
                                    stepCounter, cancellationToken))
                                {
                                    yield return msg;
                                }
                            }
                        }
                    }
                    yield break;
                }
            }

            var prevNodeIds = workflow.Edges
                .Where(e => ParseEdgeTargets(e.To).Contains(node.Id))
                .Select(e => e.From)
                .Distinct()
                .ToList();

            var prevResults = prevNodeIds
                .Where(id => nodeResults.ContainsKey(id))
                .Select(id => nodeResults[id])
                .ToList();

            var contentToReview = prevResults.Count > 0
                ? string.Join("\n\n---\n\n", prevResults)
                : upstreamResult;

            var reviewPrompt = $@"你是一个审核者，请审核以下内容。

审核标准：
{reviewCriteria}

待审核内容：
{contentToReview}

请仔细审核，如果满意请在回复中包含 {approvalKeyword}，如果不满意请给出具体的修改意见。
格式要求：
- 审核通过：在回复中包含 {approvalKeyword}
- 审核不通过：给出具体的修改意见和改进建议";

            var response = await agentInfo.Client.GetResponseAsync(
                new[] { new ChatMessage(ChatRole.User, reviewPrompt) },
                cancellationToken: cancellationToken);

            var reviewResult = response.Messages.LastOrDefault()?.Text ?? "";

            await _messageRepository.CreateAsync(new Message
            {
                SessionId = sessionId,
                CollaborationId = collaborationId,
                TaskId = taskId,
                MessageType = "workflow",
                StepNumber = stepCounter.Value,
                FromAgentId = long.TryParse(agentIdStr, out var aid) ? aid : 0,
                FromAgentName = agentInfo.Name,
                FromAgentRole = node.AgentRole ?? "Reviewer",
                Content = reviewResult
            });
            await _workflowSessionRepository.IncrementMessageCountAsync(sessionId);

            var isApproved = reviewResult.Contains(approvalKeyword, StringComparison.OrdinalIgnoreCase);

            if (isApproved)
            {
                nodeResults[node.Id] = reviewResult;
                nodeRetryCount[node.Id] = 0;

                yield return new ChatMessageDto
                {
                    Sender = agentInfo.Name,
                    Content = reviewResult,
                    Timestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["type"] = "review_approved",
                        ["step"] = stepCounter.Value,
                        ["nodeId"] = node.Id,
                        ["approved"] = true
                    }
                };

                yield return new ChatMessageDto
                {
                    Sender = "System",
                    Content = $"✅ 审核通过！进入下一步",
                    Timestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["type"] = "review_result",
                        ["approved"] = true
                    }
                };

                var approvedEdges = workflow.Edges.Where(e => e.From == node.Id && (e.Type == "approved" || e.Type == "sequential")).ToList();
                foreach (var edge in approvedEdges)
                {
                    var targets = ParseEdgeTargets(edge.To);
                    foreach (var targetId in targets)
                    {
                        if (nodeMap.TryGetValue(targetId, out var nextNode))
                        {
                            await foreach (var msg in ExecuteNodeStreamAsync(
                                nextNode, workflow, nodeMap, executedNodes, nodeResults, nodeRetryCount,
                                reviewResult, originalInput, collaborationId, agentClientMap, sessionId, taskId,
                                stepCounter, cancellationToken))
                            {
                                yield return msg;
                            }
                        }
                    }
                }
            }
            else
            {
                nodeRetryCount[node.Id] = currentRetry + 1;

                yield return new ChatMessageDto
                {
                    Sender = agentInfo.Name,
                    Content = reviewResult,
                    Timestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["type"] = "review_rejected",
                        ["step"] = stepCounter.Value,
                        ["nodeId"] = node.Id,
                        ["approved"] = false,
                        ["retry"] = currentRetry + 1,
                        ["maxRetries"] = maxRetries
                    }
                };

                if (currentRetry + 1 >= maxRetries)
                {
                    yield return new ChatMessageDto
                    {
                        Sender = "System",
                        Content = $"⚠️ 审核已达到最大重试次数({maxRetries}次)，强制通过",
                        Timestamp = DateTime.UtcNow,
                        Metadata = new Dictionary<string, object>
                        {
                            ["type"] = "review_force_approved",
                            ["retries"] = currentRetry + 1
                        }
                    };

                    nodeResults[node.Id] = reviewResult;
                    nodeRetryCount[node.Id] = 0;

                    var forceEdges = workflow.Edges.Where(e => e.From == node.Id && (e.Type == "approved" || e.Type == "sequential")).ToList();
                    foreach (var edge in forceEdges)
                    {
                        var targets = ParseEdgeTargets(edge.To);
                        foreach (var targetId in targets)
                        {
                            if (nodeMap.TryGetValue(targetId, out var nextNode))
                            {
                                await foreach (var msg in ExecuteNodeStreamAsync(
                                    nextNode, workflow, nodeMap, executedNodes, nodeResults, nodeRetryCount,
                                    reviewResult, originalInput, collaborationId, agentClientMap, sessionId, taskId,
                                    stepCounter, cancellationToken))
                                {
                                    yield return msg;
                                }
                            }
                        }
                    }
                }
                else
                {
                    yield return new ChatMessageDto
                    {
                        Sender = "System",
                        Content = $"🔄 审核不通过，打回修改 (第{currentRetry + 1}/{maxRetries}次)",
                        Timestamp = DateTime.UtcNow,
                        Metadata = new Dictionary<string, object>
                        {
                            ["type"] = "review_reject_sendback",
                            ["retry"] = currentRetry + 1,
                            ["maxRetries"] = maxRetries,
                            ["rejectTarget"] = rejectTargetNodeId
                        }
                    };

                    if (!string.IsNullOrEmpty(rejectTargetNodeId) && nodeMap.ContainsKey(rejectTargetNodeId))
                    {
                        executedNodes.Remove(rejectTargetNodeId);

                        var nodesToReset = new Queue<string>();
                        nodesToReset.Enqueue(rejectTargetNodeId);

                        while (nodesToReset.Count > 0)
                        {
                            var nodeIdToReset = nodesToReset.Dequeue();
                            executedNodes.Remove(nodeIdToReset);

                            var downstreamEdges = workflow.Edges.Where(e => e.From == nodeIdToReset).ToList();
                            foreach (var edge in downstreamEdges)
                            {
                                var targets = ParseEdgeTargets(edge.To);
                                foreach (var targetId in targets)
                                {
                                    if (targetId != node.Id && executedNodes.ContainsKey(targetId))
                                    {
                                        nodesToReset.Enqueue(targetId);
                                    }
                                }
                            }
                        }

                        var rejectNode = nodeMap[rejectTargetNodeId];
                        var rejectInput = $@"审核不通过，请根据以下审核意见修改：

审核意见：
{reviewResult}

原始内容：
{contentToReview}

请根据审核意见修改后重新提交。";

                        await foreach (var msg in ExecuteNodeStreamAsync(
                            rejectNode, workflow, nodeMap, executedNodes, nodeResults, nodeRetryCount,
                            rejectInput, originalInput, collaborationId, agentClientMap, sessionId, taskId,
                            stepCounter, cancellationToken))
                        {
                            yield return msg;
                        }
                    }
                    else
                    {
                        yield return new ChatMessageDto
                        {
                            Sender = "System",
                            Content = $"⚠️ 未配置打回目标节点，审核不通过后无法打回，强制通过",
                            Timestamp = DateTime.UtcNow,
                            Metadata = new Dictionary<string, object> { ["type"] = "warning" }
                        };

                        nodeResults[node.Id] = reviewResult;

                        var fallbackEdges = workflow.Edges.Where(e => e.From == node.Id && (e.Type == "approved" || e.Type == "sequential")).ToList();
                        foreach (var edge in fallbackEdges)
                        {
                            var targets = ParseEdgeTargets(edge.To);
                            foreach (var targetId in targets)
                            {
                                if (nodeMap.TryGetValue(targetId, out var nextNode))
                                {
                                    await foreach (var msg in ExecuteNodeStreamAsync(
                                        nextNode, workflow, nodeMap, executedNodes, nodeResults, nodeRetryCount,
                                        reviewResult, originalInput, collaborationId, agentClientMap, sessionId, taskId,
                                        stepCounter, cancellationToken))
                                    {
                                        yield return msg;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private async Task<(IChatClient ManagerClient, List<KeyValuePair<string, AgentInfo>> WorkerInfos)>
        GetCollaborationAgentInfosAsync(long collaborationId)
    {
        var collaborationAgents = await _collaborationAgentRepository.GetByCollaborationIdAsync(collaborationId);

        if (collaborationAgents.Count == 0)
        {
            throw new InvalidOperationException("协作中没有Agent");
        }

        IChatClient? managerClient = null;
        var workerInfos = new List<KeyValuePair<string, AgentInfo>>();

        foreach (var member in collaborationAgents)
        {
            var entity = await _agentRepository.GetByIdAsync(member.AgentId);
            if (entity == null) continue;

            var description = !string.IsNullOrEmpty(member.CustomPrompt)
                ? member.CustomPrompt
                : entity.SystemPrompt ?? entity.Name;

            var info = new AgentInfo
            {
                AgentId = member.AgentId.ToString(),
                Name = entity.Name,
                Description = description
            };

            if (member.Role == "Manager" && managerClient == null)
            {
                try
                {
                    managerClient = await _agentFactory.CreateAgentAsync(member.AgentId);
                    workerInfos.Insert(0, new KeyValuePair<string, AgentInfo>(entity.Name, info));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "无法创建Manager Agent {AgentId}", member.AgentId);
                }
            }
            else
            {
                workerInfos.Add(new KeyValuePair<string, AgentInfo>(entity.Name, info));
            }
        }

        if (managerClient == null)
        {
            var firstWorker = workerInfos.FirstOrDefault();
            if (firstWorker.Value != null)
            {
                try
                {
                    managerClient = await _agentFactory.CreateAgentAsync(long.Parse(firstWorker.Value.AgentId));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "无法创建第一个Agent作为Manager");
                }
            }
        }

        if (managerClient == null)
        {
            throw new InvalidOperationException("无法创建Manager Agent");
        }

        return (managerClient, workerInfos);
    }

    private WorkflowDefinitionDto? ParseWorkflowFromLlmOutput(string output)
    {
        try
        {
            var jsonStart = output.IndexOf('{');
            var jsonEnd = output.LastIndexOf('}');

            if (jsonStart < 0 || jsonEnd < 0 || jsonEnd <= jsonStart)
            {
                _logger.LogWarning("无法在LLM输出中找到JSON");
                return null;
            }

            var json = output.Substring(jsonStart, jsonEnd - jsonStart + 1);

            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var nodes = new List<WorkflowNodeDto>();
            var edges = new List<WorkflowEdgeDto>();

            if (root.TryGetProperty("nodes", out var nodesElement))
            {
                foreach (var nodeEl in nodesElement.EnumerateArray())
                {
                    var node = new WorkflowNodeDto
                    {
                        Id = nodeEl.GetProperty("id").GetString() ?? "",
                        Type = nodeEl.GetProperty("type").GetString() ?? "agent",
                        Name = nodeEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "",
                    };

                    if (nodeEl.TryGetProperty("agentId", out var agentIdEl))
                    {
                        node.AgentId = agentIdEl.GetString();
                    }

                    if (nodeEl.TryGetProperty("agentRole", out var agentRoleEl))
                    {
                        node.AgentRole = agentRoleEl.GetString();
                    }

                    if (nodeEl.TryGetProperty("inputTemplate", out var inputTemplateEl))
                    {
                        node.InputTemplate = inputTemplateEl.GetString();
                    }

                    if (nodeEl.TryGetProperty("condition", out var conditionEl))
                    {
                        node.Condition = conditionEl.GetString();
                    }

                    nodes.Add(node);
                }
            }

            if (root.TryGetProperty("edges", out var edgesElement))
            {
                foreach (var edgeEl in edgesElement.EnumerateArray())
                {
                    var edge = new WorkflowEdgeDto
                    {
                        Type = edgeEl.GetProperty("type").GetString() ?? "sequential",
                        From = edgeEl.GetProperty("from").GetString() ?? "",
                    };

                    if (edgeEl.TryGetProperty("to", out var toEl))
                    {
                        if (toEl.ValueKind == JsonValueKind.Array)
                        {
                            edge.To = toEl.EnumerateArray().Select(e => e.GetString() ?? "").ToList();
                        }
                        else
                        {
                            edge.To = toEl.GetString() ?? "";
                        }
                    }

                    if (edgeEl.TryGetProperty("condition", out var edgeConditionEl))
                    {
                        edge.Condition = edgeConditionEl.GetString();
                    }

                    if (edgeEl.TryGetProperty("description", out var descEl))
                    {
                        edge.Description = descEl.GetString();
                    }

                    edges.Add(edge);
                }
            }

            return new WorkflowDefinitionDto
            {
                Nodes = nodes,
                Edges = edges
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析LLM输出的工作流JSON失败");
            return null;
        }
    }

    private WorkflowDefinitionDto CreateDefaultWorkflow(string task, List<KeyValuePair<string, AgentInfo>> workerInfos)
    {
        var nodes = new List<WorkflowNodeDto>
        {
            new() { Id = "start", Type = "start", Name = "开始" }
        };

        var edges = new List<WorkflowEdgeDto>();

        for (var i = 0; i < workerInfos.Count; i++)
        {
            var worker = workerInfos[i];
            var nodeId = $"node-{i + 1}";

            nodes.Add(new WorkflowNodeDto
            {
                Id = nodeId,
                Type = "agent",
                Name = $"{worker.Key}执行任务",
                AgentId = worker.Value.AgentId,
                AgentRole = worker.Key,
                InputTemplate = $"请完成以下任务：{task}"
            });

            var fromId = i == 0 ? "start" : $"node-{i}";
            edges.Add(new WorkflowEdgeDto
            {
                Type = "sequential",
                From = fromId,
                To = nodeId
            });
        }

        nodes.Add(new WorkflowNodeDto
        {
            Id = "end",
            Type = "aggregator",
            Name = "汇总结果"
        });

        edges.Add(new WorkflowEdgeDto
        {
            Type = "sequential",
            From = $"node-{workerInfos.Count}",
            To = "end"
        });

        return new WorkflowDefinitionDto
        {
            Nodes = nodes,
            Edges = edges
        };
    }

    private List<string> ParseEdgeTargets(object to)
    {
        if (to is string s)
        {
            return new List<string> { s };
        }

        if (to is List<string> list)
        {
            return list;
        }

        if (to is JsonElement jsonEl)
        {
            if (jsonEl.ValueKind == JsonValueKind.Array)
            {
                return jsonEl.EnumerateArray().Select(e => e.GetString() ?? "").ToList();
            }

            return new List<string> { jsonEl.GetString() ?? "" };
        }

        return new List<string> { to.ToString() ?? "" };
    }

    private string ReplaceTemplateVariables(string template, Dictionary<string, string> nodeResults, string upstreamResult, string originalInput)
    {
        if (string.IsNullOrEmpty(template)) return upstreamResult;

        var result = template.Replace("{{input}}", upstreamResult);
        result = result.Replace("{{originalInput}}", originalInput);
        result = result.Replace("{{task}}", originalInput);

        var lastResult = nodeResults.Values.LastOrDefault() ?? "";
        result = result.Replace("{{lastResult}}", lastResult);

        foreach (var kvp in nodeResults)
        {
            result = result.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
        }

        return result;
    }

    private bool EvaluateCondition(string condition, Dictionary<string, string> nodeResults)
    {
        if (string.IsNullOrWhiteSpace(condition)) return true;

        if (condition.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
        if (condition.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;

        var lastResult = nodeResults.Values.LastOrDefault() ?? "";
        var allResults = string.Join("\n", nodeResults.Values);

        if (condition.StartsWith("result.Contains(", StringComparison.OrdinalIgnoreCase) ||
            condition.StartsWith("result.contains(", StringComparison.OrdinalIgnoreCase))
        {
            var searchTerm = ExtractStringArgument(condition, "result.Contains");
            if (searchTerm != null)
            {
                return lastResult.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
            }
        }

        if (condition.StartsWith("result.Length", StringComparison.OrdinalIgnoreCase))
        {
            return EvaluateLengthCondition(condition, "result.Length", lastResult.Length);
        }

        if (condition.StartsWith("contains:", StringComparison.OrdinalIgnoreCase))
        {
            var searchTerm = condition.Substring("contains:".Length).Trim();
            return allResults.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
        }

        if (condition.StartsWith("length>", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(condition.Substring("length>".Length).Trim(), out var minLength))
            {
                return lastResult.Length > minLength;
            }
        }

        if (condition.StartsWith("result.StartsWith(", StringComparison.OrdinalIgnoreCase) ||
            condition.StartsWith("result.startswith(", StringComparison.OrdinalIgnoreCase))
        {
            var searchTerm = ExtractStringArgument(condition, "result.StartsWith");
            if (searchTerm != null)
            {
                return lastResult.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase);
            }
        }

        if (condition.StartsWith("result.EndsWith(", StringComparison.OrdinalIgnoreCase) ||
            condition.StartsWith("result.endswith(", StringComparison.OrdinalIgnoreCase))
        {
            var searchTerm = ExtractStringArgument(condition, "result.EndsWith");
            if (searchTerm != null)
            {
                return lastResult.EndsWith(searchTerm, StringComparison.OrdinalIgnoreCase);
            }
        }

        if (condition.StartsWith("result.IsNullOrEmpty", StringComparison.OrdinalIgnoreCase) ||
            condition.StartsWith("result.isnullorempty", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrEmpty(lastResult);
        }

        if (condition.StartsWith("result.IsNullOrWhiteSpace", StringComparison.OrdinalIgnoreCase) ||
            condition.StartsWith("result.isnullorwhitespace", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(lastResult);
        }

        if (condition.StartsWith("result.Equals(", StringComparison.OrdinalIgnoreCase) ||
            condition.StartsWith("result.equals(", StringComparison.OrdinalIgnoreCase))
        {
            var searchTerm = ExtractStringArgument(condition, "result.Equals");
            if (searchTerm != null)
            {
                return lastResult.Equals(searchTerm, StringComparison.OrdinalIgnoreCase);
            }
        }

        if (condition.StartsWith("!", StringComparison.OrdinalIgnoreCase) ||
            condition.StartsWith("not ", StringComparison.OrdinalIgnoreCase))
        {
            var innerCondition = condition.StartsWith("!")
                ? condition.Substring(1).Trim()
                : condition.Substring(4).Trim();

            if (innerCondition.StartsWith("(") && innerCondition.EndsWith(")"))
            {
                innerCondition = innerCondition.Substring(1, innerCondition.Length - 2).Trim();
            }

            return !EvaluateCondition(innerCondition, nodeResults);
        }

        if (condition.Contains("&&", StringComparison.OrdinalIgnoreCase) || condition.Contains(" AND ", StringComparison.OrdinalIgnoreCase))
        {
            var parts = condition.Contains("&&")
                ? condition.Split("&&", StringSplitOptions.RemoveEmptyEntries)
                : condition.Split(" AND ", StringSplitOptions.RemoveEmptyEntries);
            return parts.All(p => EvaluateCondition(p.Trim(), nodeResults));
        }

        if (condition.Contains("||", StringComparison.OrdinalIgnoreCase) || condition.Contains(" OR ", StringComparison.OrdinalIgnoreCase))
        {
            var parts = condition.Contains("||")
                ? condition.Split("||", StringSplitOptions.RemoveEmptyEntries)
                : condition.Split(" OR ", StringSplitOptions.RemoveEmptyEntries);
            return parts.Any(p => EvaluateCondition(p.Trim(), nodeResults));
        }

        return true;
    }

    private static string? ExtractStringArgument(string expression, string methodName)
    {
        var startIdx = expression.IndexOf(methodName, StringComparison.OrdinalIgnoreCase);
        if (startIdx < 0) return null;

        var openParen = expression.IndexOf('(', startIdx + methodName.Length);
        if (openParen < 0) return null;

        var closeParen = expression.IndexOf(')', openParen);
        if (closeParen < 0) return null;

        var arg = expression.Substring(openParen + 1, closeParen - openParen - 1).Trim();

        if ((arg.StartsWith('"') && arg.EndsWith('"')) || (arg.StartsWith('\'') && arg.EndsWith('\'')))
        {
            arg = arg.Substring(1, arg.Length - 2);
        }

        return arg;
    }

    private static bool EvaluateLengthCondition(string condition, string prefix, int actualLength)
    {
        var opPart = condition.Substring(prefix.Length).Trim();

        if (opPart.StartsWith(">="))
        {
            if (int.TryParse(opPart.Substring(2).Trim(), out var val))
                return actualLength >= val;
        }
        else if (opPart.StartsWith("<="))
        {
            if (int.TryParse(opPart.Substring(2).Trim(), out var val))
                return actualLength <= val;
        }
        else if (opPart.StartsWith("==") || opPart.StartsWith("="))
        {
            var numStart = opPart.StartsWith("==") ? 2 : 1;
            if (int.TryParse(opPart.Substring(numStart).Trim(), out var val))
                return actualLength == val;
        }
        else if (opPart.StartsWith(">"))
        {
            if (int.TryParse(opPart.Substring(1).Trim(), out var val))
                return actualLength > val;
        }
        else if (opPart.StartsWith("<"))
        {
            if (int.TryParse(opPart.Substring(1).Trim(), out var val))
                return actualLength < val;
        }
        else if (opPart.StartsWith("!="))
        {
            if (int.TryParse(opPart.Substring(2).Trim(), out var val))
                return actualLength != val;
        }

        return true;
    }

    private record AgentInfo
    {
        public string AgentId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
    }

    private class StepCounter
    {
        public int Value { get; set; }
        public const int MaxSteps = 20;
        public bool HasExceededMaxSteps => Value >= MaxSteps;
    }
}
