# ==================== 构建阶段 ====================
FROM maf-studio-base:latest AS build
WORKDIR /src

# 复制 csproj 文件并还原依赖（利用 Docker 缓存层）
COPY ["src/MAFStudio.Api/MAFStudio.Api.csproj", "MAFStudio.Api/"]
COPY ["src/MAFStudio.Application/MAFStudio.Application.csproj", "MAFStudio.Application/"]
COPY ["src/MAFStudio.Core/MAFStudio.Core.csproj", "MAFStudio.Core/"]
COPY ["src/MAFStudio.Infrastructure/MAFStudio.Infrastructure.csproj", "MAFStudio.Infrastructure/"]
RUN dotnet restore "MAFStudio.Api/MAFStudio.Api.csproj"

# 复制所有源代码
COPY src/ .

# 构建项目
WORKDIR "/src/MAFStudio.Api"
RUN dotnet build "MAFStudio.Api.csproj" -c Release -o /app/build

# 发布项目
FROM build AS publish
RUN dotnet publish "MAFStudio.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==================== 运行时阶段 ====================
FROM maf-studio-base:latest AS final
WORKDIR /app

# 复制构建产物
COPY --from=publish /app/publish .

# 复制数据库迁移脚本
COPY src/MAFStudio.Api/Scripts /app/Scripts

# 设置环境变量
ENV ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_ENVIRONMENT=Production \
    Workspace__BaseDir=/app/workspace

# 暴露端口
EXPOSE 5000

# 健康检查
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# 启动应用
ENTRYPOINT ["dotnet", "MAFStudio.Api.dll"]
