# 本地开发指南

## 环境要求

### 必需软件
- .NET 8.0 SDK
- Node.js 18+
- PostgreSQL 客户端（可选，用于数据库管理）

### 数据库
- PostgreSQL 服务器: 192.168.1.250:5433
- 数据库名: mafstudio
- 用户名: pekingsot
- 密码: sunset@123

## 快速开始

### 1. 安装依赖

#### 后端依赖
```bash
cd backend
dotnet restore
```

#### 前端依赖
```bash
cd frontend
npm install
```

### 2. 启动项目

#### 方式1：使用启动脚本（推荐）
```bash
start-local.bat
```

#### 方式2：手动启动

**启动后端：**
```bash
cd backend
dotnet run
```

**启动前端：**
```bash
cd frontend
npm start
```

### 3. 访问应用

- 前端应用: http://localhost:3000
- 后端 API: http://localhost:5000
- Swagger 文档: http://localhost:5000/swagger

## 数据库配置

### 连接信息
- **主机**: 192.168.1.250
- **端口**: 5433
- **数据库**: mafstudio
- **用户名**: pekingsot
- **密码**: sunset@123

### 连接字符串
```
Host=192.168.1.250;Port=5433;Database=mafstudio;Username=pekingsot;Password=sunset@123
```

### 数据库初始化

首次运行时，需要创建数据库和表结构：

```bash
cd backend
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## 开发说明

### 后端开发

#### 项目结构
```
backend/
├── Data/                    # 数据模型
├── Services/               # 业务逻辑
├── Hubs/                   # SignalR Hub
├── Controllers/            # API 控制器
├── Program.cs             # 应用入口
└── appsettings.json       # 配置文件
```

#### 常用命令
```bash
# 还原依赖
dotnet restore

# 编译项目
dotnet build

# 运行项目
dotnet run

# 添加数据库迁移
dotnet ef migrations add MigrationName

# 更新数据库
dotnet ef database update

# 清理项目
dotnet clean
```

#### 调试
- 使用 Visual Studio 或 VS Code 调试
- 设置断点进行调试
- 查看控制台输出和日志

### 前端开发

#### 项目结构
```
frontend/
├── src/
│   ├── components/       # React 组件
│   ├── pages/            # 页面组件
│   ├── services/         # API 服务
│   ├── App.tsx
│   └── index.tsx
├── public/
├── package.json
└── tsconfig.json
```

#### 常用命令
```bash
# 安装依赖
npm install

# 启动开发服务器
npm start

# 构建生产版本
npm run build

# 运行测试
npm test

# 弹出配置
npm run eject
```

#### 热重载
- 修改代码后自动重新编译
- 浏览器自动刷新
- 保持状态（大部分情况）

## 配置说明

### 后端配置 (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=192.168.1.250;Port=5433;Database=mafstudio;Username=pekingsot;Password=sunset@123"
  },
  "AgentSettings": {
    "MaxAgents": 100,
    "DefaultTimeout": 30000,
    "EnableAutoDiscovery": true
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:3001"
    ]
  }
}
```

### 前端配置

前端通过环境变量配置：

```bash
# API 地址
REACT_APP_API_URL=http://localhost:5000/api

# Socket 地址
REACT_APP_SOCKET_URL=http://localhost:5000
```

## 常见问题

### 1. 数据库连接失败

**问题**: 无法连接到 PostgreSQL 数据库

**解决方案**:
- 检查数据库服务器是否运行
- 验证连接信息是否正确
- 检查网络连接
- 确认防火墙设置

### 2. 端口被占用

**问题**: 端口 5000 或 3000 被占用

**解决方案**:
```bash
# 查看端口占用
netstat -ano | findstr :5000
netstat -ano | findstr :3000

# 终止占用进程
taskkill /PID <进程ID> /F
```

### 3. 依赖安装失败

**问题**: npm 或 dotnet restore 失败

**解决方案**:
```bash
# 清除 npm 缓存
npm cache clean --force

# 清除 dotnet 缓存
dotnet nuget locals all --clear

# 重新安装
rm -rf node_modules
npm install
```

### 4. CORS 错误

**问题**: 前端无法访问后端 API

**解决方案**:
- 检查后端 CORS 配置
- 确认前端地址在允许列表中
- 检查浏览器控制台错误信息

### 5. SignalR 连接失败

**问题**: 实时通信无法连接

**解决方案**:
- 检查后端 SignalR 配置
- 确认 WebSocket 支持已启用
- 验证 Socket URL 配置

## 调试技巧

### 后端调试

1. **日志输出**
   - 使用 `ILogger` 记录日志
   - 设置适当的日志级别
   - 查看控制台输出

2. **断点调试**
   - 在 IDE 中设置断点
   - 启动调试模式
   - 逐步执行代码

3. **API 测试**
   - 使用 Swagger UI 测试 API
   - 使用 Postman 或 curl 测试
   - 验证请求和响应

### 前端调试

1. **浏览器开发者工具**
   - 使用 Chrome DevTools
   - 查看 Console 和 Network 标签
   - 检查 React DevTools

2. **React DevTools**
   - 安装 React DevTools 扩展
   - 查看组件状态和 props
   - 追踪状态变化

3. **网络请求**
   - 查看 Network 标签
   - 检查请求和响应
   - 验证 API 调用

## 性能优化

### 后端优化

1. **数据库查询优化**
   - 使用适当的索引
   - 避免 N+1 查询问题
   - 使用分页

2. **异步处理**
   - 使用 async/await
   - 避免阻塞调用
   - 使用 CancellationToken

3. **缓存策略**
   - 实现内存缓存
   - 使用分布式缓存
   - 设置合理的过期时间

### 前端优化

1. **代码分割**
   - 使用 React.lazy()
   - 路由级别分割
   - 组件级别分割

2. **性能监控**
   - 使用 React Profiler
   - 监控渲染性能
   - 优化重渲染

3. **资源优化**
   - 压缩图片和资源
   - 使用 CDN
   - 启用 Gzip 压缩

## 测试

### 后端测试

```bash
# 运行单元测试
dotnet test

# 运行集成测试
dotnet test --filter "Category=Integration"
```

### 前端测试

```bash
# 运行所有测试
npm test

# 运行特定测试
npm test -- --testNamePattern="Agent"

# 生成覆盖率报告
npm test -- --coverage
```

## 部署到生产环境

### 后端部署

1. **构建发布版本**
   ```bash
   cd backend
   dotnet publish -c Release -o ./publish
   ```

2. **部署到服务器**
   - 复制发布文件到服务器
   - 配置生产环境变量
   - 配置反向代理（Nginx/Apache）

3. **配置数据库**
   - 使用生产数据库连接字符串
   - 运行数据库迁移
   - 配置备份策略

### 前端部署

1. **构建生产版本**
   ```bash
   cd frontend
   npm run build
   ```

2. **部署到服务器**
   - 复制 build 目录到服务器
   - 配置 Web 服务器
   - 配置 HTTPS

3. **环境变量**
   - 设置生产 API 地址
   - 配置其他环境变量
   - 验证配置

## 停止服务

使用停止脚本：
```bash
stop-local.bat
```

或手动停止：
```bash
# 停止后端
Ctrl+C 在后端终端

# 停止前端
Ctrl+C 在前端终端
```

## 开发工作流

1. **拉取最新代码**
   ```bash
   git pull
   ```

2. **安装依赖**
   ```bash
   cd backend && dotnet restore
   cd ../frontend && npm install
   ```

3. **启动服务**
   ```bash
   start-local.bat
   ```

4. **开发功能**
   - 修改代码
   - 测试功能
   - 提交代码

5. **代码审查**
   - 创建 Pull Request
   - 代码审查
   - 合并到主分支

## 有用的资源

### 官方文档
- [.NET 文档](https://docs.microsoft.com/dotnet/)
- [React 文档](https://react.dev/)
- [Ant Design 文档](https://ant.design/)
- [Entity Framework Core 文档](https://docs.microsoft.com/ef/core/)

### 工具
- [PostgreSQL 官网](https://www.postgresql.org/)
- [pgAdmin](https://www.pgadmin.org/) - PostgreSQL 管理工具
- [Postman](https://www.postman.com/) - API 测试工具
- [DBeaver](https://dbeaver.io/) - 数据库管理工具

## 获取帮助

如遇到问题：
1. 查看本文档的常见问题部分
2. 检查项目日志
3. 查看相关技术文档
4. 提交 Issue 到项目仓库