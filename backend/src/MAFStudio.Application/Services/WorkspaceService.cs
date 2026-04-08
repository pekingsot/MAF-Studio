using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MAFStudio.Core.Interfaces.Services;

namespace MAFStudio.Application.Services;

/// <summary>
/// 工作空间服务实现
/// 管理智能体工作目录的创建和路径计算
/// </summary>
public class WorkspaceService : IWorkspaceService
{
    private readonly string _baseWorkDir;
    private readonly ILogger<WorkspaceService> _logger;

    public WorkspaceService(IConfiguration config, ILogger<WorkspaceService> logger)
    {
        _baseWorkDir = config["Workspace:BaseDir"] ?? "D:/workspace";
        _logger = logger;
        
        _logger.LogInformation("WorkspaceService 初始化，基础目录: {BaseDir}", _baseWorkDir);
    }

    /// <summary>
    /// 获取工作空间基础目录
    /// </summary>
    public string GetBaseDir()
    {
        return _baseWorkDir;
    }

    /// <summary>
    /// 获取团队工作目录
    /// </summary>
    public string GetTeamDir(long teamId)
    {
        return Path.Combine(_baseWorkDir, teamId.ToString());
    }

    /// <summary>
    /// 获取任务工作目录
    /// </summary>
    public string GetTaskDir(long teamId, long taskId)
    {
        return Path.Combine(_baseWorkDir, teamId.ToString(), taskId.ToString());
    }

    /// <summary>
    /// 获取智能体工作目录（使用名称）
    /// </summary>
    public string GetAgentDir(long teamId, long taskId, string agentName)
    {
        var safeAgentName = SanitizeAgentName(agentName);
        return Path.Combine(_baseWorkDir, teamId.ToString(), taskId.ToString(), "agents", safeAgentName);
    }

    /// <summary>
    /// 获取智能体工作目录（使用ID）
    /// </summary>
    public string GetAgentDirById(long teamId, long taskId, long agentId)
    {
        return Path.Combine(_baseWorkDir, teamId.ToString(), taskId.ToString(), "agents", agentId.ToString());
    }

    /// <summary>
    /// 获取智能体仓库目录（使用名称）
    /// </summary>
    public string GetAgentRepoDir(long teamId, long taskId, string agentName)
    {
        return Path.Combine(GetAgentDir(teamId, taskId, agentName), "repo");
    }

    /// <summary>
    /// 获取智能体仓库目录（使用ID）
    /// </summary>
    public string GetAgentRepoDirById(long teamId, long taskId, long agentId)
    {
        return Path.Combine(GetAgentDirById(teamId, taskId, agentId), "repo");
    }

    /// <summary>
    /// 确保目录存在
    /// </summary>
    public void EnsureDirectoryExists(string dirPath)
    {
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
            _logger.LogInformation("创建目录: {DirPath}", dirPath);
        }
    }

    /// <summary>
    /// 初始化任务工作空间
    /// </summary>
    public string InitializeTaskWorkspace(long teamId, long taskId)
    {
        var taskDir = GetTaskDir(teamId, taskId);
        
        EnsureDirectoryExists(taskDir);
        EnsureDirectoryExists(Path.Combine(taskDir, "agents"));
        EnsureDirectoryExists(Path.Combine(taskDir, "output"));
        
        _logger.LogInformation("初始化任务工作空间: {TaskDir}", taskDir);
        
        return taskDir;
    }

    /// <summary>
    /// 初始化智能体工作空间
    /// </summary>
    public string InitializeAgentWorkspace(long teamId, long taskId, string agentName)
    {
        var agentDir = GetAgentDir(teamId, taskId, agentName);
        
        EnsureDirectoryExists(agentDir);
        EnsureDirectoryExists(Path.Combine(agentDir, "repo"));
        EnsureDirectoryExists(Path.Combine(agentDir, "temp"));
        
        _logger.LogInformation("初始化智能体工作空间: {AgentDir}", agentDir);
        
        return agentDir;
    }

    /// <summary>
    /// 清理智能体名称，移除不安全的字符
    /// </summary>
    private static string SanitizeAgentName(string agentName)
    {
        if (string.IsNullOrWhiteSpace(agentName))
        {
            return "unknown-agent";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var safeName = string.Join("_", agentName.Split(invalidChars));
        
        return safeName.Trim();
    }
}
