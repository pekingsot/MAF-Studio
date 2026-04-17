namespace MAFStudio.Application.Skills;

public class SkillDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Version { get; set; }
    public string? Author { get; set; }
    public string? License { get; set; }
    public List<string> AllowedTools { get; set; } = new();
    public SkillPermissions? Permissions { get; set; }
    public List<SkillInput> Inputs { get; set; } = new();
    public List<SkillOutput> Outputs { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public string? Runtime { get; set; }
    public string? EntryPoint { get; set; }
    public string Instructions { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class SkillPermissions
{
    public bool Network { get; set; }
    public bool Filesystem { get; set; }
    public bool Shell { get; set; }
    public List<string> Env { get; set; } = new();
}

public class SkillInput
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public bool Required { get; set; }
    public string? Default { get; set; }
    public string? Description { get; set; }
}

public class SkillOutput
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public string? Description { get; set; }
}
