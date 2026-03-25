using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MAFStudio.Backend.Data;
using MAFStudio.Backend.Abstractions;

namespace MAFStudio.Backend.Services
{
    /// <summary>
    /// 认证服务实现
    /// 提供用户注册、登录、权限验证等功能
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<(bool Success, string Message, User? User, string? Token)> RegisterAsync(string username, string email, string password)
        {
            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                return (false, "用户名已存在", null, null);
            }

            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                return (false, "邮箱已被注册", null, null);
            }

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = "user"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            return (true, "注册成功", user, token);
        }

        public async Task<(bool Success, string Message, User? User, string? Token)> LoginAsync(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            
            if (user == null)
            {
                return (false, "用户不存在", null, null);
            }

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return (false, "密码错误", null, null);
            }

            var token = GenerateJwtToken(user);
            return (true, "登录成功", user, token);
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<bool> IsAdminAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.Role == "admin";
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "DefaultSecretKeyForDevelopment12345678";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "MAFStudio";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "MAFStudio";

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
