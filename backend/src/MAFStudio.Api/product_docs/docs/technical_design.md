# 技术架构设计文档

## 1. 系统整体架构

### 1.1 架构图

```mermaid
graph TB
    subgraph 前端层
        A[微信小程序]
        B[H5移动端页面]
    end
    
    subgraph API网关层
        C[Nginx负载均衡]
        D[Gin API服务器]
    end
    
    subgraph 业务服务层
        E[解析服务集群]
        F[广告管理服务]
        G[用户认证服务]
        H[日志统计服务]
    end
    
    subgraph 数据层
        I[(MySQL主库)]
        J[(Redis缓存)]
        K[(对象存储)]
        L[(日志归档)]
    end
    
    A --> C
    B --> C
    C --> D
    D --> E
    D --> F
    D --> G
    D --> H
    E --> I
    E --> J
    F --> I
    G --> I
    H --> L
```

### 1.2 技术栈选型

| 组件 | 技术选择 | 版本号 | 说明 |
|------|----------|--------|------|
| 前端框架 | 微信小程序基础库 | ^2.30.0 | 官方推荐版本 |
| 后端语言 | Python 3.9+ | 3.9.16 | 异步I/O性能好 |
| Web框架 | FastAPI | 0.89.1 | 自动文档生成 |
| 数据库 | MySQL | 8.0.33 | 事务支持完善 |
| 缓存 | Redis | 7.0.11 | 高性能内存存储 |
| 任务队列 | Celery + RabbitMQ | 5.2.7 | 异步任务处理 |
| 部署方案 | Docker + Nginx | - | 容器化标准化 |
| 监控体系 | Prometheus + Grafana | v2.40+v4.0 | 实时指标展示 |

---

## 2. 核心模块设计

### 2.1 视频解析引擎

#### 2.1.1 接口定义

```python
# API路由定义
@app.post("/api/v1/parse")
async def parse_video(
    request: ParseRequest,
    user_id: str = Header(...)
):
    """
    视频解析接口
    
    Parameters:
    - video_url: 分享链接
    - source_platform: 平台标识(douyin/kuaishou)
    
    Returns:
    {
        "code": 200,
        "data": {
            "title": "视频标题",
            "author": "作者昵称",
            "duration": 30,
            "video_url": "https://xxx.mp4",
            "cover_url": "https://xxx.jpg"
        }
    }
    """
```

#### 2.1.2 解析器工厂模式

```python
from abc import ABC, abstractmethod
from typing import Dict, Type

class BaseParser(ABC):
    @abstractmethod
    async def parse(self, video_url: str) -> VideoInfo:
        pass
    
    @abstractmethod
    def can_parse(self, url: str) -> bool:
        pass

class DouyinParser(BaseParser):
    def can_parse(self, url: str) -> bool:
        return "douyin.com" in url or "v.douyin.com" in url
    
    async def parse(self, video_url: str) -> VideoInfo:
        # 具体实现略
        pass

class KuaishouParser(BaseParser):
    def can_parse(self, url: str) -> bool:
        return "kuaishou.com" in url or "kv.kuaishou.com" in url
    
    async def parse(self, video_url: str) -> VideoInfo:
        # 具体实现略
        pass

class ParserFactory:
    _parsers: Dict[str, Type[BaseParser]] = {
        'douyin': DouyinParser,
        'kuaishou': KuaishouParser
    }
    
    @classmethod
    def create_parser(cls, platform: str) -> BaseParser:
        parser_class = cls._parsers.get(platform.lower())
        if not parser_class:
            raise ValueError(f"Unsupported platform: {platform}")
        return parser_class()
    
    @classmethod
    def detect_platform(cls, url: str) -> str:
        for name, parser_class in cls._parsers.items():
            instance = parser_class()
            if instance.can_parse(url):
                return name
        raise ValueError("Unable to detect platform")
```

#### 2.1.3 缓存策略

```python
import hashlib
import json
from functools import wraps
from datetime import timedelta

def cache_result(expires_in: int = 3600):
    """装饰器：缓存解析结果"""
    def decorator(func):
        @wraps(func)
        async def wrapper(*args, **kwargs):
            url = kwargs.get('video_url') or args[0]
            cache_key = f"parse:{hashlib.md5(url.encode()).hexdigest()}"
            
            # 尝试从Redis获取
            cached = await redis_client.get(cache_key)
            if cached:
                return json.loads(cached)
            
            # 执行解析并缓存
            result = await func(*args, **kwargs)
            await redis_client.setex(
                cache_key, 
                expires_in, 
                json.dumps(result)
            )
            return result
        return wrapper
    return decorator
```

### 2.2 广告管理系统

#### 2.2.1 状态管理模型

```python
from sqlalchemy import Column, Integer, String, DateTime, Boolean
from sqlalchemy.ext.declarative import declarative_base
from datetime import datetime

Base = declarative_base()

class AdRecord(Base):
    __tablename__ = 'ad_records'
    
    id = Column(Integer, primary_key=True, autoincrement=True)
    user_id = Column(String(64), nullable=False, index=True)
    device_fingerprint = Column(String(128), nullable=False)
    ip_hash = Column(String(64), nullable=False)
    
    ad_unit_id = Column(String(64), nullable=False)
    action = Column(String(20), nullable=False)  # show/click/complete
    
    reward_start_time = Column(DateTime, default=datetime.utcnow)
    reward_expire_time = Column(DateTime, nullable=True)
    
    created_at = Column(DateTime, default=datetime.utcnow)
    
    def is_valid(self) -> bool:
        """检查奖励是否有效"""
        if self.reward_expire_time is None:
            return False
        return datetime.utcnow() <= self.reward_expire_time
```

#### 2.2.2 广告验证逻辑

```python
class AdValidator:
    @staticmethod
    async def validate_user_permission(user_id: str, device_id: str, ip: str) -> bool:
        """
        验证用户是否有今日使用权
        
        优先级：
        1. 用户账号记录
        2. 设备指纹记录
        3. IP地址记录
        """
        
        # 尝试通过用户ID查询
        record = await AdRecord.query.filter_by(
            user_id=user_id,
            action='complete',
            reward_expire_time>=datetime.utcnow()
        ).first()
        
        if record and record.is_valid():
            return True
        
        # 尝试通过设备指纹查询
        device_hash = hashlib.sha256(device_id.encode()).hexdigest()
        device_record = await AdRecord.query.filter_by(
            device_fingerprint=device_hash,
            action='complete',
            reward_expire_time>=datetime.utcnow()
        ).first()
        
        if device_record and device_record.is_valid():
            return True
        
        # 最后尝试IP地址
        ip_hash = hashlib.sha256(ip.encode()).hexdigest()
        ip_record = await AdRecord.query.filter_by(
            ip_hash=ip_hash,
            action='complete',
            reward_expire_time>=datetime.utcnow()
        ).first()
        
        if ip_record and ip_record.is_valid():
            return True
        
        return False
    
    @classmethod
    async def grant_daily_pass(cls, user_id: str, device_id: str, ip: str, 
                              ad_unit_id: str):
        """授予当日免费权限"""
        now = datetime.utcnow()
        
        record = cls(
            user_id=user_id,
            device_fingerprint=hashlib.sha256(device_id.encode()).hexdigest(),
            ip_hash=hashlib.sha256(ip.encode()).hexdigest(),
            ad_unit_id=ad_unit_id,
            action='complete',
            reward_start_time=now,
            reward_expire_time=now + timedelta(days=1)
        )
        
        db_session.add(record)
        db_session.commit()
```

### 2.3 安全与限流机制

#### 2.3.1 频率限制

```python
from fastapi_limiter.depends import RateLimiter
from fastapi import HTTPException, Request
from starlette.middleware.base import BaseHTTPMiddleware

class LimitExceeded(Exception):
    pass

class RateLimitMiddleware(BaseHTTPMiddleware):
    def __init__(self, app, requests_per_hour=100):
        super().__init__(app)
        self.requests_per_hour = requests_per_hour
        self.redis = aioredis.from_url("redis://localhost")
    
    async def dispatch(self, request: Request, call_next):
        client_ip = request.client.host
        user_agent = request.headers.get("user-agent", "")
        
        key = f"rate_limit:{client_ip}:{user_agent}"
        
        # 获取当前计数器
        current_count = await self.redis.get(key)
        
        if current_count is None:
            await self.redis.setex(key, 3600, 1)
        else:
            count = int(current_count)
            if count >= self.requests_per_hour:
                raise HTTPException(
                    status_code=429,
                    detail="请求过于频繁，请稍后再试"
                )
            await self.redis.incr(key)
        
        response = await call_next(request)
        return response
```

#### 2.3.2 SQL注入防护

```python
from sqlalchemy import text

def safe_query(session, query_string: str, params: dict = None):
    """安全执行SQL查询"""
    try:
        result = session.execute(text(query_string), params or {})
        return result.fetchall()
    except Exception as e:
        logger.error(f"SQL执行错误: {str(e)}")
        raise ValueError("查询参数不合法")
```

---

## 3. 数据库设计

### 3.1 ER图概览

```
┌─────────────┐       ┌─────────────┐       ┌─────────────┐
│   users     │       │  parse_logs │       │ad_records   │
├─────────────┤       ├─────────────┤       ├─────────────┤
│ id (PK)     │◀──┐   │ id (PK)     │       │ id (PK)     │
│ device_info │   ├──▶│ user_id(FK) │       │ user_id(FK) │
│ ip_address  │   │   │ video_url   │       │ device_fp   │
│ created_at  │   │   │ platform    │       │ ip_hash     │
└─────────────┘   │   │ status      │       │ ad_unit_id  │
                  │   │ created_at  │       │ action      │
                  │   └─────────────┘       │ created_at  │
                                                  └─────────────┘
```

### 3.2 表结构设计

#### users表
```sql
CREATE TABLE users (
    id VARCHAR(64) PRIMARY KEY COMMENT '用户唯一标识',
    device_info JSON COMMENT '设备信息JSON',
    ip_address VARCHAR(45) COMMENT 'IP地址',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    INDEX idx_created_at (created_at),
    INDEX idx_ip (ip_address)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### parse_logs表
```sql
CREATE TABLE parse_logs (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id VARCHAR(64) COMMENT '用户ID',
    source_platform ENUM('douyin','kuaishou','unknown') NOT NULL,
    video_url TEXT COMMENT '原始链接',
    parsed_url TEXT COMMENT '解析后的URL',
    status ENUM('success','failed','timeout') NOT NULL,
    response_time_ms INT COMMENT '响应时间(ms)',
    error_message TEXT COMMENT '错误详情',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_user_id (user_id),
    INDEX idx_platform (source_platform),
    INDEX idx_created_at (created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### ad_records表
```sql
CREATE TABLE ad_records (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id VARCHAR(64) COMMENT '用户ID',
    device_fingerprint VARCHAR(128) COMMENT '设备指纹哈希',
    ip_hash VARCHAR(64) COMMENT 'IP地址哈希',
    ad_unit_id VARCHAR(64) NOT NULL,
    action ENUM('show','click','complete') NOT NULL,
    reward_start_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    reward_expire_time TIMESTAMP COMMENT '过期时间',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_user_exp (user_id, reward_expire_time),
    INDEX idx_device_exp (device_fingerprint, reward_expire_time),
    INDEX idx_ip_exp (ip_hash, reward_expire_time)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

## 4. API接口规范

### 4.1 统一响应格式

```json
{
  "code": 200,
  "message": "success",
  "data": {},
  "timestamp": 1678886400,
  "trace_id": "abc123def456"
}
```

**错误码定义：**

| 错误码 | 说明 |
|--------|------|
| 200 | 成功 |
| 400 | 参数错误 |
| 401 | 未授权 |
| 403 | 无权限 |
| 429 | 请求过于频繁 |
| 500 | 服务器内部错误 |

### 4.2 主要接口列表

| 方法 | 路径 | 描述 |
|------|------|------|
| POST | /api/v1/parse | 视频解析 |
| POST | /api/v1/auth/login | 用户登录（可选） |
| GET | /api/v1/history | 获取解析历史 |
| POST | /api/v1/ad/verify | 广告验证 |
| GET | /api/v1/status | 系统健康检查 |

### 4.3 请求示例

**解析请求:**
```json
POST /api/v1/parse
{
  "video_url": "https://v.douyin.com/xyz123/",
  "source_platform": "douyin"
}
```

**解析响应:**
```json
{
  "code": 200,
  "message": "success",
  "data": {
    "title": "搞笑短视频合集",
    "author": "创作者昵称",
    "duration": 30,
    "cover_url": "https://p3-pc.douyinpic.com/img/xxx~tplv-dy-360n.jpeg",
    "video_url": "https://aweme.snssdk.com/aweme/v1/play/?video_id=xxx&ratio=720p",
    "download_url": "https://cdn.example.com/download/xxx.mp4"
  },
  "timestamp": 1678886400,
  "trace_id": "req_20230315_123456"
}
```

---

## 5. 性能优化方案

### 5.1 Redis缓存策略

```python
class CacheManager:
    CACHE_TTL_PARSE = 3600  # 解析结果缓存1小时
    CACHE_TTL_USER = 86400  # 用户状态缓存24小时
    
    @staticmethod
    def get_parse_cache(video_url: str):
        key = f"parse:{hashlib.md5(video_url.encode()).hexdigest()}"
        return redis_client.get(key)
    
    @staticmethod
    def set_parse_cache(video_url: str, data: dict):
        key = f"parse:{hashlib.md5(video_url.encode()).hexdigest()}"
        redis_client.setex(key, CacheManager.CACHE_TTL_PARSE, json.dumps(data))
    
    @staticmethod
    def get_user_permission(user_id: str):
        key = f"user_perm:{user_id}"
        return redis_client.get(key)
    
    @staticmethod
    def set_user_permission(user_id: str):
        key = f"user_perm:{user_id}"
        redis_client.setex(key, CacheManager.CACHE_TTL_USER, b'1')
```

### 5.2 连接池配置

```yaml
# database.yaml
pool:
  min_size: 10
  max_size: 50
  timeout: 30
  
redis:
  pool_min_size: 5
  pool_max_size: 30
  socket_timeout: 5
  retry_on_timeout: true
```

### 5.3 并发控制

```python
from concurrent.futures import ThreadPoolExecutor
from threading import Lock

class ConcurrentParser:
    def __init__(self, max_workers=10):
        self.executor = ThreadPoolExecutor(max_workers=max_workers)
        self.lock = Lock()
        self.active_tasks = 0
    
    async def parse_concurrent(self, urls: List[str]) -> List[VideoInfo]:
        tasks = [self.parse_single(url) for url in urls]
        results = await asyncio.gather(*tasks, return_exceptions=True)
        return [r for r in results if isinstance(r, VideoInfo)]
    
    async def parse_single(self, url: str):
        with self.lock:
            if self.active_tasks >= 10:
                await asyncio.sleep(0.1)
                self.active_tasks += 1
        
        try:
            return await self.parser_factory.create_parser().parse(url)
        finally:
            with self.lock:
                self.active_tasks -= 1
```

---

## 6. 部署方案

### 6.1 Docker容器化

```dockerfile
FROM python:3.9-slim

WORKDIR /app

# 安装依赖
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# 复制代码
COPY . .

# 暴露端口
EXPOSE 8000

# 启动命令
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
```

### 6.2 Docker Compose配置

```yaml
version: '3.8'

services:
  api:
    build: .
    ports:
      - "8000:8000"
    environment:
      - DATABASE_URL=mysql+pymysql://user:pass@mysql:3306/video_db
      - REDIS_URL=redis://redis:6379
    depends_on:
      - mysql
      - redis
    restart: always

  mysql:
    image: mysql:8.0
    environment:
      MYSQL_ROOT_PASSWORD: root_password
      MYSQL_DATABASE: video_db
    volumes:
      - mysql_data:/var/lib/mysql
    restart: always

  redis:
    image: redis:7-alpine
    volumes:
      - redis_data:/data
    restart: always

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
    depends_on:
      - api
    restart: always

volumes:
  mysql_data:
  redis_data:
```

---

## 7. 监控与告警

### 7.1 关键指标

| 指标类型 | 名称 | 阈值 |
|----------|------|------|
| 性能 | QPS | >1000 |
| 性能 | P99延迟 | <2s |
| 可用性 | 成功率 | ≥99.5% |
| 资源 | CPU使用率 | <80% |
| 资源 | 内存使用率 | <85% |
| 业务 | 解析失败率 | <5% |
| 业务 | 广告完成率 | >70% |

### 7.2 告警规则

```yaml
# alert-rules.yaml
groups:
  - name: api_monitoring
    rules:
      - alert: HighErrorRate
        expr: rate(http_requests_total{status=~"5.."}[5m]) > 0.05
        for: 5m
        annotations:
          summary: "API错误率过高"
          
      - alert: SlowResponseTime
        expr: histogram_quantile(0.95, http_request_duration_seconds_bucket) > 2
        for: 10m
        annotations:
          summary: "响应时间过长"
          
      - alert: ParseFailureRate
        expr: rate(parse_logs_total{status="failed"}[1h]) / rate(parse_logs_total[1h]) > 0.1
        for: 15m
        annotations:
          summary: "解析失败率超过10%"
```

---

## 8. 安全加固措施

### 8.1 HTTPS强制启用

```nginx
server {
    listen 443 ssl http2;
    server_name example.com;
    
    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;
    
    # 强制HSTS
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    
    location / {
        proxy_pass http://backend:8000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}

# 重定向HTTP到HTTPS
server {
    listen 80;
    server_name example.com;
    return 301 https://$server_name$request_uri;
}
```

### 8.2 CORS配置

```python
from fastapi.middleware.cors import CORSMiddleware

app.add_middleware(
    CORSMiddleware,
    allow_origins=["https://your-wechat-app.com"],  # 仅允许小程序域名
    allow_credentials=True,
    allow_methods=["GET", "POST"],
    allow_headers=["*"],
)
```

### 8.3 输入验证

```python
from pydantic import BaseModel, validator
from typing import Optional
import re

class ParseRequest(BaseModel):
    video_url: str
    
    @validator('video_url')
    def validate_url(cls, v):
        pattern = r'^https?://(v\.douyin\.com|www\.douyin\.com|kv\.kuaishou\.com)/.*$'
        if not re.match(pattern, v):
            raise ValueError('不支持的视频平台或无效链接')
        return v.strip()
```

---

*技术架构师：小明*  
*测试工程师审核：安全与性能专项*  
*最后更新：$(getCurrentDateTime format="%Y-%m-%d")*