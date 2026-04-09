# Docker 基础镜像方案

## 🎯 方案概述

为了加快 Docker 镜像构建速度，我们采用了**基础镜像 + 应用镜像**的两层架构：

1. **基础镜像 (maf-studio-base)**: 包含所有运行时环境和工具
2. **应用镜像 (maf-studio-backend)**: 基于基础镜像，仅包含应用代码

## 📊 性能对比

| 构建方式 | 构建时间 | 提升倍数 |
|---------|---------|---------|
| **原始方式**（每次都安装环境） | ~7 分钟 (428秒) | - |
| **基础镜像方式**（复用环境） | ~1 分钟 (64秒) | **6.7倍** ⚡ |

## 🏗️ 架构设计

```
┌─────────────────────────────────────────┐
│   maf-studio-base (基础镜像)              │
│   - .NET 10.0 SDK & Runtime             │
│   - Python 3.12                          │
│   - Git 2.43.0                           │
│   - 常用工具 (curl, vim, htop...)        │
│   大小: 1.82GB                           │
│   构建频率: 很少（仅环境更新时）           │
└─────────────────────────────────────────┘
                    ↓ 基于基础镜像构建
┌─────────────────────────────────────────┐
│   maf-studio-backend (应用镜像)          │
│   - 应用代码                             │
│   - 依赖包                               │
│   大小: ~550MB                           │
│   构建频率: 经常（每次代码更新）           │
└─────────────────────────────────────────┘
```

## 📁 文件结构

```
maf-studio/
├── docker/
│   └── base/
│       └── Dockerfile              # 基础镜像构建文件
├── backend/
│   └── Dockerfile                  # 应用镜像构建文件（基于基础镜像）
├── build-base-image.sh             # 基础镜像构建脚本
└── docker-compose.yml              # Docker Compose 配置
```

## 🚀 使用指南

### 1. 首次使用或环境更新

构建基础镜像（只需执行一次）：

```bash
# 方式1: 使用构建脚本（推荐）
./build-base-image.sh

# 方式2: 手动构建
sudo docker build -t maf-studio-base:latest -f docker/base/Dockerfile docker/base
```

**构建时间**: 约 1-2 分钟

**何时需要重建基础镜像**:
- .NET 版本更新
- Python 版本更新
- Git 版本更新
- 添加新的系统工具

### 2. 构建应用镜像

每次代码更新后构建应用镜像：

```bash
# 构建后端镜像
sudo docker compose build backend

# 或使用完整命令
sudo docker build -t maf-studio-backend:latest -f backend/Dockerfile backend
```

**构建时间**: 约 1 分钟 ⚡

### 3. 启动服务

```bash
# 使用远程数据库
sudo docker compose -f docker-compose.standalone.yml up -d

# 或使用完整配置（包含数据库）
sudo docker compose up -d
```

## 🔧 基础镜像内容

### 包含的环境

| 工具 | 版本 | 用途 |
|------|------|------|
| .NET SDK | 10.0-preview | 后端开发和运行 |
| .NET Runtime | 10.0-preview | 应用运行时 |
| Python | 3.12.3 | 脚本和工具支持 |
| Git | 2.43.0 | 版本控制 |
| curl | latest | HTTP 客户端 |
| vim/nano | latest | 文本编辑器 |
| htop | latest | 系统监控 |

### 预设环境变量

```bash
ASPNETCORE_URLS=http://+:5000
ASPNETCORE_ENVIRONMENT=Production
Workspace__BaseDir=/app/workspace
PYTHONUNBUFFERED=1
PYTHONDONTWRITEBYTECODE=1
```

### 预设目录

- `/app` - 应用根目录
- `/app/workspace` - 工作目录（可读写）
- `/src` - 源代码目录（构建时使用）

## 💡 最佳实践

### 1. 镜像版本管理

为基础镜像打标签，方便版本回退：

```bash
# 构建带版本标签的基础镜像
sudo docker build -t maf-studio-base:1.0 -t maf-studio-base:latest -f docker/base/Dockerfile docker/base

# 查看所有版本
sudo docker images maf-studio-base
```

### 2. 定期更新基础镜像

```bash
# 每月或季度更新一次基础镜像
./build-base-image.sh

# 重新构建应用镜像
sudo docker compose build --no-cache backend
```

### 3. 多项目共享基础镜像

如果有多个 .NET 项目，可以共享同一个基础镜像：

```dockerfile
# 项目A的 Dockerfile
FROM maf-studio-base:latest
# ... 项目A的构建步骤

# 项目B的 Dockerfile
FROM maf-studio-base:latest
# ... 项目B的构建步骤
```

### 4. 本地缓存优化

Docker 会自动缓存镜像层，利用这一点加速构建：

```bash
# 第一次构建（慢）
sudo docker compose build backend

# 后续构建（快，利用缓存）
sudo docker compose build backend

# 强制重新构建（不使用缓存）
sudo docker compose build --no-cache backend
```

## 📈 性能优化建议

### 1. 减小基础镜像大小

如果不需要某些工具，可以修改 `docker/base/Dockerfile`：

```dockerfile
# 只安装必需的工具
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    git \
    python3.12 \
    curl \
    && rm -rf /var/lib/apt/lists/*
```

### 2. 使用多阶段构建

应用镜像已经使用多阶段构建，只保留运行时必需的文件：

```dockerfile
# 构建阶段
FROM maf-studio-base:latest AS build
# ... 构建步骤

# 运行时阶段（更小）
FROM maf-studio-base:latest AS final
# ... 只复制构建产物
```

### 3. 清理未使用的镜像

```bash
# 查看所有镜像
sudo docker images

# 删除旧的镜像版本
sudo docker image prune -a

# 删除特定镜像
sudo docker rmi maf-studio-backend:old-version
```

## 🔍 故障排查

### 基础镜像不存在

**错误信息**: `pull access denied for maf-studio-base`

**解决方案**:
```bash
# 先构建基础镜像
./build-base-image.sh
```

### 构建速度仍然很慢

**可能原因**:
1. Docker 缓存未生效
2. 网络问题导致依赖下载慢

**解决方案**:
```bash
# 检查 Docker 缓存
sudo docker system df

# 清理缓存后重新构建
sudo docker system prune -a
./build-base-image.sh
sudo docker compose build backend
```

### 镜像过大

**解决方案**:
```bash
# 查看镜像层详情
sudo docker history maf-studio-backend:latest

# 优化 Dockerfile，减少层数
# 合并多个 RUN 命令
```

## 🎓 学习资源

- [Docker 多阶段构建](https://docs.docker.com/build/building/multi-stage/)
- [Docker 最佳实践](https://docs.docker.com/develop/develop-images/dockerfile_best-practices/)
- [.NET Docker 指南](https://docs.microsoft.com/zh-cn/dotnet/core/docker/)

## 📝 总结

使用基础镜像方案后：

✅ **构建速度提升 6.7 倍**（从 7 分钟降到 1 分钟）
✅ **镜像复用**（多个项目可共享基础镜像）
✅ **维护简单**（环境更新只需重建基础镜像）
✅ **版本可控**（为基础镜像打标签管理版本）

这是一个值得推荐的 Docker 最佳实践！🚀
