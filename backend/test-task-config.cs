using System;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

class TestTaskConfig
{
    static async Task Main(string[] args)
    {
        var connectionString = "Host=localhost;Username=postgres;Password=postgres;Database=mafstudio";
        
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        // 查询最新的任务
        var sql = @"
            SELECT id, title, config, created_at 
            FROM collaboration_tasks 
            ORDER BY created_at DESC 
            LIMIT 5";
        
        var tasks = await connection.QueryAsync<dynamic>(sql);
        
        Console.WriteLine("最新的任务：");
        Console.WriteLine("=====================================");
        
        foreach (var task in tasks)
        {
            Console.WriteLine($"ID: {task.id}");
            Console.WriteLine($"标题: {task.title}");
            Console.WriteLine($"配置: {task.config ?? "NULL"}");
            Console.WriteLine($"创建时间: {task.created_at}");
            Console.WriteLine("-------------------------------------");
        }
        
        // 检查Config字段是否包含managerAgentId和managerCustomPrompt
        var checkSql = @"
            SELECT id, title, config 
            FROM collaboration_tasks 
            WHERE config IS NOT NULL 
            ORDER BY created_at DESC 
            LIMIT 1";
        
        var taskWithConfig = await connection.QueryFirstOrDefaultAsync<dynamic>(checkSql);
        
        if (taskWithConfig != null)
        {
            Console.WriteLine("\n检查Config字段内容：");
            Console.WriteLine("=====================================");
            Console.WriteLine($"任务ID: {taskWithConfig.id}");
            Console.WriteLine($"标题: {taskWithConfig.title}");
            Console.WriteLine($"Config: {taskWithConfig.config}");
            
            // 解析JSON
            try
            {
                var config = System.Text.Json.JsonDocument.Parse(taskWithConfig.config.ToString());
                var root = config.RootElement;
                
                Console.WriteLine("\n解析后的配置：");
                if (root.TryGetProperty("managerAgentId", out var managerAgentId))
                {
                    Console.WriteLine($"✅ managerAgentId: {managerAgentId}");
                }
                else
                {
                    Console.WriteLine("❌ managerAgentId: 未找到");
                }
                
                if (root.TryGetProperty("managerCustomPrompt", out var managerCustomPrompt))
                {
                    Console.WriteLine($"✅ managerCustomPrompt: {managerCustomPrompt}");
                }
                else
                {
                    Console.WriteLine("❌ managerCustomPrompt: 未找到");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ JSON解析失败: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("\n❌ 没有找到包含Config的任务");
        }
    }
}
