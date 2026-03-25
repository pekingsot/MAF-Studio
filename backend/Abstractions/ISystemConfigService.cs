using MAFStudio.Backend.Data;

namespace MAFStudio.Backend.Abstractions
{
    /// <summary>
    /// 系统配置服务接口
    /// 提供系统配置的增删改查功能
    /// </summary>
    public interface ISystemConfigService
    {
        /// <summary>
        /// 获取所有配置
        /// </summary>
        /// <returns>配置列表</returns>
        Task<List<SystemConfig>> GetAllConfigsAsync();

        /// <summary>
        /// 根据分组获取配置
        /// </summary>
        /// <param name="group">分组名称</param>
        /// <returns>配置列表</returns>
        Task<List<SystemConfig>> GetConfigsByGroupAsync(string group);

        /// <summary>
        /// 根据Key获取配置
        /// </summary>
        /// <param name="key">配置键</param>
        /// <returns>配置实体，不存在返回null</returns>
        Task<SystemConfig?> GetConfigByKeyAsync(string key);

        /// <summary>
        /// 获取配置值
        /// </summary>
        /// <param name="key">配置键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>配置值，不存在返回默认值</returns>
        Task<string?> GetConfigValueAsync(string key, string? defaultValue = null);

        /// <summary>
        /// 设置配置
        /// </summary>
        /// <param name="key">配置键</param>
        /// <param name="value">配置值</param>
        /// <param name="description">描述</param>
        /// <param name="group">分组</param>
        /// <returns>配置实体</returns>
        Task<SystemConfig> SetConfigAsync(string key, string value, string? description = null, string? group = null);

        /// <summary>
        /// 批量设置配置
        /// </summary>
        /// <param name="configs">配置字典</param>
        Task SetConfigsAsync(Dictionary<string, string> configs);

        /// <summary>
        /// 删除配置
        /// </summary>
        /// <param name="key">配置键</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteConfigAsync(string key);
    }
}
