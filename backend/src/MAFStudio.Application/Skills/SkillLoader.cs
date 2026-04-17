using System.Text.Json;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Application.Skills;

public class SkillLoader
{
    private readonly IAgentSkillRepository _skillRepository;
    private readonly Dictionary<string, List<SkillDefinition>> _cache = new();

    public SkillLoader(IAgentSkillRepository skillRepository)
    {
        _skillRepository = skillRepository;
    }

    public async Task<List<SkillDefinition>> LoadSkillsForAgentAsync(long agentId)
    {
        var cacheKey = $"agent_{agentId}";
        if (_cache.TryGetValue(cacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        var skills = new List<SkillDefinition>();
        var agentSkills = await _skillRepository.GetEnabledByAgentIdAsync(agentId);

        foreach (var agentSkill in agentSkills)
        {
            try
            {
                var definition = ParseSkillContent(agentSkill.SkillContent);
                definition.Name = agentSkill.SkillName;
                definition.Runtime = agentSkill.Runtime ?? definition.Runtime ?? "python";
                definition.EntryPoint = agentSkill.EntryPoint ?? definition.EntryPoint;

                if (agentSkill.AllowedTools != null)
                {
                    try
                    {
                        var tools = JsonSerializer.Deserialize<List<string>>(agentSkill.AllowedTools);
                        if (tools != null && tools.Count > 0)
                            definition.AllowedTools = tools;
                    }
                    catch { }
                }

                skills.Add(definition);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析Skill失败 {agentSkill.SkillName}: {ex.Message}");
            }
        }

        _cache[cacheKey] = skills;
        return skills;
    }

    public SkillDefinition ParseSkillContent(string content)
    {
        var definition = new SkillDefinition();

        var (frontmatter, body) = SplitFrontmatter(content);

        if (!string.IsNullOrEmpty(frontmatter))
        {
            ParseYamlFrontmatter(frontmatter, definition);
        }

        definition.Instructions = body.Trim();

        return definition;
    }

    private (string Frontmatter, string Body) SplitFrontmatter(string content)
    {
        var trimmed = content.TrimStart();
        if (!trimmed.StartsWith("---"))
        {
            return (string.Empty, content);
        }

        var endIndex = trimmed.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (endIndex < 0)
        {
            return (string.Empty, content);
        }

        var frontmatter = trimmed.Substring(3, endIndex - 3).Trim();
        var body = trimmed.Substring(endIndex + 4).Trim();

        return (frontmatter, body);
    }

    private void ParseYamlFrontmatter(string yaml, SkillDefinition definition)
    {
        var lines = yaml.Split('\n');
        string? currentKey = null;
        string? currentSubKey = null;
        var currentListItems = new List<string>();
        var currentInputItems = new List<SkillInput>();
        var currentOutputItems = new List<SkillOutput>();
        var inList = false;
        var inInputs = false;
        var inOutputs = false;
        var inAllowedTools = false;
        var inTags = false;
        var inEnv = false;
        var inPermissions = false;
        var currentInput = new SkillInput();
        var currentOutput = new SkillOutput();

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.TrimStart().StartsWith("- "))
            {
                var item = line.TrimStart().Substring(2).Trim();

                if (inAllowedTools || inTags || inEnv)
                {
                    item = item.Trim('"', '\'');
                    currentListItems.Add(item);
                    continue;
                }

                if (inInputs)
                {
                    if (item.Contains(':'))
                    {
                        var kv = item.Split(':', 2);
                        var key = kv[0].Trim().Trim('"', '\'');
                        var val = kv[1].Trim().Trim('"', '\'');

                        switch (key)
                        {
                            case "name":
                                if (!string.IsNullOrEmpty(currentInput.Name))
                                    currentInputItems.Add(currentInput);
                                currentInput = new SkillInput { Name = val };
                                break;
                            case "type":
                                currentInput.Type = val;
                                break;
                            case "required":
                                currentInput.Required = val.Equals("true", StringComparison.OrdinalIgnoreCase);
                                break;
                            case "default":
                                currentInput.Default = val;
                                break;
                            case "description":
                                currentInput.Description = val;
                                break;
                        }
                    }
                    continue;
                }

                if (inOutputs)
                {
                    if (item.Contains(':'))
                    {
                        var kv = item.Split(':', 2);
                        var key = kv[0].Trim().Trim('"', '\'');
                        var val = kv[1].Trim().Trim('"', '\'');

                        switch (key)
                        {
                            case "name":
                                if (!string.IsNullOrEmpty(currentOutput.Name))
                                    currentOutputItems.Add(currentOutput);
                                currentOutput = new SkillOutput { Name = val };
                                break;
                            case "type":
                                currentOutput.Type = val;
                                break;
                            case "description":
                                currentOutput.Description = val;
                                break;
                        }
                    }
                    continue;
                }
            }

            FlushList();

            if (line.Contains(':') && !line.TrimStart().StartsWith("-"))
            {
                var indent = line.Length - line.TrimStart().Length;
                var colonIndex = line.IndexOf(':');
                var key = line.Substring(0, colonIndex).Trim();
                var val = line.Substring(colonIndex + 1).Trim().Trim('"', '\'');

                if (inInputs && indent > 0)
                {
                    switch (key)
                    {
                        case "name":
                            if (!string.IsNullOrEmpty(currentInput.Name))
                                currentInputItems.Add(currentInput);
                            currentInput = new SkillInput { Name = val };
                            break;
                        case "type":
                            currentInput.Type = val;
                            break;
                        case "required":
                            currentInput.Required = val.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "default":
                            currentInput.Default = val;
                            break;
                        case "description":
                            currentInput.Description = val;
                            break;
                    }
                    continue;
                }

                if (inOutputs && indent > 0)
                {
                    switch (key)
                    {
                        case "name":
                            if (!string.IsNullOrEmpty(currentOutput.Name))
                                currentOutputItems.Add(currentOutput);
                            currentOutput = new SkillOutput { Name = val };
                            break;
                        case "type":
                            currentOutput.Type = val;
                            break;
                        case "description":
                            currentOutput.Description = val;
                            break;
                    }
                    continue;
                }

                if (inInputs && !string.IsNullOrEmpty(currentInput.Name))
                {
                    currentInputItems.Add(currentInput);
                    currentInput = new SkillInput();
                }
                if (inOutputs && !string.IsNullOrEmpty(currentOutput.Name))
                {
                    currentOutputItems.Add(currentOutput);
                    currentOutput = new SkillOutput();
                }

                inInputs = false;
                inOutputs = false;
                inAllowedTools = false;
                inTags = false;
                inEnv = false;

                if (indent == 0)
                {
                    inPermissions = false;
                }

                if (inPermissions)
                {
                    switch (key)
                    {
                        case "network":
                            definition.Permissions ??= new SkillPermissions();
                            definition.Permissions.Network = val.Equals("true", StringComparison.OrdinalIgnoreCase);
                            continue;
                        case "filesystem":
                            definition.Permissions ??= new SkillPermissions();
                            definition.Permissions.Filesystem = val.Equals("true", StringComparison.OrdinalIgnoreCase);
                            continue;
                        case "shell":
                            definition.Permissions ??= new SkillPermissions();
                            definition.Permissions.Shell = val.Equals("true", StringComparison.OrdinalIgnoreCase);
                            continue;
                    }
                }

                switch (key)
                {
                    case "name":
                        definition.Name = val;
                        break;
                    case "description":
                        definition.Description = val;
                        break;
                    case "version":
                        definition.Version = val;
                        break;
                    case "author":
                        definition.Author = val;
                        break;
                    case "license":
                        definition.License = val;
                        break;
                    case "runtime":
                        definition.Runtime = val;
                        break;
                    case "entryPoint" or "entry_point":
                        definition.EntryPoint = val;
                        break;
                    case "allowed-tools" or "allowed_tools":
                        inAllowedTools = true;
                        currentListItems = definition.AllowedTools;
                        break;
                    case "tags":
                        inTags = true;
                        currentListItems = definition.Tags;
                        break;
                    case "inputs":
                        inInputs = true;
                        break;
                    case "outputs":
                        inOutputs = true;
                        break;
                    case "permissions":
                        inPermissions = true;
                        definition.Permissions ??= new SkillPermissions();
                        break;
                    case "env":
                        inEnv = true;
                        if (definition.Permissions == null) definition.Permissions = new SkillPermissions();
                        currentListItems = definition.Permissions.Env;
                        break;
                }
            }
        }

        FlushList();

        if (inInputs && !string.IsNullOrEmpty(currentInput.Name))
            currentInputItems.Add(currentInput);
        if (inOutputs && !string.IsNullOrEmpty(currentOutput.Name))
            currentOutputItems.Add(currentOutput);

        definition.Inputs = currentInputItems;
        definition.Outputs = currentOutputItems;

        void FlushList()
        {
            currentListItems = new List<string>();
        }
    }

    public string BuildSkillInstructions(List<SkillDefinition> skills)
    {
        if (skills.Count == 0) return string.Empty;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("\n\n# Your Skills (Agent Skills)");
        sb.AppendLine("You have the following skills available. Follow the instructions of each skill when the relevant trigger condition is met.");
        sb.AppendLine();

        foreach (var skill in skills)
        {
            sb.AppendLine($"## Skill: {skill.Name}");
            if (!string.IsNullOrEmpty(skill.Description))
            {
                sb.AppendLine($"Description: {skill.Description}");
            }
            if (skill.AllowedTools.Count > 0)
            {
                sb.AppendLine($"Allowed Tools: {string.Join(", ", skill.AllowedTools)}");
            }
            sb.AppendLine();
            sb.AppendLine(skill.Instructions);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public void ClearCache()
    {
        _cache.Clear();
    }
}
