# 🌞 太阳与影子 · 小学物理互动实验 — 完整规划

## 一、项目概述

基于团结引擎（Tuanjie 1.9.0 / Unity 2022.3.62t8）WebGL + FastAPI + MySQL 的课堂互动实验系统。学生通过平板链接观看实验并答题，教师在大屏实时查看 12 个小组答题情况。

---

## 二、教学目标（对齐 2022 版课标 · 教科版三年级下册）

| 知识点 | 实验交互 |
|--------|----------|
| 影子的产生条件（光源 + 阻挡物 + 屏）| 可切换太阳/手电筒，移除物体验证 |
| 影子方向与光源方向相反 | 拖动时间滑块，观察影子方向变化 |
| 一天中影子长短变化（长→短→长）| 太阳东升西落弧线运动，记录影子长度 |
| 正午影子最短 | 拖动到 12:00，突出显示最短影子 |
| 太阳高度角与影子关系 | 影子长度数值实时显示 |

---

## 三、技术架构

```
                        ┌──────────────────────────┐
                        │    Nginx (:8080)           │
                        │  /sun-shadow/ → 路由分发    │
                        └─────┬──────────┬──────────┘
                              │          │
              ┌───────────────┘          └───────────────┐
              ▼                                          ▼
┌──────────────────────────┐          ┌──────────────────────────┐
│  静态文件 (Nginx 直出)     │          │  FastAPI (:9005)          │
│  ├─ Unity WebGL (Brotli) │          │  ├─ REST API              │
│  ├─ experiment.html      │          │  ├─ WebSocket (/ws)       │
│  ├─ student.html         │          │  └─ MySQL 连接池           │
│  ├─ teacher.html         │          └──────────┬───────────────┘
│  └─ index.html           │                     │
└──────────────────────────┘                     ▼
                                    ┌──────────────────────────┐
                                    │  MySQL 8.0                │
                                    │  ├─ quiz_sessions         │
                                    │  ├─ answers               │
                                    │  └─ groups                │
                                    └──────────────────────────┘
```

---

## 四、URL 设计

| 端点 | 用途 | 访问者 |
|------|------|--------|
| `/sun-shadow/` | 首页导航 | 所有人 |
| `/sun-shadow/experiment` | Unity WebGL 实验 | 学生（预习/体验）|
| `/sun-shadow/student?group=1` | 学生答题页 | 12 组学生平板 |
| `/sun-shadow/teacher` | 教师大屏仪表盘 | 教师电脑 |

---

## 五、数据库设计（MySQL）

```sql
quiz_sessions:  id, teacher_name, status, started_at, ended_at
groups:         id, session_id, group_number(1-12), device_token
questions:      id, content, type, options(JSON), correct_answer, score
answers:        id, session_id, group_number, question_id, answer, is_correct, score, submitted_at
```

---

## 六、Unity 场景设计

### 场景元素
- 卡通低面数操场（草地 + 跑道 + 围墙）
- 旗杆（主影子物体）+ 小树（辅助对比）
- 太阳（Directional Light 沿天空弧线运动）
- 地面标尺（从旗杆底部向东/西延伸，刻度 == 实际影子长度测量）
- UI 面板：时间滑块（6:00-18:00）、季节切换按钮、重置按钮、影子长度数值

### 免费素材来源
| 素材 | 来源 | 用途 |
|------|------|------|
| Nature Pack Extended | Kenney.nl (CC0) | 树木、草地、岩石 |
| Farland Skies - Low Poly | Unity Asset Store (Free) | 天空盒 + 日夜循环 |
| Simple Town Lite | Synty/Unity Asset Store (Free) | 操场围栏、建筑元素 |
| Nature Kit | Kenney.nl (CC0) | 补充植被、地形元素 |

### C# 脚本
1. **SunController.cs** — 太阳沿弧线运动（基于时间参数 t=0→1 映射 6:00→18:00）
2. **ShadowMeasurement.cs** — 计算并显示地板上的影子长度
3. **TimeSliderUI.cs** — 控制时间滑块 UI 与太阳同步
4. **SeasonToggle.cs** — 切换春夏秋冬（改变太阳弧线高度）

---

## 七、答题系统设计

### 题目（10 题，覆盖课标要求）
1. 影子产生需要什么条件？（多选：光源/阻挡物/屏/风）
2. 早上 8 点，旗杆影子指向哪个方向？（单选）
3. 一天中什么时候影子最短？（单选）
4. 太阳升得越高，影子会___？（填空）
5. 拖动太阳到正午 12:00，影子长度是___米？（互动题）
6. 下午影子的方向与上午相比___？（单选：相同/相反/不确定）
7. 阴天能看到清晰的影子吗？为什么？（简答）
8. 冬天和夏天，同一时刻影子长短一样吗？（单选 + 操作验证）
9. 如果太阳在物体的正上方，影子会在哪里？（单选）
10. 古代人用什么工具利用影子计时？（单选：日晷/沙漏/水钟）

### 教师仪表盘
- 12 宫格布局，一屏展示所有小组
- 每组显示：组号、已答/未答状态、实时得分
- 答题完成后自动汇总排名
- 可导出成绩为 Excel 表格

---

## 八、部署方案

### 部署步骤
1. 本机构建 Unity WebGL（用户用团结引擎本地 build）
2. 产物复制到服务器 `/data/{uuid}/sun-shadow/static/`
3. 执行 `deploy.sh` 一键部署（配置 nginx、启动 FastAPI、初始化 MySQL）
4. 通过网关域名 + UUID 访问

### Nginx 关键配置
- Unity WebGL 文件配置 `Content-Encoding: br` 响应头（Brotli）
- `/sun-shadow/api/` 代理到 FastAPI :9005
- `/sun-shadow/ws/` WebSocket 升级
- 静态文件直出

---

## 九、存储体积控制

| 策略 | 预期 |
|------|------|
| .gitignore 排除 Unity Library/Temp/Obj/Build 源目录 | 源码 < 15MB |
| Unity 构建产物用独立目录存放，Git LFS 或单独上传 | 构建产物 ~25MB |
| 前端用原生 JS/CSS（无 node_modules）| 前端 < 500KB |
| 后端 Python 纯代码 + requirements.txt | 后端 < 200KB |
| **仓库总大小** | **< 30MB** |

---

## 十、待确认清单

- [x] 腾讯云服务器 IP：`110.41.68.2`（广州）
- [x] GitHub 用户名：`guanwenjie928`
- [x] 域名/网关：`0e8b324e-...hwgz.zhique.cn`（平台自动分配）
- [x] 数据库：MySQL 8.0（持久化答题数据）
- [x] Unity：团结引擎 1.9.0，用户本地构建
- [x] 美术：免费低面数素材（Kenney + Unity Store）
- [x] 仓库名：`sun-shadow-experiment`，公开仓库
- [x] 12 小组答题 + 教师大屏实时展示
