# 无水印视频解析工具 - 技术架构设计文档

## 📋 文档信息

- **版本号**: v1.0.0
- **作者**: 小明-架构师
- **创建日期**: 2024-01-XX
- **最后更新**: 2024-01-XX
- **审核状态**: 待评审

---

## 一、总体架构

### 1.1 架构图

```
┌─────────────────────────────────────────────────────────────────────┐
│                           客户端层                                    │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐       │
│  │  微信小程序      │  │  H5网页        │  │  其他APP        │       │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘       │
└───────────┼─────────────────────┼─────────────────────┼──────────────┘
            │                     │                     │
            ▼                     ▼                     ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         API网关层                                    │
│  ┌────────────────────────────────────────────────────────────┐     │
│  │  • 负载均衡 (Nginx)                                         │     │
│  │  • 鉴权认证 (JWT/OAuth)                                     │     │
│  │  • 流量控制 (Rate Limiting)                                 │     │
│  │  • 请求日志                                                │     │
│  └────────────────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        业务服务层                                    │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐               │
│  │  解析服务    │  │  广告服务    │  │  用户服务    │               │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘               │
│         │                  │                 │                       │
│         ▼                  ▼                 ▼                       │
│  ┌──────────────────────────────────────────────────────────────┐    │
│  │                    消息队列 (RabbitMQ)                        │    │
│  └──────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        解析引擎层                                    │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐               │
│  │  抖音解析    │  │  快手解析    │  │  其他平台    │               │
│  │  插件模块    │  │  插件模块    │  │  解析模块    │               │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘               │
│         │                  │                 │                       │
│         ▼                  ▼                 ▼                       │
│  ┌──────────────────────────────────────────────────────────────┐    │
│  │              规则引擎 & 签名管理平台 (热更新)                  │    │
│  └──────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        数据存储层                                    │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐               │
│  │   MySQL      │  │    Redis     │  │   MongoDB    │               │
│  │  (业务数据)   │  │   (缓存)     │  │ (日志/分析)   │               │
│  └──────────────┘  └──────────────┘  └──────────────┘               │
│                                                                       │
│  ┌──────────────┐  ┌──────────────┐                                  │
│  │   对象存储    │  │  代理IP池    │                                  │
│  │  (临时文件)   │  │  (外部服务)   │                                  │
│  └──────────────┘  └──────────────┘                                  │
└─────────────────────────────────────────────────────────────────────┘
```

### 1.2 技术选型

| 层次 | 技术栈 | 选型理由 |
|-----|-------|---------|
| 前端 | 微信小程序 + UniApp | 官方支持广告组件，合规性好 |
| API网关 | Nginx + Kong | 高性能，丰富的限流插件 |
| 业务服务 | Node.js / Go | 高并发处理能力，适合IO密集型 |
| 消息队列 | RabbitMQ | 成熟稳定，支持延时队列 |
| 缓存 | Redis Cluster | 高性能分布式缓存 |
| 数据库 | MySQL 8.0 | 关系型数据强一致性 |
| 文档存储 | MongoDB | 灵活的解析规则存储 |
| 监控系统 | Prometheus + Grafana | 开源完善，支持自定义指标 |
| 部署 | Docker + K8s | 容器化编排，弹性伸缩 |

---

## 二、核心功能模块设计

### 2.1 视频解析模块

#### 2.1.1 解析流程

```
用户输入分享链接
        ↓
[1] 链接合法性校验
        ↓
[2] 识别平台来源 (抖音/快手/...)
        ↓
[3] 查询缓存 (Redis) → 命中则直接返回
        ↓
[4] 选择解析通道 (主通道/备用通道/第三方)
        ↓
[5] 执行解析 (获取真实视频URL)
        ↓
[6] 缓存结果 (设置TTL=24h)
        ↓
[7] 返回高清无水印链接
```

#### 2.1.2 解析通道设计

| 通道类型 | 实现方式 | 优点 | 缺点 | 使用场景 |
|---------|---------|-----|-----|---------|
| 主通道 | 逆向API签名算法 | 速度快，成本低 | 易被平台封禁 | 日常使用 |
| 备用通道 | WebView模拟用户 | 成功率高 | 速度慢，资源消耗大 | 主通道失败时 |
| 兜底通道 | 第三方解析服务 | 免维护 | 有费用，不可控 | 极端情况应急 |

#### 2.1.3 规则热更新机制

```javascript
// 规则加载与更新流程
class RuleManager {
  constructor() {
    this.rulesCache = new Map();
    this.versionMonitor = null;
  }

  async init() {
    // 从Redis加载规则
    await this.loadRulesFromRedis();
    
    // 订阅配置变更通知
    this.subscribeConfigUpdates();
  }

  async loadRulesFromRedis() {
    const rules = await redis.hgetall('parse_rules');
    for (const [platform, rule] of Object.entries(rules)) {
      this.rulesCache.set(platform, JSON.parse(rule));
    }
  }

  subscribeConfigUpdates() {
    const pubSub = createPubSub();
    pubSub.subscribe('rule:updated', async (data) => {
      console.log(`收到规则更新通知: ${data.platform}`);
      await this.loadRulesFromRedis();
      // 平滑切换，不影响正在处理的请求
    });
  }

  getRule(platform) {
    return this.rulesCache.get(platform);
  }
}
```

### 2.2 反爬应对策略

#### 2.2.1 多层防护体系

```
Layer 1: 请求层防护
├── IP池轮换 (住宅IP优先)
├── User-Agent指纹随机化
├── 请求间隔随机化 (100ms-2000ms)
└── Cookie池维护

Layer 2: 签名层防护
├── 多套签名算法并存
├── 签名参数动态获取
├── 设备指纹伪造
└── TLS指纹伪装

Layer 3: 行为层防护
├── 模拟真实用户浏览轨迹
├── 页面停留时间模拟
├── 滚动行为模拟
└── 交互事件伪造
```

#### 2.2.2 异常检测与自动切换

```python
# 通道健康度监控
class ChannelHealthMonitor:
    def __init__(self):
        self.channel_stats = defaultdict(lambda: {
            'success_count': 0,
            'fail_count': 0,
            'last_error': None,
            'error_history': []
        })
    
    def record_result(self, channel, success, error=None):
        stats = self.channel_stats[channel]
        if success:
            stats['success_count'] += 1
        else:
            stats['fail_count'] += 1
            stats['last_error'] = error
            stats['error_history'].append(error)
            # 保留最近100条错误记录
            if len(stats['error_history']) > 100:
                stats['error_history'] = stats['error_history'][-100:]
        
        # 计算成功率
        total = stats['success_count'] + stats['fail_count']
        if total >= 10:  # 至少有10次样本
            success_rate = stats['success_count'] / total
            if success_rate < 0.7:  # 成功率低于70%
                self.trigger_switch(channel, stats)
    
    def trigger_switch(self, channel, stats):
        # 触发通道切换
        logger.warning(f"通道 {channel} 健康度下降，准备切换")
        # 发送告警
        alert_service.send_alert({
            'type': 'channel_health',
            'channel': channel,
            'success_rate': stats['success_count'] / 
                          (stats['success_count'] + stats['fail_count']),
            'last_error': stats['last_error']
        })
```

### 2.3 广告激励模块

#### 2.3.1 广告状态管理

```java
// 广告状态服务
@Service
public class AdStatusService {
    
    @Autowired
    private RedisTemplate<String, Object> redisTemplate;
    
    private static final String AD_LIMIT_KEY_PREFIX = "ad:limit:";
    private static final String DEVICE_LIMIT_KEY_PREFIX = "ad:device:";
    private static final long CACHE_TTL_HOURS = 24;
    
    /**
     * 检查用户是否已经完成今日广告观看
     */
    public boolean hasWatchedAdToday(String userId, String deviceId) {
        String userKey = AD_LIMIT_KEY_PREFIX + userId + ":" + getTodayDate();
        Boolean watched = redisTemplate.hasKey(userKey);
        
        if (Boolean.TRUE.equals(watched)) {
            return true;
        }
        
        // 二次校验：检查设备是否也被占用
        String deviceKey = DEVICE_LIMIT_KEY_PREFIX + deviceId + ":" + getTodayDate();
        return Boolean.TRUE.equals(redisTemplate.hasKey(deviceKey));
    }
    
    /**
     * 标记用户已完成广告观看
     */
    public void markAdWatched(String userId, String deviceId) {
        String userKey = AD_LIMIT_KEY_PREFIX + userId + ":" + getTodayDate();
        String deviceKey = DEVICE_LIMIT_KEY_PREFIX + deviceId + ":" + getTodayDate();
        
        // 同时设置用户和设备标记，防止跨设备作弊
        redisTemplate.opsForValue().set(userKey, "watched", CACHE_TTL_HOURS, TimeUnit.HOURS);
        redisTemplate.opsForValue().set(deviceKey, "watched", CACHE_TTL_HOURS, TimeUnit.HOURS);
    }
    
    /**
     * 验证广告回调真实性
     */
    public boolean verifyAdCallback(String userId, String callbackId, String signature) {
        // 1. 验证签名
        String expectedSignature = generateSignature(callbackId, userId);
        if (!expectedSignature.equals(signature)) {
            log.warn("广告回调签名验证失败: userId={}, callbackId={}", userId, callbackId);
            return false;
        }
        
        // 2. 检查回调ID是否已使用（防止重放攻击）
        String usedCallbackKey = "ad:callback:used:" + callbackId;
        Boolean isUsed = redisTemplate.opsForValue().setIfAbsent(usedCallbackKey, "1", 2, TimeUnit.DAYS);
        if (Boolean.FALSE.equals(isUsed)) {
            log.warn("重复的广告回调: callbackId={}", callbackId);
            return false;
        }
        
        return true;
    }
    
    private String getTodayDate() {
        return LocalDate.now().format(DateTimeFormatter.ofPattern("yyyyMMdd"));
    }
    
    private String generateSignature(String callbackId, String userId) {
        String raw = callbackId + ":" + userId + ":" + adSecretKey;
        return DigestUtils.sha256Hex(raw);
    }
}
```

#### 2.3.2 防作弊机制

| 作弊类型 | 检测方式 | 应对策略 |
|---------|---------|---------|
| 本地时间篡改 | 服务端时间戳校验 | 以服务器时间为准 |
| 广告重放攻击 | 回调ID唯一性校验 | Redis记录已用回调ID |
| 多设备共享账号 | 设备指纹绑定 | 账号+设备双重限制 |
| 脚本自动点击 | 行为特征分析 | 检测异常操作模式 |

### 2.4 监控告警系统

#### 2.4.1 核心监控指标

```yaml
metrics:
  # 解析服务指标
  parse_success_rate:
    description: "各平台解析成功率"
    type: gauge
    threshold: 0.85  # 低于85%告警
    
  parse_latency_p95:
    description: "解析响应时间P95"
    type: histogram
    threshold: 8.0  # 超过8秒告警
    
  parse_errors_total:
    description: "解析失败总数"
    type: counter
    threshold: 100  # 每小时超过100次告警
  
  # 通道健康度
  channel_health:
    description: "各通道健康状态"
    type: gauge
    threshold: 0.7  # 低于70%告警
  
  # 广告相关
  ad_completion_rate:
    description: "广告完成率"
    type: gauge
    threshold: 0.3  # 低于30%预警
    
  ad_cheat_attempts:
    description: "疑似作弊次数"
    type: counter
    threshold: 50  # 每小时超过50次告警
```

#### 2.4.2 告警规则配置

```yaml
groups:
  - name: parse-service-alerts
    rules:
      - alert: LowParseSuccessRate
        expr: rate(parse_success_rate[5m]) < 0.85
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "解析成功率低于85%"
          description: "{{ $labels.platform }} 平台解析成功率仅为 {{ $value | humanizePercentage }}"
      
      - alert: HighParseLatency
        expr: histogram_quantile(0.95, rate(parse_latency_bucket[5m])) > 8
        for: 3m
        labels:
          severity: warning
        annotations:
          summary: "解析延迟过高"
          description: "P95延迟达到 {{ $value }} 秒"
      
      - alert: ChannelUnhealthy
        expr: channel_health < 0.7
        for: 2m
        labels:
          severity: critical
        annotations:
          summary: "解析通道不健康"
          description: "{{ $labels.channel }} 健康度为 {{ $value | humanizePercentage }}"
```

---

## 三、部署架构

### 3.1 环境规划

| 环境 | 用途 | 实例数 | 资源配置 |
|-----|-----|-------|---------|
| Dev | 开发测试 | 2 | 2C4G |
| Staging | 预发布验证 | 2 | 4C8G |
| Production | 线上生产 | 6 | 8C16G |

### 3.2 高可用设计

```
                    ┌─────────────────┐
                    │   SLB (负载均衡)  │
                    └────────┬────────┘
                             │
           ┌─────────────────┼─────────────────┐
           │                 │                 │
    ┌──────▼──────┐   ┌──────▼──────┐   ┌──────▼──────┐
    │  API Server │   │  API Server │   │  API Server │
    │    (Zone-A) │   │    (Zone-A) │   │    (Zone-B) │
    └──────┬──────┘   └──────┬──────┘   └──────┬──────┘
           │                 │                 │
           └─────────────────┼─────────────────┘
                             │
                    ┌────────▼────────┐
                    │   Redis Cluster │
                    │   (3主3从)      │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │   MySQL Master-Slave  │
                    │   (1主2从)      │
                    └─────────────────┘
```

### 3.3 扩展性设计

- **水平扩展**: 通过K8s HPA自动扩缩容
- **解析插件化**: 新增平台只需开发对应插件
- **配置中心化**: Nacos/Apollo统一管理所有配置

---

## 四、安全考虑

### 4.1 数据安全

| 数据类型 | 存储方式 | 加密方案 |
|---------|---------|---------|
| 用户信息 | MySQL | AES-256加密 |
| 解析结果 | Redis | 不存储敏感信息 |
| 广告回调 | MongoDB | 传输层TLS加密 |
| 日志数据 | ES/S3 | 脱敏后存储 |

### 4.2 接口安全

```yaml
security:
  authentication:
    type: JWT
    token_expiry: 7d
    refresh_enabled: true
    
  rate_limiting:
    enabled: true
    requests_per_minute: 60
    burst_limit: 100
    
  input_validation:
    url_pattern: "^(https?://).*$"
    max_url_length: 1024
    
  cors:
    allowed_origins: ["小程序域名"]
    allowed_methods: ["POST", "GET"]
```

---

## 五、技术风险与应对

| 风险项 | 影响程度 | 发生概率 | 应对措施 |
|-------|---------|---------|---------|
| 平台反爬升级 | 高 | 高 | 多通道冗余，规则热更新 |
| 法律合规风险 | 高 | 中 | 用户协议免责，快速下架机制 |
| 广告收入不达预期 | 中 | 中 | 多元化变现，成本优化 |
| 大规模流量冲击 | 中 | 低 | 自动扩缩容，限流熔断 |
| 第三方服务故障 | 低 | 中 | 本地兜底，故障转移 |

---

## 六、后续规划

### Phase 1 (MVP) - 4周
- [ ] 基础解析功能（抖音、快手）
- [ ] 微信小程序框架搭建
- [ ] 激励视频广告接入
- [ ] 基础监控告警

### Phase 2 (V1.0) - 4周
- [ ] 多平台支持（小红书、西瓜视频）
- [ ] 规则热更新机制
- [ ] 完整的监控体系
- [ ] 灰度发布能力

### Phase 3 (V2.0) - 6周
- [ ] 高级反爬对抗
- [ ] 用户增长体系
- [ ] 数据分析后台
- [ ] 商业化扩展

---

## 七、附录

### 7.1 依赖清单

```json
{
  "dependencies": {
    "backend": [
      "express/node.js 或 gin/go",
      "ioredis",
      "mysql2",
      "amqplib",
      "node-cron"
    ],
    "frontend": [
      "微信小程序原生框架",
      "wx.open-setting",
      "wx.showModal"
    ],
    "infrastructure": [
      "Nginx",
      "Redis Cluster",
      "MySQL 8.0",
      "RabbitMQ",
      "Prometheus + Grafana"
    ]
  }
}
```

### 7.2 参考文档

- [微信小程序开放文档 - 激励视频广告](https://developers.weixin.qq.com/miniprogram/dev/framework/open-ability/interactive-ad.html)
- [Prometheus 监控最佳实践](https://prometheus.io/docs/practices/)
- [微服务架构设计模式](https://microservices.io/patterns/)

---

**文档审批**

| 角色 | 姓名 | 签字 | 日期 | 意见 |
|-----|-----|-----|-----|-----|
| 架构师 | 小明 | | | |
| 项目经理 | 志龙 | | | |
| 产品经理 | XXX | | | |
| 测试工程师 | XXX | | | |
