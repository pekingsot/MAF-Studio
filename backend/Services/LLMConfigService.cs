using Microsoft.EntityFrameworkCore;
using MAFStudio.Backend.Data;
using MAFStudio.Backend.Abstractions;
using MAFStudio.Backend.Providers;
using MAFStudio.Backend.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Backend.Services
{
    /// <summary>
    /// 大模型配置服务实现
    /// 提供大模型配置的增删改查、测试等功能
    /// </summary>
    public class LLMConfigService : ILLMConfigService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LLMConfigService> _logger;
        private readonly LLMProviderFactory _providerFactory;

        public LLMConfigService(ApplicationDbContext context, ILogger<LLMConfigService> logger, LLMProviderFactory providerFactory)
        {
            _context = context;
            _logger = logger;
            _providerFactory = providerFactory;
        }

        /// <summary>
        /// 获取所有大模型配置
        /// </summary>
        public async Task<List<LLMConfig>> GetAllConfigsAsync(string? userId = null)
        {
            var query = _context.LLMConfigs
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(c => c.UserId == userId);
            }

            var configs = await query
                .OrderByDescending(c => c.IsDefault)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var configIds = configs.Select(c => c.Id).ToList();

            var models = await _context.LLMModelConfigs
                .AsNoTracking()
                .Where(m => configIds.Contains(m.LLMConfigId))
                .OrderBy(m => m.SortOrder)
                .ToListAsync();

            var modelIds = models.Select(m => m.Id).ToList();

            var allTestRecords = await _context.LLMTestRecords
                .AsNoTracking()
                .Where(r => r.LLMModelConfigId != null && modelIds.Contains(r.LLMModelConfigId.Value))
                .OrderByDescending(r => r.TestedAt)
                .ToListAsync();

            var modelsByConfigId = models.GroupBy(m => m.LLMConfigId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var config in configs)
            {
                if (modelsByConfigId.TryGetValue(config.Id, out var configModels))
                {
                    config.Models = configModels;
                }
                else
                {
                    config.Models = new List<LLMModelConfig>();
                }

                config.TestRecords = allTestRecords
                    .Where(r => r.LLMConfigId == config.Id)
                    .ToList();
            }

            return configs;
        }

        /// <summary>
        /// 根据ID获取配置
        /// </summary>
        public async Task<LLMConfig?> GetConfigByIdAsync(Guid id, string? userId = null)
        {
            var query = _context.LLMConfigs
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(c => c.UserId == userId);
            }

            var config = await query.FirstOrDefaultAsync(c => c.Id == id);
            if (config == null) return null;

            config.Models = await _context.LLMModelConfigs
                .AsNoTracking()
                .Where(m => m.LLMConfigId == id)
                .OrderBy(m => m.SortOrder)
                .ToListAsync();

            return config;
        }

        /// <summary>
        /// 创建大模型配置
        /// </summary>
        public async Task<LLMConfig> CreateConfigAsync(LLMConfig config, string? userId = null)
        {
            if (config.IsDefault)
            {
                var existingDefaults = await _context.LLMConfigs
                    .Where(c => c.IsDefault && c.UserId == userId)
                    .ToListAsync();
                foreach (var item in existingDefaults)
                {
                    item.IsDefault = false;
                }
            }

            config.Id = Guid.NewGuid();
            config.CreatedAt = DateTime.UtcNow;
            config.UserId = userId;

            if (config.Models != null && config.Models.Any())
            {
                foreach (var model in config.Models)
                {
                    model.Id = Guid.NewGuid();
                    model.LLMConfigId = config.Id;
                    model.CreatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                config.Models = new List<LLMModelConfig>();
            }

            _context.LLMConfigs.Add(config);
            await _context.SaveChangesAsync();

            return config;
        }

        /// <summary>
        /// 更新大模型配置
        /// </summary>
        public async Task<LLMConfig?> UpdateConfigAsync(Guid id, LLMConfig config, string? userId = null)
        {
            var existing = await _context.LLMConfigs.FindAsync(id);
            if (existing == null) return null;
            
            if (!string.IsNullOrEmpty(userId) && existing.UserId != userId) return null;

            if (config.IsDefault && !existing.IsDefault)
            {
                var existingDefaults = await _context.LLMConfigs
                    .Where(c => c.IsDefault && c.Id != id && c.UserId == userId)
                    .ToListAsync();
                foreach (var item in existingDefaults)
                {
                    item.IsDefault = false;
                }
            }

            existing.Name = config.Name;
            existing.Provider = config.Provider;
            existing.ApiKey = config.ApiKey;
            existing.Endpoint = config.Endpoint;
            existing.IsDefault = config.IsDefault;
            existing.IsEnabled = config.IsEnabled;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return existing;
        }

        /// <summary>
        /// 删除大模型配置
        /// </summary>
        public async Task<bool> DeleteConfigAsync(Guid id, string? userId = null)
        {
            var config = await _context.LLMConfigs.FindAsync(id);
            if (config == null) return false;
            
            if (!string.IsNullOrEmpty(userId) && config.UserId != userId) return false;

            _context.LLMConfigs.Remove(config);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// 设置默认配置
        /// </summary>
        public async Task<bool> SetDefaultAsync(Guid id)
        {
            var config = await _context.LLMConfigs.FindAsync(id);
            if (config == null) return false;

            var existingDefaults = await _context.LLMConfigs
                .Where(c => c.IsDefault)
                .ToListAsync();
            foreach (var item in existingDefaults)
            {
                item.IsDefault = false;
            }

            config.IsDefault = true;
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// 获取默认配置
        /// </summary>
        public async Task<LLMConfig?> GetDefaultConfigAsync()
        {
            var config = await _context.LLMConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IsDefault && c.IsEnabled);

            if (config == null) return null;

            config.Models = await _context.LLMModelConfigs
                .AsNoTracking()
                .Where(m => m.LLMConfigId == config.Id && m.IsEnabled && m.IsDefault)
                .ToListAsync();

            return config;
        }

        /// <summary>
        /// 测试大模型连通性
        /// </summary>
        public async Task<(bool success, string message, int latencyMs)> TestConnectionAsync(Guid id)
        {
            var config = await _context.LLMConfigs.FindAsync(id);
            if (config == null)
            {
                return (false, "配置不存在", 0);
            }

            if (!config.IsEnabled)
            {
                return (false, "配置已禁用", 0);
            }

            if (string.IsNullOrEmpty(config.ApiKey))
            {
                return (false, "API Key未配置", 0);
            }

            var models = await _context.LLMModelConfigs
                .AsNoTracking()
                .Where(m => m.LLMConfigId == id && m.IsEnabled)
                .OrderBy(m => m.SortOrder)
                .ToListAsync();

            var defaultModel = models.FirstOrDefault(m => m.IsDefault) ?? models.FirstOrDefault();
            if (defaultModel == null)
            {
                return (false, "未配置模型", 0);
            }

            return await TestModelConnectionInternalAsync(config, defaultModel);
        }

        /// <summary>
        /// 测试指定模型的连通性
        /// </summary>
        public async Task<(bool success, string message, int latencyMs)> TestModelConnectionAsync(Guid configId, Guid modelConfigId)
        {
            var config = await _context.LLMConfigs.FindAsync(configId);
            if (config == null)
            {
                return (false, "配置不存在", 0);
            }

            var modelConfig = await _context.LLMModelConfigs.FindAsync(modelConfigId);
            if (modelConfig == null)
            {
                return (false, "模型配置不存在", 0);
            }

            return await TestModelConnectionInternalAsync(config, modelConfig);
        }

        /// <summary>
        /// 内部测试方法
        /// </summary>
        private async Task<(bool success, string message, int latencyMs)> TestModelConnectionInternalAsync(LLMConfig config, LLMModelConfig modelConfig)
        {
            var providerId = config.Provider?.ToLower() ?? "";
            _logger.LogInformation("测试大模型连通性 - Provider: {Provider}, Model: {Model}", providerId, modelConfig.ModelName);

            var provider = _providerFactory.GetProvider(providerId);

            if (provider != null)
            {
                var result = await provider.TestModelConnectionAsync(config, modelConfig);

                var record = new LLMTestRecord
                {
                    Id = Guid.NewGuid(),
                    LLMConfigId = config.Id,
                    LLMModelConfigId = modelConfig.Id,
                    Provider = config.Provider,
                    ModelName = modelConfig.ModelName,
                    IsSuccess = result.success,
                    Message = result.message,
                    LatencyMs = result.latencyMs,
                    TestedAt = DateTime.UtcNow
                };
                _context.LLMTestRecords.Add(record);
                await _context.SaveChangesAsync();

                return result;
            }

            _logger.LogWarning("未知的供应商: {Provider}", providerId);
            return (false, $"不支持的供应商: {config.Provider}", 0);
        }

        /// <summary>
        /// 测试所有大模型连通性
        /// </summary>
        public async Task<Dictionary<Guid, (bool success, string message, int latencyMs)>> TestAllConnectionsAsync()
        {
            var configs = await _context.LLMConfigs
                .AsNoTracking()
                .Where(c => c.IsEnabled)
                .ToListAsync();

            var results = new Dictionary<Guid, (bool success, string message, int latencyMs)>();

            foreach (var config in configs)
            {
                var result = await TestConnectionAsync(config.Id);
                results[config.Id] = result;
            }

            return results;
        }

        /// <summary>
        /// 添加子模型配置
        /// </summary>
        public async Task<LLMModelConfig> AddModelConfigAsync(Guid llmConfigId, LLMModelConfig modelConfig)
        {
            var config = await _context.LLMConfigs.FindAsync(llmConfigId);
            if (config == null)
            {
                throw new ArgumentException("供应商配置不存在");
            }

            var existingModels = await _context.LLMModelConfigs
                .Where(m => m.LLMConfigId == llmConfigId)
                .ToListAsync();

            if (!existingModels.Any() || modelConfig.IsDefault)
            {
                foreach (var m in existingModels)
                {
                    m.IsDefault = false;
                }
                modelConfig.IsDefault = true;
            }

            modelConfig.Id = Guid.NewGuid();
            modelConfig.LLMConfigId = llmConfigId;
            modelConfig.CreatedAt = DateTime.UtcNow;
            modelConfig.SortOrder = existingModels.Count;

            _context.LLMModelConfigs.Add(modelConfig);
            await _context.SaveChangesAsync();

            return modelConfig;
        }

        /// <summary>
        /// 更新子模型配置
        /// </summary>
        public async Task<LLMModelConfig?> UpdateModelConfigAsync(Guid modelConfigId, LLMModelConfig modelConfig)
        {
            var existing = await _context.LLMModelConfigs.FindAsync(modelConfigId);
            if (existing == null) return null;

            if (modelConfig.IsDefault && !existing.IsDefault)
            {
                var otherModels = await _context.LLMModelConfigs
                    .Where(m => m.LLMConfigId == existing.LLMConfigId && m.Id != modelConfigId)
                    .ToListAsync();
                foreach (var m in otherModels)
                {
                    m.IsDefault = false;
                }
            }

            existing.ModelName = modelConfig.ModelName;
            existing.DisplayName = modelConfig.DisplayName;
            existing.Temperature = modelConfig.Temperature;
            existing.MaxTokens = modelConfig.MaxTokens;
            existing.ContextWindow = modelConfig.ContextWindow;
            existing.TopP = modelConfig.TopP;
            existing.FrequencyPenalty = modelConfig.FrequencyPenalty;
            existing.PresencePenalty = modelConfig.PresencePenalty;
            existing.StopSequences = modelConfig.StopSequences;
            existing.IsDefault = modelConfig.IsDefault;
            existing.IsEnabled = modelConfig.IsEnabled;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return existing;
        }

        /// <summary>
        /// 删除子模型配置
        /// </summary>
        public async Task<bool> DeleteModelConfigAsync(Guid modelConfigId)
        {
            var model = await _context.LLMModelConfigs.FindAsync(modelConfigId);
            if (model == null) return false;

            var modelCount = await _context.LLMModelConfigs
                .CountAsync(m => m.LLMConfigId == model.LLMConfigId);

            if (modelCount <= 1)
            {
                return false;
            }

            _context.LLMModelConfigs.Remove(model);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// 设置默认模型
        /// </summary>
        public async Task<bool> SetDefaultModelAsync(Guid llmConfigId, Guid modelConfigId)
        {
            var model = await _context.LLMModelConfigs.FindAsync(modelConfigId);
            if (model == null || model.LLMConfigId != llmConfigId) return false;

            var otherModels = await _context.LLMModelConfigs
                .Where(m => m.LLMConfigId == llmConfigId)
                .ToListAsync();
            foreach (var m in otherModels)
            {
                m.IsDefault = m.Id == modelConfigId;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// 获取测试记录
        /// </summary>
        public async Task<List<LLMTestRecord>> GetTestRecordsAsync(Guid? llmConfigId = null, int limit = 10)
        {
            var query = _context.LLMTestRecords
                .AsNoTracking()
                .AsQueryable();

            if (llmConfigId.HasValue)
            {
                query = query.Where(r => r.LLMConfigId == llmConfigId.Value);
            }

            var records = await query
                .OrderByDescending(r => r.TestedAt)
                .Take(limit)
                .ToListAsync();

            var configIds = records.Select(r => r.LLMConfigId).Distinct().ToList();
            var configs = await _context.LLMConfigs
                .AsNoTracking()
                .Where(c => configIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id);

            foreach (var record in records)
            {
                if (configs.TryGetValue(record.LLMConfigId, out var config))
                {
                    record.LLMConfig = config;
                }
            }

            return records;
        }

        /// <summary>
        /// 获取供应商列表
        /// </summary>
        public List<ProviderInfo> GetProviders()
        {
            return _providerFactory.GetAllProviders();
        }

        /// <summary>
        /// 获取供应商可用模型列表
        /// </summary>
        public async Task<List<string>> GetProviderModelsAsync(string providerId)
        {
            var provider = _providerFactory.GetProvider(providerId);
            if (provider == null)
            {
                return new List<string>();
            }

            return await provider.GetAvailableModelsAsync(new LLMConfig { Provider = providerId });
        }
    }
}
