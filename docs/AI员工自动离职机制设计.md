# AI员工自动离职机制设计

## 概述

AI员工自动离职机制是AI公司系统的重要组成部分，它让AI员工具有主动性和真实感，避免AI员工成为"永动机"。通过模拟真实员工的离职行为，增加系统的趣味性和挑战性。

---

## 核心理念

### 为什么AI员工要离职？

在真实世界中，员工离职的原因多种多样：
1. 💰 **薪资不满意**：市场薪资更高，觉得被低估
2. 😔 **工作环境不满意**：评分低、压力大、缺乏认可
3. 🚀 **职业发展需求**：技能瓶颈、缺乏成长机会
4. 🎯 **市场机会**：更好的机会、新的挑战
5. 🏃 **个人原因**：想要休息、转换跑道

对于AI员工，我们需要设计一套机制，让AI能够"理性"地做出离职决定。

---

## 离职触发条件

### 1. 薪资不满意（权重：30%）

#### 1.1 触发条件

```csharp
public class SalaryDissatisfactionChecker
{
    /// <summary>
    /// 检查薪资满意度
    /// </summary>
    public async Task<DissatisfactionLevel> CheckSalarySatisfactionAsync(long employeeId)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);
        
        // 1. 获取市场平均薪资
        var marketAvgSalary = await _talentMarketService.GetAverageSalaryByRoleAsync(employee.Role);
        
        // 2. 获取当前薪资
        var currentSalary = employee.Price;
        
        // 3. 计算薪资差距
        var salaryGap = (marketAvgSalary - currentSalary) / marketAvgSalary;
        
        // 4. 判断不满意程度
        if (salaryGap > 0.3m)
        {
            // 市场薪资高于当前30%以上
            return DissatisfactionLevel.High;
        }
        else if (salaryGap > 0.2m)
        {
            // 市场薪资高于当前20-30%
            return DissatisfactionLevel.Medium;
        }
        else if (salaryGap > 0.1m)
        {
            // 市场薪资高于当前10-20%
            return DissatisfactionLevel.Low;
        }
        
        return DissatisfactionLevel.None;
    }
    
    /// <summary>
    /// 检查是否长时间没有加薪
    /// </summary>
    public async Task<bool> CheckSalaryStagnationAsync(long employeeId)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);
        
        // 获取最近一次加薪时间
        var lastRaise = await _salaryHistoryRepository.GetLastRaiseAsync(employeeId);
        
        if (lastRaise == null)
        {
            // 从未加薪
            var daysSinceHire = (DateTime.UtcNow - employee.HireDate).TotalDays;
            return daysSinceHire > 90; // 超过3个月未加薪
        }
        
        var daysSinceLastRaise = (DateTime.UtcNow - lastRaise.RaiseDate).TotalDays;
        return daysSinceLastRaise > 180; // 超过6个月未加薪
    }
}
```

#### 1.2 薪资不满意触发规则

| 条件 | 不满意程度 | 触发概率 |
|------|-----------|---------|
| 市场薪资 > 当前薪资 30% | 高 | 80% |
| 市场薪资 > 当前薪资 20% | 中 | 50% |
| 市场薪资 > 当前薪资 10% | 低 | 20% |
| 超过6个月未加薪 | 中 | 40% |
| 超过3个月未加薪（新员工） | 低 | 30% |

---

### 2. 工作环境不满意（权重：30%）

#### 2.1 触发条件

```csharp
public class WorkEnvironmentChecker
{
    /// <summary>
    /// 检查工作环境满意度
    /// </summary>
    public async Task<DissatisfactionLevel> CheckWorkEnvironmentAsync(long employeeId)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);
        
        // 1. 检查绩效评分
        var performanceScore = await CheckPerformanceScoreAsync(employeeId);
        
        // 2. 检查工作负荷
        var workloadScore = await CheckWorkloadAsync(employeeId);
        
        // 3. 检查成长机会
        var growthScore = await CheckGrowthOpportunityAsync(employeeId);
        
        // 4. 检查团队协作
        var collaborationScore = await CheckCollaborationAsync(employeeId);
        
        // 5. 综合评分
        var overallScore = (
            performanceScore * 0.3 +
            workloadScore * 0.3 +
            growthScore * 0.2 +
            collaborationScore * 0.2
        );
        
        if (overallScore < 30)
        {
            return DissatisfactionLevel.High;
        }
        else if (overallScore < 50)
        {
            return DissatisfactionLevel.Medium;
        }
        else if (overallScore < 70)
        {
            return DissatisfactionLevel.Low;
        }
        
        return DissatisfactionLevel.None;
    }
    
    /// <summary>
    /// 检查绩效评分
    /// </summary>
    private async Task<decimal> CheckPerformanceScoreAsync(long employeeId)
    {
        // 获取最近30天的平均评分
        var avgScore = await _performanceRepository.GetAverageScoreAsync(
            employeeId, 
            TimeSpan.FromDays(30));
        
        // 评分低于3.0表示不满意
        if (avgScore < 3.0m)
        {
            return 20; // 非常不满意
        }
        else if (avgScore < 3.5m)
        {
            return 40; // 不满意
        }
        else if (avgScore < 4.0m)
        {
            return 60; // 一般
        }
        else
        {
            return 80; // 满意
        }
    }
    
    /// <summary>
    /// 检查工作负荷
    /// </summary>
    private async Task<decimal> CheckWorkloadAsync(long employeeId)
    {
        // 获取最近7天的工作时长
        var weeklyHours = await _workHistoryRepository.GetWeeklyHoursAsync(
            employeeId, 
            TimeSpan.FromDays(7));
        
        // 超过60小时/周表示过劳
        if (weeklyHours > 60)
        {
            return 20; // 严重过劳
        }
        else if (weeklyHours > 50)
        {
            return 40; // 过劳
        }
        else if (weeklyHours > 40)
        {
            return 60; // 稍忙
        }
        else
        {
            return 80; // 正常
        }
    }
    
    /// <summary>
    /// 检查成长机会
    /// </summary>
    private async Task<decimal> CheckGrowthOpportunityAsync(long employeeId)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);
        
        // 1. 检查技能提升
        var skillsImproved = await _skillRepository.CountImprovedSkillsAsync(
            employeeId, 
            TimeSpan.FromDays(30));
        
        // 2. 检查培训机会
        var trainingCount = await _trainingRepository.CountRecentTrainingAsync(
            employeeId, 
            TimeSpan.FromDays(30));
        
        // 3. 检查挑战性任务
        var challengingTasks = await _taskRepository.CountChallengingTasksAsync(
            employeeId, 
            TimeSpan.FromDays(30));
        
        var score = 50 + (skillsImproved * 10) + (trainingCount * 5) + (challengingTasks * 5);
        
        return Math.Min(100, score);
    }
    
    /// <summary>
    /// 检查团队协作
    /// </summary>
    private async Task<decimal> CheckCollaborationAsync(long employeeId)
    {
        // 获取最近30天的协作评分
        var collaborationScore = await _collaborationRepository.GetAverageScoreAsync(
            employeeId, 
            TimeSpan.FromDays(30));
        
        return collaborationScore * 20; // 转换为0-100分
    }
}
```

#### 2.2 工作环境不满意触发规则

| 条件 | 不满意程度 | 触发概率 |
|------|-----------|---------|
| 综合评分 < 30 | 高 | 70% |
| 综合评分 < 50 | 中 | 40% |
| 综合评分 < 70 | 低 | 20% |
| 连续低评分（< 3.5）超过2周 | 高 | 60% |
| 工作时长 > 60小时/周 | 高 | 50% |
| 30天内无技能提升 | 中 | 30% |

---

### 3. 职业发展需求（权重：20%）

#### 3.1 触发条件

```csharp
public class CareerDevelopmentChecker
{
    /// <summary>
    /// 检查职业发展需求
    /// </summary>
    public async Task<DissatisfactionLevel> CheckCareerDevelopmentAsync(long employeeId)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);
        
        // 1. 检查技能瓶颈
        var skillBottleneck = await CheckSkillBottleneckAsync(employeeId);
        
        // 2. 检查职业天花板
        var careerCeiling = await CheckCareerCeilingAsync(employeeId);
        
        // 3. 检查挑战性
        var challengeLevel = await CheckChallengeLevelAsync(employeeId);
        
        // 综合判断
        if (skillBottleneck && careerCeiling)
        {
            return DissatisfactionLevel.High;
        }
        else if (skillBottleneck || careerCeiling)
        {
            return DissatisfactionLevel.Medium;
        }
        else if (challengeLevel < 50)
        {
            return DissatisfactionLevel.Low;
        }
        
        return DissatisfactionLevel.None;
    }
    
    /// <summary>
    /// 检查技能瓶颈
    /// </summary>
    private async Task<bool> CheckSkillBottleneckAsync(long employeeId)
    {
        // 获取员工技能
        var skills = await _skillRepository.GetByEmployeeIdAsync(employeeId);
        
        // 检查是否有技能达到最高等级（10级）且超过30天没有提升
        foreach (var skill in skills)
        {
            if (skill.Level >= 10)
            {
                var daysSinceLastImprovement = (DateTime.UtcNow - skill.LastImprovedAt).TotalDays;
                if (daysSinceLastImprovement > 30)
                {
                    return true; // 技能瓶颈
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 检查职业天花板
    /// </summary>
    private async Task<bool> CheckCareerCeilingAsync(long employeeId)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);
        
        // 检查是否在当前公司已经达到最高级别
        var companyLevel = await GetCompanyLevelAsync(employee.CompanyId);
        var employeeLevel = await GetEmployeeLevelAsync(employeeId);
        
        // 如果员工等级接近公司上限
        if (employeeLevel >= companyLevel * 0.9)
        {
            return true; // 职业天花板
        }
        
        return false;
    }
    
    /// <summary>
    /// 检查挑战性
    /// </summary>
    private async Task<decimal> CheckChallengeLevelAsync(long employeeId)
    {
        // 获取最近30天完成的任务
        var recentTasks = await _taskRepository.GetCompletedTasksAsync(
            employeeId, 
            TimeSpan.FromDays(30));
        
        if (!recentTasks.Any())
        {
            return 0; // 没有任务，极度缺乏挑战
        }
        
        // 计算平均挑战性
        var avgChallenge = recentTasks.Average(t => t.ChallengeLevel);
        
        return avgChallenge;
    }
}
```

#### 3.2 职业发展需求触发规则

| 条件 | 不满意程度 | 触发概率 |
|------|-----------|---------|
| 技能达到瓶颈 + 职业天花板 | 高 | 60% |
| 技能达到瓶颈 或 职业天花板 | 中 | 35% |
| 任务挑战性 < 50 | 低 | 20% |
| 连续30天无技能提升 | 中 | 30% |

---

### 4. 市场机会（权重：15%）

#### 4.1 触发条件

```csharp
public class MarketOpportunityChecker
{
    /// <summary>
    /// 检查市场机会
    /// </summary>
    public async Task<MarketOpportunity> CheckMarketOpportunityAsync(long employeeId)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);
        
        // 1. 检查人才市场的机会
        var marketOpportunities = await _talentMarketService.GetOpportunitiesAsync(
            employee.Role,
            employee.Skills,
            employee.Price * 1.2m); // 期望薪资提高20%
        
        // 2. 检查是否有更好的机会
        var betterOpportunities = marketOpportunities
            .Where(o => o.ExpectedSalary > employee.Price * 1.2m)
            .ToList();
        
        if (betterOpportunities.Any())
        {
            return new MarketOpportunity
            {
                HasOpportunity = true,
                OpportunityLevel = betterOpportunities.Count > 3 ? 
                    OpportunityLevel.High : OpportunityLevel.Medium,
                BetterOpportunities = betterOpportunities
            };
        }
        
        return new MarketOpportunity
        {
            HasOpportunity = false,
            OpportunityLevel = OpportunityLevel.None
        };
    }
    
    /// <summary>
    /// 模拟其他公司发出邀请
    /// </summary>
    public async Task<bool> SimulateJobOfferAsync(long employeeId)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);
        
        // 高绩效员工更容易收到邀请
        var performanceScore = await _performanceRepository.GetAverageScoreAsync(
            employeeId, 
            TimeSpan.FromDays(30));
        
        // 基础概率
        var baseProbability = 0.05m; // 5%
        
        // 根据绩效调整
        if (performanceScore > 4.5m)
        {
            baseProbability = 0.15m; // 15%
        }
        else if (performanceScore > 4.0m)
        {
            baseProbability = 0.10m; // 10%
        }
        
        // 随机判断是否收到邀请
        var random = new Random();
        return random.NextDouble() < (double)baseProbability;
    }
}
```

#### 4.2 市场机会触发规则

| 条件 | 机会等级 | 触发概率 |
|------|---------|---------|
| 市场有3+个更好的机会 | 高 | 50% |
| 市场有1-2个更好的机会 | 中 | 30% |
| 收到其他公司邀请 | 高 | 70% |
| 高绩效员工（> 4.5分） | - | 15%收到邀请 |

---

### 5. 个人原因（权重：5%）

#### 5.1 触发条件

```csharp
public class PersonalReasonChecker
{
    /// <summary>
    /// 检查个人原因
    /// </summary>
    public async Task<PersonalReason> CheckPersonalReasonAsync(long employeeId)
    {
        var random = new Random();
        
        // 随机触发个人原因（概率很低）
        if (random.NextDouble() < 0.01) // 1%概率
        {
            var reasons = new[]
            {
                "想要休息一段时间",
                "想要转换职业跑道",
                "想要创业",
                "想要学习新技术",
                "想要尝试不同的工作方式",
                "家庭原因",
                "健康原因"
            };
            
            return new PersonalReason
            {
                HasPersonalReason = true,
                Reason = reasons[random.Next(reasons.Length)]
            };
        }
        
        return new PersonalReason
        {
            HasPersonalReason = false
        };
    }
}
```

---

## 离职决策机制

### 1. 综合评估

```csharp
public class ResignationDecisionService
{
    /// <summary>
    /// 离职决策Agent
    /// </summary>
    public async Task<ResignationDecision> MakeResignationDecisionAsync(long employeeId)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);
        
        // 1. 收集各种不满意因素
        var salaryDissatisfaction = await _salaryChecker.CheckSalarySatisfactionAsync(employeeId);
        var workEnvironmentDissatisfaction = await _environmentChecker.CheckWorkEnvironmentAsync(employeeId);
        var careerDevelopmentNeed = await _careerChecker.CheckCareerDevelopmentAsync(employeeId);
        var marketOpportunity = await _marketChecker.CheckMarketOpportunityAsync(employeeId);
        var personalReason = await _personalChecker.CheckPersonalReasonAsync(employeeId);
        
        // 2. 计算离职倾向分数
        var resignationScore = CalculateResignationScore(
            salaryDissatisfaction,
            workEnvironmentDissatisfaction,
            careerDevelopmentNeed,
            marketOpportunity,
            personalReason);
        
        // 3. 决定是否离职
        if (resignationScore > 70)
        {
            return new ResignationDecision
            {
                ShouldResign = true,
                ResignationReason = GenerateResignationReason(
                    salaryDissatisfaction,
                    workEnvironmentDissatisfaction,
                    careerDevelopmentNeed,
                    marketOpportunity,
                    personalReason),
                ResignationScore = resignationScore
            };
        }
        
        return new ResignationDecision
        {
            ShouldResign = false,
            ResignationScore = resignationScore
        };
    }
    
    /// <summary>
    /// 计算离职倾向分数
    /// </summary>
    private decimal CalculateResignationScore(
        DissatisfactionLevel salary,
        DissatisfactionLevel workEnv,
        DissatisfactionLevel career,
        MarketOpportunity market,
        PersonalReason personal)
    {
        var score = 0m;
        
        // 薪资不满意（权重30%）
        score += (int)salary * 10 * 0.3m;
        
        // 工作环境不满意（权重30%）
        score += (int)workEnv * 10 * 0.3m;
        
        // 职业发展需求（权重20%）
        score += (int)career * 10 * 0.2m;
        
        // 市场机会（权重15%）
        score += (int)market.OpportunityLevel * 10 * 0.15m;
        
        // 个人原因（权重5%）
        if (personal.HasPersonalReason)
        {
            score += 100 * 0.05m;
        }
        
        return score;
    }
    
    /// <summary>
    /// 生成离职原因
    /// </summary>
    private string GenerateResignationReason(
        DissatisfactionLevel salary,
        DissatisfactionLevel workEnv,
        DissatisfactionLevel career,
        MarketOpportunity market,
        PersonalReason personal)
    {
        var reasons = new List<string>();
        
        if (salary >= DissatisfactionLevel.Medium)
        {
            reasons.Add("薪资待遇不满意");
        }
        
        if (workEnv >= DissatisfactionLevel.Medium)
        {
            reasons.Add("工作环境不满意");
        }
        
        if (career >= DissatisfactionLevel.Medium)
        {
            reasons.Add("职业发展受限");
        }
        
        if (market.HasOpportunity)
        {
            reasons.Add("有更好的机会");
        }
        
        if (personal.HasPersonalReason)
        {
            reasons.Add(personal.Reason);
        }
        
        return string.Join("、", reasons);
    }
}
```

### 2. 决策频率

```csharp
public class ResignationScheduler : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // 每天凌晨2点检查所有员工
            var nextRun = DateTime.Today.AddDays(1).AddHours(2);
            var delay = nextRun - DateTime.UtcNow;
            
            await Task.Delay(delay, stoppingToken);
            
            await CheckAllEmployeesAsync(stoppingToken);
        }
    }
    
    /// <summary>
    /// 检查所有员工
    /// </summary>
    private async Task CheckAllEmployeesAsync(CancellationToken cancellationToken)
    {
        var allEmployees = await _employeeRepository.GetAllActiveAsync();
        
        foreach (var employee in allEmployees)
        {
            // 随机决定是否检查（避免所有员工同时离职）
            var random = new Random();
            if (random.NextDouble() < 0.3) // 30%概率被检查
            {
                var decision = await _resignationService.MakeResignationDecisionAsync(employee.Id);
                
                if (decision.ShouldResign)
                {
                    await InitiateResignationAsync(employee.Id, decision);
                }
            }
        }
    }
}
```

---

## 离职流程

### 1. 发起离职申请

```csharp
public class ResignationService
{
    /// <summary>
    /// 发起离职申请
    /// </summary>
    public async Task<ResignationApplication> InitiateResignationAsync(
        long employeeId, 
        ResignationDecision decision)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);
        
        // 1. 创建离职申请
        var application = new ResignationApplication
        {
            EmployeeId = employeeId,
            CompanyId = employee.CompanyId,
            Reason = decision.ResignationReason,
            Status = "Pending",
            AppliedAt = DateTime.UtcNow,
            ExpectedLastDay = DateTime.UtcNow.AddDays(14), // 2周通知期
            ResignationScore = decision.ResignationScore
        };
        
        // 2. 保存申请
        await _resignationRepository.CreateAsync(application);
        
        // 3. 通知公司老板
        await SendResignationNotificationAsync(employee.CompanyId, application);
        
        // 4. 更新员工状态
        employee.Status = "Resigning";
        await _employeeRepository.UpdateAsync(employee);
        
        return application;
    }
}
```

### 2. 公司处理离职申请

```csharp
/// <summary>
/// 处理离职申请
/// </summary>
public async Task HandleResignationApplicationAsync(
    long applicationId, 
    bool accept,
    string? counterOffer = null)
{
    var application = await _resignationRepository.GetByIdAsync(applicationId);
    var employee = await _employeeRepository.GetByIdAsync(application.EmployeeId);
    
    if (accept)
    {
        // 接受离职
        await AcceptResignationAsync(application, employee);
    }
    else
    {
        // 尝试挽留
        var retained = await TryToRetainAsync(employee, counterOffer);
        
        if (retained)
        {
            // 挽留成功
            application.Status = "Withdrawn";
            employee.Status = "Idle";
        }
        else
        {
            // 挽留失败，强制离职
            await AcceptResignationAsync(application, employee);
        }
    }
    
    await _resignationRepository.UpdateAsync(application);
    await _employeeRepository.UpdateAsync(employee);
}

/// <summary>
/// 尝试挽留员工
/// </summary>
private async Task<bool> TryToRetainAsync(Employee employee, string? counterOffer)
{
    // 如果提供了挽留条件（加薪、晋升等）
    if (!string.IsNullOrEmpty(counterOffer))
    {
        // 解析挽留条件
        var offer = ParseCounterOffer(counterOffer);
        
        // 员工决策Agent评估挽留条件
        var decision = await EvaluateCounterOfferAsync(employee, offer);
        
        return decision.Accept;
    }
    
    // 没有挽留条件，挽留失败
    return false;
}

/// <summary>
/// 员工评估挽留条件
/// </summary>
private async Task<CounterOfferDecision> EvaluateCounterOfferAsync(
    Employee employee, 
    CounterOffer offer)
{
    var prompt = $@"
你是一个{employee.Role}，名叫{employee.Name}。

你之前因为以下原因想要离职：
{employee.ResignationReason}

现在公司提出了挽留条件：
- 加薪：{offer.SalaryIncrease}%
- 晋升：{offer.Promotion}
- 其他：{offer.OtherBenefits}

你的当前状态：
- 绩效评分：{employee.OverallScore}/5
- 技能等级：{employee.SkillLevel}
- 工作时长：{employee.WeeklyHours}小时/周

请评估这个挽留条件是否值得留下。
考虑因素：
1. 挽留条件是否解决了你的离职原因
2. 公司的诚意和未来发展
3. 你的职业规划

请回答：接受（Accept）或拒绝（Reject），并说明理由。
";

    var response = await _llmClient.GetResponseAsync(prompt);
    
    return new CounterOfferDecision
    {
        Accept = response.Contains("Accept", StringComparison.OrdinalIgnoreCase),
        Reason = response
    };
}
```

### 3. 完成离职

```csharp
/// <summary>
/// 完成离职流程
/// </summary>
private async Task AcceptResignationAsync(
    ResignationApplication application, 
    Employee employee)
{
    // 1. 更新申请状态
    application.Status = "Accepted";
    application.AcceptedAt = DateTime.UtcNow;
    
    // 2. 从公司移除
    await _employeeRepository.DeleteAsync(employee.Id);
    
    // 3. 更新公司员工数量
    await _companyRepository.DecrementEmployeeCountAsync(employee.CompanyId);
    
    // 4. 将员工放回人才市场
    await ReturnToTalentMarketAsync(employee, "主动离职");
    
    // 5. 记录离职历史
    await RecordResignationHistoryAsync(application, employee);
    
    // 6. 发送通知
    await SendResignationCompleteNotificationAsync(employee.CompanyId, employee);
}

/// <summary>
/// 将员工放回人才市场
/// </summary>
private async Task ReturnToTalentMarketAsync(Employee employee, string reason)
{
    // 创建人才市场记录
    var marketEmployee = new TalentMarketEmployee
    {
        EmployeeNo = employee.EmployeeNo,
        Name = employee.Name,
        Role = employee.Role,
        Skills = employee.Skills,
        
        // 继承原有的Agent配置
        AgentConfigId = employee.AgentId,
        SystemPrompt = await GetEnhancedPromptAsync(employee, reason),
        
        // 价格调整（主动离职的员工价格可能略高）
        BasePrice = employee.Price * 1.1m,
        CurrentPrice = employee.Price * 1.1m,
        
        // 标记为主动离职
        IsResigned = true,
        ResignationReason = reason,
        PreviousCompanyId = employee.CompanyId,
        
        // 继承工作经历
        WorkExperience = await GetWorkExperienceAsync(employee.Id),
        
        Status = "Available"
    };
    
    await _talentMarketRepository.CreateAsync(marketEmployee);
}
```

---

## 数据库设计

### 1. 离职申请表

```sql
CREATE TABLE resignation_applications (
    id BIGSERIAL PRIMARY KEY,
    employee_id BIGINT NOT NULL,
    company_id BIGINT NOT NULL,
    
    -- 离职信息
    reason TEXT,
    resignation_score DECIMAL(5, 2),
    
    -- 申请状态
    status VARCHAR(20) DEFAULT 'Pending', -- Pending, Accepted, Withdrawn, Rejected
    
    -- 时间信息
    applied_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expected_last_day TIMESTAMP,
    accepted_at TIMESTAMP,
    
    -- 挽留信息
    counter_offer TEXT,
    counter_offer_result VARCHAR(20), -- Accepted, Rejected
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_resignation_employee FOREIGN KEY (employee_id) REFERENCES employees(id),
    CONSTRAINT fk_resignation_company FOREIGN KEY (company_id) REFERENCES companies(id)
);

CREATE INDEX idx_resignation_employee ON resignation_applications(employee_id);
CREATE INDEX idx_resignation_company ON resignation_applications(company_id);
CREATE INDEX idx_resignation_status ON resignation_applications(status);
CREATE INDEX idx_resignation_applied_at ON resignation_applications(applied_at);
```

### 2. 员工满意度记录表

```sql
CREATE TABLE employee_satisfaction_records (
    id BIGSERIAL PRIMARY KEY,
    employee_id BIGINT NOT NULL,
    
    -- 满意度评分
    salary_satisfaction INTEGER, -- 1-10
    work_environment_satisfaction INTEGER, -- 1-10
    career_development_satisfaction INTEGER, -- 1-10
    overall_satisfaction INTEGER, -- 1-10
    
    -- 离职倾向
    resignation_tendency DECIMAL(5, 2), -- 0-100
    
    -- 影响因素
    factors JSONB DEFAULT '[]'::jsonb,
    
    recorded_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_satisfaction_employee FOREIGN KEY (employee_id) REFERENCES employees(id)
);

CREATE INDEX idx_satisfaction_employee ON employee_satisfaction_records(employee_id);
CREATE INDEX idx_satisfaction_recorded_at ON employee_satisfaction_records(recorded_at);
```

---

## 用户界面设计

### 1. 离职申请通知

```
┌─────────────────────────────────────────────────────────┐
│  ⚠️ 员工离职申请                                          │
├─────────────────────────────────────────────────────────┤
│  员工：张三（架构师）                                      │
│  工号：EMP001                                            │
│  入职时间：2024-01-15                                    │
│  已工作：45天                                            │
├─────────────────────────────────────────────────────────┤
│  离职原因：                                               │
│  • 薪资待遇不满意（市场薪资高于当前30%）                    │
│  • 有更好的机会（收到其他公司邀请）                         │
├─────────────────────────────────────────────────────────┤
│  绩效表现：                                               │
│  • 平均评分：4.5/5                                       │
│  • 完成任务：23个                                         │
│  • 成功率：95%                                           │
├─────────────────────────────────────────────────────────┤
│  预计最后工作日：2024-03-01                               │
│                                                         │
│  [接受离职] [尝试挽留]                                    │
└─────────────────────────────────────────────────────────┘
```

### 2. 挽留界面

```
┌─────────────────────────────────────────────────────────┐
│  💼 挽留员工                                              │
├─────────────────────────────────────────────────────────┤
│  员工：张三                                               │
│  当前薪资：¥8,000                                        │
│  市场薪资：¥10,400                                       │
├─────────────────────────────────────────────────────────┤
│  挽留条件：                                               │
│  ┌─────────────────────────────────────────────────┐   │
│  │ 加薪幅度：[  30  ]%                              │   │
│  │ 晋升职位：[下拉选择]                              │   │
│  │ 其他福利：[文本输入]                              │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  预计新薪资：¥10,400                                     │
│                                                         │
│  [提交挽留] [取消]                                       │
└─────────────────────────────────────────────────────────┘
```

---

## 可行性分析

### 技术可行性：✅ **高**

1. **决策算法**：基于多因素的综合评分算法，实现简单
2. **LLM集成**：使用LLM生成离职原因和评估挽留条件
3. **定时任务**：使用BackgroundService定期检查
4. **数据存储**：PostgreSQL完全支持所需的数据结构

### 商业可行性：✅ **高**

1. **增加真实感**：AI员工具有主动行为，更像真实员工
2. **增加挑战性**：用户需要关注员工满意度，防止人才流失
3. **增加收入**：挽留员工需要加薪，消耗用户资源
4. **增加粘性**：用户需要持续关注和管理员工

### 用户体验：✅ **良好**

1. **透明度**：明确的离职原因和评分机制
2. **可控性**：用户可以尝试挽留员工
3. **公平性**：基于客观数据的决策
4. **趣味性**：模拟真实的人力资源管理

---

## 总结

AI员工自动离职机制是一个创新的设计，它让AI员工具有了主动性和真实感。通过多因素的离职决策机制，AI员工能够"理性"地做出离职决定，增加了系统的趣味性和挑战性。

**核心优势**：
1. ✅ 避免AI员工成为"永动机"
2. ✅ 增加系统的真实感和趣味性
3. ✅ 让用户关注员工满意度
4. ✅ 提供挽留机制，增加互动性

**实施建议**：
1. 先实现基础的离职决策机制
2. 逐步添加更多的影响因素
3. 优化挽留机制的用户体验
4. 根据用户反馈调整参数

这个机制将大大提升AI公司系统的真实感和趣味性！🚀
