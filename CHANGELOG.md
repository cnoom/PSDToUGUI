# PSD To UGUI 变更日志

## [1.0.0] - 2025-05-19

### 新增
- PS 导出脚本（ExtendScript .jsx），支持导出图层树、文本信息、图层样式为 JSON + PNG
- UGUI 生成器，支持像素图层→Image、文本图层→TextMeshPro、形状图层→Image(Sliced)、图层组→空节点
- 图层样式映射：投影→Shadow、描边→Outline
- 相对锚点适配策略
- 全量重新生成模式
- 导入预览窗口（EditorWindow）
- 自定义资产导入器（检测 PSD JSON 文件）
- 导入配置（ScriptableObject）
- Unity 菜单入口（Tools > PSD To UGUI）
- PS 导出脚本导出功能（菜单导出 .jsx 文件给设计师）
