# TaskConfig数据库字段说明

## ✅ Config字段已存在

### 1. 实体类定义

**文件**: `/home/pekingost/projects/maf-studio/backend/src/MAFStudio.Core/Entities/CollaborationTask.cs`

```csharp
namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("collaboration_tasks")]
public class CollaborationTask
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public long CollaborationId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Prompt { get; set; }

    public Enums.CollaborationTaskStatus Status { get; set; } = Enums.CollaborationTaskStatus.Pending;

    public long? AssignedTo { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public string? GitUrl { get; set; }

    public string? GitBranch { get; set; }

    public string? GitCredentials { get; set; }

    /// <summary>
    /// 任务配置（JSON格式）
    /// 包含：orchestrationMode, managerAgentId, maxIterations 等
    /// </summary>
    public string? Config { get; set; }  // ✅ 已存在
}
```

---

### 2. 数据库迁移脚本

**文件**: `/home/pekingost/projects/maf-studio/backend/migration_task_config.sql`

```sql
-- 为 collaboration_tasks 表添加 config 字段
-- 用于存储任务配置（执行模式、协调者ID、最大迭代次数等）

ALTER TABLE collaboration_tasks 
ADD COLUMN IF NOT EXISTS config TEXT;

COMMENT ON COLUMN collaboration_tasks.config IS '任务配置（JSON格式）：orchestrationMode, managerAgentId, maxIterations 等';
```

---

### 3. 执行迁移脚本

如果数据库中还没有Config字段，请执行以下命令：

```bash
# 连接数据库
PGPASSWORD=postgres psql -h localhost -U postgres -d mafstudio

# 或者使用Docker
docker exec -it mafstudio-postgres psql -U postgres -d mafstudio

# 执行迁移脚本
\i /path/to/migration_task_config.sql
```

或者直接执行SQL：

```bash
PGPASSWORD=postgres psql -h localhost -U postgres -d mafstudio -c "
ALTER TABLE collaboration_tasks 
ADD COLUMN IF NOT EXISTS config TEXT;

COMMENT ON COLUMN collaboration_tasks.config IS '任务配置（JSON格式）：orchestrationMode, managerAgentId, maxIterations 等';
"
```

---

### 4. 验证字段是否存在

```sql
-- 查看表结构
\d collaboration_tasks

-- 或者
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'collaboration_tasks' 
ORDER BY ordinal_position;
```

---

## 📊 Config字段存储的数据

### TaskConfig类定义

**文件**: `/home/pekingost/projects/maf-studio/backend/src/MAFStudio.Application/DTOs/TaskConfig.cs`

```csharp
public class TaskConfig
{
    [JsonPropertyName("workflowType")]
    public string WorkflowType { get; set; } = "GroupChat";

    [JsonPropertyName("orchestrationMode")]
    public string OrchestrationMode { get; set; } = "RoundRobin";

    [JsonPropertyName("managerAgentId")]
    public long? ManagerAgentId { get; set; }  // ✅ 协调者ID

    [JsonPropertyName("managerCustomPrompt")]
    public string? ManagerCustomPrompt { get; set; }  // ✅ 协调者提示词

    [JsonPropertyName("maxIterations")]
    public int MaxIterations { get; set; } = 10;

    [JsonPropertyName("workflowPlanId")]
    public long? WorkflowPlanId { get; set; }

    [JsonPropertyName("thresholds")]
    public Dictionary<string, double>? Thresholds { get; set; }

    [JsonPropertyName("maxAttempts")]
    public int? MaxAttempts { get; set; }
}
```

---

## 📝 JSON示例

### 群聊协作模式

```json
{
  "workflowType": "GroupChat",
  "orchestrationMode": "Manager",
  "maxIterations": 10,
  "managerAgentId": 123,
  "managerCustomPrompt": "你是一个群聊协调者，负责引导讨论..."
}
```

### Magentic工作流模式

```json
{
  "workflowType": "ReviewIterative",
  "orchestrationMode": "Manager",
  "maxIterations": 10,
  "managerAgentId": 123,
  "managerCustomPrompt": "你是一个Magentic协调者，负责动态规划和调度...",
  "workflowPlanId": null,
  "maxAttempts": 5,
  "thresholds": {
    "quality": 85,
    "accuracy": 90,
    "completeness": 80
  }
}
```

---

## ✅ 总结

1. **Config字段已存在** ✅
   - 实体类已定义
   - 迁移脚本已准备

2. **数据正确保存** ✅
   - 前端正确传递JSON
   - 后端正确解析JSON

3. **协调者配置完整** ✅
   - managerAgentId
   - managerCustomPrompt
   - 其他配置

---

## 🚀 下一步

如果数据库中没有Config字段，请执行迁移脚本：

```bash
cd /home/pekingost/projects/maf-studio/backend
PGPASSWORD=postgres psql -h localhost -U postgres -d mafstudio -f migration_task_config.sql
```

或者使用Docker：

```bash
docker exec -it mafstudio-postgres psql -U postgres -d mafstudio -f /path/to/migration_task_config.sql
```
