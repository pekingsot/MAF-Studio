# Docker 网络架构说明

## 🎯 核心概念

### Docker 中的 localhost

| 场景 | localhost 指向 | 说明 |
|------|---------------|------|
| **容器内部** | 容器自己 | 容器内的服务访问 localhost 只能访问容器内的服务 |
| **浏览器中** | 用户的主机 | 前端代码运行在浏览器中，localhost 指向用户的机器 |
| **主机上** | 主机自己 | 在主机上运行的进程，localhost 指向主机 |

## 📊 当前架构

### 前端架构（React SPA）

```
┌─────────────────────────────────────────────────────────┐
│                    用户浏览器                            │
│                                                          │
│  1. 访问 http://localhost:3001                          │
│     ↓ 获取静态文件                                       │
│  2. 下载并运行 React 应用                                │
│     ↓                                                    │
│  3. API 请求 http://localhost:5001/api                  │
│     ↓                                                    │
│  4. WebSocket 连接 ws://localhost:5001/ws               │
└─────────────────────────────────────────────────────────┘
         ↓                                    ↓
    ┌─────────────┐                  ┌─────────────┐
    │ 前端容器     │                  │ 后端容器     │
    │ (serve)     │                  │ (.NET)      │
    │ 端口: 3000  │                  │ 端口: 5000  │
    │ 映射: 3001  │                  │ 映射: 5001  │
    └─────────────┘                  └─────────────┘
```

### 关键点

1. **前端代码运行在浏览器中**，不在容器内
   - serve 容器只提供静态文件服务
   - React 应用下载到浏览器后执行

2. **浏览器访问的是主机端口**
   - `localhost:3001` → 主机的 3001 端口 → 前端容器
   - `localhost:5001` → 主机的 5001 端口 → 后端容器

3. **当前配置是正确的**
   - `.env.production` 中的 `http://localhost:5001/api` 是正确的
   - 因为这是浏览器访问的地址

## 🔧 配置说明

### 当前配置（正确）

**`.env.production`**:
```env
# 浏览器访问后端 API
REACT_APP_API_URL=http://localhost:5001/api
REACT_APP_WS_URL=ws://localhost:5001/ws
```

**为什么正确？**
- 前端代码在浏览器中运行
- 浏览器的 `localhost` 指向主机
- 主机的 `5001` 端口映射到后端容器

## 🌐 不同场景的配置

### 场景1: 当前方案（React SPA + 浏览器）

**适用**：前端是 SPA，代码在浏览器中运行

**配置**：
```env
# .env.production
REACT_APP_API_URL=http://localhost:5001/api
```

**访问流程**：
```
浏览器 → localhost:5001 → 主机 → 后端容器
```

### 场景2: 容器间通信（如果需要）

**适用**：
- 前端容器内有服务端渲染（SSR）
- 前端容器内需要调用后端 API
- 容器内的健康检查

**配置**：
```env
# .env.production (容器间通信)
REACT_APP_API_URL=http://backend:5000/api
```

**访问流程**：
```
前端容器 → backend:5000 → Docker 网络 → 后端容器
```

**注意**：
- `backend` 是 docker-compose.yml 中定义的服务名
- 容器间通信使用容器端口（5000），不是主机端口（5001）

### 场景3: 从容器访问主机

**适用**：容器内需要访问主机上的服务

**Linux 方案**：
```bash
# 使用 host.docker.internal
REACT_APP_API_URL=http://host.docker.internal:5001/api
```

**或使用主机 IP**：
```bash
# 使用主机的实际 IP
REACT_APP_API_URL=http://192.168.1.100:5001/api
```

## 📝 Docker Compose 网络配置

### 当前配置

```yaml
services:
  frontend:
    networks:
      - maf-network
    ports:
      - "3001:3000"  # 主机:容器

  backend:
    networks:
      - maf-network
    ports:
      - "5001:5000"  # 主机:容器

networks:
  maf-network:
    driver: bridge
```

### 网络通信方式

| 访问方式 | 地址 | 说明 |
|---------|------|------|
| **浏览器 → 前端容器** | `localhost:3001` | 主机端口映射 |
| **浏览器 → 后端容器** | `localhost:5001` | 主机端口映射 |
| **前端容器 → 后端容器** | `backend:5000` | Docker 网络（如果需要） |
| **后端容器 → 数据库容器** | `postgres:5432` | Docker 网络 |

## 🔍 验证配置

### 1. 检查端口映射

```bash
# 查看容器端口映射
sudo docker ps

# 输出示例
CONTAINER ID   PORTS                    NAMES
abc123         0.0.0.0:3001->3000/tcp   maf-studio-frontend
def456         0.0.0.0:5001->5000/tcp   maf-studio-backend
```

### 2. 测试网络连接

```bash
# 从主机访问后端
curl http://localhost:5001/api/health

# 从前端容器访问后端（如果需要）
sudo docker exec maf-studio-frontend curl http://backend:5000/api/health

# 从前端容器访问主机（Linux）
sudo docker exec maf-studio-frontend curl http://host.docker.internal:5001/api/health
```

### 3. 检查浏览器请求

打开浏览器开发者工具：
1. 访问 `http://localhost:3001`
2. 查看 Network 标签
3. 检查 API 请求地址是否为 `http://localhost:5001/api`

## ⚠️ 常见问题

### 问题1: 前端无法访问后端 API

**症状**：浏览器控制台显示网络错误

**可能原因**：
1. 后端未启动
2. 端口映射错误
3. CORS 配置问题

**解决方案**：
```bash
# 1. 检查后端是否运行
sudo docker ps | grep backend

# 2. 测试后端 API
curl http://localhost:5001/api/health

# 3. 检查 CORS 配置（后端）
# 确保 backend 允许前端域名访问
```

### 问题2: 容器内服务无法访问主机

**症状**：容器内的服务无法连接到主机上的服务

**解决方案**：
```bash
# Linux: 使用 host.docker.internal
# 需要在 docker-compose.yml 中添加:
extra_hosts:
  - "host.docker.internal:host-gateway"

# 然后在容器内使用:
# http://host.docker.internal:5001
```

### 问题3: 容器间无法通信

**症状**：前端容器无法访问后端容器

**解决方案**：
```bash
# 1. 确保在同一网络
sudo docker network inspect maf-network

# 2. 使用服务名访问
# backend:5000 (不是 localhost:5001)

# 3. 检查网络配置
sudo docker exec maf-studio-frontend ping backend
```

## 📚 总结

### 当前配置（正确）

| 组件 | 运行位置 | 访问地址 |
|------|---------|---------|
| 前端代码 | 浏览器 | - |
| 前端静态文件 | 容器 | `localhost:3001` |
| 后端 API | 容器 | `localhost:5001` |

### 关键理解

1. ✅ **前端是 SPA**：代码在浏览器中运行，使用 `localhost:5001` 访问后端
2. ✅ **端口映射**：主机端口 → 容器端口
3. ✅ **浏览器 localhost**：指向主机，不是容器

### 何时需要修改

- ❌ **当前不需要修改**：配置已经正确
- ✅ **如果使用 SSR**：需要配置容器间通信
- ✅ **如果容器内访问主机**：需要使用 `host.docker.internal`

## 🎓 最佳实践

1. **明确代码运行位置**：浏览器 vs 容器
2. **使用正确的地址**：
   - 浏览器访问：`localhost:主机端口`
   - 容器间通信：`服务名:容器端口`
3. **配置 CORS**：后端需要允许前端域名
4. **文档化网络架构**：记录端口映射和网络配置

---

**当前配置完全正确，无需修改！** 🎉
