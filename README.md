# PSD To UGUI

将 Photoshop 设计稿导出为 Unity UGUI 预制体的工具包。

## 版本

v1.0.0

## 功能特性

- Photoshop 图层结构 → Unity UGUI GameObject 层级
- 像素图层 → `Image` + Sprite
- 文本图层 → `TextMeshPro` (TMP) / `UGUI Text`（可配置）
- 图层组 → 空 GameObject（保持层级）
- 图层样式（投影/描边）→ `Shadow` / `Outline` 组件
- 相对锚点适配（根据设计稿位置自动计算 anchors）
- 全量重新生成

## 使用流程

### 第一步：Photoshop 导出

1. 在 Unity 菜单栏点击 `Tools > PSD To UGUI > 导出 PS 脚本`，保存 `ExportPSD.jsx`
2. 在 Photoshop 中打开 PSD 文件
3. 菜单 `文件 > 脚本 > 浏览`，选择 `ExportPSD.jsx`
4. 选择导出目录，等待导出完成
5. 将导出目录（`document.json` + `textures/`）复制到 Unity 项目中

### 第二步：Unity 导入

1. 菜单 `Tools > PSD To UGUI > 导入 PSD`
2. 在导入窗口中选择 JSON 文件
3. 配置导入设置（可选，创建自定义 Settings SO）
4. 点击 `导入生成 UGUI`
5. 生成的预制体放在已有 Canvas 下即可使用

## 图层类型映射

| PS 图层类型 | UGUI 组件 | 说明 |
|---|---|---|
| 像素图层 | `Image` | 导出为 PNG → Sprite |
| 文本图层 | `TMP_Text` / `Text` | 根据配置选择 |
| 形状图层 | `Image` | 导出为 Sprite |
| 图层组 | `GameObject` | 作为父节点 |
| 投影样式 | `Shadow` | UGUI Shadow 组件 |
| 描边样式 | `Outline` | UGUI Outline 组件 |

## 锚点适配

使用 **相对锚点适配** 模式时，会根据设计稿中的位置计算归一化的 anchorMin/anchorMax：

```
anchorMin.x = layer.x / referenceWidth
anchorMin.y = 1 - (layer.y + layer.h) / referenceHeight
anchorMax.x = (layer.x + layer.w) / referenceWidth
anchorMax.y = 1 - layer.y / referenceHeight
```

默认参考尺寸 1920×1080，可在 Settings 中修改。

## 目录结构

```
PSDToUGUI/
├── Runtime/
│   ├── Core/           # 数据模型（PSDDocument, PSDLayer）
│   ├── Generator/      # UGUI 生成器
│   ├── Config/         # 导入配置
│   └── *.asmdef
├── Editor/
│   ├── Importer/       # 导入窗口和资源监听
│   ├── Menu/           # 菜单入口和 PS 脚本导出
│   └── *.asmdef
└── README.md
```

## 依赖

- Unity 2020.3+（推荐 2022.3 LTS）
- TextMeshPro（可选，推荐安装）
