namespace MAFStudio.Application.Interfaces;

using MAFStudio.Application.DTOs;

public interface ICollaborationWorkflowService
{
    /// <summary>
    /// 执行顺序工作流
    /// </summary>
    Task<CollaborationResult> ExecuteSequentialAsync(long collaborationId, string input, CancellationToken cancellationToken = default);

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
    IAsyncEnumerable<ChatMessageDto> ExecuteGroupChatAsync(long collaborationId, string input, CancellationToken cancellationToken = default);
}
