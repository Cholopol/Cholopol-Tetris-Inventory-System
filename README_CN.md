# Cholopol's Tetris Inventory System

<div align="center">

<img src=".github/Images/Cover.png" alt="CTIS Cover" width="85%"/>

</div>

![Godot](https://img.shields.io/badge/Godot-4.7+-478CBF?style=flat-square\&logo=godotengine\&logoColor=white) ![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square\&logo=dotnet\&logoColor=white) ![C#](https://img.shields.io/badge/C%23-12-239120?style=flat-square\&logo=csharp\&logoColor=white) ![MVVM](https://img.shields.io/badge/MVVM-DotPudica-0A7E8C?style=flat-square) ![Source Generator](https://img.shields.io/badge/Roslyn-Source_Generator-CB4B16?style=flat-square) ![License](https://img.shields.io/badge/License-Apache_2.0-blue?style=flat-square) [![Bilibili](https://img.shields.io/badge/bilibili-鹿卜Cholopol-blue.svg?style=flat-square\&logo=bilibili)](https://space.bilibili.com/88797367) [![Stars](https://img.shields.io/github/stars/Cholopol/Cholopol-Tetris-Inventory-System?style=flat-square\&logo=github\&color=yellow)](https://github.com/Cholopol/Cholopol-Tetris-Inventory-System/stargazers) [![Forks](https://img.shields.io/github/forks/Cholopol/Cholopol-Tetris-Inventory-System?style=flat-square\&logo=github)](https://github.com/Cholopol/Cholopol-Tetris-Inventory-System/network/members)

[English](README.md) | 简体中文

**CTIS (Cholopol Tetris Inventory System)** 是面向 **Godot 4.7+ 与 .NET 8** 构建的高级网格背包管理系统。系统基于 **DotPudica MVVM** 框架与 **TetrisCoordLib** 纯数学几何库开发，实现了数据逻辑与 UI 表现的彻底解耦。完美还原了《逃离塔科夫》（Escape from Tarkov）的核心交互体验，支持异形物品、无限背包嵌套、智能快捷交换、精确异形点击过滤、浮动容器窗口、背包一键整理以及多槽位数据持久化。

### 第三方依赖

本项目基于以下独立开源库构建（仓库中通过 Release 包一并分发，源码托管于独立 GitHub 仓库）：

| 依赖库                     | GitHub 仓库                                                                             | 用途                                                |
| ----------------------- | ------------------------------------------------------------------------------------- | ------------------------------------------------- |
| **DotPudica Framework** | [Cholopol/dot-pudica-framework](https://github.com/Cholopol/dot-pudica-framework.git) | 编译期源生成器数据绑定、DI 依赖注入、声明式生命周期、窗口管理与对象池              |
| **TetrisCoordLib**      | [Cholopol/tetris-coord-lib](https://github.com/Cholopol/tetris-coord-lib.git)         | 无引擎依赖的纯数学几何库，提供 Vec2I/Mat3x3/XForm2D 等基础类型与仿射变换运算 |

### 项目结构

| 目录            | 用途                                                   |
| ------------- | ---------------------------------------------------- |
| `addons/ctis` | **CTIS 核心交付物**（包含 Core 业务库、Godot 视图层及内置可视化数据编辑器）     |
| `CTIS_Demo`   | **Showcase 演示工程**（含 EFT 风格 UI 场景、示例美术资源、JSON 配置与多语言） |

### 快速开始

本仓库为 CTIS 核心源码仓（含样例演示）。在实际游戏开发中：

1. 确保安装了 **Godot 4.7+ .NET (Mono)** 版与 **.NET 8 SDK**。
2. 从各仓库 Release 页面分别下载插件包：
   - [CTIS 发布包](https://github.com/Cholopol/Cholopol-Tetris-Inventory-System/releases) → 解压至 `addons/ctis`
   - [DotPudica Framework](https://github.com/Cholopol/dot-pudica-framework/releases) → 解压至 `addons/dot-pudica`
   - [TetrisCoordLib](https://github.com/Cholopol/tetris-coord-lib/releases) → 解压至 `addons/tetris_coord_lib`
3. 如需分别了解依赖库能力可以克隆原始仓库：
   - `git clone https://github.com/Cholopol/dot-pudica-framework.git`
   - `git clone https://github.com/Cholopol/tetris-coord-lib.git`
4. 在 Godot 编辑器中打开 **项目 -> 项目设置 -> 插件**，依次启用 **DotPudica**、**TetrisCoordLib** 与 **CTIS**。
5. 插件将自动在你的宿主 `.csproj` 中注入所需的编译配置、依赖包与项目引用，按下方「[快速使用](#快速使用最小可运行配置)」完成初始化即可。

***

## 📕 目录

- [💡 设计理念](#-设计理念)
  - [1. 为什么将背包系统迁移至 Godot .NET](#1-为什么将背包系统迁移至-godot-net)
  - [2. 相比传统背包实现的核心优势](#2-相比传统背包实现的核心优势)
  - [3. 为什么基于 DotPudica MVVM 与 TetrisCoordLib 构建](#3-为什么基于-dotpudica-mvvm-与-tetriscoordlib-构建)
- [🏗️ 全能力鸟瞰与分层架构](#️-全能力鸟瞰与分层架构)
- [🧩 核心系统与算法深度解析](#-核心系统与算法深度解析)
  - [1. Tetris 坐标系统 - 仿射变换与数值的艺术](#1-tetris-坐标系统---仿射变换与数值的艺术)
  - [2. 智能快捷交换系统 - 异形物品的拓扑互换与事务回滚](#2-智能快捷交换系统---异形物品的拓扑互换与事务回滚)
  - [3. 精确异形命中检测 - 消除透明矩形误触](#3-精确异形命中检测---消除透明矩形误触)
  - [4. 背包套娃与树形缓存 - 扁平 GUID 外键模型与 O(1) 检索](#4-背包套娃与树形缓存---扁平-guid-外键模型与-o1-检索)
  - [5. MVVM 幽灵预览与高亮瓦片系统 - 对象池化零 GC 渲染](#5-mvvm-幽灵预览与高亮瓦片系统---对象池化零-gc-渲染)
  - [6. 浮动容器窗口与右键菜单 - 无限嵌套的可视化投影](#6-浮动容器窗口与右键菜单---无限嵌套的可视化投影)
  - [7. 背包自动整理与高级特性 - 面积贪心排序与占位补丁](#7-背包自动整理与高级特性---面积贪心排序与占位补丁)
  - [8. 存档持久化系统 - 包装器模式与版本迁移](#8-存档持久化系统---包装器模式与版本迁移)
  - [9. Godot 内置数据编辑器 - 点点鼠标，又增加了一个物品](#9-godot-内置数据编辑器---点点鼠标又增加了一个物品)
- [🚀 快速使用：最小可运行配置](#-快速使用最小可运行配置)
  - [A. 环境要求](#a-环境要求)
  - [B. 宿主 .csproj 依赖注入](#b-宿主-csproj-依赖注入)
  - [C. 服务注册与运行时启动](#c-服务注册与运行时启动)
  - [D. 场景视图挂载与绑定](#d-场景视图挂载与绑定)
  - [E. 快捷键与操作说明](#e-快捷键与操作说明)
- [🤝 贡献指南](#-贡献指南)
- [📜 许可声明与开源协议](#-许可声明与开源协议)
- [📬 联系方式](#-联系方式)

***

## 💡 设计理念

### 1. 为什么将背包系统迁移至 Godot .NET

原版系统基于 Unity Engine 与第三方反射型 MVVM 框架开发。**CTIS 2.0.0** 全面迁移到了Godot引擎，并使用了自研高性能的MVVM框架与齐次仿射矩阵计算库：

- **功能零损失**：原有的系统功能全部保留，并且新增了非常多的交互功能与底层架构调整。
- **AOT友好**：本项目及插件完全支持 Native AOT 构建，并针对日常交互的代码性能进行了针对性的优化。
- **Godot 4.x + .NET 8 的高生产力**：Godot 拥有轻量且高度模块化的节点系统，配合现代 C# 12 与 .NET 8 的极速 JIT / NativeAOT 表现，是开发重度 UI 系统的理想土壤。
- **自由与开放的社区**：Godot作为开源引擎的最大优势就是开放共享的技术标准与可预见的商业稳定性。在AI智能不断发展的今天，Godot逐渐成为最热门的开源引擎，其社区充满活力，并不断吸引对游戏开发感兴趣的新生力量，并且也吸引了跨平台应用与车机应用的开发者，这使得Godot极具潜力。

### 2. 相比传统背包实现的核心优势

| 对比维度       | 传统引擎与背包写法                               | CTIS (Godot .NET 4.7+)                                        |
| ---------- | --------------------------------------- | ------------------------------------------------------------- |
| **架构分层**   | 逻辑散落在节点脚本中，UI 与数据强耦合                    | **MVVM 严格三层解耦**，ViewModel 是纯 .NET 类                           |
| **数据绑定**   | 手写信号/事件接线，手动遍历控件刷新                      | **DotPudica 编译期源生成器绑定**，零反射、零装箱、AOT 友好                        |
| **错误暴露时机** | 运行期排查（拼错路径/信号名在运行时报空）                   | **编译期静态诊断**，绑定路径错误直接导致编译中断                                    |
| **单元测试**   | 必须启动游戏引擎并实例化场景预制体                       | **Core 层脱离 Godot**，纯 C# 秒级执行全量测试套件                            |
| **异形与旋转**  | 多数仅支持简单矩形（$W \times H$）                 | **任意点集（Point Set）几何定义**，支持四向旋转与偏移矩阵                           |
| **容器嵌套**   | 深度递归对象树，易发生循环引用与反序列化溢出                  | **扁平化 GUID 外键索引 +** **`InventoryTreeCache`**，O(1) 关系检索与按需惰性加载 |
| **交互精度**   | 矩形包围盒阻挡点击，透明空白边缘频繁误触                    | **`ShapeHitTest`** **异形过滤**，只响应有方块的区域，空白处精准穿透                 |
| **生命周期**   | 手动管理 GameObject 的 Instantiate / Destroy | **声明式生命周期**，进树自动绑定，出树自动回收退订                                   |

### 3. 为什么基于 DotPudica MVVM 与 TetrisCoordLib 构建

1. **`TetrisCoordLib`**（[GitHub](https://github.com/Cholopol/tetris-coord-lib.git)）：将旋转、平移、坐标系空间变换以及几何占位运算抽象为纯粹的数学库，不依赖任何上层 UI 或引擎 API。
2. **`DotPudica Framework`**（[GitHub](https://github.com/Cholopol/dot-pudica-framework.git)）：利用 C# Roslyn Source Generator 在编译期静态生成强类型委托绑定代码，彻底告别运行时反射；提供开箱即用的依赖注入（DI）、主线程调度器（UI Dispatcher）、窗口栈管理与对象池化。
3. **数据单一真实来源**：UI 永远是 ViewModel 状态在屏幕上的几何投影，消除多副本状态不一致的可能。
4. 框架提供了完善的声明式生命周期管理与全面的对象池化，不会预先为所有物品和子容器实例化 ViewModel 或 View，而是按需加载，视图窗口关闭即回收复用。

***

## 🏗️ 全能力鸟瞰与分层架构

```mermaid
flowchart TB
  subgraph MathLayer ["1. 数学几何层 (TetrisCoordLib)"]
    Coord["TetrisCoordLib.Core\nVec2I/Vec2F · Mat3x3(3x3齐次矩阵) · XForm2D仿射变换 · ShapeData点集几何"]
  end

  subgraph CoreLayer ["2. 核心业务层 (Ctis.Core - 纯 .NET 8，零引擎依赖)"]
    Logic["InventoryLogic / InventorySimulation\n放置判定 · 快捷交换 · 自动整理 · 占位补丁"]
    Cache["InventoryTreeCache\n扁平 GUID 拓扑关系 · O(1) 容器检索"]
    VM["ViewModel 状态层 (CommunityToolkit.Mvvm)\nTetrisGridVM · TetrisItemVM · TetrisItemGhostVM · TetrisSlotVM"]
    Data["ItemCatalog & JsonSaveLoadService\n静态数据目录 · 多槽位存档 · 版本迁移"]
  end

  subgraph FrameworkLayer ["3. 框架支撑层 (DotPudica)"]
    SG["SourceGenerator 编译期强类型绑定"]
    DI["AppContext / ServiceProvider 依赖注入"]
    WM["GodotWindowManager\n窗口栈管理 · 对象池 · QueuedPopup FIFO"]
  end

  subgraph GodotLayer ["4. 表现与交互层 (Ctis.Godot)"]
    Views["DotPudicaView 控件\nTetrisGridView · TetrisItemView · TetrisItemGhostView · TetrisSlotView"]
    Overlay["HighlightOverlay\n对象池化零 GC 高亮渲染"]
    Hit["ShapeHitTest / UiPick\n精确异形点击过滤"]
    DemoWindows["FloatingInventoryWindows\n浮动容器窗口 · 右键上下文菜单 · 多语言本地化"]
  end

  subgraph EditorLayer ["5. 编辑器扩展 (Ctis.Editor)"]
    Editor["CtisDataEditorHost (Godot 内置面板)\nItems · Shapes · Equipment Layout 一站式编辑"]
  end

  Coord --> Logic
  Coord --> Views
  Logic --> VM
  Cache --> VM
  Data --> Cache
  SG --> Views
  DI --> VM
  WM --> DemoWindows
  VM --> Views
  Hit --> Views
  Overlay --> Views
  DemoWindows --> Views
  Editor -.-> Data
```

***

## 🧩 核心系统与算法深度解析

<a id="tetris-coordinate-system"></a>

### 1. Tetris 坐标系统 - 仿射变换与数值的艺术

系统的网格容器将连续的 UI 像素空间离散化为二维整数矩阵，所有碰撞、旋转与对齐均基于严格的笛卡尔坐标系运算。

```mermaid
flowchart LR
  GridOrigin["网格原点 (0, 0)\n[Top-Left 锚点]"] -->|X 轴向右增长| Col["列号 (x)"]
  GridOrigin -->|Y 轴向下增长| Row["行号 (y)"]
  Col & Row --> Calc["仿射变换计算屏幕像素坐标"]
  Calc --> Pixel["UI 像素坐标 (Px, Py)\nPx = x * W_unit\nPy = y * H_unit"]
```

#### 1. 像素位置映射公式

设网格单元像素宽高为 $W\_{unit}, H\_{unit}$，物品在网格中的逻辑原点为 $(x, y)$，则映射至 Godot UI 局部坐标 $(P\_x, P\_y)$ 的仿射变换公式为：

$$
P\_x = x \times W\_{unit}
$$

$$
P\_y = y \times H\_{unit}
$$

> \[!NOTE]
> 在 Godot Control 坐标系中，Y 轴向下为正，与网格逻辑坐标系完全一致，无需进行符号取反，计算更自然高效。

#### 2. 形状定义与四向旋转矩阵算法

物品的占位并非固定的贴图包围盒，而是相对于原点 $(0,0)$ 的离散点集：$\mathcal{S} = { (p\_{x1}, p\_{y1}), (p\_{x2}, p\_{y2}), \dots, (p\_{xn}, p\_{yn}) }$。

顺时针旋转 $90^\circ$ 的二维线性变换矩阵为：

$$
\begin{bmatrix} x' \ y' \end{bmatrix} = \begin{bmatrix} 0 & -1 \ 1 & 0 \end{bmatrix} \begin{bmatrix} x \ y \end{bmatrix} = \begin{bmatrix} -y \ x \end{bmatrix}
$$

```csharp
// TetrisCoordLib.Core / 纯数学旋转算法
public static Vec2I RotateClockwise(Vec2I point) => new(-point.Y, point.X);
public static Vec2I RotateCounterClockwise(Vec2I point) => new(point.Y, -point.X);
```

#### 3. 旋转原点偏移修正（Rotation Offset）

由于绕 $(0,0)$ 轴心旋转会导致负坐标溢出，系统通过计算几何外包围矩形，引入了 `RotationOffset` 修正向量，确保物品旋转后始终紧贴网格对齐：

$$
Target(x, y) = (Origin\_x + p\_x + Offset\_x,\ Origin\_y + p\_y + Offset\_y)
$$

***

<a id="quick-exchange-system"></a>

### 2. 智能快捷交换系统 - 异形物品的拓扑互换与事务回滚

快捷交换（Quick Exchange）允许玩家拖拽一个物品放置在已被占据的区域时，若满足特定几何覆盖条件，系统会自动将被覆盖的异形物品“挤回”并精准放入原拖拽物品腾出的空间中，实现一步互换。

```mermaid
flowchart TD
  Start(["玩家拖拽物品放置"]) --> C1{"1. 完全覆盖验证\n所有重叠物品的所有格子\n是否都在 Ghost 投影内？"}
  C1 -- 否 --> Block["显示红色高亮 / 阻止放置"]
  C1 -- 是 --> C2{"2. 边界合法性\nGhost 是否完全在网格内？"}
  C2 -- 否 --> Block
  C2 -- 是 --> Tx["开启事务：快照备份所有物品状态"]
  Tx --> Remove["从网格中临时移除所有被覆盖物品"]
  Remove --> Match{"3. 模式匹配\n尝试四向旋转拟合放入原空间"}
  Match -- 匹配失败 --> Rollback["事务回滚：还原所有被覆盖物品"]
  Rollback --> Block
  Match -- 匹配成功 --> Commit["事务提交：放置原拖拽物品至目标位置"]
  Commit --> Finish(["互换成功，高亮显示天蓝色"])
```

#### 核心判定与映射原理

1. **完全覆盖原则**：Ghost 覆盖集合 $C\_{ghost}$ 必须完全包含所有重叠物品的占据点集 $C\_{item}$：

$$
\forall Item \in Overlap, \quad C\_{item} \subseteq C\_{ghost}
$$

1. **四方向模式匹配（Pattern Matching）**：
   对于被挤出的物品，系统遍历其四个朝向 $dir \in {0^\circ, 90^\circ, 180^\circ, 270^\circ}$，选取原释放区域的参考点 $T\_0$，反推合法锚点并验证点集完全重合：

$$
Anchor = T\_0 - P\_{ref} - Offset\_{rotated}
$$

1. **事务性一致性保障**：整个交换过程具有 ACID 原子性——一旦任何一个被覆盖物品无法在源空间中找到合法契合点，立即全量回滚，绝不残留脏数据。

***

<a id="sprite-mesh-raycast-filter"></a>

### 3. 精确异形命中检测

在游戏 UI 渲染中，所有控件的包围盒默认均为**矩形**。对于 "L" 形、"T" 形或对角线长枪等异形物品，如果直接使用矩形点击，会导致右上角透明空白处拦截鼠标事件，严重破坏密集背包整理的手感。

#### 判定流水线（`ShapeHitTest.cs` & `UiPick.cs`）

```mermaid
flowchart LR
  Input["鼠标指针移动 / 点击"] --> Step1["1. 计算鼠标相对于物品 Control 局部坐标"]
  Step1 --> Step2["2. 映射为逻辑方格索引 (col, row)"]
  Step2 --> Step3{"3. 查表：当前形状在该旋转方向下\n(col, row) 是否有实体方块？"}
  Step3 -- 存在方块 --> Accept["拦截事件：命中该物品"]
  Step3 -- 空白区域 --> Pass["放行事件：穿透至下层物品或背景网格"]
```

- **体验收益**：多件复杂异形武器并排堆叠时，鼠标悬停与点击精确对应视觉图案，无任何操作盲区。
- **性能优化**：对旋转点集进行本地缓存（`_cachedOccupiedPoints`），矩阵查询复杂度为 $O(1)$，拖拽过程零 GC 压力。

***

<a id="nested-inventory-guid"></a>

### 4. 背包套娃与树形缓存 - 扁平 GUID 外键模型与 O(1) 检索

在《逃离塔科夫》类游戏中，“背包里装战术胸挂、胸挂里装弹夹、弹夹包里装子弹”是极常见的多层嵌套机制。传统方案直接使用嵌套对象树（`class Bag { List<Item> Items; }`），容易导致递归死锁、序列化层级过深以及内存开销大。

CTIS 引入了类似关系型数据库的 **扁平化存储 +** **`InventoryTreeCache`** **拓扑缓存** 设计：

```mermaid
sequenceDiagram
  autonumber
  participant View as TetrisGridView (UI)
  participant VM as TetrisGridVM
  participant Cache as InventoryTreeCache (拓扑中介)
  participant Data as 扁平数据列表 (JSON / DTO)

  Note over Cache, Data: 游戏启动 / 读档时构建缓存
  Data->>Cache: 遍历扁平列表注册 itemGuid 与 containerGuid
  Cache-->>Cache: 建立 ContainerID -> List<ItemGuid> 索引映射

  Note over View, VM: 玩家打开某个嵌套子背包
  View->>VM: 绑定至子网格 GUID (如 "Bag_GUID:1")
  VM->>Cache: 请求数据：GetItemsInContainer("Bag_GUID:1")
  Cache-->>VM: O(1) 快速返回该容器内的所有物品数据
  VM->>View: 驱动 DotPudica 数据绑定生成对应物品视图
```

#### 传统树 vs CTIS 拓扑中介对比

| 特性            | 传统嵌套对象树              | CTIS 扁平 GUID + 拓扑缓存                            |
| ------------- | -------------------- | ---------------------------------------------- |
| **内存结构**      | 深度递归引用对象             | 扁平数据列表 + GUID 外键引用                             |
| **容器查找**      | $O(N)$ 递归深搜遍历全树      | **$O(1)$ 字典哈希索引查找**                            |
| **数据安全性**     | 互相装入易发生循环引用死锁        | **Floyd 龟兔赛跑算法 O(1) 空间检测自包含**，放置前阻断循环引用        |
| **UI 视图生命周期** | 关闭 UI 时若销毁对象可能导致数据丢失 | **UI 与数据彻底分离**：关闭面板仅回收 View 节点，数据完好保留在 Cache 中 |
| **惰性加载**      | 启动必须全部反序列化生成对象       | **按需加载**：打开某个背包时才即时从 Cache 获取子节点               |

#### 高性能位棋盘占位检测（OccupancyBoard）

每个容器节点（包括主背包和任意深度的内嵌容器）都挂载一个 `OccupancyBoard` 实例，采用类似国际象棋引擎的**位棋盘（Bitboard）思想**进行 $O(1)$ 碰撞检测：

```mermaid
flowchart LR
  subgraph Board["OccupancyBoard 内部结构 (行优先一维数组)"]
    Cells["_cells: int[]<br/>index = y * Width + x<br/>0 = 空, 1..N = 占用者索引"]
    Index["_indexByGuid: Dictionary<br/>Guid → 占用者索引"]
    Footprint["_footprints: Dictionary<br/>Guid → OccupantFootprint<br/>(锚点 + 相对坐标缓存)"]
  end

  Query["放置查询 / 拖拽悬停检测"] --> Cells
  Place["物品放置"] --> Mark["Mark(): 按足迹写入占用索引"]
  Remove["物品移除"] --> Unmark["Unmark(): 按足迹清除索引<br/>(无需全表扫描)"]
  Search["寻找空位"] --> Skip["RowHasNoHole(): 跳过满行"]
```

**核心性能优化点：**

1. **一维数组行优先存储**：网格占用信息存储在连续内存的 `int[]` 中，`index = y * Width + x` 直接定位，单格查询 $O(1)$，CPU 缓存命中率极高。
2. **满行跳过优化**：`TryFindFreeOrigin` 搜索空位时先调用 `RowHasNoHole` 检查该行是否已完全填满，直接跳过整行扫描，高填充率下性能提升显著。
3. **无分配覆盖扫描**：`ScanCoverage` 方法统计重叠区域的唯一占用者数量时，仅用两个局部变量计数（0 = 空位、1 = 单物品可交换/堆叠、≥2 = 多物品冲突），**拖拽悬停每帧零 GC 分配**。
4. **足迹缓存增量更新**：每个物品的占据点集作为 `OccupantFootprint` 缓存，移除或移动物品时直接按足迹清除对应格子，无需扫描整个棋盘。
5. **统一嵌套模型**：无论主背包还是物品内嵌的子网格（ContainerId 格式为 `{itemGuid}:{index}`），均使用相同的 `OccupancyBoard` 结构与检测逻辑，嵌套深度对检测性能无影响。

#### 零分配循环依赖检测 - Floyd 龟兔赛跑算法

嵌套背包系统必须防止玩家将物品放入其自身的子容器中（例如把背包 A 放进背包 A 里的小包 B，再把小包 B 放回背包 A 形成死循环）。`InventoryTreeCache.IsDescendantContainer` 采用**字符串前缀快速路径 + Floyd 快慢指针判圈**的组合算法，空间复杂度 $O(1)$，拖拽检测过程零 GC 分配：

```mermaid
flowchart TD
  Start["检测: 能否将物品 Item 放入容器 Target?"] --> FastPath{"快速路径 (O(1))\nTargetId 以 'ItemGuid:' 开头?"}
  FastPath -- 是 --> Block["拒绝: 放入直接子容器"]
  FastPath -- 否 --> Init["初始化\n慢指针 slow = TargetId\n快指针 fast = TargetId"]

  Init --> Loop["slow 向上走一步 (父容器)\nfast 向上走两步 (父容器的父容器)"]
  Loop --> Match{"任意指针命中 ItemGuid?"}
  Match -- 是 --> Block
  Match -- 否 --> Null{"指针到达根节点(null)?"}
  Null -- 是 --> Allow["允许: 不在祖先链上"]
  Null -- 否 --> Meet{"slow == fast?"}
  Meet -- 是 --> Cycle["检测到环但不含目标\n返回拒绝(防止无限循环)"]
  Meet -- 否 --> Loop
```

**算法特性：**

1. **前缀快速路径**：容器ID采用 `{父物品Guid}:{网格索引}` 的命名约定（如 `BagA_Guid:0` 表示背包A的第0个内嵌网格），直接通过字符串前缀匹配即可 $O(1)$ 检测直接子容器，无需遍历。
2. **Floyd 快慢指针（龟兔赛跑）**：对于深层嵌套，维护两个指针：
   - **慢指针（乌龟）**：每次向上追溯1层父容器
   - **快指针（兔子）**：每次向上追溯2层父容器
   - 每一步都检查当前容器的所有者是否为目标物品
3. **环检测天然支持**：若快慢指针相遇（`slow == fast`），说明容器树中存在环但环内不含目标物品，立即终止返回，避免无限递归。
4. **零堆分配**：整个检测过程仅使用几个字符串局部变量，无需 `HashSet`/`Stack` 等访问者集合，拖拽悬停高频调用无GC压力。
5. **任意深度支持**：可正确处理背包→胸挂→弹夹包→子弹套……任意深度的嵌套场景。

***

<a id="highlight-system"></a>

### 5. MVVM 幽灵预览与高亮瓦片系统 - 对象池化零 GC 渲染

系统使用 `TetrisItemGhostVM`（幽灵物品）模拟拖拽与悬停放置，在玩家未释放鼠标前，真实背包数据绝不发生变更。

```mermaid
flowchart TD
  Drag["玩家拖拽物品悬停在网格上方"] --> Context["构建 InventoryPlacementContext 上下文\n(物品数据 · 旋转朝向 · 目标容器 · 锚点坐标)"]
  Context --> Eval{"EvaluateDrop 评估结果"}
  
  Eval -->|Vacant| C1["有效空位 -> 绿色高亮 (Valid)"]
  Eval -->|Blocked| C2["阻挡/越界 -> 红色高亮 (Invalid)"]
  Eval -->|Stack| C3["可堆叠归并 -> 黄色高亮 (CanStack)"]
  Eval -->|Exchange| C4["可快捷交换 -> 天蓝色高亮 (CanQuickExchange)"]
  Eval -->|InnerInsert| C5["可装入内嵌容器 -> 紫色高亮 (InnerInsert)"]

  C1 & C2 & C3 & C4 & C5 --> Pool["NodePool 对象池取出高亮瓦片"]
  Pool --> Render["HighlightOverlay 渲染色块"]
  Render --> Clean["鼠标移出 -> 瓦片全部归还对象池（零堆分配）"]
```

- **数据驱动配置**：所有状态的高亮颜色、透明度均通过 `PlacementConfig.json` 集中配置，支持在运行时动态切换色盲辅助模式。
- **对象池化技术**：借助 `NodePool` 循环复用高亮方块节点，高频拖拽与旋转帧率稳定无卡顿。

***

<a id="floating-window-system"></a>

### 6. 浮动容器窗口与右键菜单 - 无限嵌套的可视化投影

当玩家在背包中右键打开容器类装备（如战术背心、背包、医疗箱）时，系统会动态唤起可拖拽的浮动网格窗口（`FloatingGridWindow`）。

```mermaid
flowchart LR
  RClick["右键点击物品 / 快捷键"] --> Menu["唤起 ContextMenuWindow (检查/打开/旋转/卸下/丢弃)"]
  Menu -->|选择 '打开'| Mgr["FloatingInventoryWindows + GodotWindowManager"]
  Mgr --> Spawn["通过对象池 ShowPooled 实例化 FloatingGridWindow"]
  Spawn --> Bind["根据 '物品GUID:网格索引' 动态绑定 TetrisGridView"]
  Bind --> Focus["多窗口层级管理 (点击置顶 / 屏幕边界约束 / 窗口数量限制)"]
```

- **多窗口生命周期**：支持同时开启多个各级嵌套窗口，自动维护 Z-Index 聚焦层级与边界防溢出裁剪。
- **动态 GUID 绑定**：浮动窗口内的网格组件与主背包网格使用完全相同的 `TetrisGridView`，通过绑定不同的 GUID 即可复用全部交互逻辑。

***

<a id="organize-system"></a>

### 7. 背包自动整理与高级特性 - 面积贪心排序与占位补丁

#### 1. 一键自动整理（Auto Organization）

支持在单个网格、指定容器或全局范围内调用 `TryOrganizeGrid`。内置 **面积优先（Area-First）** 与权重贪心装箱算法：

1. 提取当前容器内所有物品并按占位面积由大到小排序。
2. 优先尝试标准朝向，若无法容纳则尝试顺时针旋转 $90^\circ$。
3. 自左上向右下寻找首个可容纳锚点，一键完成紧凑规整排列。

#### 2. 动态占位补丁（Occupancy Patch）

支持武器配件改装时动态修改物品的占位形状（如装上长弹匣或消音器后，物品在网格上的占用点集动态扩充；拆下配件后立即缩小还原），并通过 `ApplyOccupancyPatch` 自动同步至数据树。

#### 3. 网络与命令支持（Command & Replay）

所有核心操作均封装为 `InventoryCommand`（含 `CommandId` 与版本校验号），天然支持多人联机网络同步回放与撤销重做（Undo/Redo）。

***

<a id="save-load-system"></a>

### 8. 存档持久化系统 - 包装器模式与版本迁移

`JsonSaveLoadService` 提供了解耦优秀的数据持久化方案：

```mermaid
flowchart LR
  RuntimeState["运行时内存状态<br/>(ItemVMRegistry & TreeCache)"] -->|Serialize| DTO["扁平 Payload 数据字典<br/>(以 GUID 为 Key)"]
  DTO --> Wrap["包装器元数据封装<br/>(Version · Timestamp)"]
  Wrap --> SaveFile[("JSON 存档文件<br/>user://SaveData/Slot_{id}.json")]

  SaveFile -->|Deserialize| Unpack["读取 JSON 并校验版本号"]
  Unpack --> Clear["清空 Registry 与 TreeCache"]
  Clear --> Apply["将物品数据写入 TreeCache"]
  Apply --> Notify["触发 Restored 事件"]
  Notify --> VM["ViewModel 调用 RebuildFromCache<br/>从 TreeCache 重建物品集合"]
  VM --> View["DotPudica 数据绑定<br/>自动刷新 View 层"]
```

- **多槽位管理**：支持独立的存档插槽切换，记录时间戳与存档元数据。
- **版本校验**：加载时校验 `CatalogVersion`，版本不匹配时拒绝加载以避免数据损坏（预留迁移接口）。

***

<a id="item-editor"></a>

### 9. Godot 内置数据编辑器 - 点点鼠标，又增加了一个物品

CTIS 在 Godot 编辑器内集成了全功能可视化数据工作台（**项目菜单 -> 工具 -> CTIS/Data Editor**）：

<div align="center">

<img src="Images/editor_0.png" alt="CTIS Data Editor" width="80%"/>

</div>

```mermaid
flowchart TD
  Editor["CTIS Data Editor 可视化编辑器"]
  Editor --> P1["1. Items 物品管理\nID · 多语言名称 · 图标 · 尺寸重量 · 槽位类型 · 内部网格预设 · 战斗属性"]
  Editor --> P2["2. Shapes 形状编辑\n预置多米诺方块 · 自定义点集可视化点击勾选 · 实时旋转预览"]
  Editor --> P3["3. Config 规则配置\n自身容器嵌套限制 · 越界规则 · 各状态高亮颜色 RGBA 自定义"]
  Editor --> P4["4. Equipment Layout\n玩家角色纸娃娃装备槽位坐标与挂载类型布局"]
```

- **双向数据同步**：编辑器保存后直接更新 `ItemCatalog.json`、`PlacementConfig.json` 与 `EquipmentLayout.json`，无需重启编辑器即可生效。
- **多语言便捷填充**：支持一键将当前名称与描述应用至所有语言本地化词条。

***

## 🚀 快速使用：最小可运行配置

### A. 环境要求

| 依赖项                     | 最低版本要求                | 说明                                                                             |
| ----------------------- | --------------------- | ------------------------------------------------------------------------------ |
| **Godot Engine**        | **4.7.x .NET** (Mono) | 需支持 C# 的 Godot 4.7 引擎                                                          |
| **.NET SDK**            | **.NET 8.0** SDK      | 推荐 C# 12 编译器环境                                                                 |
| **DotPudica Framework** | 随 Release 包分发         | [GitHub 仓库](https://github.com/Cholopol/dot-pudica-framework.git)，MVVM 框架与源生成器 |
| **TetrisCoordLib**      | 随 Release 包分发         | [GitHub 仓库](https://github.com/Cholopol/tetris-coord-lib.git)，数学几何坐标库          |

### B. 宿主 `.csproj` 依赖注入

在 Godot 编辑器中启用 **CTIS** 插件后，`plugin.gd` 会自动在你的游戏工程 `.csproj` 中注入并维护以下配置块（**无需手动编辑**）：

```xml
<!-- Ctis:Begin -->
  <ItemGroup>
    <Compile Remove="addons/ctis/**/*.cs" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include="addons/ctis/editor/CtisDataEditorHost.cs" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="addons/ctis/Core/Ctis.Core.csproj" />
    <ProjectReference Include="addons/ctis/Godot/Ctis.Godot.csproj" />
  </ItemGroup>
<!-- Ctis:End -->
```

### C. 服务注册与运行时启动

在游戏的全局入口（如 `Main.cs` 或场景根节点）中配置 DI 依赖注入服务并完成数据加载：

```csharp
using Godot;
using Ctis.Core;
using Ctis.Presentation;
using DotPudica.Godot.Views;
using Microsoft.Extensions.DependencyInjection;

public partial class GameBootstrap : Node
{
    public override void _Ready()
    {
        // 1. 初始化窗口管理器
        var wm = GetNodeOrNull<GodotWindowManager>("WindowManager")
            ?? new GodotWindowManager { Name = "WindowManager" };
        if (wm.GetParent() == null)
            AddChild(wm);

        // 2. 初始化 AppContext 并注册服务
        var app = new AppContext().Initialize(services =>
        {
            services.AddCtis();           // 注册 CTIS 核心业务服务
            services.AddCtisGodot();      // 注册 CTIS Godot 交互服务
            services.AddSingleton<IFloatingInventoryWindows, FloatingInventoryWindows>();
            services.AddSingleton<IInventorySession, InventorySession>();
        }, wm);

        // 3. 加载物品目录与配置表
        ItemCatalogLoader.LoadInto(app.Services.GetRequiredService<IItemCatalog>());
        PlacementConfigLoader.LoadInto(app.Services.GetRequiredService<PlacementConfig>());
        EquipmentLayoutLoader.LoadInto(app.Services.GetRequiredService<EquipmentLayout>());

        // 4. 配置窗口对象池（场景路径 + 池大小）
        wm.ConfigurePool<InventoryWindow>("res://CTIS_Demo/demo/InventoryWindow.tscn", 1);
        wm.ConfigurePool<FloatingGridWindow>("res://CTIS_Demo/demo/FloatingGridWindow.tscn", 8);
        wm.ConfigurePool<ContextMenuWindow>("res://CTIS_Demo/demo/ContextMenuWindow.tscn", 2);

        CtisRuntime.Attach(this, wm);
    }
}
```

### D. 场景视图挂载与绑定

在 Godot UI 场景中，为网格控件挂载 `TetrisGridView`，通过 DotPudica 声明式绑定连接到 ViewModel：

```csharp
using Godot;
using DotPudica.Godot;
using Ctis.Presentation;

// 非对象池化视图使用标准模式
[DotPudicaView(typeof(TetrisGridVM))]
public partial class PlayerBackpackView : TetrisGridView
{
    public override void _Ready() => InitializeView();

    public override void _ExitTree()
    {
        DisposeView();
        base._ExitTree();
    }
}

// 对象池化窗口（如 FloatingGridWindow）使用 RecycleView
[DotPudicaView(typeof(FloatingGridVM), Pooled = true)]
public partial class FloatingGridWindow : GodotWindow
{
    public override void _Ready() => InitializeView();

    public override void _ExitTree()
    {
        RecycleView();  // 对象池复用：解绑但不销毁节点
        base._ExitTree();
    }
}
```

### E. 快捷键与操作说明

| 操作 / 按键          | 触发动作               | 逻辑说明                               |
| ---------------- | ------------------ | ---------------------------------- |
| **鼠标左键拖拽**       | 拿起 / 移动物品          | 激活 `TetrisItemGhostVM`，进入放置预览状态    |
| **R 键**          | 顺时针旋转物品 $90^\circ$ | 动态变换点集并重新计算 `RotationOffset` 与高亮状态 |
| **鼠标右键**         | 弹出上下文菜单            | 快捷执行查看属性、旋转、卸下、丢弃、打开子容器等操作         |
| **B 键**          | 打开 / 关闭主背包面板       | 切换背包 UI 显示，触发视图进树与出树生命周期           |
| **Ctrl + 左键点击**  | 快捷转移 / 自动归置        | 将物品快速在装备槽、主背包与外接容器间移动              |
| **右上角 Debug 按钮** | 打开运行时物品生成面板        | 实时测试任意物品生成、监控当前 VM 数量与内存状态         |

***

## 🤝 贡献指南

欢迎提交 Issue 报告缺陷或发起 Pull Request 贡献代码！请遵循以下规范：

- **代码规范**：遵循 C# 官方编码风格，类型与公开成员采用 `PascalCase`，局部变量采用 `camelCase`，核心接口需添加标准的 XML 注释文档。
- **架构约定**：保持 `Ctis.Core` 与 `TetrisCoordLib.Core` 对 Godot 引擎的**零依赖**，所有引擎相关代码必须收敛于 `Ctis.Godot` 与 `TetrisCoordLib.Godot` 中。
- **测试验证**：目前需要自行编写测试脚本进行测试。

***

## 📜 许可声明与开源协议

- 本项目采用 **Apache License 2.0** 许可证开源。详情请参阅 [LICENSE](LICENSE) 文件。
- 衍生项目或商业工程中必须包含 [NOTICE.txt](NOTICE.txt) 声明文件。
- 根据 Apache 2.0 协议 **Section 4(b)** 条款，商业项目中使用的源码版权声明不得移除；若对源码进行修改，必须在文件头注明修改者与修改日期：

```csharp
// Modified by [Your Name] [Year]:
// [Brief description of changes]
```

> \[!WARNING]
> **严禁将本项目用于任何形式的抄袭、洗稿、盗版倒卖等侵害开源权益的行为。开源精神需要共同守护，欢迎各位向作者本人提供相关侵权线索。**

***

## 📬 联系方式

如果你在接入或使用过程中遇到任何问题，或有深入交流的想法，欢迎通过以下方式联系：

- 📧 **电子邮箱**：`cholopol@163.com`
- 📺 **Bilibili 主页**：[鹿卜Cholopol](https://space.bilibili.com/88797367)
- 💬 **GitHub Issues**：[提交反馈与建议](https://github.com/Cholopol/Cholopol-Tetris-Inventory-System/issues)

