using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Infrastructure.Data.Repositories;

/// <summary>
/// 权限仓储实现
/// </summary>
public class PermissionRepository : IPermissionRepository
{
    private readonly IDapperContext _context;

    public PermissionRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<Permission?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM permissions WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<Permission>(sql, new { Id = id });
    }

    public async Task<Permission?> GetByCodeAsync(string code)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM permissions WHERE code = @Code";
        return await connection.QueryFirstOrDefaultAsync<Permission>(sql, new { Code = code });
    }

    public async Task<List<Permission>> GetAllAsync()
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM permissions ORDER BY resource, action";
        var result = await connection.QueryAsync<Permission>(sql);
        return result.ToList();
    }

    public async Task<List<Permission>> GetRolePermissionsAsync(long roleId)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            SELECT p.* 
            FROM permissions p
            INNER JOIN role_permissions rp ON p.id = rp.permission_id
            WHERE rp.role_id = @RoleId
            ORDER BY p.resource, p.action";
        var result = await connection.QueryAsync<Permission>(sql, new { RoleId = roleId });
        return result.ToList();
    }

    public async Task<List<Permission>> GetUserPermissionsAsync(long userId)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            SELECT DISTINCT p.* 
            FROM permissions p
            INNER JOIN role_permissions rp ON p.id = rp.permission_id
            INNER JOIN user_roles ur ON rp.role_id = ur.role_id
            WHERE ur.user_id = @UserId
            ORDER BY p.resource, p.action";
        var result = await connection.QueryAsync<Permission>(sql, new { UserId = userId });
        return result.ToList();
    }

    public async Task<Permission> CreateAsync(Permission permission)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            INSERT INTO permissions (id, name, code, description, resource, action, is_enabled, created_at)
            VALUES (@Id, @Name, @Code, @Description, @Resource, @Action, @IsEnabled, @CreatedAt)
            RETURNING *";
        return await connection.QueryFirstAsync<Permission>(sql, permission);
    }

    public async Task<Permission> UpdateAsync(Permission permission)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            UPDATE permissions 
            SET name = @Name, code = @Code, description = @Description, 
                resource = @Resource, action = @Action, is_enabled = @IsEnabled
            WHERE id = @Id
            RETURNING *";
        return await connection.QueryFirstAsync<Permission>(sql, permission);
    }

    public async Task DeleteAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM permissions WHERE id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task AssignPermissionToRoleAsync(long roleId, long permissionId)
    {
        using var connection = _context.CreateConnection();
        
        // 获取最大ID并加1
        const string maxIdSql = "SELECT COALESCE(MAX(id), 1100000000000000) + 1 FROM role_permissions";
        var newId = await connection.QueryFirstAsync<long>(maxIdSql);
        
        const string sql = @"
            INSERT INTO role_permissions (id, role_id, permission_id, created_at)
            VALUES (@NewId, @RoleId, @PermissionId, @CreatedAt)
            ON CONFLICT (role_id, permission_id) DO NOTHING";
        await connection.ExecuteAsync(sql, new { NewId = newId, RoleId = roleId, PermissionId = permissionId, CreatedAt = DateTime.UtcNow });
    }

    public async Task RemovePermissionFromRoleAsync(long roleId, long permissionId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM role_permissions WHERE role_id = @RoleId AND permission_id = @PermissionId";
        await connection.ExecuteAsync(sql, new { RoleId = roleId, PermissionId = permissionId });
    }

    public async Task<List<Permission>> GetByResourceAsync(string resource)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM permissions WHERE resource = @Resource ORDER BY action";
        var result = await connection.QueryAsync<Permission>(sql, new { Resource = resource });
        return result.ToList();
    }
}
