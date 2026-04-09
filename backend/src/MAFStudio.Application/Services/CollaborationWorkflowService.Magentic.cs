using MAFStudio.Application.DTOs;
using MAFStudio.Application.Interfaces;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MAFStudio.Application.Services;

public partial class CollaborationWorkflowService
{
    public async Task<CollaborationResult> ExecuteReviewIterativeAsync(
        long collaborationId,
        string input,
        ReviewIterativeParameters? parameters = null,
        long? taskId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var collaborationAgents = await _collaborationAgentRepository.GetByCollaborationIdAsync(collaborationId);
            
            if (collaborationAgents.Count == 0)
            {
                return new CollaborationResult
                {
                    Success = false,
                    Error = "协作中没有Agent"
                };
            }

            var workerAgents = collaborationAgents
                .Where(a => a.Role?.Equals("Worker", StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            if (workerAgents.Count == 0)
            {
                workerAgents = collaborationAgents.ToList();
            }

            if (workerAgents.Count == 0)
            {
                return new CollaborationResult
                {
                    Success = false,
                    Error = "Magentic工作流至少需要1个Worker Agent"
                };
            }

            parameters ??= new ReviewIterativeParameters();
            
            var workerEntities = new List<(CollaborationAgent Member, Core.Entities.Agent Entity)>();
            foreach (var worker in workerAgents)
            {
                var entity = await _agentRepository.GetByIdAsync(worker.AgentId);
                if (entity != null)
                {
                    workerEntities.Add((worker, entity));
                }
            }

            var workerNames = workerEntities.Select(w => w.Entity.Name).ToList();

            var taskLedger = parameters.TaskLedger ?? new TaskLedgerConfig
            {
                GlobalGoal = input,
                KnownFacts = new List<string>(),
                OverallPlan = "Magentic Manager将动态制定执行计划",
                Boundaries = new List<string>()
            };

            var progressLedger = parameters.ProgressLedger ?? new ProgressLedgerConfig
            {
                CurrentStepGoal = "开始执行任务",
                CompletedSteps = new List<string>(),
                ReflectionResult = string.Empty,
                NextAction = "等待Magentic Manager决策"
            };

            var metadata = new
            {
                workflowMode = "Magentic智能工作流",
                maxIterations = parameters.MaxIterations ?? 10,
                maxAttempts = parameters.MaxAttempts ?? 5,
                thresholds = parameters.Thresholds,
                workerNames,
                totalAgents = collaborationAgents.Count,
                taskLedger,
                progressLedger,
                orchestrator = "StandardMagenticManager"
            };

            var session = new WorkflowSession
            {
                CollaborationId = collaborationId,
                TaskId = taskId,
                WorkflowType = "Magentic",
                Status = "running",
                Topic = input.Length > 200 ? input.Substring(0, 200) + "..." : input,
                Metadata = JsonSerializer.Serialize(metadata),
                StartedAt = DateTime.UtcNow
            };
            session = await _workflowSessionRepository.CreateAsync(session);
            _logger.LogInformation("创建Magentic工作流会话: {SessionId}", session.Id);
            
            _logger.LogInformation($"开始Magentic工作流，Workers: {workerAgents.Count}个，最大迭代次数: {parameters.MaxIterations ?? 10}");

            var workerClients = new List<IChatClient>();
            var workerDescriptions = new List<string>();
            
            foreach (var workerAgent in workerAgents)
            {
                var workerClient = await _agentFactory.CreateAgentAsync(workerAgent.AgentId);
                workerClients.Add(workerClient);
                
                var entity = workerEntities.First(w => w.Member.AgentId == workerAgent.AgentId).Entity;
                var description = !string.IsNullOrEmpty(workerAgent.CustomPrompt) 
                    ? workerAgent.CustomPrompt 
                    : entity.SystemPrompt ?? $"Worker Agent #{workerAgents.IndexOf(workerAgent) + 1}";
                    
                workerDescriptions.Add($"- {entity.Name}: {description}");
            }

            var messages = new List<ChatMessageDto>();
            var iteration = 0;
            var maxIterations = parameters.MaxIterations ?? 10;
            var maxAttempts = parameters.MaxAttempts ?? 5;
            var isCompleted = false;
            var currentContent = input;
            var attemptCount = 0;
            var conversationHistory = new List<ChatMessage>
            {
                new(ChatRole.User, input)
            };

            var stepNumber = 0;

            var managerPrompt = $@"你是一个Magentic Manager（智能工作流协调者），负责协调多个Worker Agent完成复杂任务。

你的职责：
1. 规划：分析任务，制定执行计划，维护任务账本（Task Ledger）
2. 分派：根据当前进度决定由哪个Worker执行下一步
3. 反思：检查Worker的产出是否达标，维护进度账本（Progress Ledger）

可用的Worker Agents：
{string.Join("\n", workerDescriptions)}

任务账本（外环 - Task Ledger）：
- 全局目标：{taskLedger.GlobalGoal}
- 已知事实：{string.Join(", ", taskLedger.KnownFacts ?? new List<string>())}
- 总体规划：{taskLedger.OverallPlan}
- 任务边界：{string.Join(", ", taskLedger.Boundaries ?? new List<string>())}

进度账本（内环 - Progress Ledger）：
- 当前步骤目标：{progressLedger.CurrentStepGoal}
- 已完成步骤：{string.Join(", ", progressLedger.CompletedSteps ?? new List<string>())}
- 反思结果：{progressLedger.ReflectionResult}
- 下一步行动：{progressLedger.NextAction}

重要规则：
- 你不直接执行任务，只负责协调和决策
- 每次只能委托给一个Worker
- 必须等待Worker执行完成后再做下一步决策
- 如果Worker执行失败，可以尝试其他Worker或修改任务
- 最多执行 {maxIterations} 轮迭代
- 最多尝试 {maxAttempts} 次

请开始协调任务！";

            while (!isCompleted && iteration < maxIterations && attemptCount < maxAttempts)
            {
                iteration++;
                stepNumber++;
                _logger.LogInformation($"Magentic工作流 - 第 {iteration} 轮迭代");

                progressLedger.CurrentStepGoal = $"执行第 {iteration} 轮迭代";

                var currentPrompt = $@"当前任务：{input}

任务账本（外环 - Task Ledger）：
- 全局目标：{taskLedger.GlobalGoal}
- 已知事实：{string.Join(", ", taskLedger.KnownFacts ?? new List<string>())}
- 总体规划：{taskLedger.OverallPlan}
- 任务边界：{string.Join(", ", taskLedger.Boundaries ?? new List<string>())}

进度账本（内环 - Progress Ledger）：
- 当前步骤目标：{progressLedger.CurrentStepGoal}
- 已完成步骤：{string.Join(", ", progressLedger.CompletedSteps ?? new List<string>())}
- 反思结果：{progressLedger.ReflectionResult}
- 下一步行动：{progressLedger.NextAction}

对话历史：
{string.Join("\n", conversationHistory.Select(m => $"{m.Role}: {m.Text}"))}

请分析当前状态，决定下一步应该做什么。
你可以：
1. 使用 DelegateTask(agentName, task) 委托任务给Worker
2. 使用 CompleteWorkflow(result) 完成工作流

请做出决策。";

                var managerClient = await _agentFactory.CreateManagerClientAsync();
                
                var managerResponse = await managerClient.GetResponseAsync(
                    new[] { new ChatMessage(ChatRole.User, currentPrompt) },
                    cancellationToken: cancellationToken);

                var managerOutput = managerResponse.Messages.LastOrDefault()?.Text ?? string.Empty;
                
                await _messageRepository.CreateAsync(new Message
                {
                    SessionId = session.Id,
                    CollaborationId = collaborationId,
                    TaskId = taskId,
                    MessageType = "workflow",
                    StepNumber = stepNumber,
                    FromAgentId = 0,
                    FromAgentName = "MagenticManager",
                    FromAgentRole = "Orchestrator",
                    Content = managerOutput
                });
                await _workflowSessionRepository.IncrementMessageCountAsync(session.Id);
                
                messages.Add(new ChatMessageDto
                {
                    Sender = "MagenticManager",
                    Content = managerOutput,
                    Timestamp = DateTime.UtcNow
                });

                conversationHistory.Add(new ChatMessage(ChatRole.Assistant, managerOutput));

                _logger.LogInformation($"[MagenticManager] {managerOutput}");

                progressLedger.ReflectionResult = managerOutput;

                if (managerOutput.Contains("CompleteWorkflow", StringComparison.OrdinalIgnoreCase) ||
                    managerOutput.Contains("任务完成", StringComparison.OrdinalIgnoreCase) ||
                    managerOutput.Contains("工作流完成", StringComparison.OrdinalIgnoreCase))
                {
                    isCompleted = true;
                    currentContent = managerOutput;
                    progressLedger.NextAction = "工作流已完成";
                    break;
                }

                var workerExecuted = false;
                for (int i = 0; i < workerAgents.Count; i++)
                {
                    var workerName = workerEntities[i].Entity.Name;
                    
                    if (managerOutput.Contains(workerName, StringComparison.OrdinalIgnoreCase) ||
                        managerOutput.Contains($"Worker #{i + 1}", StringComparison.OrdinalIgnoreCase) ||
                        managerOutput.Contains($"Agent_{i + 1}", StringComparison.OrdinalIgnoreCase))
                    {
                        stepNumber++;
                        _logger.LogInformation($"MagenticManager委托任务给 {workerName}");

                        var taskForWorker = $"请执行以下任务：{input}\n\nMagenticManager的指示：{managerOutput}";

                        var workerResponse = await workerClients[i].GetResponseAsync(
                            new List<ChatMessage> { new(ChatRole.User, taskForWorker) },
                            cancellationToken: cancellationToken);

                        var workerOutput = workerResponse.Messages.LastOrDefault()?.Text ?? string.Empty;
                        
                        await _messageRepository.CreateAsync(new Message
                        {
                            SessionId = session.Id,
                            CollaborationId = collaborationId,
                            TaskId = taskId,
                            MessageType = "workflow",
                            StepNumber = stepNumber,
                            FromAgentId = workerAgents[i].AgentId,
                            FromAgentName = workerName,
                            FromAgentRole = "Worker",
                            Content = workerOutput
                        });
                        await _workflowSessionRepository.IncrementMessageCountAsync(session.Id);
                        
                        messages.Add(new ChatMessageDto
                        {
                            Sender = workerName,
                            Content = workerOutput,
                            Timestamp = DateTime.UtcNow
                        });

                        conversationHistory.Add(new ChatMessage(ChatRole.User, $"[{workerName}] {workerOutput}"));

                        _logger.LogInformation($"[{workerName}] {workerOutput}");
                        currentContent = workerOutput;
                        
                        if (progressLedger.CompletedSteps == null)
                        {
                            progressLedger.CompletedSteps = new List<string>();
                        }
                        progressLedger.CompletedSteps.Add($"{workerName}: {workerOutput.Substring(0, Math.Min(100, workerOutput.Length))}");
                        
                        workerExecuted = true;
                        break;
                    }
                }

                if (!workerExecuted)
                {
                    attemptCount++;
                    _logger.LogWarning($"第 {iteration} 轮迭代未找到合适的Worker执行任务，尝试次数：{attemptCount}/{maxAttempts}");
                }
                else
                {
                    attemptCount = 0;
                }

                if (parameters.Thresholds != null && parameters.Thresholds.Count > 0)
                {
                    var evaluationPrompt = $@"请评估以下工作成果是否满足阈值标准：

工作成果：
{currentContent}

阈值标准：
{JsonSerializer.Serialize(parameters.Thresholds, new JsonSerializerOptions { WriteIndented = true })}

请返回JSON格式的评估结果：
{{
  ""passed"": true/false,
  ""scores"": {{
    ""quality"": 85,
    ""accuracy"": 90
  }},
  ""reason"": ""评估理由""
}}";

                    var evaluationResponse = await managerClient.GetResponseAsync(
                        new[] { new ChatMessage(ChatRole.User, evaluationPrompt) },
                        cancellationToken: cancellationToken);

                    var evaluationOutput = evaluationResponse.Messages.LastOrDefault()?.Text ?? string.Empty;
                    
                    _logger.LogInformation($"[评估] {evaluationOutput}");
                    
                    if (evaluationOutput.Contains("\"passed\": true", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("工作成果已满足阈值标准，准备完成工作流");
                        progressLedger.NextAction = "工作成果已满足标准，准备完成";
                    }
                }
            }

            await _workflowSessionRepository.EndSessionAsync(session.Id, 
                conclusion: isCompleted ? "工作流完成" : "达到最大迭代次数或尝试次数");

            return new CollaborationResult
            {
                Success = isCompleted,
                Output = currentContent,
                Messages = messages,
                Metadata = new Dictionary<string, object>
                {
                    ["iterations"] = iteration,
                    ["attempts"] = attemptCount,
                    ["isCompleted"] = isCompleted,
                    ["maxIterations"] = maxIterations,
                    ["maxAttempts"] = maxAttempts,
                    ["pattern"] = "Magentic",
                    ["workerCount"] = workerAgents.Count,
                    ["sessionId"] = session.Id,
                    ["taskLedger"] = taskLedger,
                    ["progressLedger"] = progressLedger,
                    ["orchestrator"] = "StandardMagenticManager"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Magentic工作流执行失败: {ex.Message}");
            return new CollaborationResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}
