using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Infrastructure.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDapperContext _context;

    public UserRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM users WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM users WHERE username = @Username";
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Username = username });
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM users WHERE email = @Email";
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
    }

    public async Task<User> CreateAsync(User user)
    {
        using var connection = _context.CreateConnection();
        user.CreatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO users (id, username, email, password_hash, role, avatar, created_at, updated_at)
            VALUES (@Id, @Username, @Email, @PasswordHash, @Role, @Avatar, @CreatedAt, @UpdatedAt)
            RETURNING *";
        return await connection.QueryFirstAsync<User>(sql, user);
    }

    public async Task<User> UpdateAsync(User user)
    {
        using var connection = _context.CreateConnection();
        user.MarkAsUpdated();
        const string sql = @"
            UPDATE users SET 
                username = @Username,
                email = @Email,
                password_hash = @PasswordHash,
                role = @Role,
                avatar = @Avatar,
                updated_at = @UpdatedAt
            WHERE id = @Id
            RETURNING *";
        return await connection.QueryFirstAsync<User>(sql, user);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM users WHERE id = @Id";
        var rows = await connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    public async Task<bool> ExistsAsync(string username, string email)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT COUNT(*) FROM users WHERE username = @Username OR email = @Email";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Username = username, Email = email });
        return count > 0;
    }

    public async Task<List<User>> GetAllAsync()
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM users ORDER BY created_at DESC";
        var result = await connection.QueryAsync<User>(sql);
        return result.ToList();
    }
}
