namespace MAFStudio.Core.Interfaces.Services;

public interface IOperationLogService
{
    Task LogAsync(string userId, string action, string resourceType, string? description, string? details);
}
