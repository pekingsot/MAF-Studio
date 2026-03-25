namespace MAFStudio.Backend.Models
{
    /// <summary>
    /// 供应商信息
    /// 用于展示和选择大模型供应商
    /// </summary>
    public class ProviderInfo
    {
        /// <summary>
        /// 供应商标识（如 qwen、openai、zhipu）
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称（如 阿里千问、OpenAI、智谱AI）
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 默认端点地址
        /// </summary>
        public string DefaultEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// 默认模型
        /// </summary>
        public string DefaultModel { get; set; } = string.Empty;
    }
}
