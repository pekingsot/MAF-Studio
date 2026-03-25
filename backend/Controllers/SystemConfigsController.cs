using Microsoft.AspNetCore.Mvc;
using MAFStudio.Backend.Data;
using MAFStudio.Backend.Services;
using MAFStudio.Backend.Abstractions;

namespace MAFStudio.Backend.Controllers
{
    /// <summary>
    /// 系统配置控制器
    /// 提供系统配置的管理接口
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SystemConfigsController : ControllerBase
    {
        private readonly ISystemConfigService _systemConfigService;

        /// <summary>
        /// 构造函数
        /// </summary>
        public SystemConfigsController(ISystemConfigService systemConfigService)
        {
            _systemConfigService = systemConfigService;
        }

        /// <summary>
        /// 获取所有系统配置
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<SystemConfig>>> GetAllConfigs()
        {
            var configs = await _systemConfigService.GetAllConfigsAsync();
            return Ok(configs);
        }

        /// <summary>
        /// 根据分组获取配置
        /// </summary>
        [HttpGet("group/{group}")]
        public async Task<ActionResult<List<SystemConfig>>> GetConfigsByGroup(string group)
        {
            var configs = await _systemConfigService.GetConfigsByGroupAsync(group);
            return Ok(configs);
        }

        /// <summary>
        /// 根据Key获取配置
        /// </summary>
        [HttpGet("{key}")]
        public async Task<ActionResult<SystemConfig>> GetConfig(string key)
        {
            var config = await _systemConfigService.GetConfigByKeyAsync(key);
            if (config == null)
            {
                return NotFound();
            }
            return Ok(config);
        }

        /// <summary>
        /// 设置配置
        /// </summary>
        [HttpPut("{key}")]
        public async Task<ActionResult<SystemConfig>> SetConfig(string key, [FromBody] SetConfigRequest request)
        {
            var config = await _systemConfigService.SetConfigAsync(key, request.Value, request.Description, request.Group);
            return Ok(config);
        }

        /// <summary>
        /// 批量设置配置
        /// </summary>
        [HttpPost("batch")]
        public async Task<ActionResult> SetConfigs([FromBody] BatchSetConfigsRequest request)
        {
            if (request?.Configs == null || !request.Configs.Any())
            {
                return BadRequest(new { message = "配置数据为空" });
            }

            foreach (var config in request.Configs)
            {
                if (!string.IsNullOrEmpty(config.Key))
                {
                    await _systemConfigService.SetConfigAsync(config.Key, config.Value ?? "", config.Description, config.Group);
                }
            }
            
            return Ok();
        }

        /// <summary>
        /// 删除配置
        /// </summary>
        [HttpDelete("{key}")]
        public async Task<ActionResult> DeleteConfig(string key)
        {
            var result = await _systemConfigService.DeleteConfigAsync(key);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }

        /// <summary>
        /// 初始化默认系统配置
        /// </summary>
        [HttpPost("initialize")]
        public async Task<ActionResult> InitializeDefaultConfigs()
        {
            var defaultConfigs = new Dictionary<string, (string value, string description, string group)>
            {
                // 向量化配置
                { SystemConfigKeys.EmbeddingEndpoint, ("http://localhost:7997", "向量化接口地址 (Infinity)", "rag") },
                { SystemConfigKeys.RerankEndpoint, ("http://localhost:7997", "查询重排序接口地址 (Infinity)", "rag") },
                { SystemConfigKeys.VectorDbEndpoint, ("http://localhost:6333", "向量库接口地址 (Qdrant)", "rag") },
                { SystemConfigKeys.VectorDbCollection, ("maf_documents", "向量库集合名称", "rag") },
                
                // 文本分割配置
                { SystemConfigKeys.SkipSplitExtensions, (".xml,.json,.yml,.yaml,.toml,.ini,.conf,.env,.dockerfile,.sh,.bat,.ps1", "跳过文本分割的文件扩展名", "rag") },
                { SystemConfigKeys.DefaultSplitMethod, ("recursive", "默认文本分割方式 (character, recursive, semantic)", "rag") },
                { SystemConfigKeys.DefaultChunkSize, ("500", "默认分块大小", "rag") },
                { SystemConfigKeys.DefaultChunkOverlap, ("50", "默认分块重叠大小", "rag") }
            };

            foreach (var kvp in defaultConfigs)
            {
                var existing = await _systemConfigService.GetConfigByKeyAsync(kvp.Key);
                if (existing == null)
                {
                    await _systemConfigService.SetConfigAsync(kvp.Key, kvp.Value.value, kvp.Value.description, kvp.Value.group);
                }
            }

            return Ok();
        }
    }

    /// <summary>
    /// 设置配置请求模型
    /// </summary>
    public class SetConfigRequest
    {
        /// <summary>
        /// 配置值
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 配置描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 配置分组
        /// </summary>
        public string? Group { get; set; }
    }

    /// <summary>
    /// 批量设置配置请求模型
    /// </summary>
    public class BatchSetConfigsRequest
    {
        /// <summary>
        /// 配置列表
        /// </summary>
        public List<ConfigItem>? Configs { get; set; }
    }

    /// <summary>
    /// 配置项
    /// </summary>
    public class ConfigItem
    {
        /// <summary>
        /// 配置键
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// 配置值
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// 配置描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 配置分组
        /// </summary>
        public string? Group { get; set; }
    }
}
