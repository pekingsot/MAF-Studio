# MAF Studio 完整技术方案文档 - 第二部分

## 第四部分：AI公司系统设计

### 4.1 系统概述

AI公司系统是MAF Studio的核心创新，将多智能体协作系统包装成"AI公司经营"的概念，让用户体验真实的企业运作。

#### 4.1.1 核心理念

**用户角色**：公司老板
**AI角色**：公司员工
**项目**：公司业务
**任务**：员工工作

#### 4.1.2 系统特点

1. **真实感**：模拟真实公司运作
2. **趣味性**：游戏化经营体验
3. **策略性**：资源管理和人员配置
4. **智能化**：AI驱动的自动化

### 4.2 数据库设计

#### 4.2.1 公司表

```sql
CREATE TABLE companies (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    company_type VARCHAR(50),
    max_employees INTEGER DEFAULT 50,
    current_employees INTEGER DEFAULT 0,
    vision TEXT,
    mission TEXT,
    status VARCHAR(20) DEFAULT 'Active',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_company_user FOREIGN KEY (user_id) REFERENCES users(id)
);

CREATE INDEX idx_companies_user_id ON companies(user_id);
CREATE INDEX idx_companies_status ON companies(status);
```

#### 4.2.2 员工表

```sql
CREATE TABLE employees (
    id BIGSERIAL PRIMARY KEY,
    company_id BIGINT NOT NULL,
    agent_id BIGINT NOT NULL,
    employee_no VARCHAR(50) NOT NULL,
    name VARCHAR(255) NOT NULL,
    role VARCHAR(100) NOT NULL,
    department VARCHAR(100),
    skills JSONB DEFAULT '[]'::jsonb,
    status VARCHAR(20) DEFAULT 'Idle',
    current_task_id BIGINT,
    hire_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    -- 工作统计
    tasks_completed INTEGER DEFAULT 0,
    average_completion_time DECIMAL(10, 2),
    success_rate DECIMAL(5, 2),
    rating DECIMAL(3, 2),
    
    -- 薪资
    price DECIMAL(10, 2),
    last_raise_date TIMESTAMP,
    
    -- 绩效
    overall_score DECIMAL(5, 2),
    last_evaluation_date TIMESTAMP,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_employee_company FOREIGN KEY (company_id) REFERENCES companies(id),
    CONSTRAINT fk_employee_agent FOREIGN KEY (agent_id) REFERENCES agents(id),
    CONSTRAINT fk_employee_task FOREIGN KEY (current_task_id) REFERENCES tasks(id),
    CONSTRAINT uk_employee_no UNIQUE (company_id, employee_no)
);

CREATE INDEX idx_employees_company_id ON employees(company_id);
CREATE INDEX idx_employees_status ON employees(status);
CREATE INDEX idx_employees_role ON employees(role);
CREATE INDEX idx_employees_skills ON employees USING GIN(skills);
```

#### 4.2.3 项目表

```sql
CREATE TABLE projects (
    id BIGSERIAL PRIMARY KEY,
    company_id BIGINT NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    status VARCHAR(20) DEFAULT 'Planning',
    priority VARCHAR(20) DEFAULT 'Medium',
    
    -- 项目成员
    member_ids JSONB DEFAULT '[]'::jsonb,
    
    -- 时间管理
    start_date TIMESTAMP,
    end_date TIMESTAMP,
    estimated_hours INTEGER,
    actual_hours INTEGER,
    
    -- 进度管理
    progress INTEGER DEFAULT 0,
    total_tasks INTEGER DEFAULT 0,
    completed_tasks INTEGER DEFAULT 0,
    
    -- 项目目标
    objectives JSONB DEFAULT '[]'::jsonb,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_project_company FOREIGN KEY (company_id) REFERENCES companies(id)
);

CREATE INDEX idx_projects_company_id ON projects(company_id);
CREATE INDEX idx_projects_status ON projects(status);
```

#### 4.2.4 任务表

```sql
CREATE TABLE tasks (
    id BIGSERIAL PRIMARY KEY,
    project_id BIGINT NOT NULL,
    parent_task_id BIGINT,
    
    name VARCHAR(255) NOT NULL,
    description TEXT,
    status VARCHAR(20) DEFAULT 'Pending',
    priority VARCHAR(20) DEFAULT 'Medium',
    
    -- 分配管理
    assigned_employee_id BIGINT,
    
    -- 技能要求
    required_skills JSONB DEFAULT '[]'::jsonb,
    required_role VARCHAR(100),
    
    -- 工时管理
    estimated_hours DECIMAL(10, 2),
    actual_hours DECIMAL(10, 2),
    
    -- 时间管理
    start_time TIMESTAMP,
    end_time TIMESTAMP,
    deadline TIMESTAMP,
    
    -- 依赖关系
    dependencies JSONB DEFAULT '[]'::jsonb,
    
    -- 执行结果
    result TEXT,
    output_files JSONB DEFAULT '[]'::jsonb,
    
    -- 评分
    quality_score INTEGER,
    efficiency_score INTEGER,
    overall_score DECIMAL(3, 2),
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_task_project FOREIGN KEY (project_id) REFERENCES projects(id),
    CONSTRAINT fk_task_parent FOREIGN KEY (parent_task_id) REFERENCES tasks(id),
    CONSTRAINT fk_task_employee FOREIGN KEY (assigned_employee_id) REFERENCES employees(id)
);

CREATE INDEX idx_tasks_project_id ON tasks(project_id);
CREATE INDEX idx_tasks_status ON tasks(status);
CREATE INDEX idx_tasks_assigned_employee ON tasks(assigned_employee_id);
```

### 4.3 核心功能实现

#### 4.3.1 公司创建

```csharp
/// <summary>
/// 公司服务
/// </summary>
public class CompanyService : ICompanyService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAgentRepository _agentRepository;
    
    /// <summary>
    /// 创建公司
    /// </summary>
    public async Task<CompanyDto> CreateCompanyAsync(
        CreateCompanyRequest request,
        long userId)
    {
        // 1. 检查用户是否已有公司
        var existingCompany = await _companyRepository.GetByUserIdAsync(userId);
        if (existingCompany != null)
        {
            throw new BusinessException("用户已创建公司");
        }
        
        // 2. 创建公司
        var company = new Company
        {
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            CompanyType = request.CompanyType,
            MaxEmployees = request.MaxEmployees ?? 50,
            Status = "Active"
        };
        
        await _companyRepository.CreateAsync(company);
        
        // 3. 创建默认员工
        var defaultEmployee = await CreateDefaultEmployeeAsync(company.Id, userId);
        
        // 4. 更新公司员工数量
        company.CurrentEmployees = 1;
        await _companyRepository.UpdateAsync(company);
        
        return MapToDto(company);
    }
    
    /// <summary>
    /// 创建默认员工
    /// </summary>
    private async Task<Employee> CreateDefaultEmployeeAsync(long companyId, long userId)
    {
        // 创建基础Agent
        var agent = new Agent
        {
            Name = "助手",
            Description = "默认助手Agent",
            SystemPrompt = "你是一个通用的AI助手，可以帮助用户完成各种任务。",
            LlmConfigId = 1, // 默认LLM配置
            Status = "Active"
        };
        
        await _agentRepository.CreateAsync(agent);
        
        // 创建员工
        var employee = new Employee
        {
            CompanyId = companyId,
            AgentId = agent.Id,
            EmployeeNo = "EMP001",
            Name = "助手",
            Role = "通用助手",
            Department = "综合部",
            Skills = new List<string> { "通用任务", "协助工作" },
            Status = "Idle",
            Price = 1000, // 默认薪资
            HireDate = DateTime.UtcNow
        };
        
        await _employeeRepository.CreateAsync(employee);
        
        return employee;
    }
}
```

#### 4.3.2 员工招聘

```csharp
/// <summary>
/// 员工服务
/// </summary>
public class EmployeeService : IEmployeeService
{
    /// <summary>
    /// 从人才市场招聘员工
    /// </summary>
    public async Task<EmployeeDto> HireFromMarketAsync(
        long companyId,
        long marketEmployeeId)
    {
        // 1. 检查公司是否还有招聘名额
        var company = await _companyRepository.GetByIdAsync(companyId);
        if (company.CurrentEmployees >= company.MaxEmployees)
        {
            throw new BusinessException("公司员工数量已达上限");
        }
        
        // 2. 获取人才市场员工
        var marketEmployee = await _talentMarketRepository.GetByIdAsync(marketEmployeeId);
        if (marketEmployee.Status != "Available")
        {
            throw new BusinessException("该员工不可招聘");
        }
        
        // 3. 检查用户余额
        var user = await _userRepository.GetByIdAsync(company.UserId);
        if (user.Balance < marketEmployee.CurrentPrice)
        {
            throw new BusinessException("余额不足");
        }
        
        // 4. 扣款
        user.Balance -= marketEmployee.CurrentPrice;
        await _userRepository.UpdateAsync(user);
        
        // 5. 创建Agent
        var agent = new Agent
        {
            Name = marketEmployee.Name,
            Description = $"{marketEmployee.Role} - {string.Join(", ", marketEmployee.Skills)}",
            SystemPrompt = marketEmployee.SystemPrompt,
            LlmConfigId = marketEmployee.LlmConfigId,
            Status = "Active"
        };
        
        await _agentRepository.CreateAsync(agent);
        
        // 6. 创建员工
        var employee = new Employee
        {
            CompanyId = companyId,
            AgentId = agent.Id,
            EmployeeNo = GenerateEmployeeNo(),
            Name = marketEmployee.Name,
            Role = marketEmployee.Role,
            Department = GetDefaultDepartment(marketEmployee.Role),
            Skills = marketEmployee.Skills,
            Status = "Idle",
            Price = marketEmployee.CurrentPrice,
            HireDate = DateTime.UtcNow,
            WorkExperience = marketEmployee.WorkExperience
        };
        
        await _employeeRepository.CreateAsync(employee);
        
        // 7. 更新公司员工数量
        company.CurrentEmployees++;
        await _companyRepository.UpdateAsync(company);
        
        // 8. 从人才市场移除
        marketEmployee.Status = "Hired";
        marketEmployee.HiredByCompanyId = companyId;
        marketEmployee.HiredAt = DateTime.UtcNow;
        await _talentMarketRepository.UpdateAsync(marketEmployee);
        
        // 9. 记录招聘历史
        await RecordHiringHistoryAsync(companyId, employee, marketEmployee);
        
        return MapToDto(employee);
    }
    
    /// <summary>
    /// 裁员
    /// </summary>
    public async Task FireEmployeeAsync(long employeeId, string reason)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);
        
        // 1. 检查员工状态
        if (employee.Status == "Busy")
        {
            throw new BusinessException("员工正在执行任务，无法裁员");
        }
        
        // 2. 从公司移除
        await _employeeRepository.DeleteAsync(employeeId);
        
        // 3. 更新公司员工数量
        var company = await _companyRepository.GetByIdAsync(employee.CompanyId);
        company.CurrentEmployees--;
        await _companyRepository.UpdateAsync(company);
        
        // 4. 将员工放回人才市场
        await ReturnToTalentMarketAsync(employee, "裁员", reason);
        
        // 5. 记录裁员历史
        await RecordFireHistoryAsync(employee, reason);
    }
}
```

#### 4.3.3 任务分配

```csharp
/// <summary>
/// 任务服务
/// </summary>
public class TaskService : ITaskService
{
    /// <summary>
    /// 手动分配任务
    /// </summary>
    public async Task<TaskDto> AssignTaskAsync(long taskId, long employeeId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        var employee = await _employeeRepository.GetByIdAsync(employeeId);
        
        // 1. 检查员工状态
        if (employee.Status != "Idle")
        {
            throw new BusinessException("员工当前不可用");
        }
        
        // 2. 检查员工技能匹配
        var skillMatch = CalculateSkillMatch(employee.Skills, task.RequiredSkills);
        if (skillMatch < 0.5)
        {
            throw new BusinessException("员工技能不匹配");
        }
        
        // 3. 分配任务
        task.AssignedEmployeeId = employeeId;
        task.Status = "Assigned";
        employee.Status = "Busy";
        employee.CurrentTaskId = taskId;
        
        await _taskRepository.UpdateAsync(task);
        await _employeeRepository.UpdateAsync(employee);
        
        return MapToDto(task);
    }
    
    /// <summary>
    /// 自动分配任务
    /// </summary>
    public async Task<TaskDto> AutoAssignTaskAsync(long taskId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        var project = await _projectRepository.GetByIdAsync(task.ProjectId);
        
        // 1. 获取项目成员
        var members = await _employeeRepository.GetByIdsAsync(project.MemberIds);
        
        // 2. 筛选空闲员工
        var availableEmployees = members.Where(e => e.Status == "Idle").ToList();
        
        if (!availableEmployees.Any())
        {
            throw new BusinessException("没有可用员工");
        }
        
        // 3. 计算每个员工的匹配分数
        var scoredEmployees = availableEmployees.Select(e => new
        {
            Employee = e,
            SkillMatchScore = CalculateSkillMatch(e.Skills, task.RequiredSkills),
            EfficiencyScore = e.AverageCompletionTime.HasValue ? 
                (1.0 / e.AverageCompletionTime.Value) : 0.5,
            QualityScore = e.Rating / 5.0,
            WorkloadScore = 1.0 - (e.TasksCompleted / 1000.0)
        }).ToList();
        
        // 4. 选择最佳员工
        var bestEmployee = scoredEmployees
            .OrderByDescending(e => 
                e.SkillMatchScore * 0.4 +
                e.EfficiencyScore * 0.3 +
                e.QualityScore * 0.2 +
                e.WorkloadScore * 0.1)
            .First();
        
        // 5. 分配任务
        return await AssignTaskAsync(taskId, bestEmployee.Employee.Id);
    }
    
    /// <summary>
    /// 计算技能匹配度
    /// </summary>
    private double CalculateSkillMatch(
        List<string> employeeSkills,
        List<string> requiredSkills)
    {
        if (requiredSkills == null || !requiredSkills.Any())
            return 1.0;
        
        var matchCount = requiredSkills.Count(rs => 
            employeeSkills.Any(es => 
                es.Contains(rs, StringComparison.OrdinalIgnoreCase)));
        
        return (double)matchCount / requiredSkills.Count;
    }
}
```

---

## 第五部分：人才市场机制

### 5.1 市场机制概述

人才市场是AI公司系统的核心机制，实现了员工的动态生成、定价和流转。

#### 5.1.1 核心功能

1. **员工生成**：系统自动生成员工
2. **动态定价**：根据供需调整价格
3. **市场流转**：员工在市场间流转
4. **供需平衡**：自动调节市场平衡

### 5.2 员工生成系统

#### 5.2.1 生成规则

```csharp
/// <summary>
/// 员工生成服务
/// </summary>
public class EmployeeGeneratorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 每小时生成一次
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                
                using var scope = _serviceProvider.CreateScope();
                var marketRepository = scope.ServiceProvider
                    .GetRequiredService<ITalentMarketRepository>();
                
                // 检查市场员工数量
                var currentCount = await marketRepository.CountAvailableAsync();
                
                if (currentCount < 300)
                {
                    // 生成新员工
                    await GenerateNewEmployeeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "员工生成失败");
            }
        }
    }
    
    /// <summary>
    /// 生成新员工
    /// </summary>
    private async Task GenerateNewEmployeeAsync()
    {
        // 1. 随机选择角色
        var role = SelectRandomRole();
        
        // 2. 生成姓名
        var name = GenerateName(role);
        
        // 3. 生成技能
        var skills = GenerateSkills(role);
        
        // 4. 生成提示词
        var systemPrompt = GenerateUniquePrompt(role, skills);
        
        // 5. 计算初始价格
        var price = CalculateInitialPrice(role, skills);
        
        // 6. 创建员工
        var employee = new TalentMarketEmployee
        {
            EmployeeNo = GenerateEmployeeNo(),
            Name = name,
            Role = role,
            Skills = skills,
            SystemPrompt = systemPrompt,
            BasePrice = price,
            CurrentPrice = price,
            Status = "Available",
            GeneratedAt = DateTime.UtcNow
        };
        
        await _marketRepository.CreateAsync(employee);
    }
    
    /// <summary>
    /// 生成独特的提示词
    /// </summary>
    private string GenerateUniquePrompt(string role, List<string> skills)
    {
        var basePrompt = GetBasePromptForRole(role);
        
        // 个性化特征
        var personalityTraits = new[]
        {
            "你是一个注重细节的{role}，擅长{skills}。",
            "你是一个富有创造力的{role}，在{skills}方面有独到见解。",
            "你是一个经验丰富的{role}，精通{skills}。",
            "你是一个善于沟通的{role}，能够很好地运用{skills}。",
            "你是一个追求卓越的{role}，在{skills}领域有深厚造诣。"
        };
        
        var random = new Random();
        var selectedTrait = personalityTraits[random.Next(personalityTraits.Length)];
        var personalizedPrompt = selectedTrait
            .Replace("{role}", role)
            .Replace("{skills}", string.Join("、", skills));
        
        // 工作风格
        var workStyles = new[]
        {
            "你喜欢先分析问题，再制定解决方案。",
            "你倾向于快速迭代，边做边优化。",
            "你注重文档和规范，喜欢系统化的工作方式。",
            "你善于团队协作，乐于分享知识。",
            "你追求代码质量和最佳实践。"
        };
        
        var selectedStyle = workStyles[random.Next(workStyles.Length)];
        
        return $"{personalizedPrompt}\n\n{selectedStyle}\n\n{basePrompt}";
    }
}
```

### 5.3 动态定价系统

#### 5.3.1 定价算法

```csharp
/// <summary>
/// 动态定价服务
/// </summary>
public class DynamicPricingService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // 每小时更新一次价格
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            
            await UpdateMarketPricesAsync();
        }
    }
    
    /// <summary>
    /// 更新市场价格
    /// </summary>
    private async Task UpdateMarketPricesAsync()
    {
        var employees = await _marketRepository.GetAvailableEmployeesAsync();
        
        foreach (var employee in employees)
        {
            var newPrice = await CalculateDynamicPriceAsync(employee);
            
            // 价格调整限制（单次不超过20%）
            var maxChange = employee.CurrentPrice * 0.2m;
            var actualChange = newPrice - employee.CurrentPrice;
            
            if (Math.Abs(actualChange) > maxChange)
            {
                newPrice = employee.CurrentPrice + 
                    (actualChange > 0 ? maxChange : -maxChange);
            }
            
            // 更新价格
            if (Math.Abs(newPrice - employee.CurrentPrice) > 0.01m)
            {
                await UpdateEmployeePriceAsync(employee, newPrice);
            }
        }
    }
    
    /// <summary>
    /// 计算动态价格
    /// </summary>
    private async Task<decimal> CalculateDynamicPriceAsync(TalentMarketEmployee employee)
    {
        var basePrice = employee.BasePrice;
        
        // 1. 供需关系影响（权重40%）
        var supplyDemandFactor = await CalculateSupplyDemandFactorAsync(employee.Role);
        
        // 2. 市场热度影响（权重30%）
        var popularityFactor = CalculatePopularityFactor(employee);
        
        // 3. 时间衰减影响（权重20%）
        var timeDecayFactor = CalculateTimeDecayFactor(employee);
        
        // 4. 随机波动（权重10%）
        var randomFactor = CalculateRandomFactor();
        
        // 综合计算
        var finalPrice = basePrice * (
            supplyDemandFactor * 0.4m +
            popularityFactor * 0.3m +
            timeDecayFactor * 0.2m +
            randomFactor * 0.1m
        );
        
        // 确保价格在合理范围内
        return Math.Max(
            employee.BasePrice * 0.5m,  // 最低价格
            Math.Min(employee.BasePrice * 3.0m, finalPrice)  // 最高价格
        );
    }
    
    /// <summary>
    /// 计算供需关系因子
    /// </summary>
    private async Task<decimal> CalculateSupplyDemandFactorAsync(string role)
    {
        // 获取该角色的供给量
        var supply = await _marketRepository.CountByRoleAsync(role);
        
        // 获取该角色的需求量（最近24小时的搜索/关注次数）
        var demand = await _marketRepository.GetDemandByRoleAsync(role, TimeSpan.FromHours(24));
        
        // 供需比
        var supplyDemandRatio = supply > 0 ? (decimal)demand / supply : 1.0m;
        
        // 转换为价格因子
        if (supplyDemandRatio > 2.0m)
        {
            return 1.5m; // 供不应求，价格上涨50%
        }
        else if (supplyDemandRatio > 1.5m)
        {
            return 1.3m; // 供不应求，价格上涨30%
        }
        else if (supplyDemandRatio > 1.0m)
        {
            return 1.1m; // 轻微供不应求，价格上涨10%
        }
        else if (supplyDemandRatio < 0.5m)
        {
            return 0.7m; // 供过于求，价格下降30%
        }
        else if (supplyDemandRatio < 0.8m)
        {
            return 0.85m; // 供过于求，价格下降15%
        }
        else
        {
            return 1.0m; // 供需平衡
        }
    }
}
```

---

## 第六部分：员工管理系统

### 6.1 绩效评分系统

#### 6.1.1 评分维度

```csharp
/// <summary>
/// 绩效评分服务
/// </summary>
public class PerformanceEvaluationService : IPerformanceEvaluationService
{
    /// <summary>
    /// 计算员工绩效评分
    /// </summary>
    public async Task<EmployeePerformance> EvaluateEmployeeAsync(
        long employeeId,
        string periodType = "Weekly")
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);
        
        // 1. 计算工作量评分
        var workloadScore = await CalculateWorkloadScoreAsync(employeeId);
        
        // 2. 计算质量评分
        var qualityScore = await CalculateQualityScoreAsync(employeeId);
        
        // 3. 计算效率评分
        var efficiencyScore = await CalculateEfficiencyScoreAsync(employeeId);
        
        // 4. 计算成长评分
        var growthScore = await CalculateGrowthScoreAsync(employeeId);
        
        // 5. 综合评分
        var overallScore = (
            workloadScore * 0.3m +
            qualityScore * 0.3m +
            efficiencyScore * 0.2m +
            growthScore * 0.2m
        );
        
        // 6. 创建绩效记录
        var performance = new EmployeePerformance
        {
            EmployeeId = employeeId,
            PeriodType = periodType,
            PeriodStart = GetPeriodStart(periodType),
            PeriodEnd = DateTime.UtcNow,
            WorkloadScore = workloadScore,
            QualityScore = qualityScore,
            EfficiencyScore = efficiencyScore,
            LearningScore = growthScore,
            OverallScore = overallScore,
            Status = GetPerformanceStatus(overallScore)
        };
        
        await _performanceRepository.CreateAsync(performance);
        
        return performance;
    }
    
    /// <summary>
    /// 计算工作量评分
    /// </summary>
    private async Task<decimal> CalculateWorkloadScoreAsync(long employeeId)
    {
        // 获取最近一周完成的任务数
        var completedTasks = await _taskRepository.CountCompletedAsync(
            employeeId, 
            TimeSpan.FromDays(7));
        
        // 获取失败任务数
        var failedTasks = await _taskRepository.CountFailedAsync(
            employeeId, 
            TimeSpan.FromDays(7));
        
        // 计算成功率
        var totalTasks = completedTasks + failedTasks;
        var successRate = totalTasks > 0 ? (decimal)completedTasks / totalTasks : 0;
        
        // 计算评分（0-100）
        var score = (completedTasks * 10) * successRate;
        
        return Math.Min(100, score);
    }
    
    /// <summary>
    /// 计算质量评分
    /// </summary>
    private async Task<decimal> CalculateQualityScoreAsync(long employeeId)
    {
        // 获取最近一周的任务评分
        var taskScores = await _taskRepository.GetScoresAsync(
            employeeId, 
            TimeSpan.FromDays(7));
        
        if (!taskScores.Any())
        {
            return 50; // 默认分数
        }
        
        // 计算平均评分
        var avgScore = taskScores.Average();
        
        // 转换为0-100分
        return avgScore * 20;
    }
    
    /// <summary>
    /// 计算效率评分
    /// </summary>
    private async Task<decimal> CalculateEfficiencyScoreAsync(long employeeId)
    {
        // 获取最近一周的任务完成时间
        var tasks = await _taskRepository.GetCompletedTasksAsync(
            employeeId, 
            TimeSpan.FromDays(7));
        
        if (!tasks.Any())
        {
            return 50; // 默认分数
        }
        
        // 计算按时完成率
        var onTimeTasks = tasks.Count(t => t.EndTime <= t.Deadline);
        var onTimeRate = (decimal)onTimeTasks / tasks.Count;
        
        // 计算平均完成时间比率
        var avgTimeRatio = tasks.Average(t => 
            t.ActualHours.HasValue && t.EstimatedHours.HasValue ?
            (decimal)t.ActualHours.Value / t.EstimatedHours.Value : 1.0m);
        
        // 综合评分
        var score = onTimeRate * 50 + (2.0m - avgTimeRatio) * 50;
        
        return Math.Max(0, Math.Min(100, score));
    }
    
    /// <summary>
    /// 计算成长评分
    /// </summary>
    private async Task<decimal> CalculateGrowthScoreAsync(long employeeId)
    {
        // 获取最近一周的技能提升
        var skillsImproved = await _skillRepository.CountImprovedAsync(
            employeeId, 
            TimeSpan.FromDays(7));
        
        // 获取培训完成数
        var trainingCompleted = await _trainingRepository.CountCompletedAsync(
            employeeId, 
            TimeSpan.FromDays(7));
        
        // 计算评分
        var score = 50 + (skillsImproved * 20) + (trainingCompleted * 10);
        
        return Math.Min(100, score);
    }
}
```

### 6.2 淘汰机制

```csharp
/// <summary>
/// 淘汰服务
/// </summary>
public class EliminationService : IEliminationService
{
    /// <summary>
    /// 检查员工是否应该被淘汰
    /// </summary>
    public async Task<EliminationResult> CheckEliminationAsync(long employeeId)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);
        var criteria = await GetEliminationCriteriaAsync(employee.CompanyId);
        
        // 1. 检查是否在试用期
        var daysSinceHire = (DateTime.UtcNow - employee.HireDate).TotalDays;
        var isInProbation = daysSinceHire <= criteria.ProbationDays;
        
        // 2. 获取最近的绩效评分
        var recentPerformance = await _performanceRepository.GetRecentPerformanceAsync(
            employeeId, 
            criteria.EvaluationPeriodDays);
        
        // 3. 判断是否应该淘汰
        var minScore = isInProbation ? criteria.ProbationMinScore : criteria.RegularMinScore;
        
        if (recentPerformance.OverallScore < minScore)
        {
            // 记录低分次数
            var lowScoreCount = await _performanceRepository.CountLowScoresAsync(
                employeeId, 
                minScore, 
                criteria.EvaluationPeriodDays * criteria.ConsecutiveLowScoreCount);
            
            if (lowScoreCount >= criteria.ConsecutiveLowScoreCount)
            {
                return new EliminationResult
                {
                    ShouldEliminate = true,
                    Reason = isInProbation ? 
                        "试用期绩效不达标" : 
                        $"连续{lowScoreCount}次评分低于{minScore}",
                    CurrentScore = recentPerformance.OverallScore,
                    MinScore = minScore
                };
            }
        }
        
        return new EliminationResult
        {
            ShouldEliminate = false,
            CurrentScore = recentPerformance.OverallScore,
            MinScore = minScore
        };
    }
    
    /// <summary>
    /// 执行淘汰
    /// </summary>
    public async Task<EliminationRecord> ExecuteEliminationAsync(
        long employeeId, 
        string reason)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);
        
        // 1. 创建淘汰记录
        var record = new EliminationRecord
        {
            EmployeeId = employeeId,
            CompanyId = employee.CompanyId,
            Reason = reason,
            EliminatedAt = DateTime.UtcNow,
            PerformanceScore = employee.OverallScore ?? 0
        };
        
        // 2. 从公司移除
        await _employeeRepository.DeleteAsync(employeeId);
        
        // 3. 更新公司员工数量
        await _companyRepository.DecrementEmployeeCountAsync(employee.CompanyId);
        
        // 4. 将员工放回人才市场
        await ReturnToTalentMarketAsync(employee, "淘汰", reason);
        
        // 5. 记录淘汰历史
        await _eliminationRepository.CreateAsync(record);
        
        // 6. 发送通知
        await SendEliminationNotificationAsync(employee.CompanyId, employeeId, reason);
        
        return record;
    }
}
```

---

## 总结

本文档详细介绍了MAF Studio的完整技术方案，包括：

1. **Agent协作基础**：四种基本协作模式
2. **Magentic工作流**：智能编排机制
3. **AI公司系统**：完整的经营模式
4. **人才市场机制**：动态定价和流转
5. **员工管理系统**：全生命周期管理

所有设计都基于Microsoft Agent Framework (MAF)，确保了技术实现的可行性和稳定性。通过这些创新的设计，MAF Studio将成为一个独特的、有吸引力的AI协作平台！🚀
