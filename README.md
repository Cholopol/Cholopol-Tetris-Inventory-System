# **Cholopol's Tetris Inventory System**

![Unity Version](https://img.shields.io/badge/Unity-2022.3.62f3_LTS-blue.svg?logo=unity)
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
- [👨‍💻快捷交换系统 - 智能的异形物品互换](#quick-exchange-system)
- [👨‍💻SpriteMeshRaycastFilter - 精确的Tetris形状操作过滤器](#sprite-mesh-raycast-filter)
- [👨‍💻背包套娃 - GUID构建的逻辑依赖关系与MVVM模式构成的完美实现](#nested-inventory-guid)
- [👨‍💻InventoryTreeCache-TetrisItem的导航员](#inventory-tree-cache)
- [👨‍💻高亮瓦片系统-用丰富多彩的高亮提示客制化你的库存系统](#highlight-system)
- [👨‍💻定制化的物品编辑器 - 点点鼠标，又增加了一个物品](#item-editor)
- [👨‍💻运行时调试面板 - 方便的测试工具](#runtime-debug-panel)
- [👨‍💻浮动容器窗口系统 - 无限嵌套的可视化](#floating-window-system)
- [👨‍💻存档系统 - 持久化你的库存状态](#save-load-system)
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

- Unity 2022.3.62f3 或更高版本。

### 导入步骤

- 克隆仓库：  

```json
git clone https://github.com/Cholopol/Cholopol-Tetris-Inventory-System.git
```

- 在 Unity Hub 中打开项目。
- 等待 Unity 导入资源与依赖包。

### 基础使用示例

- 导航到 Assets/Cholopol_Tetris_Inventory_System_Samples/Demo/Scenes/
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

如遇问题可参考官方安装方式：<https://github.com/vovgou/loxodon-framework/blob/master/Installation.md>

### Localization

使用 Unity 官方本地化插件，通过 Unity Package Manager 安装 `com.unity.localization`。

### Newtonsoft-json

性能优秀的 JSON 序列化工具，通过 Unity Package Manager 安装 `com.unity.nuget.newtonsoft-json`。

<a id="core-components"></a>

## 👨‍💻核心组件

### 1. MVVM 架构实现

摒弃了传统的 MonoBehaviour 强耦合写法，而是采用数据驱动开发，以下是部分示例。

#### ViewModel (TetrisGridVM, TetrisItemVM)

- 这是系统的大脑。`TetrisGridVM.cs` 维护了一个二维数组 `_tetrisItemOccupiedCells` 来记录网格的占用状态，纯逻辑运算不依赖任何 `UnityEngine.Object`。
- `TetrisItemVM.cs` 处理物品的旋转状态 (`_rotated`)、坐标和尺寸数据。

#### View (TetrisGridView, TetrisItemView)

- 负责显示。它们监听 ViewModel 的属性变化（如位置改变、旋转改变），并自动更新 UI。

### 2. 全局交互管理

`InventoryManager.cs` 是系统的指挥官（Singleton），它不处理具体网格逻辑，而负责协调：

- **输入处理**：在 Update() 中监听全局按键（如 R 键旋转、B 键开关背包）。
- **射线检测**：通过 GetGridViewUnderMouse() 实时判断鼠标悬停在哪个网格上，并更新 `selectedTetrisItemGridVM`。
- **视觉反馈**：调用 `inventoryHighlight` 组件，根据 ViewModel 返回的"是否可放置"结果，在 UI 上渲染绿色（可放置）或红色（冲突）的高亮色块。

### 3. 数据持久化系统

存档系统由 `InventorySaveLoadService.cs` 驱动：

- **数据分离**：所有物品的静态数据（图标、名称、形状）存储在 `ItemDataList_SO` 中，动态数据（位置、旋转状态、容器 ID）则通过 JSON 反序列化到 `InventoryData_SO` 中，用于运行时缓存和编辑器下的实时监控。
- **GUID 映射**：每个网格容器和物品都有唯一的 GUID。系统会根据 GUID 自动重建 ViewModel 树，并通知 View 层生成对应的 UI 预制体。

<a id="loxodon-mvvm"></a>

## 👨‍💻Loxondon MVVM架构-开源精神万岁

### 1. 模型层

**职责**：定义数据的"形状"，不包含业务逻辑。

- **静态数据**：使用 `ScriptableObject` 存储游戏配置。
- 例如：`ItemDetails` 类定义了物品的名称、图标、网格形状（Point Set）等。
- **运行时数据**：
- `InventoryData_SO` 存储玩家背包的运行时数据（物品状态、位置等）。
- `InventorySaveLoadService` 充当数据仓库服务者，负责将 Model 数据注入 ViewModel。

### 2. 视图模型层

**职责**：作为 View 和 Model 的桥梁，持有 View 所需的状态并处理逻辑。

- **继承关系**：继承自 ViewModelBase（Loxodon Framework）。
- **核心机制**：依赖Loxodon Framework的数据绑定方法，可以控制数据流向，通常为`View←ViewModel`或`View←→ViewModel`。
- **属性通知**：使用 `Set(ref \_field, value)` 方法。当属性值改变时（例如 GridSizeWidth），会自动通知绑定的 View 更新。
- **逻辑封装**：
  - `TetrisSlotVM`：维护物品槽占用情况，判断物品是否可以放置。
  - `TetrisGridVM`：计算网格占用情况 (\_tetrisItemOccupiedCells)，判断物品是否可以放置。
  - `TetrisItemVM`：管理单个物品的状态（旋转方向 \_dir、坐标 \_localGridCoordinate）。

### 3. 视图层

**职责**：可视化展示，将用户输入转换为 ViewModel 的指令。

- **继承关系**：继承自 UIView 或特定基类（如 TetrisItemContainerView&lt;TetrisGridVM&gt;）。
- **核心机制**：
- **数据绑定**：在 TetrisGridView.cs 中，你可以看到如下代码：  

```plaintext
this.SetDataContext(_viewModel); // 设置数据上下文  
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

### 1. 核心设计概述

本项目的网格容器设计基于经典的 Tetris (俄罗斯方块) 变体逻辑，结合 MVVM 架构 与 笛卡尔坐标系 实现。系统通过将连续的 UI 空间离散化为二维整数矩阵，实现了物品的精确放置、旋转与碰撞检测。再次体现了人类智慧胜于LLM大模型。

- 关键类职责

  - TetrisGridVM (ViewModel) : 负责纯逻辑计算。维护一个 `TetrisItemVM[,]` 二维数组作为“逻辑网格”，记录每个格子被哪个物品占用。
  - TetrisGridVM : 负责纯逻辑计算。维护一个 `TetrisItemVM[,]` 二维数组作为“逻辑网格”，记录每个格子被哪个物品占用。
  - TetrisGridView : 负责 UI 呈现。将 ViewModel 的逻辑坐标转换为 Unity UI ( RectTransform ) 的屏幕像素坐标。
  - TetrisUtilities : 提供底层的数学运算支持，特别是矩阵旋转算法。

### 2. 坐标系统与数学公式

#### 1. 网格坐标定义

系统采用 左上角为原点 (0,0) 的坐标系（在 UI 布局中通常对应 Top-Left 锚点）。

- X 轴 : 向右增长
- Y 轴 : 向下增长
数据结构 :

```plaintext
// TetrisGridVM.cs
private TetrisItemVM[,] _tetrisItemOccupiedCells; // [width, height]
```

### 3. 像素位置计算公式

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
// InventoryHighlight.cs (类似逻辑)
Vector2 tilePos = new Vector2(
    (point.x + ghost.RotationOffset.x) * tileW,
    -((point.y + ghost.RotationOffset.y) * tileH) // Y轴取负
);
```

### 4. 物品形状与旋转算法

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
// TetrisUtilities.cs
public static List<Vector2Int> RotatePointsClockwise(List<Vector2Int> points)
{
    List<Vector2Int> rotatedPoints = new();
    foreach (var point in points)
    {
        rotatedPoints.Add(new Vector2Int(-point.y, point.x)); // 核心旋转公式
    }
    return rotatedPoints;
}
```

旋转偏移修正 :
单纯旋转会导致形状跑出原有边界（因为是以 0,0 为轴心）。系统引入了 RotationOffset 来修正旋转后的原点位置，确保物品旋转后仍然对齐网格。

$$
Offset_{Up} = (Width - 1, Height - 1)
$$

不同方向有不同的硬编码偏移量，详见 `TetrisUtilities.cs:L66`

### 5. 逻辑实现细节

#### 1. 碰撞检测

放置物品时，系统会遍历物品占据的所有点 $(p_x, p_y)$，并加上基准坐标 $(O_x, O_y)$：

$$
Target(x, y) = (O_x + p_x + Offset_x, O_y + p_y + Offset_y)
$$

验证条件 :

1. 边界检查 : $0 \le Target_x < GridWidth$ 且 $0 \le Target_y < GridHeight$
2. 占用检查 : _tetrisItemOccupiedCells[Target_x, Target_y] 必须为 null 。

#### 2. 物品放置
当验证通过后，物品会被“写入”逻辑网格：

1. 将物品的所有 Target(x, y) 坐标在 _tetrisItemOccupiedCells 中标记为该物品的引用。
2. 更新 View 层，根据公式计算 RectTransform 的 anchoredPosition 。

### 6. 优点与亮点

#### 1. 数学纯粹性 :

- 将复杂的 UI 交互转化为简单的矩阵运算。
- 旋转算法使用了标准的线性代数变换，计算位置算法在图形学中被称为 Affine Transformation（仿射变换），保证了逻辑的严密性。

#### 2. MVVM 分离 :

- 逻辑先行 : 所有的碰撞、旋转、位置计算都在 VM 层（纯 C# 类）完成，不依赖 Unity 的 Transform 或 Physics 。这使得逻辑极其高效且易于单元测试。
- View 层“傻瓜化” : View 只负责根据 VM 算出的坐标设置位置，不参与逻辑判断。

#### 3. 高性能 :

- O(1) 查找 : 通过二维数组直接访问任意格子的状态，无需遍历。
- 零 GC : 核心的高亮系统使用了对象池，且坐标计算全部基于 struct (Vector2Int)，避免了堆内存分配。

4\. 灵活的形状定义 :

- 形状不是预设的贴图，而是坐标点集。这意味着可以轻松创建任意不规则形状（如 L 型、T 型、斜线型、甚至空心形状）。

### 7. 综上所述

线性代数是Tetris坐标系统的底层骨架：

- 线性变换负责旋转。
- 向量加法负责移动。
- 矩阵运算负责把方块坐标最终投影到屏幕像素上

<a id="quick-exchange-system"></a>

## 👨‍💻快捷交换系统 - 智能的异形物品互换

快速交换（Quick Exchange）以坐标计算原理为核心，通过计算两个物品的覆盖区域，实现智能的异形物品互换。当玩家拖拽一个物品并放置到已被其他物品占据的位置时，如果满足特定条件，系统会自动将被覆盖的物品"挤回"到拖拽物品的原始位置，实现智能互换。

### 1. 核心判定条件

快速交换的触发需要同时满足以下条件：

1. **完全覆盖原则**：Ghost（幽灵物品）的覆盖区域必须**完全包含**被覆盖物品的所有格子。换言之，被覆盖物品的每一个格子都必须落在 Ghost 的投影范围内。
2. **无边界溢出**：Ghost 的所有投影格子都必须在目标网格的合法边界内。
3. **原位可容纳**：被挤出的物品必须能够在原物品的起始位置找到合法的放置方案。

### 2. 覆盖区域计算公式

#### Ghost 覆盖区域

设 Ghost 的目标锚点坐标为 $T = (T_x, T_y)$，形状坐标点集为 $S = \{P_1, P_2, ..., P_n\}$，旋转偏移为 $O = (O_x, O_y)$。

Ghost 在网格上占据的所有格子集合 $C_{ghost}$ 定义为：

$$
C_{ghost} = \{ (T_x + P_i.x + O_x, T_y + P_i.y + O_y) \mid P_i \in S \}
$$

#### 被覆盖物品区域

对于网格中已放置的物品 $Item$，其锚点为 $A = (A_x, A_y)$，形状点集为 $S_{item}$，旋转偏移为 $O_{item}$。

该物品占据的格子集合 $C_{item}$ 为：

$$
C_{item} = \{ (A_x + P_j.x + O_{item}.x, A_y + P_j.y + O_{item}.y) \mid P_j \in S_{item} \}
$$

### 3. 完全覆盖验证

系统首先收集所有与 Ghost 覆盖区域重叠的物品集合 $Overlap$：

$$
Overlap = \{ Item \mid C_{item} \cap C_{ghost} \neq \emptyset \}
$$

然后对每个重叠物品验证**完全覆盖条件**：

$$
\forall Item \in Overlap: C_{item} \subseteq C_{ghost}
$$

如果任何物品未被完全覆盖（即存在格子落在 Ghost 投影之外），则快速交换不可触发，高亮显示红色。

### 4. 网格映射算法

当完全覆盖验证通过后，系统需要计算被挤出物品应该如何放置到原物品的起始区域。这通过**网格映射（Grid Mapping）**实现。

#### 坐标映射公式

对于被覆盖物品的每个格子 $cell \in C_{item}$，计算其相对于 Ghost 锚点的偏移：

$$
relative = cell - T - O_{ghost}
$$

在 Ghost 形状点集 $S_{ghost}$ 中查找索引 $k$，使得：

$$
S_{ghost}[k] = relative
$$

再利用原物品（拖拽物品）的对应点计算原始网格坐标：

$$
origin\_cell = A_{original} + O_{original} + S_{original}[k]
$$

最终建立映射关系：

$$
Mapping: cell \rightarrow origin\_cell
$$

### 5. 模式匹配放置

被挤出的物品需要在原物品区域找到合法放置方案。系统采用**四方向旋转模式匹配**：

对于每个方向 $dir \in \{Down, Left, Up, Right\}$：

1. 计算该方向下的旋转点集 $S_{rotated}$
2. 计算对应的旋转偏移 $O_{rotated}$
3. 选取目标区域的左上角参考点 $T_0$
4. 尝试将 $S_{rotated}$ 中的每个点 $P_{ref}$ 对齐到 $T_0$，反推锚点：

$$
Anchor = T_0 - P_{ref} - O_{rotated}
$$

5. 验证该锚点下所有格子是否恰好匹配目标区域：

$$
\forall P \in S_{rotated}: (Anchor + P + O_{rotated}) \in TargetCells
$$

若匹配成功且区域空闲，则完成放置。

### 6. 事务性回滚机制

整个快速交换过程采用**事务性设计**：

```plaintext
1. 保存所有被覆盖物品的原始状态（位置、方向、旋转偏移、形状坐标）
2. 从目标网格移除所有被覆盖物品
3. 尝试将被覆盖物品放置到原物品区域
4. 若任一物品无法放置 → 回滚：恢复所有物品到原始状态
5. 验证目标区域是否已清空
6. 若未清空 → 回滚
7. 放置拖拽物品到目标位置
8. 交换完成
```

这种设计确保了数据一致性——要么全部成功，要么完全回滚，不会出现中间状态。

### 7. 算法复杂度分析

| 操作 | 时间复杂度 |
| --- | --- |
| 覆盖区域构建 | $O(n)$，$n$ 为形状点数 |
| 重叠物品检测 | $O(n)$，哈希查找 |
| 完全覆盖验证 | $O(m \cdot n)$，$m$ 为重叠物品数 |
| 网格映射计算 | $O(n^2)$，最坏情况 |
| 模式匹配放置 | $O(4 \cdot n^2)$，四方向遍历 |

在典型场景下（$n < 10$, $m \leq 3$），整个快速交换判定在毫秒级完成。

<a id="sprite-mesh-raycast-filter"></a>

## 👨‍💻SpriteMeshRaycastFilter - 精确的Tetris形状操作过滤器

它的作用是让鼠标点击更加精准，只响应物体“真实形状”的区域，忽略透明的空白区域。在 Unity（以及大多数游戏引擎）的 UI 系统里，所有的图片默认都是 **矩形** 的。
打个比方：

想象一下你有一个 "L" 形 的俄罗斯方块：

``` plaintext
🟥
🟥
🟥 🟥
```

但在计算机眼里，它其实被包在一个透明的矩形盒子里：

``` plaintext
🟥 ⬜ 
🟥 ⬜  <-- 这里的白色⬜其实是透明的，但在UI逻辑里它属于这个Object
🟥 🟥
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

- **容器**：由 ContainerNode 表示，有一个 ContainerId。
- **物品**：由 ItemNode 表示，有一个 ItemGuid。
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

### 1. 核心差异对比

| 特性  | 传统实现 | 本项目 |
| --- | --- | --- |
| 数据结构 | 嵌套对象引用`class Bag { List<Item> items; }` | 扁平化列表 + 外键`class Item { string containerGuid; }` |
| 查找方式 | 递归遍历：查找物品需要从根节点逐层向下遍历。 | 哈希索引 (O(1))：通过 InventoryTreeCache 直接查询容器内容。 |
| 序列化 | 直接序列化树：易受递归深度限制，JSON 结构层级深。 | 序列化列表：JSON 结构扁平，无嵌套层级，数据更紧凑。 |
| UI 生成 | 预制体嵌套：UI 层级往往直接对应数据层级。 | MVVM 动态绑定：UI 是数据的投影，View 与数据解耦。 |

### 2. 优点

- 💾 **数据安全性与序列化鲁棒性**
  - **避免循环引用**：在传统的嵌套对象中，如果两个物品互相引用（例如互相放入），序列化器会崩溃。扁平化 GUID 设计完全规避了无限递归问题。
  - **存取效率**：保存时只需转储一个列表，无需处理复杂的图结构。
- ⚡ **极速查询性能**
  - 利用 InventoryTreeCache.cs，查找"某个背包里有什么"是字典查找操作，复杂度为 O(1)。
  - 相比之下，传统树结构在查找"全局某个特定 ID 的物品"时，需要 O(N) 的全树遍历。
- 🧩 **灵活的父子关系重组**
  - 移动物品（例如从背包 A 移到背包 B）只需要修改该物品的 containerGuid 字段。不需要在内存中进行复杂的"从 A 列表移除 -> 添加到 B 列表"的操作。
- 💤 **惰性加载支持**
  - 系统可以只加载"物品数据"，而不立即生成所有 UI。只有当玩家真正打开某个子背包时，才通过 InventoryTreeCache 查询内容并生成对应的视图。这对含有大量嵌套物品的大型存档非常友好。

### 3. 缺点

- 🔧 **状态同步复杂度**
  - 必须维护 ViewModel (运行时状态)、InventoryTreeCache (缓存) 与持久化数据 (存档数据) 三者的一致性。
  - 如果开发者忘记在移动物品时更新缓存，会导致逻辑与数据脱节（例如 UI 显示物品在背包里，但数据认为它还在地上）。
  - 在代码层面：`InventoryService.cs` 中有专门的同步方法用于处理这种同步。

### 4. 独特之处

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

### 5. 综上所述

Cholopol-Tetris-Inventory-System 的树结构实现采用了"空间换时间"和"复杂度换灵活性"的方案。牺牲了部分代码的直观性（需要理解 GUID 和缓存机制），换取了优越的数据稳定性和高效的检索能力，非常适合实现《逃离塔科夫》这类物品数量巨大且嵌套复杂的游戏。

<a id="highlight-system"></a>

## 👨‍💻高亮瓦片系统-用丰富多彩的高亮提示客制化你的库存系统

高亮瓦片系统（Highlight System）不仅提高视觉效果，更是用户体验（UX）和性能优化的关键部分。以下是该系统的核心优点与技术亮点：

### 1. MVVM 驱动的"幽灵"预览

系统使用 TetrisItemGhostView（幽灵物品）来模拟放置效果，而不是直接移动真实物品对象。

- **优点**：解耦与安全性。在玩家拖拽物品时，真实物品数据保持不变，仅有视觉上的"幽灵"在移动。只有当玩家松开鼠标且放置合法时，才会真正提交数据更改。
- **亮点**：实时旋转预览。当玩家按下 R 键时，幽灵物品会在内存中旋转其坐标集，高亮系统会立即根据新的形状重新计算可放置性，整个过程流畅无卡顿。

### 2. 对象池化的高性能渲染

高亮瓦片系统并未为每个网格格子预先创建高亮块，而是采用了 **对象池** 技术。

- **代码引用**：InventoryHighlight.cs  

```plaintext
GameObject tile = PoolManager.Instance.GetObject(highlightTilePrefab);
```

- **优点**：零 GC 压力。无论物品形状多复杂，系统只会从池中取出有限数量的 UI 对象。当高亮消失时，这些对象被立即回收（PushObject），而不是销毁。这在频繁的 Update 循环中对性能至关重要。

### 3. 智能的放置验证上下文

高亮颜色不仅仅是简单的"绿"或"红"，它基于一个强大的上下文结构 InventoryPlacementContext 进行判断。

- **代码引用**：InventoryPlacementContext
- **优点**：可扩展的验证逻辑。系统会将当前操作的所有上下文（物品、目标容器、旋转状态、锚点位置）打包：
- **绿色**：位置空闲，完全合法。
- **红色**：位置被占用或超出边界。
- **黄色**：可以堆叠归并。
- **天蓝色**：例如快速交换功能--如果目标位置有物品，但符合交换规则，系统会显示天蓝色高亮，提示玩家松手将执行交换操作。

### 4. 数据驱动的配置

高亮颜色并不是写死在代码中的，而是通过 ScriptableObject（InventoryPlacementConfig_SO）进行配置。

- **优点**：对设计者友好。设计者可以在 Unity 编辑器中直接调整 ValidColor（有效色）和 InvalidColor（无效色）的 RGBA 值及透明度，甚至可以配置不同的高亮策略（例如色盲模式），而无需修改一行代码。


- **逻辑层** (TetrisGridVM)：负责快速的数学计算（占用检测）。
- **表现层** (InventoryHighlight)：利用对象池高效渲染。
- **数据层** (PlacementConfig)：提供灵活的视觉配置。

这种设计确保了即使在低端设备上，无论背包有多大、物品形状多复杂，拖拽操作都能保持丝般顺滑。

<a id="item-editor"></a>

## 👨‍💻定制化的物品编辑器 - 点点鼠标，又增加了一个物品

Cholopol TIS Editor 是一个功能强大的可视化数据编辑工具，让你无需编写代码即可创建和管理库存系统所需的所有数据。编辑器提供了三个核心功能模块：**物品编辑 (Items)**、**形状编辑 (Shapes)** 和 **配置设置 (Config)**。

### 快速开始

- Unity Editor中在菜单栏打开 **CTIS-> Data Editor**。
- 点`＋`创建一个新的物品数据条目自动将该物品添加到 `ItemDataList_SO`。
- 指定精灵和网格形状等信息。
- 背包类物品需指定手动设置的`GridUIPrefab`网格组预制体。

![Editor Overview](Images/editor_0.png)

### 1. 物品编辑 (Items)

物品编辑模块让你轻松管理所有游戏物品的属性：

- **General（通用属性）**：设置物品ID、名称、图标、形状类型、槽位类型和稀有度。支持基于Localization的本地化多语言，或是点击Name后的按钮 `All` 将当前名称应用于所有语言。
- **Properties（物品属性）**：配置物品的宽高、重量和默认朝向。
- **Combat（战斗属性）**：为武器类物品设置战斗相关数据。
- **Vendor（商人属性）**：配置物品的买卖价格。
- **References（引用）**：关联预制体和其他资源。
- **Description（描述）**：为物品添加多语言描述文本。

![Items Editor](Images/editor_1.png)

### 2. 形状编辑 (Shapes)

形状编辑模块提供了直观的网格预览和点编辑功能：

- **Shape Type（形状类型）**：选择或创建新的形状定义。
- **Grid Preview（网格预览）**：实时可视化形状在网格中的占用情况。
- **Points（坐标点）**：精确编辑形状的每个占用格子坐标，支持添加和删除点。

系统预置了多种经典 Tetris 形状（如 Tromino、Tetromino、Pentomino 等），你也可以创建任意自定义形状（如剑形、步枪形等）。

![Shapes Editor](Images/editor_2.png)

### 3. 配置设置 (Config)

配置模块让你自定义库存系统的行为规则和视觉效果：

- **Rules（规则）**：
  - Block Self Owned Container：阻止物品放入自身容器
  - Block Out Of Bounds：阻止物品超出边界
  - Block Slot Occupied：阻止物品放入已占用槽位
  - Block Slot Type Mismatch：阻止槽位类型不匹配

- **Highlight Colors（高亮颜色）**：
  - Valid Empty（有效空位）：绿色 - 表示可放置
  - Invalid（无效）：红色 - 表示不可放置
  - Can Stack（可堆叠）：黄色 - 表示可与现有物品堆叠
  - Can Quick Exchange（可快速交换）：蓝色 - 表示可与现有物品交换

- **Invalid Reason Colors（无效原因颜色）**：为不同的放置失败原因配置专属高亮色。

![Config Editor](Images/editor_3.png)

<a id="runtime-debug-panel"></a>

## 👨‍💻运行时调试面板 - 方便的测试工具

为了方便测试和调试，系统提供了一个无限循环的运行时的物品添加面板，允许你在游戏运行时添加物品。

- **功能入口**：点击屏幕右上角的 "Debug: Place Item Window" 按钮即可打开。
- **实时监控**：面板不仅可以添加物品，还显示了当前的 ViewModel 统计数据（ViewModel Total, ItemVM, GridVM, SlotVM），帮助你监控对象池和内存状态。
- **快速添加**：从列表中点击 "Add" 按钮，即可生成对应的物品视图，随即放置到仓库面板中。

![PlaceItemPanel](Images/PlaceItemPanel.png)

<a id="floating-window-system"></a>

## 👨‍💻浮动容器窗口系统 - 无限嵌套的可视化

浮动容器窗口（FloatingTetrisGridWindow）是实现背包嵌套可视化的核心组件。当玩家打开一个容器类物品（如防弹衣、战术胸挂、背包）时，系统会创建一个可拖拽的浮动窗口来展示其内部网格。

### 1. 异步加载机制

浮动窗口采用 **Addressables 异步加载**，避免阻塞主线程：

```csharp
// 使用 Addressables 异步实例化窗口预制体
var handle = config.floatingGridPanelTemplate.InstantiateAsync(windowContainer.transform);
var go = await handle.Task;
```

- **按需加载**：窗口预制体不会在游戏启动时全部加载，而是在玩家首次打开某类容器时才加载。
- **资源释放**：关闭窗口时自动释放 Addressables 实例，避免内存泄漏。

### 2. 窗口层级管理

系统通过 `FloatingPanelManager` 统一管理所有浮动窗口：

- **聚焦机制**：点击任意浮动窗口会将其置于最顶层，并激活关闭按钮的交互状态。
- **窗口注册**：每个窗口在创建时注册到管理器，关闭时自动注销。

### 3. 网格视图绑定

浮动窗口的核心功能是动态绑定内部网格：

1. **实例化网格 UI**：根据物品的 `gridUIPrefab` 创建内部布局。
2. **GUID 分配**：为每个子网格分配唯一的 GUID（格式：`物品GUID:网格索引`）。
3. **工厂绑定**：调用 `TetrisGridFactory.BindViewToGuid()` 将 View 与 ViewModel 关联。
4. **数据注入**：通过 `InventoryTreeCache` 自动填充该容器内的所有物品。

### 4. 交互特性

- **标题栏拖拽**：点击标题栏区域可拖动整个窗口。
- **边界约束**：窗口位置被限制在 Canvas 范围内，不会拖出屏幕。
- **尺寸自适应**：窗口大小根据内部网格尺寸自动调整。
- **多语言支持**：窗口标题会响应语言切换事件实时更新。

<a id="save-load-system"></a>

## 👨‍💻存档系统 - 持久化你的库存状态

SaveLoadManager 提供了完整的多槽位存档功能，支持将整个库存状态序列化为 JSON 文件并在需要时恢复。

### 1. 核心架构

存档系统采用 **GUID 注册** + **事件驱动** 的设计模式：

```plaintext
┌─────────────────┐     ┌──────────────────┐      ┌─────────────────┐
│   ISaveable     │────▶│  SaveLoadManager│────▶ │   JSON File     │
│  (各个可存储组件)│      │   (统一管理器)   │      │  (持久化文件)    │
└─────────────────┘     └──────────────────┘      └─────────────────┘
```

- **ISaveable 接口**：所有需要存档的组件实现此接口，提供 `GUID`、`GenerateSaveData()` 和 `RestoreData()` 方法。
- **自动注册**：组件在 Awake 时调用 `RegisterSaveable()` 向管理器注册。
- **事件触发**：通过 EventBus 订阅存档/读档事件，解耦调用方。

### 2. 多槽位管理

系统支持多个独立的存档槽位：

- **槽位路径**：`Application.persistentDataPath/SaveData/Slot_{index}.json`
- **时间戳记录**：每次存档记录时间戳，便于显示"最后保存时间"。
- **槽位验证**：读写前自动验证槽位索引合法性。

### 3. 数据结构

存档文件采用**包装器模式**，包含版本号和元数据：

```json

{
  "Version": 1,
  "Timestamp": "2026/01/01 20:30:00",
  "Payload": {
    "dataDict": {
      "c4c7b659-9a95-48a2-9334-9589d93549a0": {
        "inventoryDict": {
          "InventoryData_SO": [
            ...
          ]
        }
      }
    }
  }
}

```

- **版本控制**：支持存档格式迁移（MigrationHandler）。
- **扁平化存储**：所有数据以 GUID 为键存储在字典中，无嵌套结构。

### 4. 存档流程

```plaintext
Save(index):
1. 验证槽位索引
2. 遍历所有注册的 ISaveable
3. 调用 GenerateSaveData() 收集数据
4. 序列化为 JSON
5. 写入文件
6. 记录时间戳
```

### 5. 读档流程

```plaintext
Load(index):
1. 验证槽位索引
2. 读取 JSON 文件
3. 反序列化并执行版本迁移
4. 遍历所有注册的 ISaveable
5. 根据 GUID 匹配数据
6. 调用 RestoreData() 恢复状态
```

### 6. 调试支持

存档系统集成了详细的调试日志：

- **链式日志 ID**：每次存档/读档操作分配唯一链 ID，便于追踪。
- **耗时统计**：使用 Stopwatch 精确测量每次操作耗时。
- **状态报告**：输出成功/失败/跳过的组件数量统计。

<a id="contributing"></a>

## 🤝 贡献指南

欢迎提交Bug报告或功能请求！请遵循以下步骤：

- **代码风格**：
  - 类和方法：PascalCase
  - 变量：camelCase
  - 所有公共方法需添加 XML 注释。

<a id="future-improvements"></a>

## 🔧 未来的改进点

#### V 1.1.0 ：暂时没有发现bug，期待大家的反馈。

<a id="license"></a>

## 📜 许可声明

- 本项目采用 Apache License 2.0 许可证。请参阅 [LICENSE](LICENSE) 文件以获取更多信息。
- 你的项目中必须包含[NOTICE](NOTICE.txt)文件。
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
