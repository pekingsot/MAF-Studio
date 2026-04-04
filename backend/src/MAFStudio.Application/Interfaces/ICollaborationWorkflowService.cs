namespace MAFStudio.Application.Interfaces;

using MAFStudio.Application.DTOs;

public interface ICollaborationWorkflowService
{
    /// <summary>
    /// 执行并发工作流
    /// </summary>
    Task<CollaborationResult> ExecuteConcurrentAsync(long collaborationId, string input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行任务移交工作流
    /// </summary>
    Task<CollaborationResult> ExecuteHandoffsAsync(long collaborationId, string input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行群聊工作流
    /// </summary>
    IAsyncEnumerable<ChatMessageDto> ExecuteGroupChatAsync(
        long collaborationId, 
        string input, 
        GroupChatParameters? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行审阅迭代工作流
    /// 适用场景：A写文档 → B审阅 → 不满意 → 打回去 → A修改 → 循环直到满意
    /// </summary>
    Task<CollaborationResult> ExecuteReviewIterativeAsync(long collaborationId, string input, ReviewIterativeParameters? parameters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成Magentic计划
    /// </summary>
    Task<WorkflowDefinitionDto> GenerateMagenticPlanAsync(long collaborationId, string task, CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成并保存工作流计划
    /// </summary>
    Task<WorkflowPlanDto> GenerateAndSavePlanAsync(long collaborationId, string task, long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取工作流计划
    /// </summary>
    Task<WorkflowPlanDto?> GetPlanAsync(long planId);

    /// <summary>
    /// 获取协作的所有工作流计划
    /// </summary>
    Task<List<WorkflowPlanDto>> GetPlansByCollaborationAsync(long collaborationId);

    /// <summary>
    /// 更新工作流计划
    /// </summary>
    Task<WorkflowPlanDto> UpdatePlanAsync(long planId, WorkflowDefinitionDto workflow);

    /// <summary>
    /// 执行工作流计划
    /// </summary>
    Task<CollaborationResult> ExecutePlanAsync(long planId, string input, long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除工作流计划
    /// </summary>
    Task<bool> DeletePlanAsync(long planId);

    /// <summary>
    /// 执行自定义工作流
    /// </summary>
    Task<CollaborationResult> ExecuteCustomWorkflowAsync(long collaborationId, WorkflowDefinitionDto workflow, string input, CancellationToken cancellationToken = default);
}
