using Microsoft.Agents.AI.Workflows;

namespace MAFStudio.Application.Events;

public class ManagerThoughtEvent : WorkflowEvent
{
    public string ManagerName { get; }
    public string Thought { get; }

    public ManagerThoughtEvent(string managerName, string thought)
    {
        ManagerName = managerName;
        Thought = thought;
    }
}
