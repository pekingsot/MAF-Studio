using MAFStudio.Backend.Data;

namespace MAFStudio.Backend.Abstractions
{
    /// <summary>
    /// 智能体类型服务接口
    /// 提供智能体类型的增删改查操作
    /// </summary>
    public interface IAgentTypeService
    {
        /// <summary>
        /// 获取所有智能体类型（系统类型+用户自己的类型）
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>类型列表</returns>
        Task<List<AgentType>> GetAllTypesAsync(string? userId = null);

        /// <summary>
        /// 获取启用的智能体类型
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>启用的类型列表</returns>
        Task<List<AgentType>> GetEnabledTypesAsync(string? userId = null);

        /// <summary>
        /// 根据ID获取类型
        /// </summary>
        /// <param name="id">类型ID</param>
        /// <returns>类型实体，不存在返回null</returns>
        Task<AgentType?> GetByIdAsync(Guid id);

        /// <summary>
        /// 根据编码获取类型
        /// </summary>
        /// <param name="code">类型编码</param>
        /// <returns>类型实体，不存在返回null</returns>
        Task<AgentType?> GetByCodeAsync(string code);

        /// <summary>
        /// 创建智能体类型
        /// </summary>
        /// <param name="type">类型实体</param>
        /// <param name="userId">用户ID</param>
        /// <returns>创建的类型实体</returns>
        Task<AgentType> CreateTypeAsync(AgentType type, string? userId = null);

        /// <summary>
        /// 更新智能体类型
        /// </summary>
        /// <param name="id">类型ID</param>
        /// <param name="type">更新数据</param>
        /// <param name="userId">用户ID</param>
        /// <returns>更新后的类型实体，不存在返回null</returns>
        Task<AgentType?> UpdateTypeAsync(Guid id, AgentType type, string? userId = null);

        /// <summary>
        /// 删除智能体类型
        /// </summary>
        /// <param name="id">类型ID</param>
        /// <param name="userId">用户ID</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteTypeAsync(Guid id, string? userId = null);

        /// <summary>
        /// 启用/禁用智能体类型
        /// </summary>
        /// <param name="id">类型ID</param>
        /// <param name="isEnabled">是否启用</param>
        /// <param name="userId">用户ID</param>
        /// <returns>更新后的类型实体，不存在返回null</returns>
        Task<AgentType?> ToggleEnableAsync(Guid id, bool isEnabled, string? userId = null);

        /// <summary>
        /// 初始化默认类型
        /// 创建系统预置的智能体类型
        /// </summary>
        Task InitializeDefaultTypesAsync();
    }
}
