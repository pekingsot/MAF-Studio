namespace MAFStudio.Application.Workflows;

public class ManagerThinkingEventArgs : EventArgs
{
    public string ManagerName { get; }
    public string Thinking { get; }
    public string? SelectedAgent { get; }
    public int IterationCount { get; }

    public ManagerThinkingEventArgs(string managerName, string thinking, string? selectedAgent, int iterationCount)
    {
        ManagerName = managerName;
        Thinking = thinking;
        SelectedAgent = selectedAgent;
        IterationCount = iterationCount;
    }
}
