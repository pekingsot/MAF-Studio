# ==================== 构建阶段 ====================
FROM maf-studio-frontend-base:latest AS build
WORKDIR /app

# 构建阶段需要安装 devDependencies
ENV NODE_ENV=development

# 复制package文件（利用Docker缓存层）
COPY package*.json ./
RUN npm ci

# 复制源代码
COPY . .

# 构建生产版本
RUN npm run build

# ==================== 运行时阶段 ====================
FROM maf-studio-frontend-base:latest AS final
WORKDIR /app

# 复制构建产物
COPY --from=build /app/build ./build

# 暴露端口
EXPOSE 3000

# 健康检查
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:3000 || exit 1

# 启动 serve
CMD ["serve", "-s", "build", "-l", "3000"]
