namespace MAFStudio.Core.Configuration;

public class WorkspaceOptions
{
    public const string SectionName = "Workspace";
    public string BaseDir { get; set; } = "/tmp/maf-workspace";
}
