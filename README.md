# 🌞 太阳与影子 — 小学科学互动实验

基于团结引擎 WebGL + FastAPI + MySQL 的课堂互动实验系统。面向小学 3~4 年级科学课程（教科版）。

## 功能特性

- 🔬 **Unity WebGL 互动实验**：拖动时间滑块，实时观察太阳位置与影子变化
- 📝 **12 组学生答题**：10 道选择题/填空题/简答题，平板友好
- 🖥️ **教师大屏实时监控**：WebSocket 推送，12 宫格进度 + 排行榜
- 🌍 **季节切换**：春夏秋冬太阳高度角不同
- 📊 **数据持久化**：MySQL 存储答题记录

## 快速开始

### 前置依赖
- Python 3.8+
- MySQL 8.0
- Nginx (含 brotli 模块)
- 团结引擎 1.9.0 (Unity 2022.3.62t8)

### 一键部署

```bash
bash deploy.sh
```

### Unity 构建

详见 [unity-project/README.md](unity-project/README.md)

## 项目结构

```
sun-shadow-experiment/
├── unity-project/          # 团结引擎工程（C# 脚本 + 场景指南）
│   ├── Assets/_Project/
│   │   ├── Scripts/        # C# 脚本
│   │   ├── Scenes/
│   │   ├── Materials/
│   │   └── Prefabs/
│   └── README.md           # Unity 搭建详细指南
├── backend/                # FastAPI 后端
│   ├── server.py           # API + WebSocket
│   ├── models.py           # SQLAlchemy 数据模型
│   ├── database.py         # MySQL 初始化 + 种子数据
│   └── requirements.txt
├── frontend/               # 原生 HTML/CSS/JS
│   ├── index.html          # 首页
│   ├── experiment.html     # Unity WebGL 嵌入页
│   ├── student.html        # 学生答题端
│   ├── teacher.html        # 教师大屏
│   ├── css/style.css
│   └── js/
│       ├── student.js
│       └── teacher.js
├── nginx/
│   └── nginx-sun-shadow.conf
├── static/                 # Unity WebGL 构建产物（gitignore）
│   ├── Build/
│   └── TemplateData/
├── deploy.sh               # 一键部署脚本
├── PLAN.md                 # 完整项目规划
└── .gitignore
```

## URL 结构

| 端点 | 用途 |
|------|------|
| `/sun-shadow/` | 首页导航 |
| `/sun-shadow/experiment` | Unity WebGL 实验 |
| `/sun-shadow/student?group=1` | 学生答题 |
| `/sun-shadow/teacher` | 教师大屏 |

## 技术栈

| 层 | 技术 |
|---|------|
| 3D 引擎 | 团结引擎 1.9.0 / WebGL + Brotli |
| 后端 | FastAPI + WebSocket |
| 数据库 | MySQL 8.0 |
| 前端 | 原生 HTML/CSS/JS（零构建依赖）|
| 部署 | Nginx + deploy.sh |

## 许可证

MIT License — 仅供教育用途
