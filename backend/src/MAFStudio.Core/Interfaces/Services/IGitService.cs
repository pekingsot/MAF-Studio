namespace MAFStudio.Core.Interfaces.Services;

/// <summary>
/// Git操作服务接口
/// 提供Git仓库的克隆、提交、推送等操作
/// </summary>
public interface IGitService
{
    /// <summary>
    /// 克隆仓库
    /// </summary>
    /// <param name="repoUrl">仓库地址</param>
    /// <param name="localPath">本地路径</param>
    /// <param name="credentials">凭证（Token或密码）</param>
    /// <param name="branch">分支名称（可选）</param>
    /// <returns>克隆结果</returns>
    Task<GitResult> CloneAsync(string repoUrl, string localPath, GitCredentials? credentials = null, string? branch = null);

    /// <summary>
    /// 设置Git用户身份
    /// </summary>
    /// <param name="repoPath">仓库路径</param>
    /// <param name="userName">用户名</param>
    /// <param name="userEmail">邮箱</param>
    Task<GitResult> SetIdentityAsync(string repoPath, string userName, string userEmail);

    /// <summary>
    /// 创建并切换到新分支
    /// </summary>
    /// <param name="repoPath">仓库路径</param>
    /// <param name="branchName">分支名称</param>
    Task<GitResult> CreateBranchAsync(string repoPath, string branchName);

    /// <summary>
    /// 切换分支
    /// </summary>
    /// <param name="repoPath">仓库路径</param>
    /// <param name="branchName">分支名称</param>
    Task<GitResult> CheckoutAsync(string repoPath, string branchName);

    /// <summary>
    /// 拉取最新代码
    /// </summary>
    /// <param name="repoPath">仓库路径</param>
    /// <param name="credentials">凭证</param>
    Task<GitResult> PullAsync(string repoPath, GitCredentials? credentials = null);

    /// <summary>
    /// 添加文件到暂存区
    /// </summary>
    /// <param name="repoPath">仓库路径</param>
    /// <param name="filePattern">文件模式，默认为"."表示所有文件</param>
    Task<GitResult> AddAsync(string repoPath, string filePattern = ".");

    /// <summary>
    /// 提交更改
    /// </summary>
    /// <param name="repoPath">仓库路径</param>
    /// <param name="message">提交消息</param>
    Task<GitResult> CommitAsync(string repoPath, string message);

    /// <summary>
    /// 推送到远程
    /// </summary>
    /// <param name="repoPath">仓库路径</param>
    /// <param name="branch">分支名称</param>
    /// <param name="credentials">凭证</param>
    /// <param name="setUpstream">是否设置上游分支</param>
    Task<GitResult> PushAsync(string repoPath, string branch, GitCredentials? credentials = null, bool setUpstream = false);

    /// <summary>
    /// 获取Git状态
    /// </summary>
    /// <param name="repoPath">仓库路径</param>
    Task<GitResult<string>> GetStatusAsync(string repoPath);

    /// <summary>
    /// 获取提交历史
    /// </summary>
    /// <param name="repoPath">仓库路径</param>
    /// <param name="count">数量</param>
    Task<GitResult<string>> GetLogAsync(string repoPath, int count = 10);

    /// <summary>
    /// 检查是否是有效的Git仓库
    /// </summary>
    /// <param name="repoPath">仓库路径</param>
    bool IsValidRepository(string repoPath);

    /// <summary>
    /// 验证仓库URL是否可访问
    /// </summary>
    /// <param name="repoUrl">仓库URL</param>
    /// <param name="credentials">凭证</param>
    Task<GitResult> ValidateRepositoryAccessAsync(string repoUrl, GitCredentials? credentials = null);
}

/// <summary>
/// Git操作结果
/// </summary>
public class GitResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Error { get; set; }

    public static GitResult Ok(string message) => new() { Success = true, Message = message };
    public static GitResult Fail(string error) => new() { Success = false, Error = error };
}

/// <summary>
/// Git操作结果（带返回值）
/// </summary>
public class GitResult<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Error { get; set; }
    public T? Data { get; set; }

    public static GitResult<T> Ok(T data, string message = "") => new() { Success = true, Data = data, Message = message };
    public static GitResult<T> Fail(string error) => new() { Success = false, Error = error };
}

/// <summary>
/// Git凭证
/// </summary>
public class GitCredentials
{
    /// <summary>
    /// 凭证类型
    /// </summary>
    public GitCredentialType Type { get; set; }

    /// <summary>
    /// Token（用于GitHub/GitLab等）
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// 用户名（用于密码认证）
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    public string? Password { get; set; }
}

/// <summary>
/// Git凭证类型
/// </summary>
public enum GitCredentialType
{
    Token,
    UsernamePassword
}
