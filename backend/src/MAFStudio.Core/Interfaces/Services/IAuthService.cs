using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Services;

public interface IAuthService
{
    Task<User?> ValidateUserAsync(string username, string password);
    Task<User> RegisterAsync(string username, string email, string password);
    Task<bool> IsAdminAsync(long userId);
    string GenerateJwtToken(User user, List<string> roles, List<string> permissions);
    Task<User?> GetByIdAsync(long id);
    Task<(List<string> roles, List<string> permissions)> GetUserRolesAndPermissionsAsync(long userId);
}
