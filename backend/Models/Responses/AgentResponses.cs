namespace MAFStudio.Backend.Models.Responses
{
    /// <summary>
    /// 智能体运行时状态响应
    /// </summary>
    public class AgentRuntimeStatusResponse
    {
        /// <summary>
        /// 智能体ID
        /// </summary>
        public Guid AgentId { get; set; }

        /// <summary>
        /// 运行时状态
        /// </summary>
        public string State { get; set; } = "Uninitialized";

        /// <summary>
        /// 最后活跃时间
        /// </summary>
        public DateTime? LastActiveTime { get; set; }

        /// <summary>
        /// 任务数量
        /// </summary>
        public int TaskCount { get; set; }

        /// <summary>
        /// 最后错误信息
        /// </summary>
        public string? LastError { get; set; }

        /// <summary>
        /// 是否存活
        /// </summary>
        public bool IsAlive { get; set; }
    }

    /// <summary>
    /// 智能体测试请求
    /// </summary>
    public class AgentTestRequest
    {
        /// <summary>
        /// 测试输入
        /// </summary>
        public string? Input { get; set; }
    }

    /// <summary>
    /// 智能体测试响应
    /// </summary>
    public class AgentTestResponse
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 响应内容
        /// </summary>
        public string? Response { get; set; }

        /// <summary>
        /// 延迟（毫秒）
        /// </summary>
        public int LatencyMs { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string? State { get; set; }
    }
}
