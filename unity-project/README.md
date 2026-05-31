# Unity 项目搭建指南

## 环境信息
- **引擎**: 团结引擎 (Tuanjie) 1.9.0
- **对应 Unity 版本**: 2022.3.62t8
- **渲染管线**: URP (Universal Render Pipeline)
- **目标平台**: WebGL

---

## 步骤 1：在 Tuanjie Hub 创建新项目

1. 打开 Tuanjie Hub
2. 点击「新建项目」
3. 模板选择：**3D (URP)**
4. 项目名称：`SunShadowExperiment`
5. 项目路径：任意（建议放在本仓库的 `unity-project/` 下）
6. 点击创建

## 步骤 2：导入免费素材（全部免费）

### 2.1 Kenney Nature Pack Extended（CC0 树木/石头）
- 下载地址：https://kenney.nl/assets/nature-pack-extended
- 下载 `.zip`，在 Tuanjie 中：`Assets → Import Package → Custom Package` 导入

### 2.2 Farland Skies - Low Poly（天空盒 + 日夜循环）
- 打开 Tuanjie → `Window → Asset Store`
- 搜索 `Farland Skies - Low Poly`（免费）
- 点击 Download → Import

### 2.3 Simple Sky & Simple Town Lite（可选补充素材）
- Asset Store 搜索 `Simple Sky` (Synty, 免费)
- Asset Store 搜索 `Simple Town Lite` (Synty, 免费)
- 作为操场场景元素的补充

## 步骤 3：导入项目脚本

将本仓库 `unity-project/Assets/_Project/` 整个文件夹复制到你 Tuanjie 项目的 `Assets/` 目录下。

目录结构应为：
```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── SunController.cs
│   │   ├── ShadowMeasurement.cs
│   │   ├── TimeSliderUI.cs
│   │   ├── SeasonToggle.cs
│   │   └── GameManager.cs
│   ├── Scenes/
│   ├── Materials/
│   └── Prefabs/
├── Farland Skies/   (导入后)
└── Kenney/          (导入后)
```

## 步骤 4：搭建场景

### 4.1 基础场景
1. 创建场景：`File → New Scene`，保存到 `Assets/_Project/Scenes/MainScene.unity`
2. 删除默认 Directional Light

### 4.2 地面
1. `GameObject → 3D Object → Plane`，重命名为 `Ground`
2. Scale = (10, 1, 10)，Position = (0, 0, 0)
3. 创建 Material：`Assets/_Project/Materials/Ground.mat`
   - 颜色：草绿色 `#7EC850`
   - 拖到 Plane 上

### 4.3 旗杆（影子物体）
1. `GameObject → 3D Object → Cylinder`，重命名为 `Flagpole`
2. Scale = (0.15, 5, 0.15)，Position = (0, 2.5, 0)
3. 创建子对象作为顶杆：`GameObject → 3D Object → Cylinder`
   - Scale = (2, 0.08, 0.08)，Position = (1, 5, 0)
4. 将 `ShadowMeasurement.cs` 脚本拖到 Flagpole 上
   - `Pole Top` 拖入旗杆顶部 Transform
   - `Pole Bottom` 拖入旗杆底部 Transform
   - `Object Height` = 5
   - `Ground Y` = 0

### 4.4 影子指示器
1. `GameObject → 3D Object → Sphere`，重命名为 `ShadowIndicator`
2. Scale = (0.2, 0.05, 0.2)
3. 创建黑色 Material 拖上去
4. 拖到 Flagpole 上 `ShadowMeasurement.Shadow Indicator` 字段

### 4.5 太阳（Directional Light）
1. `GameObject → Light → Directional Light`，重命名为 `Sun`
2. Position = (0, 50, 0)
3. 将 `SunController.cs` 脚本拖到 Sun 上

### 4.6 天空
- 从 Assets 中找到 Farland Skies 的 Prefab，拖入场景
- 或者手动设置：`Window → Rendering → Lighting` → Environment → Skybox Material 设置为 Farland Skies 的材质

### 4.7 场景装饰
- 从 Kenney Nature Pack 拖入：几棵树、石头、栅栏
- 放在旗杆周围作为操场场景

## 步骤 5：搭建 UI

1. `GameObject → UI → Canvas`（自动创建 Canvas + EventSystem）
2. Canvas → Render Mode = `Screen Space - Overlay`
3. Canvas Scaler → UI Scale Mode = `Scale With Screen Size`
   - Reference Resolution = 1920 × 1080
4. 添加时间标签：
   - `GameObject → UI → Text - TextMeshPro`（如未安装 TMP，先导入 TMP Essentials）
   - 放在顶部中央，重命名为 `TimeLabel`
5. 添加影子长度标签：
   - 同上，放在顶部右侧，重命名为 `ShadowLengthText`
6. 添加季节标签：
   - 同上，放在顶部左侧，重命名为 `SeasonLabel`
7. 添加时间滑块：
   - `GameObject → UI → Slider`
   - 放在底部，重命名为 `TimeSlider`
   - 将 `TimeSliderUI.cs` 拖到 TimeSlider 上
8. 添加快捷按钮（4 个跳转按钮 + 自动演示 + 4 个季节）：

```
按钮布局（底部）：

[🌸春] [☀️夏] [🍂秋] [❄️冬]  ───  [🌅早] [☀️午] [🌤️午] [🌇傍] [▶自动]
```

9. 创建空 GameObject `GameManager`，将 `GameManager.cs` 拖上去
   - 把所有组件拖入对应字段

### UI 绑定清单

| 脚本 | 字段 | 绑定目标 |
|------|------|----------|
| ShadowMeasurement | Shadow Length Text | ShadowLengthText (TMPro) |
| ShadowMeasurement | Time Text | TimeLabel (TMPro) |
| ShadowMeasurement | Altitude Text | 新建 AltitudeText (TMPro) |
| ShadowMeasurement | Shadow Indicator | ShadowIndicator (Sphere) |
| TimeSliderUI | Time Slider | TimeSlider |
| TimeSliderUI | Time Label | TimeLabel |
| TimeSliderUI | Shadow Hint Text | 新建 HintText (TMPro) |
| SeasonToggle | Season Buttons | 4 个季节 Button |
| SeasonToggle | Season Label | SeasonLabel |
| GameManager | Sun Controller | Sun GameObject |
| GameManager | Shadow Measurement | Flagpole |
| GameManager | Time Slider UI | TimeSlider GameObject |
| GameManager | Season Toggle | SeasonToggle GameObject |
| GameManager | Btn Morning/Noon/Afternoon/Evening | 对应 Button |
| GameManager | Btn Auto Play | 自动演示 Button |
| GameManager | Status Text | 新建 StatusText (TMPro) |

## 步骤 6：WebGL 构建设置

1. `File → Build Settings`
2. Platform 切换到 **WebGL**（点击 Switch Platform）
3. 点击 **Player Settings**：

```
Player Settings → WebGL:
  - Resolution → Default Canvas Width: 1024, Height: 600
  - Publishing Settings:
    - Compression Format: Brotli
    - Decompression Fallback: ☐ (不勾选，由服务器处理)
  - Code Optimization: Size (减少体积)
  - Managed Stripping Level: High

Player Settings → Other Settings:
  - Color Space: Linear
  - Auto Graphics API: ☑ (WebGL 2.0)
  - Strip Engine Code: ☑
  - Scripting Backend: IL2CPP

Player Settings → Quality:
  - 删除所有 Quality Level 只留 "Medium"
```

4. Unity Splash Image → 关闭（或自定义）
5. 点击 **Build**，选择输出目录为 `Build/`

## 步骤 7：部署构建产物

构建完成后，将 `Build/` 目录的内容复制到本仓库的 `static/` 目录：

```bash
# 在 Tuanjie 构建输出目录中：
cp -r Build/* /path/to/sun-shadow-experiment/static/
```

目录结构应为：
```
static/
├── Build/
│   ├── Build.data.br
│   ├── Build.framework.js.br
│   ├── Build.wasm.br
│   └── Build.loader.js
└── TemplateData/
    ├── favicon.ico
    ├── style.css
    └── ...
```

## 步骤 8：运行部署

```bash
cd sun-shadow-experiment
bash deploy.sh
```

---

## 常见问题

### Q: TextMeshPro 显示 "Missing TMP Essential Resources"
A: `Window → TextMeshPro → Import TMP Essential Resources`

### Q: 构建后浏览器打不开
A: 检查 nginx 是否正确配置了 Brotli Content-Encoding 头

### Q: 影子没有显示
A: 检查 `ShadowMeasurement.cs` 中 `Ground Y` 的值是否等于地面 Plane 的 Y 坐标

