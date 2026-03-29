namespace MAFStudio.Application.Capabilities;

using System.Reflection;

public interface ICapability
{
    string Name { get; }
    string Description { get; }
    IEnumerable<MethodInfo> GetTools();
}
