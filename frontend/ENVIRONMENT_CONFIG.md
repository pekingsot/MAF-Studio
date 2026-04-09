# 前端配置说明

## 🎯 配置概述

前端使用环境变量配置后端 API 地址，支持开发环境和生产环境。

## 📁 环境变量文件

### 1. 开发环境 (`.env.development`)

```env
# 后端 API 地址（开发环境）
REACT_APP_API_URL=http://localhost:5001/api

# WebSocket 地址
REACT_APP_WS_URL=ws://localhost:5001/ws

# 环境
NODE_ENV=development
```

**使用场景**：
- 本地开发 (`npm start`)
- 前端开发服务器代理到后端

### 2. 生产环境 (`.env.production`)

```env
# 后端 API 地址（生产环境/Docker）
REACT_APP_API_URL=http://localhost:5001/api

# WebSocket 地址
REACT_APP_WS_URL=ws://localhost:5001/ws

# 环境
NODE_ENV=production
```

**使用场景**：
- Docker 部署
- 生产环境构建

### 3. 本地配置 (`.env.local`)

```env
# 复制 .env.example 为 .env.local 并修改
REACT_APP_API_URL=http://localhost:5001/api
REACT_APP_WS_URL=ws://localhost:5001/ws
```

**使用场景**：
- 个人本地配置（不提交到 Git）
- 覆盖默认配置

## 🔧 配置说明

### API 地址配置

前端通过环境变量 `REACT_APP_API_URL` 配置后端 API 地址：

```typescript
// src/services/api.ts
const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5001/api';
```

### 端口映射

| 服务 | 容器端口 | 主机端口 | 访问地址 |
|------|---------|---------|---------|
| 后端 API | 5000 | 5001 | http://localhost:5001 |
| 前端应用 | 3000 | 3001 | http://localhost:3001 |

### 开发环境代理

开发环境下，前端使用 `package.json` 中的 `proxy` 配置：

```json
{
  "proxy": "http://localhost:5001"
}
```

这样前端开发服务器会自动代理 API 请求到后端。

## 🚀 使用方法

### 本地开发

```bash
# 1. 启动后端（在 backend 目录）
cd backend
dotnet run

# 2. 启动前端（在 frontend 目录）
cd frontend
npm start

# 访问
# 前端: http://localhost:3000
# 后端: http://localhost:5001
```

### Docker 部署

```bash
# 1. 构建镜像
sudo docker compose build

# 2. 启动服务
sudo docker compose up -d

# 访问
# 前端: http://localhost:3001
# 后端: http://localhost:5001
```

## 📝 重要说明

### 1. 环境变量优先级

React 应用读取环境变量的优先级：

1. `.env.local` (最高优先级，不提交到 Git)
2. `.env.development.local` / `.env.production.local`
3. `.env.development` / `.env.production`
4. `.env` (最低优先级)

### 2. 构建时注入

React 应用的环境变量是在**构建时**注入的，不是运行时：

```bash
# 构建时会读取 .env.production
npm run build

# 构建产物中的 API 地址是固定的
```

### 3. Docker 构建注意事项

`.dockerignore` 文件配置：

```
# 排除本地配置
.env
.env.local
.env.development.local
.env.test.local
.env.production.local

# 保留环境配置
# .env.development 和 .env.production 会被复制到构建环境
```

### 4. 浏览器访问

前端是 React SPA，运行在浏览器中：

- ✅ 浏览器访问前端：`http://localhost:3001`
- ✅ 浏览器请求后端 API：`http://localhost:5001/api`
- ❌ 不是容器间通信（前端不在容器内运行代码）

## 🔍 故障排查

### API 请求失败

**问题**：前端无法访问后端 API

**检查**：
1. 后端是否启动：`curl http://localhost:5001/api/health`
2. 前端环境变量是否正确：检查 `.env.development` 或 `.env.production`
3. 浏览器控制台是否有 CORS 错误

**解决方案**：
```bash
# 检查环境变量
cat frontend/.env.development

# 重新构建前端
cd frontend
npm run build
```

### WebSocket 连接失败

**问题**：WebSocket 无法连接

**检查**：
1. 后端 WebSocket 端点是否可用
2. `REACT_APP_WS_URL` 配置是否正确

**解决方案**：
```env
# .env.development
REACT_APP_WS_URL=ws://localhost:5001/ws
```

### Docker 构建后 API 地址错误

**问题**：Docker 容器中的前端访问错误的后端地址

**原因**：构建时使用了错误的环境变量文件

**解决方案**：
1. 确保 `.env.production` 文件存在
2. 确保 `.dockerignore` 没有排除 `.env.production`
3. 重新构建镜像：`sudo docker compose build --no-cache frontend`

## 📚 相关文件

- [前端 Dockerfile](file:///home/pekingost/projects/maf-studio/frontend/Dockerfile)
- [API 配置](file:///home/pekingost/projects/maf-studio/frontend/src/services/api.ts)
- [Docker Compose 配置](file:///home/pekingost/projects/maf-studio/docker-compose.yml)
- [Docker 部署指南](file:///home/pekingost/projects/maf-studio/DOCKER_README.md)
