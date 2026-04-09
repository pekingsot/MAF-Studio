# ==================== MAF Studio 前端基础镜像 ====================
# 包含: Node.js 18, serve (静态文件服务器)
# 用途: 作为前端应用镜像的基础镜像，加快构建速度

FROM node:18-alpine

LABEL maintainer="MAF Studio Team"
LABEL description="MAF Studio Frontend Base - Node.js 18 with serve"
LABEL version="1.0"

WORKDIR /app

# 安装构建工具和运行时工具
RUN apk add --no-cache \
    python3 \
    make \
    g++ \
    git \
    curl \
    bash \
    htop \
    vim

# 全局安装 serve（静态文件服务器）
RUN npm install -g serve

# 创建静态文件目录
RUN mkdir -p /app/build && \
    chmod 777 /app/build

# 设置环境变量
ENV NODE_ENV=production \
    PORT=3000

# 验证安装
RUN node --version && \
    npm --version && \
    git --version && \
    serve --version && \
    echo "✓ 前端基础环境安装完成"

# 暴露端口
EXPOSE 3000

# 默认命令
CMD ["serve", "-s", "build", "-l", "3000"]
