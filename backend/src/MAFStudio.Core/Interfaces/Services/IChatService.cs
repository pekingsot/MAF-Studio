using Microsoft.Extensions.AI;

namespace MAFStudio.Core.Interfaces.Services;

/// <summary>
/// 统一的聊天服务接口
/// 基于 IChatClient 提供统一的大模型调用能力
/// </summary>
public interface IChatService
{
    /// <summary>
    /// 发送消息并获取响应
    /// </summary>
    /// <param name="llmConfigId">大模型配置ID</param>
    /// <param name="modelConfigId">模型配置ID（可选）</param>
    /// <param name="messages">消息列表</param>
    /// <param name="options">聊天选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>聊天响应</returns>
    Task<ChatResponse> SendMessageAsync(
        long llmConfigId,
        long? modelConfigId,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送消息并获取流式响应
    /// </summary>
    /// <param name="llmConfigId">大模型配置ID</param>
    /// <param name="modelConfigId">模型配置ID（可选）</param>
    /// <param name="messages">消息列表</param>
    /// <param name="options">聊天选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>流式聊天响应</returns>
    IAsyncEnumerable<StreamingChatResponse> SendMessageStreamingAsync(
        long llmConfigId,
        long? modelConfigId,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 测试大模型连接
    /// </summary>
    /// <param name="llmConfigId">大模型配置ID</param>
    /// <param name="modelConfigId">模型配置ID（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>测试结果</returns>
    Task<ConnectionTestResult> TestConnectionAsync(
        long llmConfigId,
        long? modelConfigId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取可用的模型列表
    /// </summary>
    /// <param name="llmConfigId">大模型配置ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>模型列表</returns>
    Task<IReadOnlyList<string>> GetAvailableModelsAsync(
        long llmConfigId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 流式聊天响应
/// </summary>
public class StreamingChatResponse
{
    /// <summary>
    /// 响应文本
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// 是否完成
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// 工具调用信息
    /// </summary>
    public string? ToolCallInfo { get; set; }

    /// <summary>
    /// 使用统计
    /// </summary>
    public UsageDetails? Usage { get; set; }
}
