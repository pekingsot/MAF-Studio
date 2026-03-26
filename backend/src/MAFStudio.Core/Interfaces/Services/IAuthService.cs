using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Services;

public interface IAuthService
{
    Task<User?> ValidateUserAsync(string username, string password);
    Task<User> RegisterAsync(string username, string email, string password);
    Task<bool> IsAdminAsync(string userId);
    string GenerateJwtToken(User user);
    Task<User?> GetByIdAsync(string id);
}
