using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MAFStudio.Application.Services;
using MAFStudio.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Skills;

public class SkillExecutor
{
    private readonly IAgentSkillRepository _skillRepository;
    private readonly SkillLoader _skillLoader;
    private readonly ILogger<SkillExecutor>? _logger;

    public SkillExecutor(
        IAgentSkillRepository skillRepository,
        SkillLoader skillLoader,
        ILogger<SkillExecutor>? logger = null)
    {
        _skillRepository = skillRepository;
        _skillLoader = skillLoader;
        _logger = logger;
    }

    public async Task<string> ExecuteSkillAsync(
        long agentId,
        string skillName,
        Dictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var agentSkills = await _skillRepository.GetEnabledByAgentIdAsync(agentId);
        var agentSkill = agentSkills.FirstOrDefault(s => s.SkillName == skillName);

        if (agentSkill == null)
        {
            throw new NotFoundException($"Agent {agentId} 没有启用技能 {skillName}");
        }

        var definition = _skillLoader.ParseSkillContent(agentSkill.SkillContent);
        definition.Name = agentSkill.SkillName;
        definition.Runtime = agentSkill.Runtime ?? definition.Runtime ?? "python";
        definition.EntryPoint = agentSkill.EntryPoint ?? definition.EntryPoint;

        if (string.IsNullOrEmpty(definition.EntryPoint))
        {
            return "Skill has no executable script. It provides instructions only.";
        }

        var entryPoint = Path.Combine(
            Path.Combine(Directory.GetCurrentDirectory(), "skills", skillName),
            definition.EntryPoint);

        if (!File.Exists(entryPoint))
        {
            throw new NotFoundException($"Skill入口文件不存在：{entryPoint}");
        }

        _logger?.LogInformation("执行Skill: {SkillName}, Runtime: {Runtime}, EntryPoint: {EntryPoint}",
            skillName, definition.Runtime, entryPoint);

        return definition.Runtime?.ToLower() switch
        {
            "python" => await ExecutePythonScriptAsync(entryPoint, parameters, cancellationToken),
            "node" => await ExecuteNodeScriptAsync(entryPoint, parameters, cancellationToken),
            "bash" => await ExecuteBashScriptAsync(entryPoint, parameters, cancellationToken),
            _ => throw new NotSupportedException($"不支持的运行时：{definition.Runtime}")
        };
    }

    private async Task<string> ExecutePythonScriptAsync(
        string scriptPath,
        Dictionary<string, object>? parameters,
        CancellationToken cancellationToken)
    {
        var args = new StringBuilder(scriptPath);
        AppendParameters(args, parameters);
        return await ExecuteCommandAsync("python3", args.ToString(), cancellationToken);
    }

    private async Task<string> ExecuteNodeScriptAsync(
        string scriptPath,
        Dictionary<string, object>? parameters,
        CancellationToken cancellationToken)
    {
        var args = new StringBuilder(scriptPath);
        AppendParameters(args, parameters);
        return await ExecuteCommandAsync("node", args.ToString(), cancellationToken);
    }

    private async Task<string> ExecuteBashScriptAsync(
        string scriptPath,
        Dictionary<string, object>? parameters,
        CancellationToken cancellationToken)
    {
        var args = new StringBuilder(scriptPath);
        AppendParameters(args, parameters);
        return await ExecuteCommandAsync("bash", args.ToString(), cancellationToken);
    }

    private static void AppendParameters(StringBuilder args, Dictionary<string, object>? parameters)
    {
        if (parameters == null) return;
        foreach (var param in parameters)
        {
            args.Append($" --{param.Key} \"{param.Value}\"");
        }
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

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                output.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                error.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromMinutes(5));

        await process.WaitForExitAsync(cts.Token);

        if (error.Length > 0 && output.Length == 0)
        {
            throw new Exception($"执行失败：{error}");
        }

        return output.ToString();
    }
}
