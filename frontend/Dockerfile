# ==================== 构建阶段 ====================
FROM node:18-alpine AS build
WORKDIR /app

# 复制package文件（利用Docker缓存层）
COPY package*.json ./
RUN npm ci --only=production

# 复制源代码
COPY . .

# 构建生产版本
RUN npm run build

# ==================== 运行时阶段 ====================
FROM nginx:alpine AS final

# 安装curl用于健康检查
RUN apk add --no-cache curl

# 复制构建产物
COPY --from=build /app/build /usr/share/nginx/html

# 复制nginx配置
COPY nginx.conf /etc/nginx/conf.d/default.conf

# 创建nginx缓存目录
RUN mkdir -p /var/cache/nginx && \
    chown -R nginx:nginx /var/cache/nginx && \
    chown -R nginx:nginx /usr/share/nginx/html

# 暴露端口
EXPOSE 80

# 健康检查
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:80 || exit 1

# 启动nginx
CMD ["nginx", "-g", "daemon off;"]
