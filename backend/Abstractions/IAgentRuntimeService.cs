using MAFStudio.Backend.Data;
using MAFStudio.Backend.Models;

namespace MAFStudio.Backend.Abstractions
{
    /// <summary>
    /// 智能体运行时服务接口
    /// 负责管理智能体的生命周期、资源分配和回收
    /// </summary>
    public interface IAgentRuntimeService
    {
        /// <summary>
        /// 初始化智能体
        /// 创建 AIAgent 实例并建立与大模型的连接
        /// </summary>
        /// <param name="agentId">智能体ID</param>
        /// <returns>智能体运行时实例</returns>
        Task<AgentRuntimeInstance> InitializeAgentAsync(Guid agentId);

        /// <summary>
        /// 获取智能体运行时实例
        /// </summary>
        /// <param name="agentId">智能体ID</param>
        /// <returns>运行时实例，如果不存在返回null</returns>
        Task<AgentRuntimeInstance?> GetRuntimeInstanceAsync(Guid agentId);

        /// <summary>
        /// 激活智能体（从休眠状态唤醒）
        /// </summary>
        /// <param name="agentId">智能体ID</param>
        /// <returns>激活后的运行时实例</returns>
        Task<AgentRuntimeInstance> ActivateAgentAsync(Guid agentId);

        /// <summary>
        /// 让智能体休眠（释放资源但保留配置）
        /// </summary>
        /// <param name="agentId">智能体ID</param>
        Task SleepAgentAsync(Guid agentId);

        /// <summary>
        /// 销毁智能体运行时实例（完全释放资源）
        /// </summary>
        /// <param name="agentId">智能体ID</param>
        Task DestroyAgentAsync(Guid agentId);

        /// <summary>
        /// 测试智能体连接
        /// 复用已有的 LLM Provider 架构进行连通性测试
        /// </summary>
        /// <param name="agentId">智能体ID</param>
        /// <returns>测试结果（是否成功、消息、延迟毫秒数）</returns>
        Task<(bool success, string message, int latencyMs)> TestAgentAsync(Guid agentId);

        /// <summary>
        /// 执行智能体任务
        /// </summary>
        /// <param name="agentId">智能体ID</param>
        /// <param name="input">用户输入</param>
        /// <returns>智能体响应</returns>
        Task<string> ExecuteAsync(Guid agentId, string input);

        /// <summary>
        /// 流式执行智能体任务
        /// </summary>
        /// <param name="agentId">智能体ID</param>
        /// <param name="input">用户输入</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>智能体响应流</returns>
        IAsyncEnumerable<string> ExecuteStreamAsync(Guid agentId, string input, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有活跃的智能体
        /// </summary>
        /// <returns>智能体ID到运行时实例的映射</returns>
        IReadOnlyDictionary<Guid, AgentRuntimeInstance> GetActiveAgents();

        /// <summary>
        /// 启动空闲检测服务
        /// 定期检查并清理长时间未使用的智能体
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        Task StartIdleDetectionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 获取智能体状态
        /// </summary>
        /// <param name="agentId">智能体ID</param>
        /// <returns>运行时状态</returns>
        AgentRuntimeState GetAgentState(Guid agentId);
    }
}
