using MAFStudio.Backend.Data;

namespace MAFStudio.Backend.Abstractions
{
    /// <summary>
    /// 消息服务接口
    /// 提供智能体消息的发送、查询和管理功能
    /// </summary>
    public interface IMessageService
    {
        /// <summary>
        /// 获取智能体的消息列表
        /// </summary>
        /// <param name="agentId">智能体ID</param>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="before">获取此时间之前的消息</param>
        /// <returns>消息列表</returns>
        Task<List<AgentMessage>> GetMessagesForAgentAsync(Guid agentId, int page = 1, int pageSize = 50, DateTime? before = null);

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="fromAgentId">发送方智能体ID</param>
        /// <param name="toAgentId">接收方智能体ID</param>
        /// <param name="content">消息内容</param>
        /// <param name="type">消息类型</param>
        /// <returns>创建的消息实体</returns>
        Task<AgentMessage> SendMessageAsync(Guid fromAgentId, Guid toAgentId, string content, MessageType type);

        /// <summary>
        /// 发送协作消息（支持用户消息和@提及）
        /// </summary>
        /// <param name="content">消息内容</param>
        /// <param name="collaborationId">协作ID</param>
        /// <param name="mentionedAgentIds">提及的智能体ID列表</param>
        /// <param name="senderName">发送者名称</param>
        /// <returns>创建的消息实体</returns>
        Task<AgentMessage> SendCollaborationMessageAsync(string content, Guid collaborationId, List<Guid>? mentionedAgentIds = null, string? senderName = null);

        /// <summary>
        /// 发送智能体响应消息
        /// </summary>
        /// <param name="agentId">智能体ID</param>
        /// <param name="content">消息内容</param>
        /// <param name="collaborationId">协作ID</param>
        /// <returns>创建的消息实体</returns>
        Task<AgentMessage> SendAgentResponseAsync(Guid agentId, string content, Guid collaborationId);

        /// <summary>
        /// 更新消息状态
        /// </summary>
        /// <param name="messageId">消息ID</param>
        /// <param name="status">新状态</param>
        /// <returns>更新后的消息实体，不存在返回null</returns>
        Task<AgentMessage?> UpdateMessageStatusAsync(Guid messageId, MessageStatus status);

        /// <summary>
        /// 获取两个智能体之间的对话
        /// </summary>
        /// <param name="agent1Id">智能体1 ID</param>
        /// <param name="agent2Id">智能体2 ID</param>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="before">获取此时间之前的消息</param>
        /// <returns>消息列表</returns>
        Task<List<AgentMessage>> GetConversationAsync(Guid agent1Id, Guid agent2Id, int page = 1, int pageSize = 50, DateTime? before = null);

        /// <summary>
        /// 获取历史消息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="isAdmin">是否为管理员</param>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="before">获取此时间之前的消息</param>
        /// <returns>消息列表</returns>
        Task<List<AgentMessage>> GetHistoryMessagesAsync(string userId, bool isAdmin, int page = 1, int pageSize = 50, DateTime? before = null);

        /// <summary>
        /// 获取协作消息
        /// </summary>
        /// <param name="collaborationId">协作ID</param>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="before">获取此时间之前的消息</param>
        /// <returns>消息列表</returns>
        Task<List<AgentMessage>> GetCollaborationMessagesAsync(Guid collaborationId, int page = 1, int pageSize = 20, DateTime? before = null);

        /// <summary>
        /// 获取协作消息数量
        /// </summary>
        /// <param name="collaborationId">协作ID</param>
        /// <returns>消息数量</returns>
        Task<int> GetCollaborationMessagesCountAsync(Guid collaborationId);
    }
}
