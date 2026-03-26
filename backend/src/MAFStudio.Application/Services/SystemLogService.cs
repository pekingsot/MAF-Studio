using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;

namespace MAFStudio.Application.Services;

public class SystemLogService : ISystemLogService
{
    private readonly ISystemLogRepository _logRepository;

    public SystemLogService(ISystemLogRepository logRepository)
    {
        _logRepository = logRepository;
    }

    public async Task LogAsync(string level, string category, string message, string? exception = null, string? stackTrace = null, string? userId = null, string? requestPath = null, string? additionalData = null)
    {
        var log = new SystemLog
        {
            Level = level,
            Category = category,
            Message = message,
            Exception = exception,
            StackTrace = stackTrace,
            UserId = userId,
            RequestPath = requestPath,
            AdditionalData = additionalData,
        };

        await _logRepository.CreateAsync(log);
    }

    public async Task<List<SystemLog>> GetAsync(string? level = null, string? category = null, string? userId = null, int limit = 100)
    {
        return await _logRepository.GetAsync(level, category, userId, limit);
    }
}
