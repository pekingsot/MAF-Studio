namespace MAFStudio.Application.Skills;

public class Skill
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Author { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
    public string? EntryPoint { get; set; }
    public string? Runtime { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();
    public DateTime LoadedAt { get; set; }
}

public class SkillManifest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Author { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
    public string? EntryPoint { get; set; }
    public string? Runtime { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();
}
