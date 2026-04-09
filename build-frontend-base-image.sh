#!/bin/bash

# ==================== 构建前端基础环境镜像 ====================
# 用途: 构建包含 Node.js 18 和 serve 的基础镜像
# 使用: ./build-frontend-base-image.sh

set -e

echo "=========================================="
echo "  MAF Studio 前端基础环境镜像构建脚本"
echo "=========================================="
echo ""

# 颜色定义
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 镜像信息
IMAGE_NAME="maf-studio-frontend-base"
IMAGE_TAG="latest"
DOCKERFILE_PATH="docker/frontend-base/Dockerfile"

echo -e "${YELLOW}[1/4] 检查 Docker 环境...${NC}"
if ! command -v docker &> /dev/null; then
    echo "错误: Docker 未安装"
    exit 1
fi
echo "✓ Docker 已安装"

echo ""
echo -e "${YELLOW}[2/4] 检查基础镜像文件...${NC}"
if [ ! -f "$DOCKERFILE_PATH" ]; then
    echo "错误: Dockerfile 不存在于 $DOCKERFILE_PATH"
    exit 1
fi
echo "✓ Dockerfile 存在"

echo ""
echo -e "${YELLOW}[3/4] 构建前端基础镜像...${NC}"
echo "镜像名称: $IMAGE_NAME:$IMAGE_TAG"
echo "Dockerfile: $DOCKERFILE_PATH"
echo ""

# 构建镜像
sudo docker build \
    -t "$IMAGE_NAME:$IMAGE_TAG" \
    -f "$DOCKERFILE_PATH" \
    --label "build-date=$(date -Iseconds)" \
    --label "version=1.0" \
    docker/frontend-base

echo ""
echo -e "${YELLOW}[4/4] 验证镜像...${NC}"

# 验证镜像
if sudo docker images | grep -q "$IMAGE_NAME"; then
    echo -e "${GREEN}✓ 前端基础镜像构建成功！${NC}"
    echo ""
    echo "镜像信息:"
    sudo docker images "$IMAGE_NAME:$IMAGE_TAG"
    echo ""
    echo "测试镜像环境:"
    echo "  Node.js 版本:"
    sudo docker run --rm "$IMAGE_NAME:$IMAGE_TAG" node --version
    echo "  NPM 版本:"
    sudo docker run --rm "$IMAGE_NAME:$IMAGE_TAG" npm --version
    echo "  Git 版本:"
    sudo docker run --rm "$IMAGE_NAME:$IMAGE_TAG" git --version
    echo "  Serve 版本:"
    sudo docker run --rm "$IMAGE_NAME:$IMAGE_TAG" serve --version
    echo ""
    echo -e "${GREEN}=========================================="
    echo "  前端基础镜像构建完成！"
    echo "==========================================${NC}"
    echo ""
    echo "下一步:"
    echo "  1. 构建前端应用镜像: sudo docker compose build frontend"
    echo "  2. 启动服务: sudo docker compose up -d"
else
    echo "错误: 镜像构建失败"
    exit 1
fi
