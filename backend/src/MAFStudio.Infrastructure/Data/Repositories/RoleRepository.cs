using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Infrastructure.Data.Repositories;

/// <summary>
/// 角色仓储实现
/// </summary>
public class RoleRepository : IRoleRepository
{
    private readonly IDapperContext _context;

    public RoleRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM roles WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<Role>(sql, new { Id = id });
    }

    public async Task<Role?> GetByCodeAsync(string code)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM roles WHERE code = @Code";
        return await connection.QueryFirstOrDefaultAsync<Role>(sql, new { Code = code });
    }

    public async Task<List<Role>> GetAllAsync()
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM roles ORDER BY name";
        var result = await connection.QueryAsync<Role>(sql);
        return result.ToList();
    }

    public async Task<List<Role>> GetUserRolesAsync(long userId)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            SELECT r.* 
            FROM roles r
            INNER JOIN user_roles ur ON r.id = ur.role_id
            WHERE ur.user_id = @UserId
            ORDER BY r.name";
        var result = await connection.QueryAsync<Role>(sql, new { UserId = userId });
        return result.ToList();
    }

    public async Task<Role> CreateAsync(Role role)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            INSERT INTO roles (id, name, code, description, is_system, is_enabled, created_at, updated_at)
            VALUES (@Id, @Name, @Code, @Description, @IsSystem, @IsEnabled, @CreatedAt, @UpdatedAt)
            RETURNING *";
        return await connection.QueryFirstAsync<Role>(sql, role);
    }

    public async Task<Role> UpdateAsync(Role role)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            UPDATE roles 
            SET name = @Name, code = @Code, description = @Description, 
                is_system = @IsSystem, is_enabled = @IsEnabled, updated_at = @UpdatedAt
            WHERE id = @Id
            RETURNING *";
        return await connection.QueryFirstAsync<Role>(sql, role);
    }

    public async Task DeleteAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM roles WHERE id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<bool> AssignRoleToUserAsync(long userId, long roleId)
    {
        using var connection = _context.CreateConnection();
        
        // 获取最大ID并加1
        const string maxIdSql = "SELECT COALESCE(MAX(id), 4000000000000000) + 1 FROM user_roles";
        var newId = await connection.QueryFirstAsync<long>(maxIdSql);
        
        const string sql = @"
            INSERT INTO user_roles (id, user_id, role_id, created_at)
            VALUES (@NewId, @UserId, @RoleId, @CreatedAt)
            ON CONFLICT (user_id, role_id) DO NOTHING";
        var rows = await connection.ExecuteAsync(sql, new { NewId = newId, UserId = userId, RoleId = roleId, CreatedAt = DateTime.UtcNow });
        return rows > 0;
    }

    public async Task<bool> RemoveRoleFromUserAsync(long userId, long roleId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM user_roles WHERE user_id = @UserId AND role_id = @RoleId";
        var rows = await connection.ExecuteAsync(sql, new { UserId = userId, RoleId = roleId });
        return rows > 0;
    }

    public async Task<List<Role>> GetRolesByPermissionIdAsync(long permissionId)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            SELECT r.* 
            FROM roles r
            INNER JOIN role_permissions rp ON r.id = rp.role_id
            WHERE rp.permission_id = @PermissionId
            ORDER BY r.name";
        var result = await connection.QueryAsync<Role>(sql, new { PermissionId = permissionId });
        return result.ToList();
    }
}
