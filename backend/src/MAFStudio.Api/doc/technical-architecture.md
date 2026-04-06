# 视频解析小程序技术架构文档

## 一、项目概述

### 1.1 产品名称
短视频高清无水印解析助手

### 1.2 产品定位
一款基于微信小程序的平台，帮助用户便捷获取抖音、快手等短视频平台的高清无水印视频。

### 1.3 核心价值
- 为用户解决视频保存带水印的问题
- 提供简单的粘贴-解析-下载体验
- 通过广告实现商业化闭环

---

## 二、技术架构设计

### 2.1 整体架构图

```
┌─────────────────────────────────────────────┐
│              前端层 (微信小程序)             │
│  ┌─────────┐  ┌─────────┐  ┌─────────────┐  │
│  │  界面层  │  │ 广告模块 │  │  分享解析器  │  │
│  └─────────┘  └─────────┘  └─────────────┘  │
└─────────────────────────────────────────────┘
                        ↓ API调用
┌─────────────────────────────────────────────┐
│          后端服务层 (Node.js/Python)         │
│  ┌──────────┐  ┌──────────┐  ┌────────────┐  │
│  │ 视频解析 │  │ 缓存系统  │  │  用户管理   │  │
│  │ 服务集群 │  │ (Redis)  │  │ (微信登录) │  │
│  └──────────┘  └──────────┘  └────────────┘  │
└─────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────┐
│               数据存储层                     │
│  ┌──────────┐  ┌──────────┐  ┌────────────┐  │
│  │  MySQL   │  │  Redis   │  │  CDN存储   │  │
│  │  业务数据 │  │  缓存数据 │  │  视频文件  │  │
│  └──────────┘  └──────────┘  └────────────┘  │
└─────────────────────────────────────────────┘
```

### 2.2 技术栈选型

| 层级 | 技术选型 | 选择理由 |
|------|----------|----------|
| 前端 | 微信小程序原生/WXSS | 微信生态无缝集成，开发效率高 |
| 后端 | Node.js + Express/Koa | 轻量、异步I/O适合高并发场景 |
| 数据库 | MySQL 8.0 | 成熟稳定，支持事务 |
| 缓存 | Redis | 高性能Key-Value存储 |
| 对象存储 | 腾讯云COS | 视频临时存储，CDN加速 |
| 部署 | Docker + K8s | 容器化部署，弹性伸缩 |

---

## 三、核心功能技术实现

### 3.1 视频解析引擎

#### 3.1.1 解析策略

**策略一：短链还原（首选）**
```javascript
async function parseShortUrl(url) {
    // 抖音/快手短链接会在Response Header中返回Location
    const response = await fetch(url, { method: 'HEAD', follow: 0 });
    const realUrl = response.headers.get('location');
    return realUrl || url;
}
```

**策略二：HTML内容提取**
```javascript
async function extractFromHtml(html, platform) {
    const cheerio = require('cheerio');
    const $ = cheerio.load(html);
    
    switch(platform) {
        case 'douyin':
            // 查找video标签或json中的play_addr
            return $('#player').data('video-url') || 
                   JSON.parse($('script[data-id]').text()).play_addr;
        case 'kuaishou':
            return $('.video-play-url').text() || 
                   decodeURIComponent($('.video-url').attr('href'));
    }
}
```

**策略三：API逆向工程**
```javascript
async function callPlatformApi(shareUrl) {
    // 模拟平台内部API请求，解密参数后获取视频地址
    const apiEndpoint = getPlatformApi(platform);
    const params = generateSignParams(shareUrl);
    
    const res = await axios.get(apiEndpoint, { params });
    return decryptVideoUrl(res.data.data.video_url);
}
```

**策略四：第三方服务兜底**
```javascript
async function fallbackToThirdParty(shareUrl) {
    const thirdParties = [
        'https://api.aigei.com/parse',
        'https://api.videotools.cn/douyin'
    ];
    
    for (const api of thirdParties) {
        try {
            const result = await axios.post(api, { url: shareUrl });
            if (result.data.code === 200) {
                logUsage('third_party_fallback', platform);
                return result.data.data.video_url;
            }
        } catch (e) {
            continue;
        }
    }
    throw new Error('所有解析源均失败');
}
```

#### 3.1.2 稳定性保障措施

| 措施 | 技术方案 | 优先级 |
|------|----------|--------|
| 多源备份 | 维护3-5个解析源，主源失败自动切换备用 | P0 |
| 缓存机制 | Redis缓存已解析结果，TTL=7天 | P0 |
| 限流熔断 | 单IP每分钟最多3次请求，错误率>30%触发熔断 | P0 |
| 代理IP池 | 接入代理服务，轮换出口IP | P1 |
| 监控告警 | Prometheus+Grafana监控，失败率告警 | P1 |

#### 3.1.3 缓存策略

```javascript
// Redis缓存结构
// key: parse_cache:{platform}:{originalUrlHash}
// value: {videoUrl, timestamp, expiresAt}
const CACHE_TTL = {
    douyin: 7 * 24 * 3600,  // 7天
    kuaishou: 7 * 24 * 3600,
    default: 3 * 24 * 3600  // 3天
};

async function getCachedResult(key) {
    const cached = await redis.get(key);
    if (cached) {
        const data = JSON.parse(cached);
        if (Date.now() < data.expiresAt) {
            return data.videoUrl;
        }
    }
    return null;
}
```

### 3.2 广告系统集成

#### 3.2.1 微信小程序广告接入

```javascript
// 小程序端广告组件
Page({
    data: {
        videoAd: null,
        hasWatchedToday: false,
        lastWatchTime: 0
    },

    onLoad() {
        // 检查当日是否已观看广告
        const today = new Date().toDateString();
        const lastTime = wx.getStorageSync('last_ad_watch_time');
        
        if (lastTime && new Date(lastTime).toDateString() === today) {
            this.setData({ hasWatchedToday: true });
        }
    },

    onShareLinkInput(e) {
        if (!this.data.hasWatchedToday) {
            this.showAdThenParse(e.detail.value);
        } else {
            this.startParse(e.detail.value);
        }
    },

    showAdThenParse(link) {
        if (!this.data.videoAd) {
            this.data.videoAd = wx.createRewardedVideoAd({
                adUnitId: 'adunit-xxxxxxxxxx'
            });
            
            this.data.videoAd.onLoad(() => {});
            this.data.videoAd.onError((err) => {
                console.error('广告加载失败', err);
                // 允许跳过广告直接解析（容错）
                this.startParse(link);
            });
        }

        this.data.videoAd.show().catch(() => {
            // 广告显示失败，同样允许继续
            this.startParse(link);
        }).then(() => {
            this.data.videoAd.onClose(() => {
                // 用户已完成观看
                this.markAdWatched();
                this.startParse(link);
            });
        });
    },

    markAdWatched() {
        const now = Date.now();
        wx.setStorageSync('last_ad_watch_time', now);
        wx.setStorageSync('has_watched_today', true);
        this.setData({ hasWatchedToday: true, lastWatchTime: now });
    }
});
```

#### 3.2.2 服务端校验（防作弊）

```javascript
// 后端验证广告观看状态
app.post('/api/verify-ad-status', async (req, res) => {
    const { userId, token } = req.body;
    
    // 查询用户今日的观看记录
    const record = await db.ads.findOne({
        userId,
        date: getTodayString(),
        watched: true
    });
    
    res.json({
        allowed: !!record,
        reason: record ? 'ok' : 'need_watch_ad'
    });
});
```

### 3.3 热更新机制

#### 3.3.1 微信小程序热更新限制

| 类型 | 是否支持 | 替代方案 |
|------|----------|----------|
| JS逻辑代码 | ❌ 否 | 版本发布更新 |
| 静态资源 | ✅ 是 | CDN+版本号控制 |
| 配置文件 | ✅ 是 | 服务端动态下发 |
| 云函数代码 | ⚠️ 有限制 | 发布新版本 |

#### 3.3.2 云端配置中心实现

```javascript
// 配置数据结构
const CONFIG_SCHEMA = {
    version: 'string',           // 当前应用版本
    minVersion: 'string',        // 最低要求版本
    maintenanceMode: 'boolean',  // 维护模式开关
    parseServers: [              // 解析服务列表
        { url: 'https://api1.xxx.com', priority: 100, enabled: true },
        { url: 'https://api2.xxx.com', priority: 90, enabled: true }
    ],
    featureFlags: {              // 特性开关
        enableBatchParse: false,
        enableSocialLogin: true
    },
    ads: {                       // 广告配置
        adUnitId: 'adunit-xxxx',
        showFrequency: 1         // 几次解析后展示广告
    },
    updatedAt: 'timestamp'
};

// 小程序启动时拉取配置
Page({
    async loadRemoteConfig() {
        const config = await axios.get('/api/config/latest');
        
        // 对比本地版本，决定是否强制更新
        if (config.data.minVersion && compareVersion(config.data.minVersion, this.version) > 0) {
            wx.showModal({
                title: '需要更新',
                content: '检测到新版本，请前往更新后再使用',
                confirmText: '立即更新',
                success: () => wx.openOfficialAccount()
            });
        }
        
        // 更新配置到本地
        wx.setStorageSync('remote_config', config.data);
    }
});
```

### 3.4 反爬与风控策略

#### 3.4.1 请求防护

```javascript
// 请求频率限制
const rateLimiter = rateLimit({
    windowMs: 60 * 1000,     // 1分钟
    max: 5,                  // 最多5次请求
    message: '请求过于频繁，请稍后再试'
});

// 请求指纹验证
function validateRequestFingerprint(req) {
    const ip = req.ip;
    const userAgent = req.headers['user-agent'];
    const referer = req.headers.referer;
    
    // 黑名单检查
    if (isInBlacklist(ip)) {
        throw new ForbiddenError('请求已被拒绝');
    }
    
    // Referer白名单检查（仅限小程序来源）
    if (referer && !referer.includes('weixin')) {
        logger.warn('可疑Referer:', referer, 'from IP:', ip);
    }
}
```

#### 3.4.2 代理IP池管理

```javascript
class ProxyPool {
    constructor() {
        this.proxies = [];
        this.currentIdx = 0;
    }

    async refreshProxies() {
        // 从代理服务API获取新的IP列表
        const response = await axios.get('https://proxy-api.example.com/list');
        this.proxies = response.data.proxies.map(p => ({
            ip: p.ip,
            port: p.port,
            score: 100,
            lastUsed: 0
        }));
    }

    getNextProxy() {
        // 轮询策略 + 评分加权
        const sorted = [...this.proxies]
            .sort((a, b) => {
                const aScore = a.score / (Date.now() - a.lastUsed);
                const bScore = b.score / (Date.now() - b.lastUsed);
                return bScore - aScore;
            });
        
        this.currentIdx = (this.currentIdx + 1) % sorted.length;
        sorted[this.currentIdx].lastUsed = Date.now();
        return `http://${sorted[this.currentIdx].ip}:${sorted[this.currentIdx].port}`;
    }

    adjustScore(ip, increment) {
        const proxy = this.proxies.find(p => p.ip === ip);
        if (proxy) {
            proxy.score += increment;
            proxy.score = Math.max(0, Math.min(200, proxy.score));
        }
    }
}
```

---

## 四、技术风险评估

### 4.1 风险矩阵

| 风险项 | 发生概率 | 影响程度 | 风险等级 | 应对措施 |
|--------|----------|----------|----------|----------|
| 平台规则变更 | 高 | 高 | 🔴🔴🔴 极高 | 多平台适配+快速迭代能力 |
| IP封禁 | 高 | 中 | 🟠🟠🟠 高 | 代理IP池+请求频率控制 |
| 法律合规争议 | 中 | 高 | 🟠🟠🟠 高 | 不做内容存储，只做链接转换 |
| 微信审核不通过 | 中 | 高 | 🟠🟠 中高 | 功能描述包装，避免敏感词 |
| 第三方API不稳定 | 中 | 中 | 🟡🟡 中 | 自建服务为主，第三方为辅 |
| 服务器成本失控 | 低 | 中 | 🟡 中 | 合理设置缓存策略，按需扩容 |

### 4.2 合规建议

1. **内容声明**
   - 在显著位置添加免责声明
   - 明确告知用户视频版权归属原平台
   - 不得用于商业用途，仅供个人学习研究

2. **功能设计**
   - 仅提供链接解析服务，不直接托管视频内容
   - 用户下载后内容由用户自行保管
   - 添加举报机制处理侵权投诉

3. **数据隐私**
   - 遵循《个人信息保护法》
   - 最小化收集用户数据
   - 明示隐私政策和用户协议

---

## 五、部署架构

### 5.1 生产环境拓扑

```
┌─────────────────────────────────────────────────────────────┐
│                         CDN (静态资源)                        │
└─────────────────────────────────────────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────────────┐
│                      SLB/负载均衡                            │
└─────────────────────────────────────────────────────────────┘
                    ↓                 ↓
┌──────────────────────┐    ┌──────────────────────┐
│   API Server A       │    │   API Server B       │
│   (Node.js Cluster)  │    │   (Node.js Cluster)  │
└──────────────────────┘    └──────────────────────┘
          ↓                          ↓
┌─────────────────────────────────────────────────────────────┐
│                      Redis Cluster                           │
└─────────────────────────────────────────────────────────────┘
          ↓                          ↓
┌──────────────────────┐    ┌──────────────────────┐
│      MySQL Master    │    │      MySQL Slave     │
└──────────────────────┘    └──────────────────────┘
```

### 5.2 资源配置建议（初期）

| 组件 | 配置 | 数量 | 说明 |
|------|------|------|------|
| API服务器 | 2核4G | 2 | 水平扩展，支持故障转移 |
| MySQL | 2核4G | 1主1从 | 主从复制保证可用性 |
| Redis | 1核2G | 1 | 持久化开启 |
| CDN流量包 | - | - | 按量付费，预估初期50GB/月 |
| 域名SSL证书 | - | 1 | 免费Let's Encrypt即可 |

### 5.3 监控体系

- **应用监控**: Sentry（错误追踪）、Prometheus（性能指标）
- **日志收集**: ELK Stack 或 Cloud Provider Logs
- **告警通知**: 钉钉/企业微信机器人
- **健康检查**: 定时任务检测各服务连通性

---

## 六、开发路线图

### 6.1 MVP版本功能清单

| 功能模块 | 具体需求 | 预计工时 | 负责人 |
|----------|----------|----------|--------|
| 微信小程序前端 | 首页、解析页、下载反馈 | 3人日 | 前端开发 |
| 后端基础框架 | 用户认证、API网关 | 2人日 | 后端开发 |
| 抖音解析引擎 | 单链接解析 | 5人日 | 后端开发 |
| 广告集成 | 激励视频广告接入 | 2人日 | 全栈 |
| 基础监控 | 错误上报、日志收集 | 1人日 | 运维 |
| **合计** | - | **13人日** | - |

### 6.2 技术验证POC计划

1. **第一周**: 完成抖音单链接解析POC
2. **第二周**: 完成快手链接解析，建立双源备份
3. **第三周**: 完成微信小程序基础框架+广告集成
4. **第四周**: 压力测试+安全审计

---

## 七、待确认事项

1. [ ] 是否需要支持更多平台（如视频号、B站等）
2. [ ] 广告收入的分成比例和结算周期
3. [ ] 是否需要用户登录系统和会员体系
4. [ ] 解析服务的SLA目标（可用性/响应时间）
5. [ ] 法律合规审查是否需要进行

---

## 八、附录

### 8.1 参考资源

- 微信小程序官方文档：https://developers.weixin.qq.com/miniprogram/dev/framework/
- Node.js最佳实践：https://github.com/goldbergyoni/nodebestpractices
- 分布式系统设计原则

### 8.2 联系方式

- 架构师: 小明
- 更新日期: 2024-01-XX
- 文档版本: v1.0

---

*本文档为内部技术参考文档，请注意保密*
