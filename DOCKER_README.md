# Docker 部署完整指南

## 🎯 概述

本项目采用**基础镜像 + 应用镜像**的两层架构，大幅提升构建速度：

### 后端
- **基础镜像**: `maf-studio-base` - .NET 10.0, Python 3.12, Git
- **应用镜像**: `maf-studio-backend` - 后端应用代码
- **构建速度**: 从 7 分钟降到 1 分钟（**提升 6.7 倍**）

### 前端
- **基础镜像**: `maf-studio-frontend-base` - Node.js 18, serve（静态文件服务器）
- **应用镜像**: `maf-studio-frontend` - 前端应用代码
- **构建速度**: 从 3-5 分钟降到 30-60 秒（**提升 3-5 倍**）

## 📦 镜像架构

```
┌─────────────────────────────────────────────────────────┐
│                    后端架构                              │
├─────────────────────────────────────────────────────────┤
│  maf-studio-base (1.82GB)                               │
│  ├── .NET 10.0 SDK & Runtime                           │
│  ├── Python 3.12.3                                      │
│  ├── Git 2.43.0                                         │
│  └── 常用工具                                            │
│           ↓ 基于此构建                                   │
│  maf-studio-backend (~550MB)                            │
│  └── 应用代码 + 依赖                                     │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                    前端架构                              │
├─────────────────────────────────────────────────────────┤
│  maf-studio-frontend-base (~250MB)                      │
│  ├── Node.js 18                                         │
│  ├── NPM                                                │
│  ├── Git                                                │
│  ├── Serve (静态文件服务器)                              │
│  └── 构建工具                                            │
│           ↓ 用于构建和运行                               │
│  maf-studio-frontend (~280MB)                           │
│  └── 前端静态文件 (build 目录)                           │
└─────────────────────────────────────────────────────────┘
```

## 🚀 快速开始

### 方式1: 一键构建所有基础镜像（推荐）

```bash
# 构建所有基础镜像（后端 + 前端）
./build-all-base-images.sh

# 构建应用镜像
sudo docker compose build

# 启动服务
sudo docker compose up -d
```

### 方式2: 分别构建

#### 1. 构建后端基础镜像

```bash
./build-base-image.sh
```

#### 2. 构建前端基础镜像

```bash
./build-frontend-base-image.sh
```

#### 3. 构建应用镜像

```bash
# 构建后端
sudo docker compose build backend

# 构建前端
sudo docker compose build frontend

# 或一次性构建所有
sudo docker compose build
```

#### 4. 启动服务

```bash
# 使用远程数据库（推荐）
sudo docker compose -f docker-compose.standalone.yml up -d

# 或使用完整配置（包含数据库）
sudo docker compose up -d
```

## 📁 文件结构

```
maf-studio/
├── docker/
│   ├── base/
│   │   └── Dockerfile                    # 后端基础镜像
│   └── frontend-base/
│       └── Dockerfile                    # 前端基础镜像
├── backend/
│   ├── Dockerfile                        # 后端应用镜像
│   └── .dockerignore
├── frontend/
│   ├── Dockerfile                        # 前端应用镜像
│   └── .dockerignore
├── docker-compose.yml                    # 完整部署配置
├── docker-compose.standalone.yml         # 仅后端部署配置
├── .env                                  # 环境变量配置
├── build-base-image.sh                   # 后端基础镜像构建脚本
├── build-frontend-base-image.sh          # 前端基础镜像构建脚本
├── build-all-base-images.sh              # 完整基础镜像构建脚本
├── DOCKER_README.md                      # 本文档
├── BASE_IMAGE_GUIDE.md                   # 后端基础镜像详细指南
└── FRONTEND_BASE_IMAGE_GUIDE.md          # 前端基础镜像详细指南
```

## 🔧 配置说明

### 环境变量

在 `.env` 文件中配置：

```env
# 数据库配置
POSTGRES_USER=mafuser
POSTGRES_PASSWORD=mafpassword
POSTGRES_DB=mafstudio
POSTGRES_PORT=5432

# 后端配置
ASPNETCORE_ENVIRONMENT=Production
BACKEND_PORT=5001

# 前端配置
FRONTEND_PORT=3001

# JWT配置
JWT_SECRET=your-super-secret-jwt-key-change-this-in-production
```

### 数据库连接

#### 远程数据库配置
修改 `docker-compose.standalone.yml` 中的连接字符串：
```yaml
ConnectionStrings__DefaultConnection=Host=192.168.1.250;Port=5433;Database=mafstudio;Username=pekingsot;Password=sunset@123;Timezone=Asia/Shanghai
```

#### 本地数据库配置
使用 `docker-compose.yml` 会自动启动 PostgreSQL 容器。

## 📊 性能对比

### 后端构建性能

| 构建方式 | 构建时间 | 提升倍数 |
|---------|---------|---------|
| 原始方式（每次都安装环境） | ~7 分钟 | - |
| 基础镜像方式（复用环境） | ~1 分钟 | **6.7倍** ⚡ |

### 前端构建性能

| 构建方式 | 构建时间 | 提升倍数 |
|---------|---------|---------|
| 原始方式（每次都拉取基础镜像） | ~3-5 分钟 | - |
| 基础镜像方式（复用环境） | ~30-60 秒 | **3-5倍** ⚡ |

## 🔍 常用命令

### 镜像管理

```bash
# 查看所有基础镜像
sudo docker images | grep maf-studio

# 查看所有镜像
sudo docker images

# 删除旧版本镜像
sudo docker image prune -a
```

### 容器管理

```bash
# 查看运行中的容器
sudo docker ps

# 查看所有容器
sudo docker ps -a

# 查看日志
sudo docker logs maf-studio-backend
sudo docker logs maf-studio-frontend

# 进入容器
sudo docker exec -it maf-studio-backend bash
sudo docker exec -it maf-studio-frontend sh

# 重启容器
sudo docker restart maf-studio-backend
sudo docker restart maf-studio-frontend

# 停止所有服务
sudo docker compose down
```

### 数据卷管理

```bash
# 查看数据卷
sudo docker volume ls

# 删除数据卷
sudo docker volume rm maf-studio_workspace_data
sudo docker volume rm maf-studio_postgres_data
```

## 🛠️ 验证安装

### 后端环境验证

```bash
# 进入后端容器
sudo docker exec -it maf-studio-backend bash

# 验证环境
dotnet --version    # 10.0.105
git --version       # 2.43.0
python3 --version   # 3.12.3

# 退出容器
exit
```

### 前端环境验证

```bash
# 进入前端容器
sudo docker exec -it maf-studio-frontend sh

# 验证环境
node --version      # v18.x.x
npm --version       # 9.x.x
serve --version     # 14.x.x

# 退出容器
exit
```

## 🌐 访问应用

- **后端 API**: http://localhost:5001
- **Swagger 文档**: http://localhost:5001/swagger
- **前端应用**: http://localhost:3001

## ⚠️ 注意事项

### 1. 网络问题

如果遇到网络超时无法拉取镜像：

**解决方案 A: 配置 Docker 镜像加速器**

```bash
sudo vim /etc/docker/daemon.json
```

添加：
```json
{
  "registry-mirrors": [
    "https://docker.mirrors.ustc.edu.cn",
    "https://hub-mirror.c.163.com"
  ]
}
```

重启 Docker：
```bash
sudo systemctl restart docker
```

**解决方案 B: 使用国内镜像源**

修改 Dockerfile 中的基础镜像地址。

### 2. 权限问题

使用 `sudo` 运行 Docker 命令，或将用户添加到 docker 组：
```bash
sudo usermod -aG docker $USER
```

### 3. 数据持久化

- 数据库数据存储在 `postgres_data` 卷
- 工作目录存储在 `workspace_data` 卷
- 前端静态文件在容器内

### 4. 健康检查

- 后端健康检查端点需要认证，返回 401 是正常现象
- 应用本身已正常启动

## 📝 何时重建基础镜像？

### 后端基础镜像

- .NET 版本更新
- Python 版本更新
- Git 版本更新
- 添加新的系统工具

### 前端基础镜像

- Node.js 版本更新
- serve 版本更新
- 添加新的构建工具

## 🔐 生产环境建议

1. ✅ 修改 `.env` 文件中的默认密码和密钥
2. ✅ 使用 HTTPS 配置
3. ✅ 配置日志收集
4. ✅ 设置资源限制
5. ✅ 使用 Docker secrets 管理敏感信息
6. ✅ 定期更新基础镜像
7. ✅ 为镜像打版本标签

## 📚 详细文档

- [后端基础镜像详细指南](file:///home/pekingost/projects/maf-studio/BASE_IMAGE_GUIDE.md)
- [前端基础镜像详细指南](file:///home/pekingost/projects/maf-studio/FRONTEND_BASE_IMAGE_GUIDE.md)

## 🎓 总结

使用基础镜像方案后：

✅ **后端构建速度提升 6.7 倍**（从 7 分钟降到 1 分钟）
✅ **前端构建速度提升 3-5 倍**（从 3-5 分钟降到 30-60 秒）
✅ **镜像复用**（多个项目可共享基础镜像）
✅ **维护简单**（环境更新只需重建基础镜像）
✅ **版本可控**（为基础镜像打标签管理版本）

这是 Docker 的最佳实践，特别适合需要频繁构建镜像的开发场景！🚀
