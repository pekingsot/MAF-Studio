using MAFStudio.Application.DTOs;
using MAFStudio.Application.Interfaces;
using MAFStudio.Core.Interfaces.Repositories;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Services;

public partial class CollaborationWorkflowService
{
    /// <summary>
    /// 执行审阅迭代工作流
    /// 适用场景：A写文档 → B审阅 → 不满意 → 打回去 → A修改 → 循环直到满意
    /// </summary>
    public async Task<CollaborationResult> ExecuteReviewIterativeAsync(
        long collaborationId,
        string input,
        ReviewIterativeParameters? parameters = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var agents = await GetAgentsAsync(collaborationId);
            
            if (agents.Count < 2)
            {
                return new CollaborationResult
                {
                    Success = false,
                    Error = "审阅迭代工作流至少需要2个Agent（编写者和审阅者）"
                };
            }

            parameters ??= new ReviewIterativeParameters();
            
            var creatorAgent = agents[0];  // 编写者
            var reviewerAgent = agents[1];  // 审阅者
            
            var messages = new List<ChatMessageDto>();
            var currentContent = input;
            var iteration = 0;
            var maxIterations = parameters.MaxIterations ?? 10;
            var isApproved = false;

            _logger.LogInformation($"开始审阅迭代工作流，最大迭代次数: {maxIterations}");

            while (iteration < maxIterations && !isApproved)
            {
                iteration++;
                _logger.LogInformation($"第 {iteration} 轮迭代");

                // 步骤1: 编写者创建/修改内容
                var creatorPrompt = iteration == 1
                    ? $"请根据以下要求创建文档：\n{input}"
                    : $"请根据审阅意见修改文档：\n审阅意见：{currentContent}\n\n请修改文档。";

                var creatorResponse = await creatorAgent.GetResponseAsync(
                    new[] { new ChatMessage(ChatRole.User, creatorPrompt) },
                    cancellationToken: cancellationToken);

                var createdContent = creatorResponse.Messages.LastOrDefault()?.Text ?? string.Empty;
                
                messages.Add(new ChatMessageDto
                {
                    Sender = "编写者",
                    Content = createdContent,
                    Timestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["iteration"] = iteration,
                        ["step"] = "create"
                    }
                });

                // 步骤2: 审阅者审阅内容
                var reviewerPrompt = $@"请审阅以下文档：

{createdContent}

审阅要求：
{(string.IsNullOrEmpty(parameters.ReviewCriteria) ? "请从内容完整性、逻辑性、格式规范性等方面进行审阅。" : parameters.ReviewCriteria)}

如果满意，请回复：[APPROVED]
如果不满意，请指出具体问题并给出修改建议。";

                var reviewerResponse = await reviewerAgent.GetResponseAsync(
                    new[] { new ChatMessage(ChatRole.User, reviewerPrompt) },
                    cancellationToken: cancellationToken);

                var reviewFeedback = reviewerResponse.Messages.LastOrDefault()?.Text ?? string.Empty;
                
                messages.Add(new ChatMessageDto
                {
                    Sender = "审阅者",
                    Content = reviewFeedback,
                    Timestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["iteration"] = iteration,
                        ["step"] = "review"
                    }
                });

                // 步骤3: 判断是否通过审阅
                if (reviewFeedback.Contains("[APPROVED]", StringComparison.OrdinalIgnoreCase))
                {
                    isApproved = true;
                    _logger.LogInformation($"审阅通过，迭代次数: {iteration}");
                    
                    messages.Add(new ChatMessageDto
                    {
                        Sender = "系统",
                        Content = $"审阅通过！共经过 {iteration} 轮迭代。",
                        Timestamp = DateTime.UtcNow,
                        Metadata = new Dictionary<string, object>
                        {
                            ["iteration"] = iteration,
                            ["step"] = "approved"
                        }
                    });
                }
                else
                {
                    currentContent = reviewFeedback;
                    _logger.LogInformation($"审阅未通过，进入下一轮迭代");
                }
            }

            // 如果达到最大迭代次数仍未通过
            if (!isApproved)
            {
                _logger.LogWarning($"达到最大迭代次数 {maxIterations}，审阅仍未通过");
                
                messages.Add(new ChatMessageDto
                {
                    Sender = "系统",
                    Content = $"达到最大迭代次数 {maxIterations}，审阅仍未通过。请考虑调整文档要求或增加迭代次数。",
                    Timestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["iteration"] = iteration,
                        ["step"] = "max_iterations_reached"
                    }
                });
            }

            return new CollaborationResult
            {
                Success = isApproved,
                Output = messages.LastOrDefault(m => m.Sender == "编写者")?.Content ?? string.Empty,
                Messages = messages,
                Metadata = new Dictionary<string, object>
                {
                    ["iterations"] = iteration,
                    ["isApproved"] = isApproved,
                    ["maxIterations"] = maxIterations
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"审阅迭代工作流执行失败: {ex.Message}");
            return new CollaborationResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}

/// <summary>
/// 审阅迭代工作流参数
/// </summary>
public class ReviewIterativeParameters
{
    /// <summary>
    /// 最大迭代次数（默认10次）
    /// </summary>
    public int? MaxIterations { get; set; } = 10;

    /// <summary>
    /// 审阅标准（可选）
    /// </summary>
    public string? ReviewCriteria { get; set; }

    /// <summary>
    /// 通过关键词（默认 [APPROVED]）
    /// </summary>
    public string ApprovalKeyword { get; set; } = "[APPROVED]";

    /// <summary>
    /// 是否在每次迭代后保存版本（默认true）
    /// </summary>
    public bool SaveVersions { get; set; } = true;
}
