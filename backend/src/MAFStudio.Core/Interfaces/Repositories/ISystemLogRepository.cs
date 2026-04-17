using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface ISystemLogRepository
{
    Task<SystemLog?> GetByIdAsync(long id);
    Task<(List<SystemLog> Data, int Total)> GetPagedAsync(string? level = null, string? category = null, string? keyword = null, DateTime? startTime = null, DateTime? endTime = null, int page = 1, int pageSize = 20);
    Task<SystemLog> CreateAsync(SystemLog log);
    Task<bool> DeleteAsync(long id);
    Task<int> DeleteBeforeAsync(DateTime cutoff);
    Task<List<string>> GetCategoriesAsync();
    Task<Dictionary<string, int>> GetLevelCountsAsync(int days = 7);
    Task<List<object>> GetDailyCountsAsync(int days = 7);
}
