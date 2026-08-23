# Changelog

## [CTIS - 2.0.1]

### 修复导出与本地化问题 🔧

- 修复导出时报错 CS0246：编辑器专用代码（`EditorSpinSlider` 等）现以 `#if TOOLS` 包裹，Release 构建不再引用 GodotSharpEditor
- 修复导出后多语言失效（界面只显示 key）：导出环境自动加载 `.translation` 资源并注册到 TranslationServer
- 内置 Noto Sans SC 子集字体（GB2312 一级 + 项目字符，约 1.1MB）并设为项目默认字体

## [CTIS - 2.0.0]

### 全新CTIS2.0.0🎉

- 基于 Godot 4.x 重构 CTIS（Cholopol Tetris Inventory System）
- 支持 C# / .NET 8.0 架构与 MVVM 数据绑定（基于 DotPudica）
- 高性能TetrisCoordLib数学引擎
- 更多功能与优化
