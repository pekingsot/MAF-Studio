using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Services;

public interface ISystemLogService
{
    Task LogAsync(string level, string category, string message, string? exception = null, string? stackTrace = null, long? userId = null, string? requestPath = null, string? requestMethod = null, string? source = null, string? additionalData = null);
    Task<(List<SystemLog> Data, int Total)> GetPagedAsync(string? level = null, string? category = null, string? keyword = null, DateTime? startTime = null, DateTime? endTime = null, int page = 1, int pageSize = 20);
    Task<bool> DeleteAsync(long id);
    Task<int> ClearBeforeAsync(int beforeDays);
    Task<List<string>> GetCategoriesAsync();
    Task<Dictionary<string, int>> GetLevelCountsAsync(int days = 7);
    Task<List<object>> GetDailyCountsAsync(int days = 7);
}
