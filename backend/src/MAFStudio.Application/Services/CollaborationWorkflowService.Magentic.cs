using MAFStudio.Application.DTOs;
using MAFStudio.Application.Interfaces;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
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

            var managerAgent = collaborationAgents.FirstOrDefault(a => 
                a.Role?.Equals("Manager", StringComparison.OrdinalIgnoreCase) == true);
            
            if (managerAgent == null)
            {
                _logger.LogWarning("未指定Manager Agent，使用第一个Agent作为Manager");
                managerAgent = collaborationAgents[0];
            }

            var workerAgents = collaborationAgents
                .Where(a => a.Id != managerAgent.Id)
                .ToList();

            if (workerAgents.Count == 0)
            {
                return new CollaborationResult
                {
                    Success = false,
                    Error = "Magentic工作流至少需要1个Manager和1个Worker"
                };
            }

            parameters ??= new ReviewIterativeParameters();
            
            var managerEntity = await _agentRepository.GetByIdAsync(managerAgent.AgentId);
            var workerEntities = new List<(CollaborationAgent Member, Core.Entities.Agent Entity)>();
            foreach (var worker in workerAgents)
            {
                var entity = await _agentRepository.GetByIdAsync(worker.AgentId);
                if (entity != null)
                {
                    workerEntities.Add((worker, entity));
                }
            }

            var managerName = managerEntity?.Name ?? "Manager";
            var workerNames = workerEntities.Select(w => w.Entity.Name).ToList();

            var metadata = new
            {
                workflowMode = "智能工作流",
                maxIterations = parameters.MaxIterations ?? 10,
                managerNames = new[] { managerName },
                workerNames,
                totalAgents = collaborationAgents.Count
            };

            var session = new WorkflowSession
            {
                CollaborationId = collaborationId,
                TaskId = taskId,
                WorkflowType = "Magentic",
                Status = "running",
                Topic = input.Length > 200 ? input.Substring(0, 200) + "..." : input,
                Metadata = System.Text.Json.JsonSerializer.Serialize(metadata),
                StartedAt = DateTime.UtcNow
            };
            session = await _workflowSessionRepository.CreateAsync(session);
            _logger.LogInformation("创建Magentic工作流会话: {SessionId}", session.Id);
            
            _logger.LogInformation($"开始Magentic工作流，Manager: {managerName}, Workers: {workerAgents.Count}个，最大迭代次数: {parameters.MaxIterations ?? 10}");

            var managerClient = await _agentFactory.CreateAgentAsync(managerAgent.AgentId);

            var workerDescriptions = new List<string>();
            var workerClients = new List<IChatClient>();
            
            foreach (var workerAgent in workerAgents)
            {
                var workerClient = await _agentFactory.CreateAgentAsync(workerAgent.AgentId);
                workerClients.Add(workerClient);
                
                var description = !string.IsNullOrEmpty(workerAgent.CustomPrompt) 
                    ? workerAgent.CustomPrompt 
                    : $"Worker Agent #{workerAgents.IndexOf(workerAgent) + 1}";
                    
                workerDescriptions.Add($"- Agent_{workerAgents.IndexOf(workerAgent) + 1}: {description}");
            }

            var managerInstructions = $@"你是一个智能工作流协调者（Magentic Manager），负责协调多个Worker Agent完成复杂任务。

你的职责：
1. 分析用户任务，制定执行计划
2. 根据任务需求，选择合适的Worker Agent执行
3. 评估执行结果，决定下一步行动
4. 在任务完成或遇到问题时，做出最终决策

你可以使用的Worker Agents：
{string.Join("\n", workerDescriptions)}

工作流程：
1. 首先分析任务，制定初步计划
2. 使用 DelegateTask 函数将任务委托给合适的Worker
3. 根据Worker的执行结果，决定：
   - 如果任务完成，使用 CompleteWorkflow 结束工作流
   - 如果需要继续，继续委托任务给其他Worker
   - 如果需要修改，委托任务给同一个Worker进行修改

重要规则：
- 每次只能委托给一个Worker
- 必须等待Worker执行完成后再做下一步决策
- 如果Worker执行失败，可以尝试其他Worker或修改任务
- 最多执行 {parameters.MaxIterations ?? 10} 轮迭代

请开始协调任务！";

            var messages = new List<ChatMessageDto>();
            var iteration = 0;
            var maxIterations = parameters.MaxIterations ?? 10;
            var isCompleted = false;
            var currentContent = input;
            var conversationHistory = new List<ChatMessage>
            {
                new(ChatRole.User, input)
            };

            var stepNumber = 0;

            while (!isCompleted && iteration < maxIterations)
            {
                iteration++;
                stepNumber++;
                _logger.LogInformation($"Magentic工作流 - 第 {iteration} 轮迭代");

                var managerPrompt = $@"当前任务：{input}

对话历史：
{string.Join("\n", conversationHistory.Select(m => $"{m.Role}: {m.Text}"))}

请分析当前状态，决定下一步应该做什么。
你可以：
1. 使用 DelegateTask(agentName, task) 委托任务给Worker
2. 使用 CompleteWorkflow(result) 完成工作流

请做出决策。";

                var managerResponse = await managerClient.GetResponseAsync(
                    new[] { new ChatMessage(ChatRole.User, managerPrompt) },
                    cancellationToken: cancellationToken);

                var managerOutput = managerResponse.Messages.LastOrDefault()?.Text ?? string.Empty;
                
                await _messageRepository.CreateAsync(new Message
                {
                    SessionId = session.Id,
                    CollaborationId = collaborationId,
                    TaskId = taskId,
                    MessageType = "workflow",
                    StepNumber = stepNumber,
                    FromAgentId = managerAgent.AgentId,
                    FromAgentName = managerName,
                    FromAgentRole = "Manager",
                    Content = managerOutput
                });
                await _workflowSessionRepository.IncrementMessageCountAsync(session.Id);
                
                messages.Add(new ChatMessageDto
                {
                    Sender = managerName,
                    Content = managerOutput,
                    Timestamp = DateTime.UtcNow
                });

                conversationHistory.Add(new ChatMessage(ChatRole.Assistant, managerOutput));

                _logger.LogInformation($"[{managerName}] {managerOutput}");

                if (managerOutput.Contains("CompleteWorkflow", StringComparison.OrdinalIgnoreCase) ||
                    managerOutput.Contains("任务完成", StringComparison.OrdinalIgnoreCase) ||
                    managerOutput.Contains("工作流完成", StringComparison.OrdinalIgnoreCase))
                {
                    isCompleted = true;
                    currentContent = managerOutput;
                    break;
                }

                for (int i = 0; i < workerAgents.Count; i++)
                {
                    var workerName = workerEntities[i].Entity.Name;
                    var workerIndex = $"Agent_{i + 1}";
                    
                    if (managerOutput.Contains(workerIndex, StringComparison.OrdinalIgnoreCase) ||
                        managerOutput.Contains(workerName, StringComparison.OrdinalIgnoreCase) ||
                        managerOutput.Contains($"Worker #{i + 1}", StringComparison.OrdinalIgnoreCase))
                    {
                        stepNumber++;
                        _logger.LogInformation($"Manager委托任务给 {workerName}");

                        var taskForWorker = $"请执行以下任务：{input}\n\nManager的指示：{managerOutput}";

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
                        break;
                    }
                }
            }

            await _workflowSessionRepository.EndSessionAsync(session.Id, 
                conclusion: isCompleted ? "工作流完成" : "达到最大迭代次数");

            return new CollaborationResult
            {
                Success = isCompleted,
                Output = currentContent,
                Messages = messages,
                Metadata = new Dictionary<string, object>
                {
                    ["iterations"] = iteration,
                    ["isCompleted"] = isCompleted,
                    ["maxIterations"] = maxIterations,
                    ["pattern"] = "Magentic",
                    ["managerAgentId"] = managerAgent.AgentId,
                    ["workerCount"] = workerAgents.Count,
                    ["sessionId"] = session.Id
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
