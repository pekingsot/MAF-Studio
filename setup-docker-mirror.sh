#!/bin/bash

# ==================== 配置 Docker 镜像加速器 ====================
# 用途: 配置 Docker 使用国内镜像加速器，解决拉取镜像超时问题
# 使用: sudo ./setup-docker-mirror.sh

set -e

echo "=========================================="
echo "  Docker 镜像加速器配置脚本"
echo "=========================================="
echo ""

# 颜色定义
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# 检查是否以 root 运行
if [ "$EUID" -ne 0 ]; then 
    echo -e "${RED}请使用 sudo 运行此脚本${NC}"
    exit 1
fi

echo -e "${YELLOW}[1/4] 备份现有配置...${NC}"
if [ -f /etc/docker/daemon.json ]; then
    cp /etc/docker/daemon.json /etc/docker/daemon.json.backup.$(date +%Y%m%d_%H%M%S)
    echo "✓ 已备份现有配置"
else
    echo "✓ 现有配置不存在，将创建新配置"
fi

echo ""
echo -e "${YELLOW}[2/4] 创建 Docker 配置文件...${NC}"

# 创建配置文件
cat > /etc/docker/daemon.json <<EOF
{
  "registry-mirrors": [
    "https://docker.m.daocloud.io",
    "https://dockerhub.azk8s.cn",
    "https://hub-mirror.c.163.com",
    "https://mirror.baidubce.com"
  ],
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  },
  "storage-driver": "overlay2"
}
EOF

echo "✓ 配置文件已创建"

echo ""
echo -e "${YELLOW}[3/4] 重启 Docker 服务...${NC}"
systemctl daemon-reload
systemctl restart docker

echo "✓ Docker 服务已重启"

echo ""
echo -e "${YELLOW}[4/4] 验证配置...${NC}"
if docker info | grep -q "Registry Mirrors"; then
    echo -e "${GREEN}✓ 镜像加速器配置成功！${NC}"
    echo ""
    echo "配置的镜像源："
    docker info | grep -A 5 "Registry Mirrors"
else
    echo -e "${RED}✗ 配置可能未生效，请检查${NC}"
fi

echo ""
echo -e "${GREEN}=========================================="
echo "  Docker 镜像加速器配置完成！"
echo "==========================================${NC}"
echo ""
echo "下一步:"
echo "  1. 重新构建前端基础镜像: ./build-frontend-base-image.sh"
echo "  2. 构建应用镜像: sudo docker compose build"
echo "  3. 启动服务: sudo docker compose up -d"
