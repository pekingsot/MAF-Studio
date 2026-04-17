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

    public async Task LogAsync(string level, string category, string message, string? exception = null, string? stackTrace = null, long? userId = null, string? requestPath = null, string? requestMethod = null, string? source = null, string? additionalData = null)
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
            RequestMethod = requestMethod,
            Source = source,
            AdditionalData = additionalData,
        };

        await _logRepository.CreateAsync(log);
    }

    public async Task<(List<SystemLog> Data, int Total)> GetPagedAsync(string? level = null, string? category = null, string? keyword = null, DateTime? startTime = null, DateTime? endTime = null, int page = 1, int pageSize = 20)
    {
        return await _logRepository.GetPagedAsync(level, category, keyword, startTime, endTime, page, pageSize);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        return await _logRepository.DeleteAsync(id);
    }

    public async Task<int> ClearBeforeAsync(int beforeDays)
    {
        var cutoff = DateTime.UtcNow.AddDays(-beforeDays);
        return await _logRepository.DeleteBeforeAsync(cutoff);
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        return await _logRepository.GetCategoriesAsync();
    }

    public async Task<Dictionary<string, int>> GetLevelCountsAsync(int days = 7)
    {
        return await _logRepository.GetLevelCountsAsync(days);
    }

    public async Task<List<object>> GetDailyCountsAsync(int days = 7)
    {
        return await _logRepository.GetDailyCountsAsync(days);
    }
}
