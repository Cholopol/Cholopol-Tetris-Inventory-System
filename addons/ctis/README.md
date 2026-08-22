# CTIS

Cholopol Tetris Inventory System for Godot 4.7 .NET。依赖 [DotPudica](https://github.com/) 与 Tetris Coord Lib，三个插件必须作为兄弟目录放在 `addons/` 下。

## 安装

1. 复制到宿主项目：
   - `addons/dot-pudica`
   - `addons/tetris_coord_lib`
   - `addons/ctis`
2. 在 **项目设置 → 插件** 中按顺序启用：DotPudica、Tetris Coord Lib、CTIS。
3. 确认宿主 `.csproj` 出现 `<!-- Ctis:Begin -->` 注入块（以及 DotPudica / TetrisCoordLib 块），然后编译。

插件不会把 C# 编进宿主程序集，而是通过 `ProjectReference` 引用 `Ctis.Core` 与 `Ctis.Godot`。

## 最小 Bootstrap

主场景常驻节点（或 Autoload）中初始化一次：

```csharp
using Ctis.Core;
using Ctis.Presentation;
using DotPudica.Godot.Views;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using AppContext = DotPudica.Godot.AppContext;

public partial class GameBootstrap : Node
{
    private AppContext? _app;

    public override void _Ready()
    {
        var wm = GetNodeOrNull<GodotWindowManager>("WindowManager")
            ?? new GodotWindowManager { Name = "WindowManager" };
        if (wm.GetParent() == null)
            AddChild(wm);

        _app = new AppContext().Initialize(services =>
        {
            services.AddCtis();
            services.AddCtisGodot();
        }, wm);

        CtisLocale.LoadCsv();
        ItemCatalogLoader.LoadInto(_app.Services.GetRequiredService<IItemCatalog>());
        PlacementConfigLoader.LoadInto(_app.Services.GetRequiredService<PlacementConfig>());
        EquipmentLayoutLoader.LoadInto(_app.Services.GetRequiredService<EquipmentLayout>());
        CtisRuntime.Attach(this, wm);
    }

    public override void _ExitTree()
    {
        _app?.Dispose();
        _app = null;
    }
}
```

打开背包：`AppContext.Current.Services.GetRequiredService<IInventorySession>()` 的 `EnterNewGame` / `ToggleInventory`。

## 资源

| 用途 | 默认位置 | 说明 |
|---|---|---|
| UI chrome（关闭钮、槽位底板、横幅） | `addons/ctis/Art/` | 插件自带；也可放在宿主 `res://Art/` 作为回退 |
| 物品图标 | catalog 的 `iconKey` | 建议 `res://Art/Items/...`，由宿主提供；优先拼接图，见下 |
| 物品 / 放置配置 | 项目设置 `ctis/item_catalog`, `ctis/placement_config` | 宿主用 **CTIS / Data Editor** 制作并配置路径；插件核心不绑定任何特定业务路径 |
| 装备槽布局 | 项目设置 `ctis/equipment_layout` | 无内置槽位；宿主通过项目设置指定 JSON 路径并在解析 `IInventorySession` 之前 `LoadInto`。Character 槽的 `offsetX` / `offsetY` 是人型栏左上角像素坐标 |
| 本地化 CSV | 项目设置 `ctis/locale` | 由项目设置指定多语言 CSV 路径 |
| 口袋 / 保险箱网格场景 | 项目设置 `ctis/scenes/pocket`, `ctis/scenes/coffer` | 插件内置默认提供 `GP_Pocket.tscn` / `GP_Coffer.tscn`，支持通过项目设置覆盖 |
| 物品内格布局 | `ItemDetails.GridPanelSceneKey` | 容器物品必须指定网格布局场景；不设则无法打开内部网格 |

缺贴图或 JSON 解析失败会在调试器打出 `[CTIS] Missing texture` / `Failed to parse`，而不是静默空白。

### 物品图标：优先拼接 PNG

同一张 PNG 上的多个物品在运行时共享一张 GPU 贴图（`AtlasTexture` 切区域），背包格子、拖影、详情窗同时画出时可以合批。每个物品一张独立小图也能用，但彼此不能合批。

推荐：

- 把同一批、会同时出现的图标画在一张规则网格图上（例如 2×2 的 `UnionSoldier.png`），放到 `res://Art/Items/`。
- 数据编辑器里设 Cols / Rows，点选格子。catalog 会写成 `res://Art/Items/UnionSoldier.png:0,0,40,40`。
- 不要为了合批把全游戏图标打成一张超大图；按系列或同时入包的一组来拼即可。
- 单张 PNG（`res://Art/Items/Bandage.png`）仍然有效，适合只有一件、或不和别的图标一起出现的资源。

编辑器左侧物品列表会单独裁出小图显示，因为 `ItemList` 不会按切块绘制；这不影响运行时。

## 项目设置

启用插件后会写入：

- `ctis/item_catalog`
- `ctis/placement_config`
- `ctis/equipment_layout`
- `ctis/locale`

数据编辑器：编辑器菜单 **CTIS / Data Editor**。
