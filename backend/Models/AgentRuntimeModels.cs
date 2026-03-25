using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MAFStudio.Backend.Models
{
    /// <summary>
    /// 智能体运行时状态枚举
    /// 定义智能体在生命周期中的各种状态
    /// </summary>
    public enum AgentRuntimeState
    {
        /// <summary>
        /// 未初始化 - 智能体已创建但未加载到内存
        /// </summary>
        Uninitialized,
        /// <summary>
        /// 就绪 - 智能体已初始化，等待任务
        /// </summary>
        Ready,
        /// <summary>
        /// 忙碌 - 正在处理任务
        /// </summary>
        Busy,
        /// <summary>
        /// 休眠 - 长时间未使用，已释放大部分资源
        /// </summary>
        Sleeping,
        /// <summary>
        /// 错误 - 初始化或运行时发生错误
        /// </summary>
        Error
    }

    /// <summary>
    /// 智能体运行时实例
    /// 包含智能体运行时的所有信息
    /// </summary>
    public class AgentRuntimeInstance
    {
        /// <summary>
        /// 智能体ID
        /// </summary>
        public Guid AgentId { get; set; }

        /// <summary>
        /// AI代理实例
        /// </summary>
        public AIAgent? AIAgent { get; set; }

        /// <summary>
        /// 聊天客户端
        /// </summary>
        public IChatClient? ChatClient { get; set; }

        /// <summary>
        /// 当前运行时状态
        /// </summary>
        public AgentRuntimeState State { get; set; } = AgentRuntimeState.Uninitialized;

        /// <summary>
        /// 最后活跃时间
        /// </summary>
        public DateTime LastActiveTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 初始化时间
        /// </summary>
        public DateTime InitializedTime { get; set; }

        /// <summary>
        /// 已处理任务数量
        /// </summary>
        public int TaskCount { get; set; }

        /// <summary>
        /// 最后一次错误信息
        /// </summary>
        public string? LastError { get; set; }

        /// <summary>
        /// 元数据字典
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
