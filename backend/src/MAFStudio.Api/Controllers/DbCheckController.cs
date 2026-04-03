using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Dapper;
using MAFStudio.Infrastructure.Data;
using Npgsql;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // 允许匿名访问
public class DbCheckController : ControllerBase
{
    private readonly IDapperContext _context;

    public DbCheckController(IDapperContext context)
    {
        _context = context;
    }

    [HttpGet("check-table-structure")]
    public async Task<ActionResult> CheckTableStructure()
    {
        using var connection = _context.CreateOpenConnection();
        
        var columns = await connection.QueryAsync(@"
            SELECT column_name, data_type, is_nullable 
            FROM information_schema.columns 
            WHERE table_name = 'collaboration_agents' 
            ORDER BY ordinal_position");
        
        var migrations = await connection.QueryAsync(@"
            SELECT script_name, executed_at 
            FROM __migration_history 
            ORDER BY executed_at DESC 
            LIMIT 5");
        
        return Ok(new { columns, migrations });
    }

    [HttpGet("test")]
    [AllowAnonymous]
    public ActionResult Test()
    {
        return Ok("Test OK");
    }
}
