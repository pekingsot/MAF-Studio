using MAFStudio.Backend.Data;
using MAFStudio.Backend.Models;

namespace MAFStudio.Backend.Abstractions
{
    /// <summary>
    /// 大模型配置服务接口
    /// 提供大模型配置的增删改查、测试等功能
    /// </summary>
    public interface ILLMConfigService
    {
        /// <summary>
        /// 获取所有大模型配置（包含子模型）
        /// </summary>
        /// <param name="userId">用户ID，为空则获取所有</param>
        /// <returns>配置列表</returns>
        Task<List<LLMConfig>> GetAllConfigsAsync(string? userId = null);

        /// <summary>
        /// 根据ID获取配置（包含子模型）
        /// </summary>
        /// <param name="id">配置ID</param>
        /// <param name="userId">用户ID，用于权限验证</param>
        /// <returns>配置实体，不存在返回null</returns>
        Task<LLMConfig?> GetConfigByIdAsync(Guid id, string? userId = null);

        /// <summary>
        /// 创建大模型配置
        /// </summary>
        /// <param name="config">配置实体</param>
        /// <param name="userId">用户ID</param>
        /// <returns>创建的配置实体</returns>
        Task<LLMConfig> CreateConfigAsync(LLMConfig config, string? userId = null);

        /// <summary>
        /// 更新大模型配置
        /// </summary>
        /// <param name="id">配置ID</param>
        /// <param name="config">更新数据</param>
        /// <param name="userId">用户ID，用于权限验证</param>
        /// <returns>更新后的配置实体，不存在返回null</returns>
        Task<LLMConfig?> UpdateConfigAsync(Guid id, LLMConfig config, string? userId = null);

        /// <summary>
        /// 删除大模型配置
        /// </summary>
        /// <param name="id">配置ID</param>
        /// <param name="userId">用户ID，用于权限验证</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteConfigAsync(Guid id, string? userId = null);

        /// <summary>
        /// 设置默认配置
        /// </summary>
        /// <param name="id">配置ID</param>
        /// <returns>是否设置成功</returns>
        Task<bool> SetDefaultAsync(Guid id);

        /// <summary>
        /// 获取默认配置
        /// </summary>
        /// <returns>默认配置实体，不存在返回null</returns>
        Task<LLMConfig?> GetDefaultConfigAsync();

        /// <summary>
        /// 测试大模型连通性
        /// </summary>
        /// <param name="id">配置ID</param>
        /// <returns>测试结果（是否成功、消息、延迟毫秒数）</returns>
        Task<(bool success, string message, int latencyMs)> TestConnectionAsync(Guid id);

        /// <summary>
        /// 测试指定模型的连通性
        /// </summary>
        /// <param name="configId">配置ID</param>
        /// <param name="modelConfigId">模型配置ID</param>
        /// <returns>测试结果（是否成功、消息、延迟毫秒数）</returns>
        Task<(bool success, string message, int latencyMs)> TestModelConnectionAsync(Guid configId, Guid modelConfigId);

        /// <summary>
        /// 测试所有大模型连通性
        /// </summary>
        /// <returns>配置ID到测试结果的映射</returns>
        Task<Dictionary<Guid, (bool success, string message, int latencyMs)>> TestAllConnectionsAsync();

        /// <summary>
        /// 添加子模型配置
        /// </summary>
        /// <param name="llmConfigId">大模型配置ID</param>
        /// <param name="modelConfig">模型配置实体</param>
        /// <returns>创建的模型配置实体</returns>
        Task<LLMModelConfig> AddModelConfigAsync(Guid llmConfigId, LLMModelConfig modelConfig);

        /// <summary>
        /// 更新子模型配置
        /// </summary>
        /// <param name="modelConfigId">模型配置ID</param>
        /// <param name="modelConfig">更新数据</param>
        /// <returns>更新后的模型配置实体，不存在返回null</returns>
        Task<LLMModelConfig?> UpdateModelConfigAsync(Guid modelConfigId, LLMModelConfig modelConfig);

        /// <summary>
        /// 删除子模型配置
        /// </summary>
        /// <param name="modelConfigId">模型配置ID</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteModelConfigAsync(Guid modelConfigId);

        /// <summary>
        /// 设置默认模型
        /// </summary>
        /// <param name="llmConfigId">大模型配置ID</param>
        /// <param name="modelConfigId">模型配置ID</param>
        /// <returns>是否设置成功</returns>
        Task<bool> SetDefaultModelAsync(Guid llmConfigId, Guid modelConfigId);

        /// <summary>
        /// 获取测试记录
        /// </summary>
        /// <param name="llmConfigId">配置ID，为空则获取所有</param>
        /// <param name="limit">返回数量限制</param>
        /// <returns>测试记录列表</returns>
        Task<List<LLMTestRecord>> GetTestRecordsAsync(Guid? llmConfigId = null, int limit = 10);

        /// <summary>
        /// 获取供应商列表
        /// </summary>
        /// <returns>供应商信息列表</returns>
        List<ProviderInfo> GetProviders();

        /// <summary>
        /// 获取供应商可用模型列表
        /// </summary>
        /// <param name="providerId">供应商标识</param>
        /// <returns>模型名称列表</returns>
        Task<List<string>> GetProviderModelsAsync(string providerId);
    }
}
