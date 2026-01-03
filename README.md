# **Cholopol's Tetris Inventory System**

![Unity Version](https://img.shields.io/badge/Unity-2022.3.52f1_LTS-blue.svg?logo=unity)
![GitHub license](https://img.shields.io/github/license/cholopol/Cholopols-Tetris-Inventory-System-Beta)
![Platform](https://img.shields.io/badge/Platform-PC-lightgrey.svg)
[![Website](https://img.shields.io/badge/Website-cholopol.github.io-blue.svg?style=flat-square)](https://cholopol.github.io/Cholopol-Tetris-Inventory-System/)
[![Bilibili](https://img.shields.io/badge/bilibili-鹿卜Cholopol-blue.svg?style=flat-square&logo=bilibili)](https://space.bilibili.com/88797367)
![GitHub Repo stars](https://img.shields.io/github/stars/Cholopol/Cholopol-Tetris-Inventory-System)

[English](./README_EN.md) | 中文

![Cover](Images/Cover.png)

这是一个基于 Unity Engine 2022 构建的高级背包管理系统，采用 MVVM (Model-View-ViewModel) 架构开发，数据逻辑与 UI 表现的深度解耦，完美还原了《逃离塔科夫》中的核心体验，并支持异形物品处理、无穷嵌套以及极高的自定义扩展性。

## 📕目录

- [⭐核心功能亮点](#core-features)
- [🚀快速开始](#quick-start)
- [🎭第三方依赖](#third-party)
- [👨‍💻核心组件](#core-components)
- [👨‍💻Loxondon MVVM架构-开源精神万岁](#loxodon-mvvm)
- [👨‍💻Tetris坐标系统 -数值的艺术](#tetris-coordinate-system)
- [👨‍💻SpriteMeshRaycastFilter - 精确的Tetris形状操作过滤器](#sprite-mesh-raycast-filter)
- [👨‍💻背包套娃 - GUID构建的逻辑依赖关系与MVVM模式构成的完美实现](#nested-inventory-guid)
- [👨‍💻InventoryTreeCache-TetrisItem的导航员](#inventory-tree-cache)
- [👨‍💻高亮瓦片系统-用丰富多彩的高亮提示客制化你的库存系统](#highlight-system)
- [👨‍💻定制化的物品编辑器 - 点点鼠标，又增加了一个物品](#item-editor)
- [🤝 贡献指南](#contributing)
- [🔧 未来的改进点](#future-improvements)
- [📜 许可声明](#license)
- [📬 联系方式](#contact)
- [☕ 赞助我一杯咖啡 - ❤️你的支持是我最大的动力](#sponsor)

<a id="core-features"></a>

## ⭐核心功能亮点

- 🧩 **完美的Tetris瓦片设计**：支持物品旋转与基于网格的放置逻辑以及更多样的操作，更能按Tetris形状过滤用户操作。
- 🏗️ **MVVM 架构**：Item/Grid/Slot/Ghost系统实现View/ViewModel/Model逻辑解耦。
- 💾 **存档系统**：强大的序列化系统用于持久化库存状态。
- 🛠️ **自定义编辑器**：内置 UI Toolkit 编辑器方便创建物品。
- 🌍 **本地化**：集成多语言支持。

![Demo](Images/demo.png)

<a id="quick-start"></a>

## 🚀快速开始

### 环境要求

- Unity 2022.3.52f1 或更高版本。

### 导入步骤

- 克隆仓库：  

```json
git clone https://github.com/Cholopol/Cholopol-Tetris-Inventory-System.git
```

- 在 Unity Hub 中打开项目。
- 等待 Unity 导入资源与依赖包。

### 基础使用示例

- 导航到 Assets/Scenes/。
- 打开 EFT Like UI 场景。
- 点击播放（Play）。
- 控制

  - 按 B：打开/关闭背包。
  - 按 R：在拖拽时旋转物品。
  - 鼠标左键：拖动物品。
  - 鼠标右键：打开物品操作上下文菜单。

<a id="third-party"></a>

## 🎭第三方依赖

一般情况下无需手动安装，如遇问题请参考以下内容：

### Loxodon 框架

Loxodon Framework 是一个优秀的 Unity MVVM 架构开源框架，简单易上手。仓库地址：<https://github.com/vovgou/loxodon-framework.git>  
在本项目中通过向 Packages/manifest.json 添加以下内容安装：  

```plaintext
{
  "dependencies": {
    // ...
    "com.unity.modules.xr": "1.0.0",
    "com.vovgou.loxodon-framework": "2.6.7"
  },
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.vovgou"
      ]
    }
  ]
}

```

如遇问题可参考官方安装方式：<https://github.com/vovgou/loxodon-framework/blob/master/Installation.md。>

### Localization

使用 Unity 官方本地化插件，通过 Unity Package Manager 安装 `com.unity.localization`。

### Newtonsoft-json

性能优秀的 JSON 序列化工具，通过 Unity Package Manager 安装 `com.unity.nuget.newtonsoft-json`。

<a id="core-components"></a>

## 👨‍💻核心组件

### 1\. MVVM 架构实现

摒弃了传统的 MonoBehaviour 强耦合写法，而是采用数据驱动开发，以下是部分示例。

#### ViewModel (TetrisGridVM, TetrisItemVM)

- 这是系统的大脑。`TetrisGridVM.cs` 维护了一个二维数组 `_tetrisItemOccupiedCells` 来记录网格的占用状态，纯逻辑运算不依赖任何 `UnityEngine.Object`。
- `TetrisItemVM.cs` 处理物品的旋转状态 (`_rotated`)、坐标和尺寸数据。

#### View (TetrisGridView, TetrisItemView)

- 负责显示。它们监听 ViewModel 的属性变化（如位置改变、旋转改变），并自动更新 UI。

### 2\. 全局交互管理

`InventoryManager.cs` 是系统的指挥官（Singleton），它不处理具体网格逻辑，而负责协调：

- **输入处理**：在 Update() 中监听全局按键（如 R 键旋转、B 键开关背包）。
- **射线检测**：通过 GetGridViewUnderMouse() 实时判断鼠标悬停在哪个网格上，并更新 `selectedTetrisItemGridVM`。
- **视觉反馈**：调用 `inventoryHighlight` 组件，根据 ViewModel 返回的"是否可放置"结果，在 UI 上渲染绿色（可放置）或红色（冲突）的高亮色块。

### 3\. 数据持久化系统

存档系统由 `InventorySaveLoadService.cs` 驱动：

- **数据分离**：所有物品的静态数据（图标、名称、形状）存储在 `ItemDataList_SO` 中，动态数据（位置、旋转状态、容器 ID）则通过 JSON 反序列化到 `InventoryData_SO` 中，用于运行时缓存和编辑器下的实时监控。
- **GUID 映射**：每个网格容器和物品都有唯一的 GUID。系统会根据 GUID 自动重建 ViewModel 树，并通知 View 层生成对应的 UI 预制体。

<a id="loxodon-mvvm"></a>

## 👨‍💻Loxondon MVVM架构-开源精神万岁

### 1\. 模型层

**职责**：定义数据的"形状"，不包含业务逻辑。

- **静态数据**：使用 `ScriptableObject` 存储游戏配置。
- 例如：`ItemDetails` 类定义了物品的名称、图标、网格形状（Point Set）等。
- **运行时数据**：
- `InventoryData_SO` 存储玩家背包的运行时数据（物品状态、位置等）。
- `InventorySaveLoadService` 充当数据仓库服务者，负责将 Model 数据注入 ViewModel。

### 2\. 视图模型层

**职责**：作为 View 和 Model 的桥梁，持有 View 所需的状态并处理逻辑。

- **继承关系**：继承自 ViewModelBase（Loxodon Framework）。
- **核心机制**：依赖Loxodon Framework的数据绑定方法，可以控制数据流向，通常为`View←ViewModel`或`View←→ViewModel`。
- **属性通知**：使用 `Set(ref \_field, value)` 方法。当属性值改变时（例如 GridSizeWidth），会自动通知绑定的 View 更新。
- **逻辑封装**：
  - `TetrisSlotVM`：维护物品槽占用情况，判断物品是否可以放置。
  - `TetrisGridVM`：计算网格占用情况 (\_tetrisItemOccupiedCells)，判断物品是否可以放置。
  - `TetrisItemVM`：管理单个物品的状态（旋转方向 \_dir、坐标 \_localGridCoordinate）。

### 3\. 视图层

**职责**：可视化展示，将用户输入转换为 ViewModel 的指令。

- **继承关系**：继承自 UIView 或特定基类（如 TetrisItemContainerView&lt;TetrisGridVM&gt;）。
- **核心机制**：
- **数据绑定**：在 TetrisGridView.cs 中，你可以看到如下代码：  

```plaintext
this.SetDataContext(\_viewModel); // 设置数据上下文  
// 之后 Loxodon 会自动同步 VM 的属性到 View 的组件
```

- **事件监听**：View 监听 Unity 的 OnPointerEnter 等事件，并将其转发给 Manager 或直接调用 VM 的方法。

### 🔄 数据流向示例

以"旋转物品"为例：

- **用户操作**：玩家按下 R 键。
- **管理器 (Manager)**：`InventoryManager.cs` 捕获按键，调用 `RotateItemGhost()`。
- **视图模型 (ViewModel)**：代码修改 `TetrisItemVM` 的 `_rotated` 属性。

```plaintext
// TetrisItemVM.cs  
public bool Rotated { get => _rotated; set => Set(ref _rotated, value); }
```

- **框架 (Framework)**：Loxodon 框架检测到 Rotated 属性变化，自动触发绑定。
- **视图 (View)**：TetrisItemGhostView.cs 接收到变更通知，更新 RectTransform 的旋转角度。

<a id="tetris-coordinate-system"></a>

## 👨‍💻Tetris坐标系统 - 数值的艺术

### 1\. 核心设计概述

本项目的网格容器设计基于经典的 Tetris (俄罗斯方块) 变体逻辑，结合 MVVM 架构 与 笛卡尔坐标系 实现。系统通过将连续的 UI 空间离散化为二维整数矩阵，实现了物品的精确放置、旋转与碰撞检测。再次体现了人类智慧胜于LLM大模型。

- 关键类职责

  - TetrisGridVM (ViewModel) : 负责纯逻辑计算。维护一个 `TetrisItemVM[,]` 二维数组作为“逻辑网格”，记录每个格子被哪个物品占用。
  - TetrisGridView (View) : 负责 UI 呈现。将 ViewModel 的逻辑坐标转换为 Unity UI ( RectTransform ) 的屏幕像素坐标。
  - TetrisUtilities : 提供底层的数学运算支持，特别是矩阵旋转算法。

### 2\. 坐标系统与数学公式

1\. 网格坐标定义 (Grid Coordinates)

系统采用 左上角为原点 (0,0) 的坐标系（在 UI 布局中通常对应 Top-Left 锚点）。

- X 轴 : 向右增长 (Column Index)
- Y 轴 : 向下增长 (Row Index)
数据结构 :

```plaintext
// TetrisGridVM.cs
private TetrisItemVM[,] _tetrisItemOccupiedCells; // [width, height]
```

### 3\. 像素位置计算公式

将逻辑网格坐标 $(x, y)$ 转换为 UI 局部坐标 $(P_x, P_y)$ 的公式如下：

$$
P_x = x \times W_{unit}
$$
$$
P_y = - (y \times H_{unit})
$$

- $x, y$: 物品在网格中的整数坐标（列号，行号）。
- $W_{unit}, H_{unit}$: 单个网格单元的像素宽高（通常定义在 `Settings.gridTileSizeWidth` ，默认 20f）。
- 注意 : $P_y$ 取负值是因为 Unity UI 的坐标系中，Y 轴向上为正，而网格布局通常从上向下排列（类似文本流）。
代码实现参考 :

```plaintext
// InventoryHighlight.cs (类似逻辑)
Vector2 tilePos = new Vector2(
    (point.x + ghost.RotationOffset.x) * tileW,
    -((point.y + ghost.RotationOffset.y) * tileH) // Y轴取负
);
```

### 4\. 物品形状与旋转算法

物品占据的格子被定义为一组相对于物品原点 $(0,0)$ 的坐标点集 `List<Vector2Int>` 。

旋转变换公式 (顺时针 90°) :
对于形状中的任意一点 $(x, y)$，其旋转后的新坐标 $(x', y')$ 为：

$$
x' = -y
$$
$$
y' = x
$$

代码实现 :

```plaintext
// TetrisUtilities.cs
public static List<Vector2Int> RotatePointsClockwise(List<Vector2Int> points)
{
    List<Vector2Int> rotatedPoints = new();
    foreach (var point in points)
    {
        rotatedPoints.Add(new Vector2Int(-point.y, point.x)); // 核心旋转公式
    }
    return rotatedPoints;
}
```

旋转偏移修正 (Pivot Offset) :
单纯旋转会导致形状跑出原有边界（因为是以 0,0 为轴心）。系统引入了 RotationOffset 来修正旋转后的原点位置，确保物品旋转后仍然对齐网格。

$$
Offset_{Up} = (Width - 1, Height - 1)
$$
不同方向有不同的硬编码偏移量，详见 `TetrisUtilities.cs:L66`

### 5\. 逻辑实现细节

1\. 碰撞检测 (Collision Detection)

放置物品时，系统会遍历物品占据的所有点 $(p_x, p_y)$，并加上基准坐标 $(O_x, O_y)$：

$$
Target(x, y) = (O_x + p_x + Offset_x, O_y + p_y + Offset_y)
$$

验证条件 :

1. 边界检查 : $0 \le Target_x < GridWidth$ 且 $0 \le Target_y < GridHeight$
2. 占用检查 : _tetrisItemOccupiedCells[Target_x, Target_y] 必须为 null 。

2\. 物品放置 (Placement)
当验证通过后，物品会被“写入”逻辑网格：

1. 将物品的所有 Target(x, y) 坐标在 _tetrisItemOccupiedCells 中标记为该物品的引用。
2. 更新 View 层，根据公式计算 RectTransform 的 anchoredPosition 。

### 6\. 优点与亮点

1\. 数学纯粹性 (Mathematical Purity) :

- 将复杂的 UI 交互转化为简单的矩阵运算。
- 旋转算法使用了标准的线性代数变换，计算位置算法在图形学中被称为 Affine Transformation（仿射变换），保证了逻辑的严密性。

2\. MVVM 分离 (MVVM Separation) :

- 逻辑先行 : 所有的碰撞、旋转、位置计算都在 VM 层（纯 C# 类）完成，不依赖 Unity 的 Transform 或 Physics 。这使得逻辑极其高效且易于单元测试。
- View 层“傻瓜化” : View 只负责根据 VM 算出的坐标设置位置，不参与逻辑判断。

3\. 高性能 (High Performance) :

- O(1) 查找 : 通过二维数组直接访问任意格子的状态，无需遍历。
- 零 GC : 核心的高亮系统使用了对象池，且坐标计算全部基于 struct (Vector2Int)，避免了堆内存分配。

4\. 灵活的形状定义 :

- 形状不是预设的贴图，而是坐标点集。这意味着可以轻松创建任意不规则形状（如 L 型、T 型、斜线型、甚至空心形状）。

### 7\. 未来改进点

1\. 稀疏矩阵优化 :

- 当前使用 TetrisItemVM[,] 二维数组。如果网格非常大（例如 1000x1000），内存占用会显著增加且包含大量空引用。
- 建议 : 对于超大网格容器，也许会改用 Dictionary<Vector2Int, TetrisItemVM> 存储占用信息。

### 8\. 综上所述

线性代数是Tetris坐标系统的底层骨架：

- 线性变换负责旋转。
- 向量加法负责移动。
- 矩阵运算负责把方块坐标最终投影到屏幕像素上

<a id="sprite-mesh-raycast-filter"></a>

## 👨‍💻SpriteMeshRaycastFilter - 精确的Tetris形状操作过滤器

它的作用是让鼠标点击更加精准，只响应物体“真实形状”的区域，忽略透明的空白区域。在 Unity（以及大多数游戏引擎）的 UI 系统里，所有的图片默认都是 **矩形** 的。
打个比方：

想象一下你有一个 "L" 形 的俄罗斯方块：

``` plaintext
🟥
🟥
🟥 🟥
```

但在计算机眼里，它其实被包在一个透明的矩形盒子里：

``` plaintext
🟥 ⬜ 
🟥 ⬜  <-- 这里的白色⬜其实是透明的，但在UI逻辑里它属于这个Object
🟥 🟥
```

如果没有这个脚本 ：
当你点击右上角的空白处（⬜）时，虽然你看起来点的是空气，或者是后面的一件装备，但系统会判定你点到了这个 "L" 形方块。这会导致玩家经常发生“误触”——明明想点后面的东西，却被前面物体的透明边缘挡住了。

**这个脚本就像精准的“裁剪刀”。它告诉 Unity 的点击检测系统（Raycaster）：** “只有当鼠标真正碰到那些有颜色的格状方块时，才算点中了我。如果点在透明的格子里，请无视我，让点击穿透过去。”

这个脚本挂在物品UI和物品幽灵UI上，每当你移动鼠标或者点击时，它会快速进行以下三步判断：
1. 定位 ：它先看你的鼠标在当前这个Image的哪个位置（比如左上角、右下角）。
2. 查表 ：它去读取这个物品的“形状数据”（是 L 型、T 型还是方块？）。它知道哪几个格子里有东西，哪几个格子是空的。
   - 代码参考 ： TryGetShapeConfig 方法就是去问 ViewModel：“你是啥形状的？”
3. 判定 ：
   - 如果鼠标位置对应的格子里 有东西 -> 拦截点击 （返回 true ，算点中了）。
   - 如果鼠标位置对应的格子里 是空的 -> 放行点击 （返回 false ，就像这个物体不存在一样，你可以点到它后面的东西）。
   - 代码参考 ： IsRaycastLocationValid 是核心裁判。

它的必要作用是：

- 为了体验 ：在背包整理游戏中，物品堆叠很密集。如果不支持这种“异形点击”，玩家在拿起形状复杂的物品时会非常难受，经常点错。
- 为了性能 ：脚本里做了一些优化（ _cachedPoints ），它会把形状“记下来”。只要物品没有旋转或变形，它就不需要每次都重新算。

<a id="nested-inventory-guid"></a>

## 👨‍💻背包套娃 - GUID构建的逻辑依赖关系与MVVM模式构成的完美实现

在 Cholopol-Tetris-Inventory-System 中，背包的嵌套（例如：背包里装了一个防弹衣，防弹衣里又装了一个绷带）与还原是通过 GUID 索引和 InventoryTreeCache 的组合机制实现的。这种设计避免了复杂的递归对象结构，将数据扁平化存储，在运行时重构为逻辑树状结构。

### GUID 链式引用

系统中的每个"容器"（如口袋、背包网格）和每个"物品"都有唯一的 GUID：

- **容器 (Container)**：由 ContainerNode 表示，有一个 ContainerId。
- **物品 (Item)**：由 ItemNode 表示，有一个 ItemGuid。
- **嵌套关系**：
  - 每个物品数据 (TetrisItemPersistentData) 包含一个 containerGuid 字段，指向它所在的父容器。
  - 如果一个物品本身也是容器（例如防弹衣），它会拥有一个自己的网格组，各网格的 ID 与该物品的 GUID 关联，并由缓存管理维护一个映射表。

### 数据重组流程

当调用 BuildRuntimeCache() 时，系统会遍历所有物品数据，并在 Cache 中建立索引。

- 扁平数据输入 ： `InventoryData_SO` 提供了一个包含所有物品的列表 `List<TetrisItemPersistentData>` 。每个物品都有 itemGuid 和 containerGuid （指向它所在的容器）。
- 构建索引 ( PlaceItem ) ：
  - 遍历列表中的每个物品。
  - 调用 `cache.PlaceItem(containerId, data)` 。
  - 关键动作 ：Cache 会查找或创建一个 ContainerNode （容器节点），并将该物品数据加入该节点的列表中。
  - 结果 ：Cache 内部形成了 `ContainerID -> List<Items>` 的映射关系。

### 嵌套还原机制

还原过程并非一次性生成所有 UI，而是 按需递归 的。

- 第一步：顶层容器绑定
  1. 在 Start 或 UI 打开时，系统首先识别场景中已经存在的持久化网格（例如玩家身上的主背包面板）。系统读取该面板上的 DataGUID 组件获取 GUID（例如 Depository_GUID ）。
  2. 调用 `TetrisGridFactory.BindViewToGuid(gridView, guid)` 。
- 第二步：VM 初始化与数据拉取
  1. 当 `TetrisGridVM` 被绑定到某个 GUID 时，它会执行 `PrimeFromCache()` （数据填充）：
  2. 询问 Cache ：VM 向 `InventoryTreeCache` 发送请求：“请给我 Depository_GUID 的所有物品。”
  3. 获取数据 ：Cache 返回 `IEnumerable<TetrisItemPersistentData>` 对象。
  4. 生成子物品 ：VM 遍历这些数据，为每个物品创建子 TetrisItemVM 。 
- 第三步：递归处理
  1. 如果生成的子物品本身也是一个容器（例如是一个“战术胸挂”）：
  2. 识别容器 ：该子物品的 `TetrisItemVM` 会被标记为拥有内部网格。
  3. 分配 GUID ：子网格的 GUID 通常就是该物品的 itemGuid 再附加层级索引，例如：

    ```json
    "persistentGridGuid": "ff3764e8-5a2e-43d4-9b4e-cafdc418ca84:3"
    ```

  4. 递归触发 ：
  - 当玩家 打开 这个“战术胸挂”的 UI 时，一个新的 `TetrisGridView` 被创建。
  - 这个新 View 绑定到上述的子网格 GUID。
  - 重复第二步 ：新 View 绑定的 VM 再次向 Cache 询问：“请给我 ChestRig_GUID 里的所有物品。”
  - Cache 返回胸挂里的弹匣、手雷等数据。

<a id="inventory-tree-cache"></a>

## 👨‍💻InventoryTreeCache-TetrisItem的导航员

`InventoryTreeCache` 并不直接存储树，它存储的是“关系”。这种“查表法”完美解决了无限嵌套的问题，因为无论层级多深，都只是简单的 Key-Value 查找，不需要复杂的递归遍历算法。树结构实现采用了一种 **"扁平化存储 + 运行时关系缓存"** 的模式，这类似于关系型数据库的设计理念。以下是其与传统 Unity 背包实现的对比分析：

1\. 核心差异对比

| 特性  | 传统实现 (Traditional) | 本项目 (This Project) |
| --- | --- | --- |
| 数据结构 | 嵌套对象引用`class Bag { List<Item> items; }` | 扁平化列表 + 外键`class Item { string containerGuid; }` |
| 查找方式 | 递归遍历：查找物品需要从根节点逐层向下遍历。 | 哈希索引 (O(1))：通过 InventoryTreeCache 直接查询容器内容。 |
| 序列化 | 直接序列化树：易受递归深度限制，JSON 结构层级深。 | 序列化列表：JSON 结构扁平，无嵌套层级，数据更紧凑。 |
| UI 生成 | 预制体嵌套：UI 层级往往直接对应数据层级。 | MVVM 动态绑定：UI 是数据的投影，View 与数据解耦。 |

2\. 优点

- 💾 **数据安全性与序列化鲁棒性**
  - **避免循环引用**：在传统的嵌套对象中，如果两个物品互相引用（例如互相放入），序列化器会崩溃。扁平化 GUID 设计完全规避了无限递归问题。
  - **存取效率**：保存时只需转储一个列表，无需处理复杂的图结构。
- ⚡ **极速查询性能**
  - 利用 InventoryTreeCache.cs，查找"某个背包里有什么"是字典查找操作，复杂度为 O(1)。
  - 相比之下，传统树结构在查找"全局某个特定 ID 的物品"时，需要 O(N) 的全树遍历。
- 🧩 **灵活的父子关系重组**
  - 移动物品（例如从背包 A 移到背包 B）只需要修改该物品的 containerGuid 字段。不需要在内存中进行复杂的"从 A 列表移除 -> 添加到 B 列表"的操作。
- 💤 **惰性加载 (Lazy Loading) 支持**
  - 系统可以只加载"物品数据"，而不立即生成所有 UI。只有当玩家真正打开某个子背包时，才通过 InventoryTreeCache 查询内容并生成对应的视图。这对含有大量嵌套物品的大型存档非常友好。

3\. 缺点

- 🔧 **状态同步复杂度**
  - 必须维护 ViewModel (运行时状态)、InventoryTreeCache (缓存) 与持久化数据 (存档数据) 三者的一致性。
  - 如果开发者忘记在移动物品时更新缓存，会导致逻辑与数据脱节（例如 UI 显示物品在背包里，但数据认为它还在地上）。
  - 在代码层面：`InventoryService.cs` 中有专门的同步方法用于处理这种同步。

4\. 独特之处

- 🌟 **中介缓存层**  
    大多数库存系统要么直接操作 UI（初级），要么直接操作数据（中级）。Cholopol-Tetris-Inventory-System 引入了 InventoryTreeCache 作为中间层。
  - 它不关心 UI 如何显示，也不关心数据如何存储。
  - 它纯粹维护"谁属于谁"的关系拓扑图。  
    这种设计让 MVVM ViewModel 非常轻量化--VM 只需要向缓存请求数据，而无需自己持有沉重的数据列表。
- 🌟 **真正的解耦架构**  
    在传统实现中，删除一个背包 UI 往往意味着销毁里面的数据对象。但在 Cholopol-Tetris-Inventory-System 中：
  - **销毁 UI (View)**：只是回收了 GameObject。
  - **数据 (ViewModel/Model/Cache)**：依然完好地保留在内存中。  
    这意味着你可以随时关闭背包面板（将所有 UI 回收至对象池），当再次打开时，通过缓存瞬间还原状态，原始状态完全保留。

5\. 综上所述

Cholopol-Tetris-Inventory-System 的树结构实现采用了"空间换时间"和"复杂度换灵活性"的方案。牺牲了部分代码的直观性（需要理解 GUID 和缓存机制），换取了优越的数据稳定性和高效的检索能力，非常适合实现《逃离塔科夫》这类物品数量巨大且嵌套复杂的游戏。

<a id="highlight-system"></a>

## 👨‍💻高亮瓦片系统-用丰富多彩的高亮提示客制化你的库存系统

高亮瓦片系统（Highlight System）不仅提高视觉效果，更是用户体验（UX）和性能优化的关键部分。以下是该系统的核心优点与技术亮点：

### 1. MVVM 驱动的"幽灵"预览 (Ghost Preview)

系统使用 TetrisItemGhostView（幽灵物品）来模拟放置效果，而不是直接移动真实物品对象。

- **优点**：解耦与安全性。在玩家拖拽物品时，真实物品数据（Model）保持不变，仅有视觉上的"幽灵"在移动。只有当玩家松开鼠标且放置合法时，才会真正提交数据更改。
- **亮点**：实时旋转预览。当玩家按下 R 键时，幽灵物品会在内存中旋转其坐标集（Point Set），高亮系统会立即根据新的形状重新计算可放置性，整个过程流畅无卡顿。

### 2. 对象池化的高性能渲染 (Pooled Rendering)

高亮系统并未为每个网格格子预先创建高亮块，而是采用了 **对象池（Object Pooling）** 技术。

- **代码引用**：InventoryHighlight.cs  

```plaintext
GameObject tile = PoolManager.Instance.GetObject(highlightTilePrefab);
```

- **优点**：零 GC 压力。无论物品形状多复杂，系统只会从池中取出有限数量的 UI 对象。当高亮消失时，这些对象被立即回收（PushObject），而不是销毁。这在频繁的 Update 循环中对性能至关重要。

### 3. 智能的放置验证上下文 (Context-Aware Validation)

高亮颜色不仅仅是简单的"绿"或"红"，它基于一个强大的上下文结构 InventoryPlacementContext 进行判断。

- **代码引用**：InventoryPlacementContext
- **优点**：可扩展的验证逻辑。系统会将当前操作的所有上下文（物品、目标容器、旋转状态、锚点位置）打包：
- **绿色**：位置空闲，完全合法。
- **红色**：位置被占用或超出边界。
- **黄色**：可以堆叠归并。
- **天蓝色**：例如快速交换功能--如果目标位置有物品，但符合交换规则，系统会显示天蓝色高亮，提示玩家松手将执行交换操作。

### 4. 数据驱动的配置 (Configurable Palette)

高亮颜色并不是写死在代码中的，而是通过 ScriptableObject（InventoryPlacementConfig_SO）进行配置。

- **优点**：对设计者友好。设计者可以在 Unity 编辑器中直接调整 ValidColor（有效色）和 InvalidColor（无效色）的 RGBA 值及透明度，甚至可以配置不同的高亮策略（例如色盲模式），而无需修改一行代码。


- **逻辑层** (TetrisGridVM)：负责快速的数学计算（占用检测）。
- **表现层** (InventoryHighlight)：利用对象池高效渲染。
- **数据层** (PlacementConfig)：提供灵活的视觉配置。

这种设计确保了即使在低端设备上，无论背包有多大、物品形状多复杂，拖拽操作都能保持丝般顺滑。

<a id="item-editor"></a>

## 👨‍💻定制化的物品编辑器 - 点点鼠标，又增加了一个物品

- Unity Editor中在菜单栏打开 **ChosTIS-> Item Editor**。
- 点`＋`创建一个新的物品数据条目自动将该物品添加到 `ItemDataList_SO`。
- 指定精灵和网格形状等信息。
- 背包类物品需指定手动设置的`GridUIPrefab`网格组预制体。

<a id="contributing"></a>

## 🤝 贡献指南

欢迎提交Bug报告或功能请求！请遵循以下步骤：

- **代码风格**：
  - 类和方法：PascalCase
  - 变量：camelCase
  - 所有公共方法需添加 XML 注释。

<a id="future-improvements"></a>

## 🔧 未来的改进点

**V 1.0.0** ：

1\. 已知快速交换位置算法的缺陷，各位可能会在实际测试时发现物品处于某个方向的某种状态时无法交换的问题，未来我会优化这部分算法逻辑。

2\. 拖拽时可能会触发`ArgumentException: Mesh can not have more than 65000 vertices`报错： unityEngine.UI.Text在生成Mesh时，顶点数超过了65000的上限, 于是 VertexHelper.FillMesh 直接抛出异常，`InventoryTreeCacheMonitor.cs`会输出缓存树监测信息，它会把缓存内容拼成很多行,然后 outputText.text =_sb.ToString();持续刷新(默认 maxLines =200)。场景里默认给这个 outputText 额外增加富文本导致字符更多，很容易在物品数量一多、拖拽触发频繁重建时越界。该脚本用于开发中实时监控缓存信息，不影响库存系统的正常使用，你可以关掉它。

<a id="license"></a>

## 📜 许可声明

- 本项目采用 Apache License 2.0 许可证。请参阅 [LICENSE](LICENSE) 文件以获取更多信息。
- 你的项目中必须包含[NOTICE](NOTICE.md)文件。
- 如有必要你的项目需创建专门的 `THIRD-PARTY-NOTICES` 文件
- 基于Section 4(b)条款，商业用途的项目中有关本系统的源码中的版权声明不得移除，如有修改必须注明修改位置与修改时间。例如：

```plaintext
Modified by [Your Name] [Year]:
 [Brief description of changes]
```

- **严禁将本项目用于任何形式的抄袭、盗版盈利等行为，作者本人将对这类行为追责到底，开源精神应被每一个人尊重，欢迎各位向`鹿卜Cholopol`本人提交相关侵权线索。**

<a id="contact"></a>

## 📬 联系方式

如果你有任何问题、建议或想法，请随时联系我：

- 📧 电子邮箱：`cholopol@163.com`
- 💬 社区讨论：敬请期待
- 🌐 视频介绍：敬请期待

<a id="sponsor"></a>

## ☕ 赞助我一杯咖啡 - ❤️你的支持是我最大的动力

<div align="center">
  <table>
    <tr>
      <th>Alipay</th>
      <th>WeChat Pay</th>
    </tr>
    <tr>
      <td><img src="Images/alipay.png" alt="alipay"></td>
      <td><img src="Images/wechatpay.png" alt="wechatpay"></td>
    </tr>
  </table>
</div>

---

<div align="center">

  ![Intro](Images/Intro.gif)

  **Copyright (c) 2026 Cholopol. All rights reserved.**

</div>
