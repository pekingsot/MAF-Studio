using MAFStudio.Backend.Data;

namespace MAFStudio.Backend.Abstractions
{
    /// <summary>
    /// 智能体服务接口
    /// 提供智能体的增删改查操作
    /// </summary>
    public interface IAgentService
    {
        /// <summary>
        /// 获取所有智能体
        /// </summary>
        /// <returns>智能体列表</returns>
        Task<List<Agent>> GetAllAgentsAsync();

        /// <summary>
        /// 根据用户ID获取智能体列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="isAdmin">是否为管理员</param>
        /// <returns>智能体列表</returns>
        Task<List<Agent>> GetAgentsByUserIdAsync(string userId, bool isAdmin);

        /// <summary>
        /// 根据ID获取智能体
        /// </summary>
        /// <param name="id">智能体ID</param>
        /// <returns>智能体实体，不存在返回null</returns>
        Task<Agent?> GetAgentByIdAsync(Guid id);

        /// <summary>
        /// 创建智能体
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="description">描述</param>
        /// <param name="type">类型</param>
        /// <param name="configuration">配置JSON</param>
        /// <param name="avatar">头像</param>
        /// <param name="userId">用户ID</param>
        /// <param name="llmConfigId">大模型配置ID</param>
        /// <returns>创建的智能体实体</returns>
        Task<Agent> CreateAgentAsync(string name, string description, string type, string configuration, string? avatar, string userId, Guid? llmConfigId = null);

        /// <summary>
        /// 更新智能体
        /// </summary>
        /// <param name="id">智能体ID</param>
        /// <param name="name">名称</param>
        /// <param name="description">描述</param>
        /// <param name="configuration">配置JSON</param>
        /// <param name="avatar">头像</param>
        /// <param name="llmConfigId">大模型配置ID</param>
        /// <returns>更新后的智能体实体，不存在返回null</returns>
        Task<Agent?> UpdateAgentAsync(Guid id, string? name, string? description, string? configuration, string? avatar, Guid? llmConfigId = null);

        /// <summary>
        /// 删除智能体
        /// </summary>
        /// <param name="id">智能体ID</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteAgentAsync(Guid id);

        /// <summary>
        /// 更新智能体状态
        /// </summary>
        /// <param name="id">智能体ID</param>
        /// <param name="status">新状态</param>
        /// <returns>更新后的智能体实体，不存在返回null</returns>
        Task<Agent?> UpdateAgentStatusAsync(Guid id, AgentStatus status);
    }
}
