# 部署指南

## 环境要求

- Docker 20.10+
- Docker Compose 2.0+
- .NET 8.0 SDK (本地开发)
- Node.js 18+ (本地开发)

## 快速部署

### 1. 克隆项目

```bash
cd d:/trae/maf-studio
```

### 2. 配置环境变量

复制环境变量示例文件：

```bash
cp backend/.env.example backend/.env
```

根据需要修改 `.env` 文件中的配置。

### 3. 启动服务

使用 Docker Compose 启动所有服务：

```bash
docker-compose up -d
```

### 4. 访问应用

- 前端应用: http://localhost:3000
- 后端 API: http://localhost:5000
- Swagger 文档: http://localhost:5000/swagger
- PostgreSQL: localhost:5432

## 本地开发

### 后端开发

```bash
cd backend
dotnet restore
dotnet build
dotnet run
```

### 前端开发

```bash
cd frontend
npm install
npm start
```

### 数据库迁移

```bash
cd backend
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Docker Compose 服务说明

### PostgreSQL

- 端口: 5432
- 数据库: mafstudio
- 用户名: postgres
- 密码: postgres

### Backend

- 端口: 5000
- 框架: ASP.NET Core 8.0
- 依赖: PostgreSQL

### Frontend

- 端口: 3000
- 框架: React 18 + Ant Design
- 依赖: Backend

## 常用命令

### 查看日志

```bash
# 查看所有服务日志
docker-compose logs -f

# 查看特定服务日志
docker-compose logs -f backend
docker-compose logs -f frontend
docker-compose logs -f postgres
```

### 停止服务

```bash
docker-compose down
```

### 重启服务

```bash
docker-compose restart
```

### 清理数据

```bash
# 停止并删除容器、网络、卷
docker-compose down -v

# 删除所有镜像
docker-compose down --rmi all
```

## 故障排除

### 端口冲突

如果端口被占用，修改 `docker-compose.yml` 中的端口映射：

```yaml
ports:
  - "5001:5000"  # 修改为其他端口
```

### 数据库连接失败

检查 PostgreSQL 容器是否正常运行：

```bash
docker-compose ps postgres
```

### 前端无法连接后端

检查 CORS 配置和环境变量：

```bash
# 检查后端 CORS 配置
docker-compose exec backend env | grep Cors

# 检查前端 API URL
docker-compose exec frontend env | grep REACT_APP_API_URL
```

## 生产环境部署

### 1. 使用生产配置

修改 `docker-compose.yml` 中的环境变量：

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
```

### 2. 使用 HTTPS

配置 Nginx 反向代理和 SSL 证书。

### 3. 数据库备份

定期备份 PostgreSQL 数据：

```bash
docker-compose exec postgres pg_dump -U postgres mafstudio > backup.sql
```

### 4. 监控和日志

使用 Docker 日志驱动或集成监控系统（如 Prometheus、Grafana）。

## 更新部署

### 拉取最新代码

```bash
git pull
```

### 重新构建和部署

```bash
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

## 安全建议

1. 修改默认数据库密码
2. 使用环境变量管理敏感信息
3. 启用 HTTPS
4. 配置防火墙规则
5. 定期更新依赖包
6. 实施访问控制和认证机制