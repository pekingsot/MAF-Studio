namespace MAFStudio.Backend.Models.Requests
{
    /// <summary>
    /// 大模型配置请求
    /// </summary>
    public class LLMConfigRequest
    {
        /// <summary>
        /// 配置名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 供应商标识（如 qwen、openai、zhipu）
        /// </summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// API密钥
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// 端点地址
        /// </summary>
        public string? Endpoint { get; set; }

        /// <summary>
        /// 是否为默认配置
        /// </summary>
        public bool? IsDefault { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool? IsEnabled { get; set; }

        /// <summary>
        /// 子模型配置列表
        /// </summary>
        public List<LLMModelConfigRequest>? Models { get; set; }
    }

    /// <summary>
    /// 子模型配置请求
    /// </summary>
    public class LLMModelConfigRequest
    {
        /// <summary>
        /// 模型名称
        /// </summary>
        public string ModelName { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// 温度参数（0-2）
        /// </summary>
        public double? Temperature { get; set; }

        /// <summary>
        /// 最大令牌数
        /// </summary>
        public int? MaxTokens { get; set; }

        /// <summary>
        /// 上下文窗口大小
        /// </summary>
        public int? ContextWindow { get; set; }

        /// <summary>
        /// TopP参数
        /// </summary>
        public double? TopP { get; set; }

        /// <summary>
        /// 频率惩罚
        /// </summary>
        public double? FrequencyPenalty { get; set; }

        /// <summary>
        /// 存在惩罚
        /// </summary>
        public double? PresencePenalty { get; set; }

        /// <summary>
        /// 停止序列（JSON数组格式）
        /// </summary>
        public string? StopSequences { get; set; }

        /// <summary>
        /// 是否为默认模型
        /// </summary>
        public bool? IsDefault { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool? IsEnabled { get; set; }
    }
}
