namespace MAFStudio.Core.Interfaces.Services;

/// <summary>
/// 工作空间服务接口
/// 管理智能体工作目录的创建和路径计算
/// </summary>
public interface IWorkspaceService
{
    /// <summary>
    /// 获取工作空间基础目录
    /// </summary>
    string GetBaseDir();

    /// <summary>
    /// 获取团队工作目录
    /// </summary>
    /// <param name="teamId">团队ID</param>
    string GetTeamDir(long teamId);

    /// <summary>
    /// 获取任务工作目录
    /// </summary>
    /// <param name="teamId">团队ID</param>
    /// <param name="taskId">任务ID</param>
    string GetTaskDir(long teamId, long taskId);

    /// <summary>
    /// 获取智能体工作目录
    /// </summary>
    /// <param name="teamId">团队ID</param>
    /// <param name="taskId">任务ID</param>
    /// <param name="agentName">智能体名称</param>
    string GetAgentDir(long teamId, long taskId, string agentName);

    /// <summary>
    /// 获取智能体工作目录（使用ID）
    /// </summary>
    /// <param name="teamId">团队ID</param>
    /// <param name="taskId">任务ID</param>
    /// <param name="agentId">智能体ID</param>
    string GetAgentDirById(long teamId, long taskId, long agentId);

    /// <summary>
    /// 获取智能体仓库目录
    /// </summary>
    /// <param name="teamId">团队ID</param>
    /// <param name="taskId">任务ID</param>
    /// <param name="agentName">智能体名称</param>
    string GetAgentRepoDir(long teamId, long taskId, string agentName);

    /// <summary>
    /// 获取智能体仓库目录（使用ID）
    /// </summary>
    /// <param name="teamId">团队ID</param>
    /// <param name="taskId">任务ID</param>
    /// <param name="agentId">智能体ID</param>
    string GetAgentRepoDirById(long teamId, long taskId, long agentId);

    /// <summary>
    /// 确保目录存在
    /// </summary>
    /// <param name="dirPath">目录路径</param>
    void EnsureDirectoryExists(string dirPath);

    /// <summary>
    /// 初始化任务工作空间
    /// </summary>
    /// <param name="teamId">团队ID</param>
    /// <param name="taskId">任务ID</param>
    string InitializeTaskWorkspace(long teamId, long taskId);

    /// <summary>
    /// 初始化智能体工作空间
    /// </summary>
    /// <param name="teamId">团队ID</param>
    /// <param name="taskId">任务ID</param>
    /// <param name="agentName">智能体名称</param>
    string InitializeAgentWorkspace(long teamId, long taskId, string agentName);
}
