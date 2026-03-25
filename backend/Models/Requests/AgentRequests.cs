using MAFStudio.Backend.Data;

namespace MAFStudio.Backend.Models.Requests
{
    /// <summary>
    /// 创建智能体请求
    /// </summary>
    public class CreateAgentRequest
    {
        /// <summary>
        /// 智能体名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 智能体描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 智能体类型
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 智能体配置（JSON格式）
        /// </summary>
        public string Configuration { get; set; } = "{}";

        /// <summary>
        /// 头像
        /// </summary>
        public string? Avatar { get; set; }

        /// <summary>
        /// 大模型配置ID
        /// </summary>
        public Guid? LLMConfigId { get; set; }
    }

    /// <summary>
    /// 更新智能体请求
    /// </summary>
    public class UpdateAgentRequest
    {
        /// <summary>
        /// 智能体名称
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 智能体描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 智能体配置（JSON格式）
        /// </summary>
        public string? Configuration { get; set; }

        /// <summary>
        /// 头像
        /// </summary>
        public string? Avatar { get; set; }

        /// <summary>
        /// 大模型配置ID
        /// </summary>
        public Guid? LLMConfigId { get; set; }
    }

    /// <summary>
    /// 更新智能体状态请求
    /// </summary>
    public class UpdateAgentStatusRequest
    {
        /// <summary>
        /// 新状态
        /// </summary>
        public AgentStatus Status { get; set; }
    }
}
