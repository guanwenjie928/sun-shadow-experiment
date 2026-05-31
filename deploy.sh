#!/bin/bash
# ═══════════════════════════════════════════════════════════
# 太阳与影子 — 一键部署脚本
# ==========================================================
# 用法: bash deploy.sh
# 前提: bash deploy.sh 已在项目根目录执行
# ═══════════════════════════════════════════════════════════
set -e

# ─── 配置 ──────────────────────────────────────────────
PROJECT_UUID="ceb33133-7d4d-4bc7-aa6b-5fda9fbb8831"
PROJECT_NAME="sun-shadow-experiment"
DEPLOY_BASE="/data/${PROJECT_UUID}"
BACKEND_PORT=9005

# 检测当前脚本所在目录（项目根目录）
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="${SCRIPT_DIR}"

echo "╔════════════════════════════════════════════════════╗"
echo "║   🌞 太阳与影子实验 — 部署脚本                    ║"
echo "╚════════════════════════════════════════════════════╝"
echo ""
echo "  项目 UUID: ${PROJECT_UUID}"
echo "  部署路径: ${DEPLOY_BASE}/${PROJECT_NAME}"
echo "  后端端口: ${BACKEND_PORT}"
echo ""

# ─── 1. 复制文件 ─────────────────────────────────────
echo "[1/5] 复制项目文件到部署目录..."
mkdir -p "${DEPLOY_BASE}/${PROJECT_NAME}"
rsync -av --progress \
  --exclude '.git' \
  --exclude '__pycache__' \
  --exclude '*.pyc' \
  --exclude '.DS_Store' \
  --exclude 'unity-project/Library' \
  --exclude 'unity-project/Temp' \
  --exclude 'unity-project/Obj' \
  --exclude 'unity-project/Logs' \
  "${PROJECT_ROOT}/" "${DEPLOY_BASE}/${PROJECT_NAME}/"

# ─── 2. 安装 Python 依赖 ─────────────────────────────
echo ""
echo "[2/5] 安装 Python 依赖..."
cd "${DEPLOY_BASE}/${PROJECT_NAME}/backend"

# 确保 pip 可用
pip3 install --quiet --upgrade pip 2>/dev/null || true
pip3 install -r requirements.txt --quiet

# ─── 3. 初始化数据库 ─────────────────────────────────
echo ""
echo "[3/5] 初始化 MySQL 数据库..."
python3 -c "
from database import init_db
init_db()
print('Database initialized OK')
"

# ─── 4. 启动/重启后端服务 ─────────────────────────────
echo ""
echo "[4/5] 启动 FastAPI 后端服务..."

# 先停掉旧进程
OLD_PID=$(lsof -ti:${BACKEND_PORT} 2>/dev/null || true)
if [ -n "$OLD_PID" ]; then
  echo "  停止旧进程 PID=${OLD_PID}..."
  kill -9 $OLD_PID 2>/dev/null || true
  sleep 1
fi

# 启动新进程（后台）
cd "${DEPLOY_BASE}/${PROJECT_NAME}/backend"
nohup python3 server.py > /tmp/sun-shadow-server.log 2>&1 &
NEW_PID=$!
echo "  后端服务已启动 PID=${NEW_PID}，日志: /tmp/sun-shadow-server.log"

# 等 2 秒验证
sleep 2
if lsof -ti:${BACKEND_PORT} > /dev/null 2>&1; then
  echo "  ✅ 后端服务运行正常 (port ${BACKEND_PORT})"
else
  echo "  ⚠️  后端服务可能启动失败，查看日志: tail /tmp/sun-shadow-server.log"
fi

# ─── 5. 重载 Nginx ───────────────────────────────────
echo ""
echo "[5/5] 重载 Nginx 配置..."

# 检查 nginx 配置是否已存在
if [ ! -f /etc/nginx/conf.d/sun-shadow.conf ]; then
  echo "  首次部署，复制 nginx 配置..."

  # 替换配置中的 UUID 占位符
  sed "s/__PROJECT_UUID__/${PROJECT_UUID}/g" \
    "${PROJECT_ROOT}/nginx/nginx-sun-shadow.conf" \
    > /etc/nginx/conf.d/sun-shadow.conf
fi

# 验证配置
nginx -t && systemctl reload nginx
echo "  ✅ Nginx 已重载"

# ─── 完成 ────────────────────────────────────────────
echo ""
echo "╔════════════════════════════════════════════════════╗"
echo "║   🎉 部署完成！                                   ║"
echo "║                                                    ║"
echo "║   实验页:   /${PROJECT_UUID}/sun-shadow/           ║"
echo "║   学生端:   /${PROJECT_UUID}/sun-shadow/student    ║"
echo "║   教师端:   /${PROJECT_UUID}/sun-shadow/teacher    ║"
echo "║                                                    ║"
echo "║   外部访问: 通过网关域名 + 上述路径               ║"
echo "║   后端日志: tail -f /tmp/sun-shadow-server.log    ║"
echo "╚════════════════════════════════════════════════════╝"
