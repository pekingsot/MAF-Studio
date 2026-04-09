# MAF高级群聊系统架构设计文档 v3.0（动态配置版）

## 📋 核心问题与解决方案

### ❌ 旧设计的问题

1. **语义规则写死**：`semanticRules`写死在代码里，一万种情况要写一万种
2. **协调者显示控制不足**：只支持显示/隐藏，没有细粒度控制
3. **配置不够灵活**：所有配置都是静态的，无法动态调整

### ✅ 新设计的核心理念

> **一切皆配置，配置皆动态**

1. **语义规则动态化**：存储在数据库，支持AI自动生成
2. **协调者显示分级**：支持多个显示级别，实时切换
3. **配置模板化**：支持模板继承、覆盖、组合

---

## 🏗️ 动态配置架构

### 1. 数据库设计

#### 1.1 语义规则表（semantic_rules）

```sql
CREATE TABLE semantic_rules (
    id BIGSERIAL PRIMARY KEY,
    collaboration_id BIGINT,  -- NULL表示全局规则
    
    -- 规则内容
    rule_name VARCHAR(200) NOT NULL,
    keywords TEXT NOT NULL,  -- JSON数组：["安全", "漏洞", "风险"]
    target_agent_type VARCHAR(100) NOT NULL,  -- 目标Agent类型
    priority INT DEFAULT 5,
    
    -- 规则来源
    source VARCHAR(50) DEFAULT 'manual',  -- manual, ai_generated, template
    is_active BOOLEAN DEFAULT true,
    
    -- 元数据
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100),
    
    -- 统计信息
    match_count INT DEFAULT 0,  -- 匹配次数
    success_rate DECIMAL(5,2),  -- 成功率
    
    CONSTRAINT fk_collaboration FOREIGN KEY (collaboration_id) 
        REFERENCES collaborations(id) ON DELETE CASCADE
);

-- 索引
CREATE INDEX idx_semantic_rules_collaboration ON semantic_rules(collaboration_id);
CREATE INDEX idx_semantic_rules_active ON semantic_rules(is_active);
CREATE INDEX idx_semantic_rules_priority ON semantic_rules(priority DESC);
```

**示例数据**：

```sql
-- 全局规则（所有协作共享）
INSERT INTO semantic_rules (rule_name, keywords, target_agent_type, priority, source) VALUES
('安全相关', '["安全", "漏洞", "风险", "攻击", "防护"]', 'SecurityExpert', 10, 'template'),
('代码实现', '["代码", "实现", "编程", "开发", "函数"]', 'Coder', 8, 'template'),
('架构设计', '["架构", "设计", "流程", "系统", "模块"]', 'Architect', 9, 'template'),
('测试相关', '["测试", "单元测试", "集成测试", "QA"]', 'Tester', 7, 'template');

-- 协作专属规则（特定协作使用）
INSERT INTO semantic_rules (collaboration_id, rule_name, keywords, target_agent_type, priority, source) VALUES
(1, '数据库优化', '["SQL", "查询优化", "索引", "性能"]', 'DBA', 8, 'ai_generated'),
(1, '前端UI', '["界面", "UI", "样式", "CSS", "组件"]', 'FrontendDev', 7, 'manual');
```

#### 1.2 协调者配置表（orchestrator_configs）

```sql
CREATE TABLE orchestrator_configs (
    id BIGSERIAL PRIMARY KEY,
    collaboration_id BIGINT,  -- NULL表示全局配置模板
    
    -- 协调模式
    mode VARCHAR(50) DEFAULT 'Intelligent',  -- RoundRobin, Manager, Intelligent
    
    -- 显示控制（核心改进）
    visibility_level VARCHAR(50) DEFAULT 'Hidden',  -- Hidden, Minimal, Normal, Detailed, Full
    show_task_ledger BOOLEAN DEFAULT false,
    show_progress_ledger BOOLEAN DEFAULT false,
    show_decision_process BOOLEAN DEFAULT false,
    show_agent_selection BOOLEAN DEFAULT false,
    
    -- 发言人选择策略
    speaker_selection_mode VARCHAR(50) DEFAULT 'Auto',  -- Auto, Semantic, Hybrid
    enable_dynamic_rules BOOLEAN DEFAULT true,  -- 启用动态规则加载
    rule_refresh_interval INT DEFAULT 300,  -- 规则刷新间隔（秒）
    
    -- 迭代控制
    max_iterations INT DEFAULT 10,
    max_attempts INT DEFAULT 5,
    allow_repeat_speaker BOOLEAN DEFAULT false,
    
    -- 人在回路
    enable_hitl BOOLEAN DEFAULT false,
    hitl_trigger_points TEXT,  -- JSON数组：关键决策点
    hitl_notification_method VARCHAR(50) DEFAULT 'push',  -- push, email, webhook
    
    -- AI自动生成规则
    enable_auto_rule_generation BOOLEAN DEFAULT true,
    auto_rule_threshold INT DEFAULT 3,  -- 相同模式出现N次后自动生成规则
    
    -- 元数据
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_collaboration FOREIGN KEY (collaboration_id) 
        REFERENCES collaborations(id) ON DELETE CASCADE
);
```

**显示级别详解**：

| 级别 | 说明 | 显示内容 | 适用场景 |
|------|------|---------|---------|
| **Hidden** | 完全隐藏 | 协调者不可见，用户只看到Agent对话 | 生产环境 |
| **Minimal** | 最小显示 | 只显示关键决策点 | 调试模式 |
| **Normal** | 正常显示 | 显示Task Ledger和Progress Ledger | 开发环境 |
| **Detailed** | 详细显示 | 显示决策过程和Agent选择逻辑 | 深度调试 |
| **Full** | 完全显示 | 显示所有内部状态和思考过程 | 研究/教学 |

#### 1.3 规则生成历史表（rule_generation_history）

```sql
CREATE TABLE rule_generation_history (
    id BIGSERIAL PRIMARY KEY,
    collaboration_id BIGINT NOT NULL,
    
    -- 触发信息
    trigger_pattern TEXT NOT NULL,  -- 触发模式
    occurrence_count INT DEFAULT 1,  -- 出现次数
    
    -- 生成的规则
    generated_rule_id BIGINT,  -- 生成的规则ID
    
    -- AI生成信息
    generation_prompt TEXT,
    generation_model VARCHAR(100),
    generation_confidence DECIMAL(5,2),
    
    -- 状态
    status VARCHAR(50) DEFAULT 'pending',  -- pending, approved, rejected
    reviewed_by VARCHAR(100),
    reviewed_at TIMESTAMP,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_generated_rule FOREIGN KEY (generated_rule_id) 
        REFERENCES semantic_rules(id) ON DELETE SET NULL
);
```

---

### 2. 动态规则加载机制

#### 2.1 规则加载服务

```csharp
public interface ISemanticRuleService
{
    /// <summary>
    /// 加载协作的语义规则（全局 + 协作专属）
    /// </summary>
    Task<List<SemanticRule>> LoadRulesAsync(long collaborationId);
    
    /// <summary>
    /// 匹配最佳Agent
    /// </summary>
    Task<string?> MatchAgentAsync(string content, List<SemanticRule> rules);
    
    /// <summary>
    /// AI自动生成规则
    /// </summary>
    Task<SemanticRule?> GenerateRuleAsync(long collaborationId, string pattern);
    
    /// <summary>
    /// 更新规则统计信息
    /// </summary>
    Task UpdateRuleStatsAsync(long ruleId, bool isSuccess);
}

public class SemanticRuleService : ISemanticRuleService
{
    private readonly ISemanticRuleRepository _ruleRepository;
    private readonly IRuleGenerationHistoryRepository _historyRepository;
    private readonly IChatClient _aiClient;
    private readonly ILogger<SemanticRuleService> _logger;
    
    // 规则缓存（定期刷新）
    private readonly ConcurrentDictionary<long, List<SemanticRule>> _ruleCache;
    private readonly Timer _refreshTimer;

    public async Task<List<SemanticRule>> LoadRulesAsync(long collaborationId)
    {
        // 1. 尝试从缓存获取
        if (_ruleCache.TryGetValue(collaborationId, out var cachedRules))
        {
            return cachedRules;
        }
        
        // 2. 从数据库加载
        var globalRules = await _ruleRepository.GetGlobalRulesAsync();
        var collaborationRules = await _ruleRepository.GetCollaborationRulesAsync(collaborationId);
        
        // 3. 合并规则（协作规则优先级更高）
        var allRules = globalRules
            .Concat(collaborationRules)
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.Priority)
            .ToList();
        
        // 4. 缓存规则
        _ruleCache[ collaborationId] = allRules;
        
        return allRules;
    }
    
    public async Task<string?> MatchAgentAsync(string content, List<SemanticRule> rules)
    {
        foreach (var rule in rules)
        {
            var keywords = JsonSerializer.Deserialize<List<string>>(rule.Keywords);
            if (keywords == null) continue;
            
            // 使用正则表达式匹配关键词
            var pattern = string.Join("|", keywords.Select(k => Regex.Escape(k)));
            if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
            {
                _logger.LogInformation("规则匹配: {RuleName} -> {AgentType}", 
                    rule.RuleName, rule.TargetAgentType);
                
                // 异步更新统计信息
                _ = UpdateRuleStatsAsync(rule.Id, true);
                
                return rule.TargetAgentType;
            }
        }
        
        return null;
    }
    
    public async Task<SemanticRule?> GenerateRuleAsync(long collaborationId, string pattern)
    {
        // 1. 检查是否已经出现过这个模式
        var history = await _historyRepository.GetByPatternAsync(collaborationId, pattern);
        
        if (history == null)
        {
            // 首次出现，记录
            await _historyRepository.CreateAsync(new RuleGenerationHistory
            {
                CollaborationId = collaborationId,
                TriggerPattern = pattern,
                OccurrenceCount = 1,
                Status = "pending"
            });
            
            return null;
        }
        
        // 2. 更新出现次数
        history.OccurrenceCount++;
        await _historyRepository.UpdateAsync(history);
        
        // 3. 达到阈值，AI生成规则
        var config = await GetOrchestratorConfigAsync(collaborationId);
        if (history.OccurrenceCount >= config.AutoRuleThreshold)
        {
            _logger.LogInformation("触发自动规则生成: {Pattern}", pattern);
            
            // 4. 使用AI生成规则
            var prompt = $@"分析以下对话模式，生成语义规则：

模式：{pattern}

请返回JSON格式：
{{
  ""ruleName"": ""规则名称"",
  ""keywords"": [""关键词1"", ""关键词2""],
  ""targetAgentType"": ""目标Agent类型"",
  ""priority"": 5
}}";

            var response = await _aiClient.GetResponseAsync(prompt);
            var ruleJson = response.Messages.Last().Text;
            
            var rule = JsonSerializer.Deserialize<SemanticRule>(ruleJson);
            if (rule != null)
            {
                rule.CollaborationId = collaborationId;
                rule.Source = "ai_generated";
                rule.IsActive = false;  // 默认不激活，需要人工审核
                
                // 保存到数据库
                var savedRule = await _ruleRepository.CreateAsync(rule);
                
                // 更新生成历史
                history.GeneratedRuleId = savedRule.Id;
                history.GenerationPrompt = prompt;
                history.GenerationModel = "gpt-4o";
                history.Status = "pending";
                await _historyRepository.UpdateAsync(history);
                
                return savedRule;
            }
        }
        
        return null;
    }
    
    public async Task UpdateRuleStatsAsync(long ruleId, bool isSuccess)
    {
        var rule = await _ruleRepository.GetByIdAsync(ruleId);
        if (rule != null)
        {
            rule.MatchCount++;
            // 计算成功率
            // ...
            await _ruleRepository.UpdateAsync(rule);
        }
    }
}
```

#### 2.2 动态发言人选择策略

```csharp
public class DynamicSemanticSpeakerSelector : ISpeakerSelectionStrategy
{
    private readonly ISemanticRuleService _ruleService;
    private readonly IOrchestratorConfigService _configService;
    private readonly long _collaborationId;
    
    public async Task<string?> SelectNextSpeakerAsync(
        IReadOnlyList<ChatMessageContent> history,
        IReadOnlyList<Agent> agents)
    {
        // 1. 动态加载规则
        var rules = await _ruleService.LoadRulesAsync(_collaborationId);
        
        // 2. 获取最后一条消息
        var lastMessage = history.LastOrDefault();
        if (lastMessage == null) return null;
        
        // 3. 匹配规则
        var matchedAgent = await _ruleService.MatchAgentAsync(
            lastMessage.Content, 
            rules);
        
        if (!string.IsNullOrEmpty(matchedAgent))
        {
            return matchedAgent;
        }
        
        // 4. 检查是否需要自动生成规则
        var config = await _configService.GetConfigAsync(_collaborationId);
        if (config.EnableAutoRuleGeneration)
        {
            // 提取模式（简化版，实际可以用NLP）
            var pattern = ExtractPattern(lastMessage.Content);
            
            // 异步生成规则（不阻塞当前流程）
            _ = _ruleService.GenerateRuleAsync(_collaborationId, pattern);
        }
        
        // 5. 默认：LLM自动选择
        return await AutoSelectByLLM(history, agents);
    }
    
    private string ExtractPattern(string content)
    {
        // 简化版：提取关键词
        // 实际可以使用NLP技术提取主题
        var words = content.Split(new[] { ' ', ',', '.', '。', '，' }, 
            StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words.Take(5));
    }
}
```

---

### 3. 协调者显示级别控制

#### 3.1 显示配置模型

```csharp
public class OrchestratorVisibility
{
    public VisibilityLevel Level { get; set; } = VisibilityLevel.Hidden;
    
    // 细粒度控制
    public bool ShowTaskLedger { get; set; }
    public bool ShowProgressLedger { get; set; }
    public bool ShowDecisionProcess { get; set; }
    public bool ShowAgentSelection { get; set; }
    public bool ShowInternalState { get; set; }
    
    // 根据级别自动设置
    public static OrchestratorVisibility FromLevel(VisibilityLevel level)
    {
        return level switch
        {
            VisibilityLevel.Hidden => new OrchestratorVisibility
            {
                Level = level,
                ShowTaskLedger = false,
                ShowProgressLedger = false,
                ShowDecisionProcess = false,
                ShowAgentSelection = false,
                ShowInternalState = false
            },
            VisibilityLevel.Minimal => new OrchestratorVisibility
            {
                Level = level,
                ShowTaskLedger = false,
                ShowProgressLedger = false,
                ShowDecisionProcess = true,  // 只显示关键决策
                ShowAgentSelection = false,
                ShowInternalState = false
            },
            VisibilityLevel.Normal => new OrchestratorVisibility
            {
                Level = level,
                ShowTaskLedger = true,
                ShowProgressLedger = true,
                ShowDecisionProcess = true,
                ShowAgentSelection = false,
                ShowInternalState = false
            },
            VisibilityLevel.Detailed => new OrchestratorVisibility
            {
                Level = level,
                ShowTaskLedger = true,
                ShowProgressLedger = true,
                ShowDecisionProcess = true,
                ShowAgentSelection = true,
                ShowInternalState = false
            },
            VisibilityLevel.Full => new OrchestratorVisibility
            {
                Level = level,
                ShowTaskLedger = true,
                ShowProgressLedger = true,
                ShowDecisionProcess = true,
                ShowAgentSelection = true,
                ShowInternalState = true
            },
            _ => throw new ArgumentException($"Unknown level: {level}")
        };
    }
}

public enum VisibilityLevel
{
    Hidden,    // 完全隐藏
    Minimal,   // 最小显示
    Normal,    // 正常显示
    Detailed,  // 详细显示
    Full       // 完全显示
}
```

#### 3.2 协调者消息过滤

```csharp
public class OrchestratorMessageFilter
{
    private readonly OrchestratorVisibility _visibility;
    
    public ChatMessageDto? FilterMessage(OrchestratorInternalMessage internalMessage)
    {
        // 根据显示级别过滤消息
        return internalMessage.Type switch
        {
            "TaskLedger" => _visibility.ShowTaskLedger 
                ? ConvertToUserMessage(internalMessage) 
                : null,
            
            "ProgressLedger" => _visibility.ShowProgressLedger 
                ? ConvertToUserMessage(internalMessage) 
                : null,
            
            "DecisionProcess" => _visibility.ShowDecisionProcess 
                ? ConvertToUserMessage(internalMessage) 
                : null,
            
            "AgentSelection" => _visibility.ShowAgentSelection 
                ? ConvertToUserMessage(internalMessage) 
                : null,
            
            "InternalState" => _visibility.ShowInternalState 
                ? ConvertToUserMessage(internalMessage) 
                : null,
            
            _ => null
        };
    }
    
    private ChatMessageDto ConvertToUserMessage(OrchestratorInternalMessage msg)
    {
        return new ChatMessageDto
        {
            Sender = "Orchestrator",
            Content = FormatContent(msg),
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["type"] = msg.Type,
                ["level"] = _visibility.Level.ToString()
            }
        };
    }
}
```

#### 3.3 实时切换显示级别

```csharp
public class OrchestratorConfigService
{
    private readonly ConcurrentDictionary<long, OrchestratorVisibility> _visibilityCache;
    
    /// <summary>
    /// 实时切换显示级别（无需重启）
    /// </summary>
    public async Task SetVisibilityLevelAsync(long collaborationId, VisibilityLevel level)
    {
        var visibility = OrchestratorVisibility.FromLevel(level);
        
        // 更新缓存
        _visibilityCache[collaborationId] = visibility;
        
        // 更新数据库
        var config = await _configRepository.GetByCollaborationIdAsync(collaborationId);
        if (config != null)
        {
            config.VisibilityLevel = level.ToString();
            config.ShowTaskLedger = visibility.ShowTaskLedger;
            config.ShowProgressLedger = visibility.ShowProgressLedger;
            config.ShowDecisionProcess = visibility.ShowDecisionProcess;
            config.ShowAgentSelection = visibility.ShowAgentSelection;
            
            await _configRepository.UpdateAsync(config);
        }
        
        // 发送事件通知
        await _eventBus.PublishAsync(new VisibilityChangedEvent
        {
            CollaborationId = collaborationId,
            NewLevel = level
        });
    }
}
```

---

### 4. 配置模板系统

#### 4.1 模板表设计

```sql
CREATE TABLE orchestrator_templates (
    id BIGSERIAL PRIMARY KEY,
    template_name VARCHAR(200) NOT NULL,
    template_category VARCHAR(100),  -- basic, advanced, custom
    
    -- 配置内容（JSON）
    config_json TEXT NOT NULL,
    
    -- 元数据
    description TEXT,
    is_builtin BOOLEAN DEFAULT false,
    usage_count INT DEFAULT 0,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100)
);
```

**内置模板**：

```sql
-- 模板1：简单协作
INSERT INTO orchestrator_templates (template_name, template_category, config_json, is_builtin) VALUES
('简单协作', 'basic', '{
  "mode": "RoundRobin",
  "visibilityLevel": "Hidden",
  "maxIterations": 5,
  "enableHitl": false
}', true);

-- 模板2：专业评审
INSERT INTO orchestrator_templates (template_name, template_category, config_json, is_builtin) VALUES
('专业评审', 'advanced', '{
  "mode": "Intelligent",
  "visibilityLevel": "Minimal",
  "speakerSelectionMode": "Semantic",
  "enableAutoRuleGeneration": true,
  "enableHitl": true,
  "hitlTriggerPoints": ["是否批准", "是否部署"]
}', true);

-- 模板3：教学演示
INSERT INTO orchestrator_templates (template_name, template_category, config_json, is_builtin) VALUES
('教学演示', 'advanced', '{
  "mode": "Intelligent",
  "visibilityLevel": "Full",
  "showTaskLedger": true,
  "showProgressLedger": true,
  "showDecisionProcess": true,
  "showAgentSelection": true
}', true);
```

#### 4.2 模板应用服务

```csharp
public interface IOrchestratorTemplateService
{
    /// <summary>
    /// 获取模板列表
    /// </summary>
    Task<List<OrchestratorTemplate>> GetTemplatesAsync(string? category = null);
    
    /// <summary>
    /// 应用模板到协作
    /// </summary>
    Task ApplyTemplateAsync(long collaborationId, long templateId);
    
    /// <summary>
    /// 从协作创建模板
    /// </summary>
    Task<OrchestratorTemplate> CreateTemplateFromCollaborationAsync(
        long collaborationId, 
        string templateName);
    
    /// <summary>
    /// 组合多个模板
    /// </summary>
    Task<OrchestratorConfig> CombineTemplatesAsync(params long[] templateIds);
}
```

---

### 5. 前端动态配置界面

#### 5.1 规则管理界面

```tsx
<RuleManager collaborationId={collaborationId}>
  {/* 规则列表 */}
  <RuleTable 
    dataSource={rules}
    columns={[
      { title: '规则名称', dataIndex: 'ruleName' },
      { title: '关键词', dataIndex: 'keywords', render: renderKeywords },
      { title: '目标Agent', dataIndex: 'targetAgentType' },
      { title: '优先级', dataIndex: 'priority' },
      { title: '来源', dataIndex: 'source', render: renderSource },
      { title: '匹配次数', dataIndex: 'matchCount' },
      { title: '成功率', dataIndex: 'successRate' },
      { title: '操作', render: renderActions }
    ]}
  />
  
  {/* 添加规则 */}
  <Button onClick={showAddRuleModal}>添加规则</Button>
  
  {/* AI生成规则 */}
  <Button onClick={generateRulesByAI}>AI自动生成规则</Button>
  
  {/* 规则审核（AI生成的规则需要人工审核） */}
  <RuleReviewPanel pendingRules={pendingRules} />
</RuleManager>

{/* 添加规则对话框 */}
<Modal title="添加语义规则" open={addRuleModalVisible}>
  <Form form={addRuleForm}>
    <Form.Item label="规则名称" name="ruleName" required>
      <Input placeholder="如：安全相关" />
    </Form.Item>
    
    <Form.Item label="关键词" name="keywords" required>
      <Select
        mode="tags"
        placeholder="输入关键词，回车添加"
        tokenSeparators={[',', ' ']}
      />
    </Form.Item>
    
    <Form.Item label="目标Agent类型" name="targetAgentType" required>
      <Select placeholder="选择Agent类型">
        {agentTypes.map(type => (
          <Option key={type} value={type}>{type}</Option>
        ))}
      </Select>
    </Form.Item>
    
    <Form.Item label="优先级" name="priority">
      <InputNumber min={1} max={10} defaultValue={5} />
    </Form.Item>
  </Form>
</Modal>
```

#### 5.2 协调者显示控制界面

```tsx
<OrchestratorVisibilityControl collaborationId={collaborationId}>
  {/* 显示级别选择 */}
  <Form.Item label="显示级别">
    <Radio.Group value={visibilityLevel} onChange={handleVisibilityChange}>
      <Space direction="vertical">
        <Radio value="Hidden">
          <Space>
            <EyeInvisibleOutlined />
            <span>完全隐藏</span>
            <Tag color="default">生产环境</Tag>
          </Space>
        </Radio>
        <Radio value="Minimal">
          <Space>
            <EyeOutlined />
            <span>最小显示</span>
            <Tag color="blue">调试模式</Tag>
          </Space>
        </Radio>
        <Radio value="Normal">
          <Space>
            <EyeOutlined />
            <span>正常显示</span>
            <Tag color="green">开发环境</Tag>
          </Space>
        </Radio>
        <Radio value="Detailed">
          <Space>
            <EyeOutlined />
            <span>详细显示</span>
            <Tag color="orange">深度调试</Tag>
          </Space>
        </Radio>
        <Radio value="Full">
          <Space>
            <EyeOutlined />
            <span>完全显示</span>
            <Tag color="red">研究/教学</Tag>
          </Space>
        </Radio>
      </Space>
    </Radio.Group>
  </Form.Item>
  
  {/* 细粒度控制（仅在Detailed和Full级别可用） */}
  {visibilityLevel === 'Detailed' || visibilityLevel === 'Full' ? (
    <Form.Item label="细粒度控制">
      <Checkbox.Group>
        <Checkbox value="showTaskLedger">显示任务账本</Checkbox>
        <Checkbox value="showProgressLedger">显示进度账本</Checkbox>
        <Checkbox value="showDecisionProcess">显示决策过程</Checkbox>
        <Checkbox value="showAgentSelection">显示Agent选择</Checkbox>
        {visibilityLevel === 'Full' && (
          <Checkbox value="showInternalState">显示内部状态</Checkbox>
        )}
      </Checkbox.Group>
    </Form.Item>
  ) : null}
  
  {/* 实时预览 */}
  <Alert
    message="实时预览"
    description={
      <div>
        <p>当前级别：<Tag color="blue">{visibilityLevel}</Tag></p>
        <p>显示内容：</p>
        <ul>
          {currentVisibility.showTaskLedger && <li>✅ 任务账本</li>}
          {currentVisibility.showProgressLedger && <li>✅ 进度账本</li>}
          {currentVisibility.showDecisionProcess && <li>✅ 决策过程</li>}
          {currentVisibility.showAgentSelection && <li>✅ Agent选择</li>}
          {currentVisibility.showInternalState && <li>✅ 内部状态</li>}
        </ul>
      </div>
    }
    type="info"
  />
</OrchestratorVisibilityControl>
```

#### 5.3 模板选择界面

```tsx
<TemplateSelector>
  {/* 模板分类 */}
  <Tabs defaultActiveKey="basic">
    <TabPane tab="基础模板" key="basic">
      <TemplateCard template={simpleTemplate} onSelect={applyTemplate} />
    </TabPane>
    <TabPane tab="高级模板" key="advanced">
      <TemplateCard template={reviewTemplate} onSelect={applyTemplate} />
      <TemplateCard template={teachingTemplate} onSelect={applyTemplate} />
    </TabPane>
    <TabPane tab="自定义模板" key="custom">
      <TemplateList templates={customTemplates} onSelect={applyTemplate} />
      <Button onClick={createTemplateFromCurrent}>从当前配置创建模板</Button>
    </TabPane>
  </Tabs>
</TemplateSelector>
```

---

## 🔄 完整工作流程

### 1. 初始化流程

```
用户创建协作
    ↓
选择模板（可选）
    ↓
加载全局规则 + 协作专属规则
    ↓
初始化协调者（根据配置）
    ↓
开始群聊
```

### 2. 运行时流程

```
Agent发言
    ↓
协调者分析内容
    ↓
动态加载规则（从缓存/数据库）
    ↓
匹配最佳Agent
    ↓
如果匹配失败 && 启用自动生成
    ↓
记录模式出现次数
    ↓
达到阈值 → AI生成规则
    ↓
人工审核规则
    ↓
激活规则
    ↓
下次自动使用新规则
```

### 3. 显示控制流程

```
协调者内部消息
    ↓
根据显示级别过滤
    ↓
转换为用户可见消息
    ↓
推送到前端
    ↓
用户实时切换显示级别
    ↓
立即生效（无需重启）
```

---

## ✅ 核心优势

### 1. **完全动态化**
- ✅ 规则存储在数据库，支持运行时加载
- ✅ AI自动生成规则，无需人工编写
- ✅ 配置实时生效，无需重启

### 2. **灵活可扩展**
- ✅ 支持全局规则 + 协作专属规则
- ✅ 支持模板继承和组合
- ✅ 支持细粒度显示控制

### 3. **智能自适应**
- ✅ 自动识别模式并生成规则
- ✅ 规则统计和成功率跟踪
- ✅ 低效规则自动降级

### 4. **用户友好**
- ✅ 可视化规则管理界面
- ✅ 实时预览显示效果
- ✅ 模板一键应用

---

## 📊 对比总结

| 维度 | 旧设计（v2.0） | 新设计（v3.0） |
|------|---------------|---------------|
| **规则存储** | 写死在代码里 | 数据库动态存储 |
| **规则生成** | 手动编写 | AI自动生成 |
| **显示控制** | 只支持显示/隐藏 | 5个级别 + 细粒度控制 |
| **配置方式** | 静态配置 | 动态配置 + 模板 |
| **扩展性** | 差（需要改代码） | 好（数据库配置） |
| **自适应** | 无 | 自动识别模式 |

---

## 🚀 下一步实施

**请确认此设计后，我将开始实施：**

1. ✅ 创建数据库表结构
2. ✅ 实现动态规则加载服务
3. ✅ 实现AI自动生成规则
4. ✅ 实现协调者显示控制
5. ✅ 实现配置模板系统
6. ✅ 实现前端管理界面

**这个设计完全解决了之前的问题：规则不再写死，显示控制更灵活，所有配置都支持动态调整！** 🎯
