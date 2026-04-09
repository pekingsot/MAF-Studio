# ==================== MAF Studio 基础环境镜像 ====================
# 包含: .NET 10.0, Python 3.12, Git, 常用工具
# 用途: 作为应用镜像的基础镜像，加快构建速度

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS base

LABEL maintainer="MAF Studio Team"
LABEL description="MAF Studio Base Environment - .NET 10.0, Python 3.12, Git"
LABEL version="1.0"

WORKDIR /app

# 安装系统依赖和工具
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    # 版本控制
    git \
    # SSH 客户端
    openssh-client \
    # Python 环境
    python3.12 \
    python3-pip \
    python3.12-venv \
    # 常用工具
    curl \
    wget \
    vim \
    nano \
    htop \
    procps \
    net-tools \
    iputils-ping \
    # 构建工具
    build-essential \
    # 清理缓存
    && rm -rf /var/lib/apt/lists/* \
    # 创建 Python 软链接
    && ln -sf /usr/bin/python3.12 /usr/bin/python3 \
    && ln -sf /usr/bin/python3.12 /usr/bin/python

# 创建工作目录
RUN mkdir -p /app/workspace && \
    chmod 777 /app/workspace

# 设置环境变量
ENV ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_ENVIRONMENT=Production \
    Workspace__BaseDir=/app/workspace \
    PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1

# 验证安装
RUN dotnet --version && \
    git --version && \
    python3 --version && \
    echo "✓ 基础环境安装完成"

# 暴露端口
EXPOSE 5000

# 默认命令
CMD ["bash"]
