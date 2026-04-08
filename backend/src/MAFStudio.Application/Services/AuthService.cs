using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Core.Utils;

namespace MAFStudio.Application.Services;

/// <summary>
/// 认证服务实现
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _configuration = configuration;
    }

    public async Task<User?> ValidateUserAsync(string username, string password)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null)
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return null;
        }

        return user;
    }

    public async Task<User> RegisterAsync(string username, string email, string password)
    {
        if (await _userRepository.ExistsAsync(username, email))
        {
            throw new InvalidOperationException("用户名或邮箱已存在");
        }

        var user = new User
        {
            Id = SnowflakeIdGenerator.Instance.NextId(),
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdUser = await _userRepository.CreateAsync(user);

        // 为新用户分配默认角色（普通用户）
        // 注意：这里需要根据实际的用户ID和角色ID进行分配
        // 暂时跳过，因为 User.Id 是 string 类型，而角色系统使用 long 类型

        return createdUser;
    }

    public async Task<bool> IsAdminAsync(long userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        // 简单判断：如果用户角色是 admin，则返回 true
        return user.Role == "admin";
    }

    public string GenerateJwtToken(User user, List<string> roles, List<string> permissions)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "your-super-secret-key-with-at-least-32-characters";
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "MAFStudio";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "MAFStudio";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim("permissions", string.Join(",", permissions))
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<User?> GetByIdAsync(long id)
    {
        return await _userRepository.GetByIdAsync(id);
    }

    public async Task<(List<string> roles, List<string> permissions)> GetUserRolesAndPermissionsAsync(long userId)
    {
        var roles = await _roleRepository.GetUserRolesAsync(userId);
        var permissions = await _permissionRepository.GetUserPermissionsAsync(userId);

        var roleCodes = roles.Select(r => r.Code).ToList();
        var permissionCodes = permissions.Select(p => p.Code).ToList();

        return (roleCodes, permissionCodes);
    }
}
