using Microsoft.EntityFrameworkCore;
using MAFStudio.Backend.Data;
using MAFStudio.Backend.Abstractions;

namespace MAFStudio.Backend.Services
{
    /// <summary>
    /// 系统配置服务实现
    /// 提供系统配置的增删改查功能
    /// </summary>
    public class SystemConfigService : ISystemConfigService
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        public SystemConfigService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 获取所有配置
        /// </summary>
        public async Task<List<SystemConfig>> GetAllConfigsAsync()
        {
            return await _context.SystemConfigs
                .AsNoTracking()
                .OrderBy(c => c.Group)
                .ThenBy(c => c.Key)
                .ToListAsync();
        }

        /// <summary>
        /// 根据分组获取配置
        /// </summary>
        public async Task<List<SystemConfig>> GetConfigsByGroupAsync(string group)
        {
            return await _context.SystemConfigs
                .AsNoTracking()
                .Where(c => c.Group == group)
                .OrderBy(c => c.Key)
                .ToListAsync();
        }

        /// <summary>
        /// 根据Key获取配置
        /// </summary>
        public async Task<SystemConfig?> GetConfigByKeyAsync(string key)
        {
            return await _context.SystemConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Key == key);
        }

        /// <summary>
        /// 获取配置值
        /// </summary>
        public async Task<string?> GetConfigValueAsync(string key, string? defaultValue = null)
        {
            var config = await _context.SystemConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Key == key);

            return config?.Value ?? defaultValue;
        }

        /// <summary>
        /// 设置配置
        /// </summary>
        public async Task<SystemConfig> SetConfigAsync(string key, string value, string? description = null, string? group = null)
        {
            var config = await _context.SystemConfigs.FirstOrDefaultAsync(c => c.Key == key);

            if (config == null)
            {
                // 创建新配置
                config = new SystemConfig
                {
                    Id = Guid.NewGuid(),
                    Key = key,
                    Value = value,
                    Description = description,
                    Group = group,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SystemConfigs.Add(config);
            }
            else
            {
                // 更新现有配置
                config.Value = value;
                if (description != null) config.Description = description;
                if (group != null) config.Group = group;
                config.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return config;
        }

        /// <summary>
        /// 批量设置配置
        /// </summary>
        public async Task SetConfigsAsync(Dictionary<string, string> configs)
        {
            foreach (var kvp in configs)
            {
                await SetConfigAsync(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// 删除配置
        /// </summary>
        public async Task<bool> DeleteConfigAsync(string key)
        {
            var config = await _context.SystemConfigs.FirstOrDefaultAsync(c => c.Key == key);
            if (config == null) return false;

            _context.SystemConfigs.Remove(config);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
