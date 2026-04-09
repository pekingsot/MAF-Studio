# ==================== 完整基础镜像构建脚本 ====================
# 用途: 一次性构建所有基础镜像（后端+前端）
# 使用: ./build-all-base-images.sh

set -e

echo "=========================================="
echo "  MAF Studio 完整基础镜像构建脚本"
echo "=========================================="
echo ""

# 颜色定义
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 检查 Docker
if ! command -v docker &> /dev/null; then
    echo "错误: Docker 未安装"
    exit 1
fi

echo -e "${BLUE}本脚本将构建以下基础镜像:${NC}"
echo "  1. maf-studio-base (后端基础镜像)"
echo "  2. maf-studio-frontend-build-base (前端构建基础镜像)"
echo "  3. maf-studio-frontend-runtime-base (前端运行时基础镜像)"
echo ""

read -p "是否继续? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "已取消"
    exit 0
fi

echo ""
echo -e "${YELLOW}[1/3] 构建后端基础镜像...${NC}"
./build-base-image.sh

echo ""
echo -e "${YELLOW}[2/3] 构建前端基础镜像...${NC}"
./build-frontend-base-image.sh

echo ""
echo -e "${YELLOW}[3/3] 显示所有构建的镜像...${NC}"
sudo docker images | grep maf-studio

echo ""
echo -e "${GREEN}=========================================="
echo "  所有基础镜像构建完成！"
echo "==========================================${NC}"
echo ""
echo "下一步:"
echo "  1. 构建应用镜像: sudo docker compose build"
echo "  2. 启动服务: sudo docker compose up -d"
echo ""
