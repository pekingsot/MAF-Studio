using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<bool> DeleteAsync(long id);
    Task<bool> ExistsAsync(string username, string email);
    Task<List<User>> GetAllAsync();
}
