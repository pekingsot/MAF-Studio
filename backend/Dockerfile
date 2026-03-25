# ==================== 构建阶段 ====================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 复制项目文件并还原依赖（利用Docker缓存层）
COPY ["MAFStudio.Backend.csproj", "./"]
RUN dotnet restore "MAFStudio.Backend.csproj"

# 复制所有源代码
COPY . .

# 构建项目
RUN dotnet build "MAFStudio.Backend.csproj" -c Release -o /app/build --no-restore

# ==================== 发布阶段 ====================
FROM build AS publish
RUN dotnet publish "MAFStudio.Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# ==================== 运行时阶段 ====================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# 创建非root用户运行应用
RUN adduser --disabled-password --gecos '' appuser

# 安装curl用于健康检查
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# 复制发布文件
COPY --from=publish /app/publish .

# 设置文件所有权
RUN chown -R appuser:appuser /app

# 切换到非root用户
USER appuser

# 暴露端口
EXPOSE 5000

# 设置环境变量
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# 健康检查
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# 启动应用
ENTRYPOINT ["dotnet", "MAFStudio.Backend.dll"]
