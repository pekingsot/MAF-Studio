using MAFStudio.Application.DTOs;
using MAFStudio.Application.Interfaces;
using MAFStudio.Core.Interfaces.Repositories;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json;

namespace MAFStudio.Application.Services;

/// <summary>
/// Magentic工作流服务 - 实现动态Agent编排
/// 
/// 核心思想：
/// 1. Manager Agent根据任务动态决定调用哪个Worker
/// 2. Manager通过FunctionCall来委托任务
/// 3. 工作流根据Manager的决策动态路由
/// 
/// 适用场景：
/// - 复杂任务需要多个Agent协作
/// - 任务路径不确定，需要动态决策
/// - 需要迭代优化和反馈循环
/// </summary>
public partial class CollaborationWorkflowService
{
    /// <summary>
    /// 执行Magentic工作流（动态Agent编排）
    /// </summary>
    public async Task<CollaborationResult> ExecuteReviewIterativeAsync(
        long collaborationId,
        string input,
        ReviewIterativeParameters? parameters = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var chatClients = await GetAgentsAsync(collaborationId);
            
            if (chatClients.Count < 2)
            {
                return new CollaborationResult
                {
                    Success = false,
                    Error = "Magentic工作流至少需要2个Agent（Manager和至少一个Worker）"
                };
            }

            parameters ??= new ReviewIterativeParameters();
            
            _logger.LogInformation($"开始Magentic工作流，最大迭代次数: {parameters.MaxIterations ?? 10}");

            // 准备Worker Agents的描述信息
            var workerDescriptions = new List<string>();
            for (int i = 1; i < chatClients.Count; i++)
            {
                workerDescriptions.Add($"- Agent_{i}: Worker Agent #{i}");
            }

            // 创建Manager Agent
            // Manager的System Prompt需要明确告诉它有哪些下属，以及如何协调他们
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

            // 创建Manager Agent（使用第一个Agent的LLM配置）
            var managerClient = chatClients[0];
            
            // 创建Worker Agents
            var workers = new List<AIAgent>();
            for (int i = 1; i < chatClients.Count; i++)
            {
                var workerAgent = chatClients[i].AsAIAgent(
                    instructions: $@"你是一个专业的Worker Agent #{i}。
你的职责是执行Manager分配给你的任务。
请认真完成任务并提供详细的执行结果。",
                    name: $"Agent_{i}",
                    description: $"Worker Agent #{i}");
                workers.Add(workerAgent);
            }

            // 执行Magentic工作流
            var messages = new List<ChatMessageDto>();
            var iteration = 0;
            var maxIterations = parameters.MaxIterations ?? 10;
            var isCompleted = false;
            var currentContent = input;
            var conversationHistory = new List<ChatMessage>
            {
                new(ChatRole.User, input)
            };

            // 开始迭代执行
            while (!isCompleted && iteration < maxIterations)
            {
                iteration++;
                _logger.LogInformation($"Magentic工作流 - 第 {iteration} 轮迭代");

                // Manager分析当前状态并做出决策
                var managerPrompt = $@"当前任务：{input}

对话历史：
{string.Join("\n", conversationHistory.Select(m => $"{m.Role}: {m.Text}"))}

请分析当前状态，决定下一步应该做什么。
你可以：
1. 使用 DelegateTask(agentName, task) 委托任务给Worker
2. 使用 CompleteWorkflow(result) 完成工作流

请做出决策。";

                // 调用Manager Agent
                var managerResponse = await managerClient.GetResponseAsync(
                    new[] { new ChatMessage(ChatRole.User, managerPrompt) },
                    cancellationToken: cancellationToken);

                var managerOutput = managerResponse.Messages.LastOrDefault()?.Text ?? string.Empty;
                
                messages.Add(new ChatMessageDto
                {
                    Sender = "Manager",
                    Content = managerOutput,
                    Timestamp = DateTime.UtcNow
                });

                conversationHistory.Add(new ChatMessage(ChatRole.Assistant, managerOutput));

                _logger.LogInformation($"[Manager] {managerOutput}");

                // 解析Manager的决策
                // 检查是否要完成工作流
                if (managerOutput.Contains("CompleteWorkflow", StringComparison.OrdinalIgnoreCase) ||
                    managerOutput.Contains("任务完成", StringComparison.OrdinalIgnoreCase) ||
                    managerOutput.Contains("工作流完成", StringComparison.OrdinalIgnoreCase))
                {
                    isCompleted = true;
                    currentContent = managerOutput;
                    break;
                }

                // 检查是否要委托任务
                // 简化处理：如果Manager提到某个Agent，就委托给那个Agent
                foreach (var worker in workers)
                {
                    if (managerOutput.Contains(worker.Name ?? "", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation($"Manager委托任务给 {worker.Name}");

                        // 提取任务描述（简化处理）
                        var taskForWorker = $"请执行以下任务：{input}\n\nManager的指示：{managerOutput}";

                        // 执行Worker Agent
                        var workerResponse = await worker.RunAsync(
                            new List<ChatMessage> { new(ChatRole.User, taskForWorker) },
                            cancellationToken: cancellationToken);

                        var workerOutput = workerResponse.Messages.LastOrDefault()?.Text ?? string.Empty;
                        
                        messages.Add(new ChatMessageDto
                        {
                            Sender = worker.Name ?? "Worker",
                            Content = workerOutput,
                            Timestamp = DateTime.UtcNow
                        });

                        conversationHistory.Add(new ChatMessage(ChatRole.User, $"[{worker.Name}] {workerOutput}"));

                        _logger.LogInformation($"[{worker.Name}] {workerOutput}");
                        currentContent = workerOutput;
                        break;
                    }
                }
            }

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
                    ["pattern"] = "Magentic"
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
