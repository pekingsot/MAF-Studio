using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;

namespace MAFStudio.Application.Services;

public class OperationLogService : IOperationLogService
{
    private readonly IOperationLogRepository _logRepository;

    public OperationLogService(IOperationLogRepository logRepository)
    {
        _logRepository = logRepository;
    }

    public async Task LogAsync(string userId, string action, string resourceType, string? description, string? details)
    {
        var log = new OperationLog
        {
            UserId = userId,
            Action = action,
            ResourceType = resourceType,
            Description = description,
            Details = details,
        };

        await _logRepository.CreateAsync(log);
    }
}
