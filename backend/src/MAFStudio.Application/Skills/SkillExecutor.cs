using System.Diagnostics;
using System.Text;
using MAFStudio.Application.Services;

namespace MAFStudio.Application.Skills;

public class SkillExecutor
{
    private readonly SkillLoader _skillLoader;

    public SkillExecutor(SkillLoader skillLoader)
    {
        _skillLoader = skillLoader;
    }

    public async Task<string> ExecuteSkillAsync(
        string skillId, 
        Dictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var skill = _skillLoader.GetSkill(skillId);
        if (skill == null)
        {
            throw new NotFoundException($"Skill {skillId} 不存在");
        }

        var entryPoint = Path.Combine(skill.Path, skill.EntryPoint ?? "main.py");
        if (!File.Exists(entryPoint))
        {
            throw new NotFoundException($"Skill入口文件不存在：{entryPoint}");
        }

        return skill.Runtime?.ToLower() switch
        {
            "python" => await ExecutePythonScriptAsync(entryPoint, parameters, cancellationToken),
            "node" => await ExecuteNodeScriptAsync(entryPoint, parameters, cancellationToken),
            "bash" => await ExecuteBashScriptAsync(entryPoint, parameters, cancellationToken),
            _ => throw new NotSupportedException($"不支持的运行时：{skill.Runtime}")
        };
    }

    private async Task<string> ExecutePythonScriptAsync(
        string scriptPath, 
        Dictionary<string, object>? parameters,
        CancellationToken cancellationToken)
    {
        var arguments = new StringBuilder(scriptPath);
        
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                arguments.Append($" --{param.Key} \"{param.Value}\"");
            }
        }

        return await ExecuteCommandAsync("python", arguments.ToString(), cancellationToken);
    }

    private async Task<string> ExecuteNodeScriptAsync(
        string scriptPath, 
        Dictionary<string, object>? parameters,
        CancellationToken cancellationToken)
    {
        var arguments = new StringBuilder(scriptPath);
        
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                arguments.Append($" --{param.Key} \"{param.Value}\"");
            }
        }

        return await ExecuteCommandAsync("node", arguments.ToString(), cancellationToken);
    }

    private async Task<string> ExecuteBashScriptAsync(
        string scriptPath, 
        Dictionary<string, object>? parameters,
        CancellationToken cancellationToken)
    {
        var arguments = new StringBuilder(scriptPath);
        
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                arguments.Append($" --{param.Key} \"{param.Value}\"");
            }
        }

        return await ExecuteCommandAsync("bash", arguments.ToString(), cancellationToken);
    }

    private async Task<string> ExecuteCommandAsync(
        string command, 
        string arguments,
        CancellationToken cancellationToken)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

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

        await process.WaitForExitAsync(cancellationToken);

        if (error.Length > 0)
        {
            throw new Exception($"执行失败：{error}");
        }

        return output.ToString();
    }
}
