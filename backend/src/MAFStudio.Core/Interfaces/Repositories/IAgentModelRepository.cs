using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

/// <summary>
/// 智能体模型配置仓储接口
/// </summary>
public interface IAgentModelRepository
{
    /// <summary>
    /// 根据ID获取
    /// </summary>
    Task<AgentModel?> GetByIdAsync(long id);

    /// <summary>
    /// 根据智能体ID获取所有模型配置（按优先级排序）
    /// </summary>
    Task<List<AgentModel>> GetByAgentIdAsync(long agentId);

    /// <summary>
    /// 获取智能体的主模型
    /// </summary>
    Task<AgentModel?> GetPrimaryModelAsync(long agentId);

    /// <summary>
    /// 获取智能体的可用模型列表（按优先级排序，用于故障转移）
    /// </summary>
    Task<List<AgentModel>> GetAvailableModelsAsync(long agentId);

    /// <summary>
    /// 创建
    /// </summary>
    Task<AgentModel> CreateAsync(AgentModel agentModel);

    /// <summary>
    /// 更新
    /// </summary>
    Task<AgentModel> UpdateAsync(AgentModel agentModel);

    /// <summary>
    /// 删除
    /// </summary>
    Task<bool> DeleteAsync(long id);

    /// <summary>
    /// 删除智能体的所有模型配置
    /// </summary>
    Task<bool> DeleteByAgentIdAsync(long agentId);

    /// <summary>
    /// 设置主模型
    /// </summary>
    Task SetPrimaryAsync(long agentId, long agentModelId);

    /// <summary>
    /// 批量创建
    /// </summary>
    Task<List<AgentModel>> CreateBatchAsync(List<AgentModel> agentModels);
}
