# 前端基础镜像方案（使用 serve）

## 🎯 方案概述

前端采用**基础镜像 + 应用镜像**的两层架构：

1. **基础镜像 (maf-studio-frontend-base)**: 包含 Node.js 18 和 serve（静态文件服务器）
2. **应用镜像 (maf-studio-frontend)**: 基于基础镜像，仅包含前端代码

## 📊 性能提升

| 构建方式 | 预期构建时间 | 提升倍数 |
|---------|-------------|---------|
| **原始方式**（每次都拉取基础镜像） | ~3-5 分钟 | - |
| **基础镜像方式**（复用环境） | ~30-60 秒 | **3-5倍** ⚡ |

## 🏗️ 架构设计

```
┌─────────────────────────────────────────┐
│   maf-studio-frontend-base              │
│   (前端基础镜像)                          │
│   - Node.js 18                          │
│   - NPM                                  │
│   - Git                                  │
│   - Serve (静态文件服务器)                │
│   - 构建工具 (make, g++, python3)        │
│   大小: ~250MB                           │
│   构建频率: 很少（仅环境更新时）           │
└─────────────────────────────────────────┘
                    ↓ 用于构建和运行
┌─────────────────────────────────────────┐
│   maf-studio-frontend (应用镜像)         │
│   - 前端静态文件 (build 目录)            │
│   - 使用 serve 提供静态文件服务           │
│   大小: ~280MB                           │
│   构建频率: 经常（每次代码更新）           │
└─────────────────────────────────────────┘
```

## 📁 文件结构

```
maf-studio/
├── docker/
│   ├── base/
│   │   └── Dockerfile              # 后端基础镜像
│   └── frontend-base/
│       └── Dockerfile              # 前端基础镜像
├── frontend/
│   └── Dockerfile                  # 前端应用镜像（基于基础镜像）
├── build-base-image.sh             # 后端基础镜像构建脚本
└── build-frontend-base-image.sh    # 前端基础镜像构建脚本
```

## 🚀 使用指南

### 1. 首次使用或环境更新

构建前端基础镜像（只需执行一次）：

```bash
# 使用构建脚本（推荐）
./build-frontend-base-image.sh

# 或手动构建
sudo docker build -t maf-studio-frontend-base:latest \
  -f docker/frontend-base/Dockerfile \
  docker/frontend-base
```

**构建时间**: 约 1-2 分钟

**何时需要重建前端基础镜像**:
- Node.js 版本更新
- serve 版本更新
- 添加新的构建工具

### 2. 构建应用镜像

每次代码更新后构建应用镜像：

```bash
# 构建前端镜像
sudo docker compose build frontend

# 或使用完整命令
sudo docker build -t maf-studio-frontend:latest -f frontend/Dockerfile frontend
```

**预期构建时间**: 约 30-60 秒 ⚡

### 3. 启动服务

```bash
# 启动所有服务
sudo docker compose up -d

# 或分别启动
sudo docker compose up -d backend
sudo docker compose up -d frontend
```

## 🔧 基础镜像内容

### 前端基础镜像 (maf-studio-frontend-base)

| 工具 | 版本 | 用途 |
|------|------|------|
| Node.js | 18.x | JavaScript 运行时 |
| NPM | latest | 包管理器 |
| Git | latest | 版本控制 |
| Serve | latest | 静态文件服务器 |
| Python3 | latest | 构建依赖 |
| Make | latest | 构建工具 |
| G++ | latest | C++ 编译器 |

## 💡 serve 的优势

### 1. 简单易用

```bash
# 一行命令启动静态文件服务器
serve -s build -l 3000
```

### 2. 轻量级

- 基于 Node.js，无需额外安装 Nginx
- 镜像大小更小（~250MB vs Nginx 方案的 ~225MB）
- 配置简单，无需复杂的 Nginx 配置文件

### 3. 功能完整

- ✅ 静态文件服务
- ✅ SPA 路由支持（自动重定向到 index.html）
- ✅ CORS 支持
- ✅ 缓存控制
- ✅ Gzip 压缩

### 4. 生产就绪

serve 是一个成熟的生产级静态文件服务器，被广泛使用：
- GitHub Stars: 9k+
- 周下载量: 100万+
- 维护活跃

## ⚠️ 网络问题解决方案

如果遇到网络超时无法拉取基础镜像，可以使用以下方案：

### 方案1: 配置 Docker 镜像加速器

编辑 Docker 配置文件：

```bash
sudo vim /etc/docker/daemon.json
```

添加镜像加速器：

```json
{
  "registry-mirrors": [
    "https://docker.mirrors.ustc.edu.cn",
    "https://hub-mirror.c.163.com",
    "https://mirror.ccs.tencentyun.com"
  ]
}
```

重启 Docker：

```bash
sudo systemctl daemon-reload
sudo systemctl restart docker
```

### 方案2: 使用国内镜像源

修改 `docker/frontend-base/Dockerfile`：

```dockerfile
# 使用阿里云镜像
FROM registry.cn-hangzhou.aliyuncs.com/library/node:18-alpine
```

### 方案3: 手动拉取镜像

```bash
# 使用代理或镜像加速器手动拉取
sudo docker pull node:18-alpine

# 然后再构建基础镜像
./build-frontend-base-image.sh
```

## 💡 最佳实践

### 1. 镜像版本管理

为前端基础镜像打标签：

```bash
# 构建带版本标签的基础镜像
sudo docker build -t maf-studio-frontend-base:1.0 -t maf-studio-frontend-base:latest \
  -f docker/frontend-base/Dockerfile \
  docker/frontend-base
```

### 2. 优化构建缓存

利用 Docker 缓存加速构建：

```dockerfile
# 先复制 package.json，安装依赖
COPY package*.json ./
RUN npm ci --only=production

# 再复制源代码（代码变更不影响依赖缓存）
COPY . .
RUN npm run build
```

### 3. 多环境配置

为不同环境创建不同的配置：

```bash
# 开发环境（包含更多工具）
maf-studio-frontend-base:dev

# 生产环境（最小化）
maf-studio-frontend-base:prod
```

### 4. serve 配置选项

可以自定义 serve 的行为：

```dockerfile
# 添加 CORS 支持
CMD ["serve", "-s", "build", "-l", "3000", "--cors"]

# 禁用缓存
CMD ["serve", "-s", "build", "-l", "3000", "--no-clipboard", "-n"]

# 自定义配置文件
CMD ["serve", "-s", "build", "-l", "3000", "-c", "serve.json"]
```

## 📈 性能优化建议

### 1. 减小镜像大小

```dockerfile
# 使用 Alpine 基础镜像（更小）
FROM node:18-alpine

# 清理缓存
RUN npm ci --only=production && \
    npm cache clean --force
```

### 2. 使用多阶段构建

前端已经使用多阶段构建：

```dockerfile
# 构建阶段
FROM maf-studio-frontend-base AS build
# ... 构建步骤

# 运行时阶段（使用同一个基础镜像）
FROM maf-studio-frontend-base AS final
# ... 只复制构建产物
```

### 3. 优化构建产物

在 `package.json` 中配置构建优化：

```json
{
  "scripts": {
    "build": "react-app-rewired build"
  }
}
```

## 🔍 故障排查

### 基础镜像不存在

**错误信息**: `pull access denied for maf-studio-frontend-base`

**解决方案**:
```bash
# 先构建基础镜像
./build-frontend-base-image.sh
```

### 网络超时

**错误信息**: `i/o timeout`

**解决方案**:
```bash
# 配置镜像加速器（见上文）
# 或使用国内镜像源
```

### 构建失败

**错误信息**: `npm install failed`

**解决方案**:
```bash
# 清理 npm 缓存
sudo docker run --rm maf-studio-frontend-base npm cache clean --force

# 重新构建
sudo docker compose build --no-cache frontend
```

### serve 启动失败

**错误信息**: `serve: command not found`

**解决方案**:
```bash
# 验证 serve 是否安装
sudo docker run --rm maf-studio-frontend-base serve --version

# 如果未安装，重新构建基础镜像
./build-frontend-base-image.sh
```

## 📊 与 Nginx 方案对比

| 特性 | Nginx 方案 | serve 方案 |
|------|-----------|-----------|
| **镜像大小** | ~225MB (构建+运行时) | ~250MB (单一镜像) |
| **配置复杂度** | 高（需要 Nginx 配置文件） | 低（一行命令） |
| **性能** | 极高（C 语言实现） | 高（Node.js 实现） |
| **功能** | 非常丰富（反向代理、负载均衡等） | 基础功能（静态文件服务） |
| **适用场景** | 复杂生产环境 | 简单部署、开发环境 |
| **维护成本** | 中等（需要维护配置文件） | 低（无需配置） |

## 📝 总结

前端基础镜像方案的优势：

✅ **构建速度提升 3-5 倍**（从 3-5 分钟降到 30-60 秒）
✅ **配置简单**（无需 Nginx 配置文件）
✅ **镜像统一**（构建和运行时使用同一基础镜像）
✅ **易于维护**（环境更新只需重建基础镜像）
✅ **适合简单部署**（serve 提供足够的静态文件服务功能）

与后端方案配合使用，可以大幅提升整个项目的 Docker 构建效率！🚀
