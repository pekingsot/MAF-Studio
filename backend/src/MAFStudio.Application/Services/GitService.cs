using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MAFStudio.Core.Interfaces.Services;

namespace MAFStudio.Application.Services;

/// <summary>
/// Git操作服务实现
/// 使用命令行git命令执行Git操作
/// </summary>
public class GitService : IGitService
{
    private readonly ILogger<GitService> _logger;

    public GitService(ILogger<GitService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 克隆仓库
    /// </summary>
    public async Task<GitResult> CloneAsync(string repoUrl, string localPath, GitCredentials? credentials = null, string? branch = null)
    {
        try
        {
            _logger.LogInformation("开始克隆仓库: {RepoUrl} -> {LocalPath}", repoUrl, localPath);

            var authenticatedUrl = BuildAuthenticatedUrl(repoUrl, credentials);
            var args = $"clone \"{authenticatedUrl}\" \"{localPath}\"";
            
            if (!string.IsNullOrEmpty(branch))
            {
                args += $" -b {branch}";
            }

            var result = await ExecuteGitCommandAsync(args);

            if (result.Contains("fatal") || result.Contains("error"))
            {
                _logger.LogError("克隆仓库失败: {Error}", result);
                return GitResult.Fail($"克隆失败: {result}");
            }

            _logger.LogInformation("克隆仓库成功: {LocalPath}", localPath);
            return GitResult.Ok($"成功克隆仓库到 {localPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "克隆仓库异常");
            return GitResult.Fail($"克隆异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 设置Git用户身份
    /// </summary>
    public async Task<GitResult> SetIdentityAsync(string repoPath, string userName, string userEmail)
    {
        try
        {
            _logger.LogInformation("设置Git身份: {UserName} <{UserEmail}>", userName, userEmail);

            await ExecuteGitCommandAsync($"config user.name \"{userName}\"", repoPath);
            await ExecuteGitCommandAsync($"config user.email \"{userEmail}\"", repoPath);

            return GitResult.Ok($"已设置身份: {userName} <{userEmail}>");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置Git身份失败");
            return GitResult.Fail($"设置身份失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建并切换到新分支
    /// </summary>
    public async Task<GitResult> CreateBranchAsync(string repoPath, string branchName)
    {
        try
        {
            _logger.LogInformation("创建分支: {BranchName}", branchName);

            var result = await ExecuteGitCommandAsync($"checkout -b {branchName}", repoPath);

            if (result.Contains("fatal") || result.Contains("error"))
            {
                return GitResult.Fail($"创建分支失败: {result}");
            }

            return GitResult.Ok($"已创建并切换到分支: {branchName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建分支失败");
            return GitResult.Fail($"创建分支异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 切换分支
    /// </summary>
    public async Task<GitResult> CheckoutAsync(string repoPath, string branchName)
    {
        try
        {
            _logger.LogInformation("切换分支: {BranchName}", branchName);

            var result = await ExecuteGitCommandAsync($"checkout {branchName}", repoPath);

            if (result.Contains("fatal") || result.Contains("error"))
            {
                return GitResult.Fail($"切换分支失败: {result}");
            }

            return GitResult.Ok($"已切换到分支: {branchName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换分支失败");
            return GitResult.Fail($"切换分支异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 拉取最新代码
    /// </summary>
    public async Task<GitResult> PullAsync(string repoPath, GitCredentials? credentials = null)
    {
        try
        {
            _logger.LogInformation("拉取最新代码: {RepoPath}", repoPath);

            var result = await ExecuteGitCommandAsync("pull", repoPath);

            if (result.Contains("fatal") || result.Contains("error"))
            {
                return GitResult.Fail($"拉取失败: {result}");
            }

            return GitResult.Ok($"拉取成功: {result}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "拉取失败");
            return GitResult.Fail($"拉取异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 添加文件到暂存区
    /// </summary>
    public async Task<GitResult> AddAsync(string repoPath, string filePattern = ".")
    {
        try
        {
            _logger.LogInformation("添加文件到暂存区: {FilePattern}", filePattern);

            var result = await ExecuteGitCommandAsync($"add {filePattern}", repoPath);

            return GitResult.Ok("已添加文件到暂存区");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加文件失败");
            return GitResult.Fail($"添加文件异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 提交更改
    /// </summary>
    public async Task<GitResult> CommitAsync(string repoPath, string message)
    {
        try
        {
            _logger.LogInformation("提交更改: {Message}", message);

            var result = await ExecuteGitCommandAsync($"commit -m \"{message}\"", repoPath);

            if (result.Contains("fatal") || result.Contains("error"))
            {
                return GitResult.Fail($"提交失败: {result}");
            }

            return GitResult.Ok($"提交成功: {message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交失败");
            return GitResult.Fail($"提交异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 推送到远程
    /// </summary>
    public async Task<GitResult> PushAsync(string repoPath, string branch, GitCredentials? credentials = null, bool setUpstream = false)
    {
        try
        {
            _logger.LogInformation("推送到远程: {Branch}", branch);

            var args = setUpstream ? $"push -u origin {branch}" : $"push origin {branch}";
            var result = await ExecuteGitCommandAsync(args, repoPath);

            if (result.Contains("fatal") || result.Contains("error"))
            {
                return GitResult.Fail($"推送失败: {result}");
            }

            return GitResult.Ok($"推送成功: {branch}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送失败");
            return GitResult.Fail($"推送异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取Git状态
    /// </summary>
    public async Task<GitResult<string>> GetStatusAsync(string repoPath)
    {
        try
        {
            var result = await ExecuteGitCommandAsync("status", repoPath);
            return GitResult<string>.Ok(result, "获取状态成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取状态失败");
            return GitResult<string>.Fail($"获取状态异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取提交历史
    /// </summary>
    public async Task<GitResult<string>> GetLogAsync(string repoPath, int count = 10)
    {
        try
        {
            var result = await ExecuteGitCommandAsync($"log --oneline -{count}", repoPath);
            return GitResult<string>.Ok(result, "获取历史成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取历史失败");
            return GitResult<string>.Fail($"获取历史异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 检查是否是有效的Git仓库
    /// </summary>
    public bool IsValidRepository(string repoPath)
    {
        var gitDir = Path.Combine(repoPath, ".git");
        return Directory.Exists(gitDir);
    }

    /// <summary>
    /// 验证仓库URL是否可访问
    /// </summary>
    public async Task<GitResult> ValidateRepositoryAccessAsync(string repoUrl, GitCredentials? credentials = null)
    {
        try
        {
            _logger.LogInformation("验证仓库访问权限: {RepoUrl}", repoUrl);

            var authenticatedUrl = BuildAuthenticatedUrl(repoUrl, credentials);
            var result = await ExecuteGitCommandAsync($"ls-remote \"{authenticatedUrl}\"");

            if (result.Contains("fatal") || result.Contains("error"))
            {
                return GitResult.Fail($"无法访问仓库: {result}");
            }

            return GitResult.Ok("仓库可访问");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证仓库访问失败");
            return GitResult.Fail($"验证异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 执行Git命令
    /// </summary>
    private async Task<string> ExecuteGitCommandAsync(string arguments, string? workingDirectory = null)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (!string.IsNullOrEmpty(workingDirectory))
        {
            processInfo.WorkingDirectory = workingDirectory;
        }

        using var process = new Process { StartInfo = processInfo };
        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                output.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                error.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        return error.Length > 0 ? error.ToString() : output.ToString();
    }

    /// <summary>
    /// 构建带认证的URL
    /// </summary>
    private string BuildAuthenticatedUrl(string repoUrl, GitCredentials? credentials)
    {
        if (credentials == null)
        {
            return repoUrl;
        }

        // HTTPS URL格式: https://token@github.com/user/repo.git
        // 或 https://username:password@github.com/user/repo.git
        if (repoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (credentials.Type == GitCredentialType.Token && !string.IsNullOrEmpty(credentials.Token))
            {
                return repoUrl.Replace("https://", $"https://{credentials.Token}@");
            }
            else if (credentials.Type == GitCredentialType.UsernamePassword 
                && !string.IsNullOrEmpty(credentials.Username) 
                && !string.IsNullOrEmpty(credentials.Password))
            {
                return repoUrl.Replace("https://", $"https://{credentials.Username}:{credentials.Password}@");
            }
        }

        return repoUrl;
    }
}
