import { create } from 'zustand'

// 语言配置
export type Language = 'zh-CN' | 'en-US'

interface Translation {
  [key: string]: string
}

interface TranslationStore {
  currentLanguage: Language
  translations: Record<Language, Translation>
  setLanguage: (lang: Language) => void
  t: (key: string) => string
}

// 翻译内容
const translations: Record<Language, Translation> = {
  'zh-CN': {
    // 导航菜单
    'nav.home': '首页',
    'nav.gettingStarted': '快速开始',
    'nav.documentation': '开发文档',
    'nav.api': 'API 文档',
    'nav.examples': '示例',
    'nav.contributing': '贡献指南',
    
    // 首页标题
    'home.title': 'Cholopol\'s Tetris Inventory System Wiki',
    'home.subtitle': '完整的技术文档和API参考，帮助你快速上手并深入了解项目架构',
    
    // 首页卡片
    'card.github.title': 'Github仓库',
    'card.github.description': '查看Cholopol\'s Tetris Inventory System的完整源代码和项目介绍。',
    'card.github.link': '访问仓库',
    
    'card.quickstart.title': '快速开始',
    'card.quickstart.description': '快速上手Cholopol\'s Tetris Inventory System Wiki，了解项目基本用法和配置。',
    'card.quickstart.link': '快速开始',
    
    'card.documentation.title': '开发文档',
    'card.documentation.description': '详细的开发文档，包含项目架构、配置说明和最佳实践。',
    'card.documentation.link': '查看文档',
    
    'card.api.title': 'API文档',
    'card.api.description': '完整的API参考文档，包含所有接口说明和使用示例。',
    'card.api.link': 'API文档',
    
    'card.examples.title': '示例',
    'card.examples.description': '丰富的代码示例和使用案例，帮助你更好地理解和使用项目。',
    'card.examples.link': '查看示例',
    
    'card.contributing.title': '贡献指南',
    'card.contributing.description': '了解如何为项目做出贡献，包含开发规范提交流程等。',
    'card.contributing.link': '贡献指南',
    
    'card.external.title.bilibili': 'Bilibili',
    'card.external.title.youtube': 'YouTube',
    'card.external.title.xiaohongshu': '小红书',
    'card.external.description.bilibili': '访问作者的Bilibili主页',
    'card.external.description.youtube': '访问作者的YouTube频道',
    'card.external.description.xiaohongshu': '访问作者的小红书主页',
    'card.external.link': '访问链接',

    // Getting Started
    'gettingStarted.title': '快速开始',
    'gettingStarted.subtitle': '本指南将帮助你快速开始使用 Cholopol\'s Tetris Inventory System。',
    'gettingStarted.requirements.title': '环境要求',
    'gettingStarted.requirements.item1': 'Unity 2022.3.52f1 或更高版本。',
    'gettingStarted.import.title': '导入步骤',
    'gettingStarted.import.step1.title': '1. 克隆仓库',
    'gettingStarted.import.step2.item1': '在 Unity Hub 中打开项目。',
    'gettingStarted.import.step2.item2': '等待 Unity 导入资源与依赖包。',
    'gettingStarted.basicUsage.title': '基础使用示例',
    'gettingStarted.basicUsage.item1': '导航到',
    'gettingStarted.basicUsage.item2': '打开',
    'gettingStarted.basicUsage.item3': '点击播放（Play）。',
    'gettingStarted.basicUsage.controls.title': '控制说明',
    'gettingStarted.basicUsage.controls.b': '按 B',
    'gettingStarted.basicUsage.controls.b.desc': '：打开/关闭背包。',
    'gettingStarted.basicUsage.controls.r': '按 R',
    'gettingStarted.basicUsage.controls.r.desc': '：在拖拽时旋转物品。',
    'gettingStarted.basicUsage.controls.lmb': '鼠标左键',
    'gettingStarted.basicUsage.controls.lmb.desc': '：拖动物品。',
    'gettingStarted.basicUsage.controls.rmb': '鼠标右键',
    'gettingStarted.basicUsage.controls.rmb.desc': '：打开物品操作上下文菜单。',
    'gettingStarted.thirdParty.title': '第三方依赖',
    'gettingStarted.thirdParty.subtitle': '一般情况下无需手动安装，如遇问题请参考以下内容：',
    'gettingStarted.thirdParty.loxodon.title': 'Loxodon 框架',
    'gettingStarted.thirdParty.loxodon.desc': 'Loxodon Framework 是一个优秀的 Unity MVVM 架构开源框架。在本项目中通过向',
    'gettingStarted.thirdParty.loxodon.desc2': '添加以下内容安装：',
    'gettingStarted.thirdParty.loxodon.link': '如遇问题可参考官方安装方式：Loxodon Installation',
    'gettingStarted.thirdParty.localization.title': 'Localization',
    'gettingStarted.thirdParty.localization.desc': '使用 Unity 官方本地化插件，通过 Unity Package Manager 安装',
    'gettingStarted.thirdParty.json.title': 'Newtonsoft-json',
    'gettingStarted.thirdParty.json.desc': '性能优秀的 JSON 序列化工具，通过 Unity Package Manager 安装',
    'gettingStarted.nextSteps.title': '下一步',
    'gettingStarted.nextSteps.docs': '开发文档',
    'gettingStarted.nextSteps.docsDesc': '深入了解核心架构和实现细节',
    
    // Documentation
    'doc.title': '开发文档',
    'doc.subtitle': '详细的开发文档，包含项目架构、配置说明和最佳实践。帮助你快速理解项目结构并开始开发。',

    // Core Features
    'doc.core.title': '核心功能亮点',
    'doc.core.feature1.title': '完美的 Tetris 瓦片设计',
    'doc.core.feature1.desc': '支持物品旋转与基于网格的放置逻辑以及更多样的操作，更能按 Tetris 形状过滤用户操作。',
    'doc.core.feature2.title': 'MVVM 架构',
    'doc.core.feature2.desc': 'Item/Grid/Slot/Ghost 系统实现 View/ViewModel/Model 逻辑解耦。',
    'doc.core.feature3.title': '存档系统',
    'doc.core.feature3.desc': '强大的序列化系统用于持久化库存状态。',
    'doc.core.feature4.title': '更多特性',
    'doc.core.feature4.item1': '自定义编辑器：内置 UI Toolkit 编辑器方便创建物品',
    'doc.core.feature4.item2': '本地化：集成多语言支持',

    // Core Components
    'doc.components.title': '核心组件',
    'doc.components.mvvm.title': '1. MVVM 架构实现',
    'doc.components.mvvm.desc': '摒弃了传统的 MonoBehaviour 强耦合写法，而是采用数据驱动开发。',
    'doc.components.mvvm.vm.title': 'ViewModel (TetrisGridVM, TetrisItemVM)',
    'doc.components.mvvm.vm.desc': '这是系统的大脑。TetrisGridVM.cs 维护了一个二维数组 _tetrisItemOccupiedCells 来记录网格的占用状态，纯逻辑运算不依赖任何 UnityEngine.Object。 TetrisItemVM.cs 处理物品的旋转状态 (_rotated)、坐标和尺寸数据。',
    'doc.components.mvvm.view.title': 'View (TetrisGridView, TetrisItemView)',
    'doc.components.mvvm.view.desc': '负责显示。它们监听 ViewModel 的属性变化（如位置改变、旋转改变），并自动更新 UI。',

    'doc.components.interaction.title': '2. 全局交互管理',
    'doc.components.interaction.desc': 'InventoryManager.cs 是系统的指挥官（Singleton），它不处理具体网格逻辑，而负责协调：',
    'doc.components.interaction.item1': '输入处理',
    'doc.components.interaction.item1.desc': '：在 Update() 中监听全局按键（如 R 键旋转、B 键开关背包）。',
    'doc.components.interaction.item2': '射线检测',
    'doc.components.interaction.item2.desc': '：通过 GetGridViewUnderMouse() 实时判断鼠标悬停在哪个网格上。',
    'doc.components.interaction.item3': '视觉反馈',
    'doc.components.interaction.item3.desc': '：调用 inventoryHighlight 组件，根据 ViewModel 返回的结果渲染高亮色块。',

    'doc.components.persistence.title': '3. 数据持久化系统',
    'doc.components.persistence.desc': '存档系统由 InventorySaveLoadService.cs 驱动：',
    'doc.components.persistence.item1': '数据分离',
    'doc.components.persistence.item1.desc': '：静态数据（图标、名称、形状）存储在 ItemDataList_SO 中，动态数据通过 JSON 反序列化到 InventoryData_SO 中。',
    'doc.components.persistence.item2': 'GUID 映射',
    'doc.components.persistence.item2.desc': '：每个网格容器和物品都有唯一的 GUID。系统会根据 GUID 自动重建 ViewModel 树。',

    // Loxodon MVVM
    'doc.loxodon.title': 'Loxondon MVVM 架构',
    'doc.loxodon.desc': '本项目基于 Loxodon Framework 实现 MVVM 架构。',
    'doc.loxodon.model.title': '1. 模型层 (Model)',
    'doc.loxodon.model.desc': '定义数据的"形状"，不包含业务逻辑。',
    'doc.loxodon.model.item1': '静态数据',
    'doc.loxodon.model.item1.desc': '：使用 ScriptableObject 存储游戏配置（如 ItemDetails）。',
    'doc.loxodon.model.item2': '运行时数据',
    'doc.loxodon.model.item2.desc': '：InventoryData_SO 存储玩家背包的运行时数据。',

    'doc.loxodon.vm.title': '2. 视图模型层 (ViewModel)',
    'doc.loxodon.vm.desc': '作为 View 和 Model 的桥梁，持有 View 所需的状态并处理逻辑。',

    'doc.loxodon.view.title': '3. 视图层 (View)',
    'doc.loxodon.view.desc': '可视化展示，将用户输入转换为 ViewModel 的指令。',
    'doc.loxodon.view.desc2': 'View 监听 Unity 的事件（如 OnPointerEnter），并将其转发给 Manager 或直接调用 VM 的方法。通过 Loxodon 的数据绑定机制，自动同步 VM 的属性到 View。',

    // Tetris Coordinate System
    'doc.coords.title': 'Tetris 坐标系统 - 数值的艺术',
    'doc.coords.desc': '本项目的网格容器设计基于经典的 Tetris 变体逻辑，结合 MVVM 架构与笛卡尔坐标系实现。系统通过将连续的 UI 空间离散化为二维整数矩阵，实现了物品的精确放置、旋转与碰撞检测。',
    'doc.coords.def.title': '坐标定义与公式',
    'doc.coords.grid.title': '网格坐标 (Grid Coordinates)',
    'doc.coords.grid.desc': '系统采用左上角为原点 (0,0) 的坐标系。X 轴向右增长，Y 轴向下增长。',
    'doc.coords.pixel.title': '像素位置计算公式',
    'doc.coords.pixel.note': '注意：P_y 取负值是因为 Unity UI 的坐标系中，Y 轴向上为正，而网格布局通常从上向下排列。',
    'doc.coords.rotate.title': '旋转变换公式 (顺时针 90°)',

    // Raycast Filter
    'doc.raycast.title': 'SpriteMeshRaycastFilter',
    'doc.raycast.subtitle': '精确的 Tetris 形状操作过滤器',
    'doc.raycast.desc': '它的作用是让鼠标点击更加精准，只响应物体“真实形状”的区域，忽略透明的空白区域。',
    'doc.raycast.step1': '定位',
    'doc.raycast.step1.desc': '：判断鼠标在当前 Image 的位置。',
    'doc.raycast.step2': '查表',
    'doc.raycast.step2.desc': '：读取物品的“形状数据”（L 型、T 型等）。',
    'doc.raycast.step3': '判定',
    'doc.raycast.step3.desc': '：如果对应格子有东西则拦截点击，否则放行。',

    // Nested Inventory
    'doc.nested.title': '背包套娃',
    'doc.nested.subtitle': 'GUID 构建的逻辑依赖关系',
    'doc.nested.desc': '背包的嵌套（例如：背包里装了一个防弹衣，防弹衣里又装了一个绷带）与还原是通过 GUID 索引和 InventoryTreeCache 的组合机制实现的。',
    'doc.nested.guid.title': 'GUID 链式引用',
    'doc.nested.guid.desc': '每个容器和物品都有唯一的 GUID。每个物品数据包含一个 containerGuid 字段，指向它所在的父容器。',
    'doc.nested.restore.title': '数据重组与还原',
    'doc.nested.restore.desc': '系统遍历所有物品数据，在 Cache 中建立 ContainerID -> List<Items> 的映射关系。还原过程并非一次性生成所有 UI，而是按需递归的。',

    // InventoryTreeCache
    'doc.cache.title': 'InventoryTreeCache',
    'doc.cache.subtitle': 'TetrisItem 的导航员',
    'doc.cache.desc': 'InventoryTreeCache 并不直接存储树，它存储的是“关系”。这种“查表法”完美解决了无限嵌套的问题，因为无论层级多深，都只是简单的 Key-Value 查找，不需要复杂的递归遍历算法。',

    // Search
    'search.placeholder': '搜索文档...',
    'search.noResults': '未找到与 "{query}" 相关的结果',
    'search.tryOther': '试试其他关键词或检查拼写',
    'search.navigate': '切换',
    'search.select': '选择',
    'search.close': '关闭',
    'search.esc': 'Esc',

    // Contributing
    'contributing.title': '贡献指南',
    'contributing.subtitle': '感谢你对本项目的关注！我们欢迎各种形式的贡献。',
    
    // Overview
    'contributing.overview.title': '开始之前',
    'contributing.overview.desc': '在贡献代码之前，请先阅读以下指南：',
    'contributing.overview.item1': '确保你已经阅读了项目的 README 文件',
    'contributing.overview.item2': '检查现有的 Issues 和 Pull Requests',
    'contributing.overview.item3': '了解项目的代码风格和规范',
    'contributing.overview.item4': '考虑添加测试用例',

    // Dev Setup
    'contributing.setup.title': '开发环境设置',
    'contributing.setup.desc': '按照以下步骤设置本地开发环境：',
    'contributing.setup.fork': '1. Fork 项目仓库',
    'contributing.setup.clone': '2. 克隆你的 fork',
    'contributing.setup.unity': '3. 在 Unity Hub 中添加并打开项目',
    'contributing.setup.unityDesc': '(版本: 2022.3.52f1 LTS)',
    'contributing.setup.branch': '4. 创建新分支',
    
    // Submitting
    'contributing.submit.title': '提交更改',
    'contributing.submit.desc': '我们使用以下提交消息格式：',
    'contributing.submit.types.title': '提交类型',
    'contributing.submit.type.feat': '新功能',
    'contributing.submit.type.fix': '修复bug',
    'contributing.submit.type.docs': '文档更新',
    'contributing.submit.type.style': '代码格式调整',
    'contributing.submit.type.refactor': '代码重构',
    'contributing.submit.type.test': '测试相关',
    'contributing.submit.type.chore': '构建过程或辅助工具的变动',

    // Coming Soon
    'comingSoon.title': '敬请期待',
    'comingSoon.desc': '该模块正在紧锣密鼓地开发中，完整的文档和示例即将上线。',
    'comingSoon.back': '返回首页',

    // 语言切换
    'language.switch': '切换语言',
    'language.zh-CN': '简体中文',
    'language.en-US': 'English'
  },
  'en-US': {
    // Navigation
    'nav.home': 'Home',
    'nav.gettingStarted': 'Getting Started',
    'nav.documentation': 'Documentation',
    'nav.api': 'API Docs',
    'nav.examples': 'Examples',
    'nav.contributing': 'Contributing',
    
    // Home title
    'home.title': 'Cholopol\'s Tetris Inventory System Wiki',
    'home.subtitle': 'Complete technical documentation and API reference to help you get started quickly and understand the project architecture',
    
    // Home cards
    'card.github.title': 'GitHub Repository',
    'card.github.description': 'View the complete source code and project introduction for Cholopols Tetris Inventory System.',
    'card.github.link': 'Visit Repository',
    
    'card.quickstart.title': 'Quick Start',
    'card.quickstart.description': 'Get started quickly with CholopolTetrisInventorySystemWiki, learn basic usage and configuration.',
    'card.quickstart.link': 'Quick Start',
    
    'card.documentation.title': 'Documentation',
    'card.documentation.description': 'Detailed development documentation including project architecture, configuration instructions, and best practices.',
    'card.documentation.link': 'View Docs',
    
    'card.api.title': 'API Documentation',
    'card.api.description': 'Complete API reference documentation with all interface descriptions and usage examples.',
    'card.api.link': 'API Docs',
    
    'card.examples.title': 'Examples',
    'card.examples.description': 'Rich code examples and use cases to help you better understand and use the project.',
    'card.examples.link': 'View Examples',
    
    'card.contributing.title': 'Contributing',
    'card.contributing.description': 'Learn how to contribute to the project, including development guidelines and submission processes.',
    'card.contributing.link': 'Contributing',
    
    'card.external.title.bilibili': 'Bilibili',
    'card.external.title.youtube': 'YouTube',
    'card.external.title.xiaohongshu': 'rednote',
    'card.external.description.bilibili': 'Visit author\'s Bilibili page',
    'card.external.description.youtube': 'Visit author\'s YouTube channel',
    'card.external.description.xiaohongshu': 'Visit author\'s rednote profile',
    'card.external.link': 'Visit Link',

    // Getting Started
    'gettingStarted.title': 'Getting Started',
    'gettingStarted.subtitle': 'This guide will help you get started quickly with Cholopol\'s Tetris Inventory System.',
    'gettingStarted.requirements.title': 'Requirements',
    'gettingStarted.requirements.item1': 'Unity 2022.3.52f1 or later.',
    'gettingStarted.import.title': 'Import Steps',
    'gettingStarted.import.step1.title': '1. Clone Repository',
    'gettingStarted.import.step2.item1': 'Open project in Unity Hub.',
    'gettingStarted.import.step2.item2': 'Wait for Unity to import assets and dependencies.',
    'gettingStarted.basicUsage.title': 'Basic Usage',
    'gettingStarted.basicUsage.item1': 'Navigate to',
    'gettingStarted.basicUsage.item2': 'Open',
    'gettingStarted.basicUsage.item3': 'Click Play.',
    'gettingStarted.basicUsage.controls.title': 'Controls',
    'gettingStarted.basicUsage.controls.b': 'Press B',
    'gettingStarted.basicUsage.controls.b.desc': ': Open/Close Inventory.',
    'gettingStarted.basicUsage.controls.r': 'Press R',
    'gettingStarted.basicUsage.controls.r.desc': ': Rotate item while dragging.',
    'gettingStarted.basicUsage.controls.lmb': 'Left Mouse Button',
    'gettingStarted.basicUsage.controls.lmb.desc': ': Drag items.',
    'gettingStarted.basicUsage.controls.rmb': 'Right Mouse Button',
    'gettingStarted.basicUsage.controls.rmb.desc': ': Open item context menu.',
    'gettingStarted.thirdParty.title': 'Third Party Dependencies',
    'gettingStarted.thirdParty.subtitle': 'Usually no manual installation required. If issues occur, refer to:',
    'gettingStarted.thirdParty.loxodon.title': 'Loxodon Framework',
    'gettingStarted.thirdParty.loxodon.desc': 'Loxodon Framework is an excellent Unity MVVM framework. Installed by adding to',
    'gettingStarted.thirdParty.loxodon.desc2': ':',
    'gettingStarted.thirdParty.loxodon.link': 'If issues occur, refer to official installation: Loxodon Installation',
    'gettingStarted.thirdParty.localization.title': 'Localization',
    'gettingStarted.thirdParty.localization.desc': 'Unity official localization package, installed via Unity Package Manager',
    'gettingStarted.thirdParty.json.title': 'Newtonsoft-json',
    'gettingStarted.thirdParty.json.desc': 'High-performance JSON serializer, installed via Unity Package Manager',
    'gettingStarted.nextSteps.title': 'Next Steps',
    'gettingStarted.nextSteps.docs': 'Documentation',
    'gettingStarted.nextSteps.docsDesc': 'Deep dive into core architecture and implementation details',

    // Documentation
    'doc.title': 'Documentation',
    'doc.subtitle': 'Detailed development documentation including project architecture, configuration instructions, and best practices. Helps you quickly understand the project structure and start development.',

    // Core Features
    'doc.core.title': 'Core Features Highlights',
    'doc.core.feature1.title': 'Perfect Tetris Tile Design',
    'doc.core.feature1.desc': 'Supports item rotation, grid-based placement logic, and diverse operations. Filters user actions based on Tetris shapes.',
    'doc.core.feature2.title': 'MVVM Architecture',
    'doc.core.feature2.desc': 'Item/Grid/Slot/Ghost system implements View/ViewModel/Model logic decoupling.',
    'doc.core.feature3.title': 'Save System',
    'doc.core.feature3.desc': 'Powerful serialization system for persisting inventory state.',
    'doc.core.feature4.title': 'More Features',
    'doc.core.feature4.item1': 'Custom Editor: Built-in UI Toolkit editor for easy item creation',
    'doc.core.feature4.item2': 'Localization: Integrated multi-language support',

    // Core Components
    'doc.components.title': 'Core Components',
    'doc.components.mvvm.title': '1. MVVM Architecture Implementation',
    'doc.components.mvvm.desc': 'Abandoning traditional MonoBehaviour strong coupling in favor of data-driven development.',
    'doc.components.mvvm.vm.title': 'ViewModel (TetrisGridVM, TetrisItemVM)',
    'doc.components.mvvm.vm.desc': 'The brain of the system. TetrisGridVM.cs maintains a 2D array _tetrisItemOccupiedCells to record grid occupancy, using pure logic operations without UnityEngine.Object dependencies. TetrisItemVM.cs handles item rotation (_rotated), coordinates, and size data.',
    'doc.components.mvvm.view.title': 'View (TetrisGridView, TetrisItemView)',
    'doc.components.mvvm.view.desc': 'Responsible for display. They listen to ViewModel property changes (like position, rotation) and automatically update the UI.',

    'doc.components.interaction.title': '2. Global Interaction Management',
    'doc.components.interaction.desc': 'InventoryManager.cs is the commander (Singleton) of the system. It coordinates rather than handling specific grid logic:',
    'doc.components.interaction.item1': 'Input Handling',
    'doc.components.interaction.item1.desc': ': Listens for global keys in Update() (e.g., R to rotate, B to toggle inventory).',
    'doc.components.interaction.item2': 'Raycast Detection',
    'doc.components.interaction.item2.desc': ': Uses GetGridViewUnderMouse() to determine which grid the mouse is hovering over in real-time.',
    'doc.components.interaction.item3': 'Visual Feedback',
    'doc.components.interaction.item3.desc': ': Calls the inventoryHighlight component to render highlight blocks based on ViewModel results.',

    'doc.components.persistence.title': '3. Data Persistence System',
    'doc.components.persistence.desc': 'The save system is driven by InventorySaveLoadService.cs:',
    'doc.components.persistence.item1': 'Data Separation',
    'doc.components.persistence.item1.desc': ': Static data (icons, names, shapes) is stored in ItemDataList_SO, while dynamic data is deserialized from JSON into InventoryData_SO.',
    'doc.components.persistence.item2': 'GUID Mapping',
    'doc.components.persistence.item2.desc': ': Each grid container and item has a unique GUID. The system automatically reconstructs the ViewModel tree based on GUIDs.',

    // Loxodon MVVM
    'doc.loxodon.title': 'Loxodon MVVM Architecture',
    'doc.loxodon.desc': 'This project implements MVVM architecture based on Loxodon Framework.',
    'doc.loxodon.model.title': '1. Model Layer',
    'doc.loxodon.model.desc': 'Defines the "shape" of data, containing no business logic.',
    'doc.loxodon.model.item1': 'Static Data',
    'doc.loxodon.model.item1.desc': ': Uses ScriptableObject to store game configuration (e.g., ItemDetails).',
    'doc.loxodon.model.item2': 'Runtime Data',
    'doc.loxodon.model.item2.desc': ': InventoryData_SO stores player inventory runtime data.',

    'doc.loxodon.vm.title': '2. ViewModel Layer',
    'doc.loxodon.vm.desc': 'Acts as a bridge between View and Model, holding state required by the View and handling logic.',

    'doc.loxodon.view.title': '3. View Layer',
    'doc.loxodon.view.desc': 'Visual presentation, converting user input into ViewModel commands.',
    'doc.loxodon.view.desc2': 'The View listens for Unity events (like OnPointerEnter) and forwards them to the Manager or directly calls VM methods. Through Loxodon\'s data binding mechanism, VM properties are automatically synchronized to the View.',

    // Tetris Coordinate System
    'doc.coords.title': 'Tetris Coordinate System - The Art of Numbers',
    'doc.coords.desc': 'The grid container design is based on classic Tetris variant logic, combined with MVVM architecture and Cartesian coordinate system. By discretizing continuous UI space into a 2D integer matrix, the system achieves precise item placement, rotation, and collision detection.',
    'doc.coords.def.title': 'Coordinate Definitions & Formulas',
    'doc.coords.grid.title': 'Grid Coordinates',
    'doc.coords.grid.desc': 'The system uses a coordinate system with the top-left corner as origin (0,0). X-axis increases to the right, Y-axis increases downwards.',
    'doc.coords.pixel.title': 'Pixel Position Formula',
    'doc.coords.pixel.note': 'Note: P_y is negative because in Unity UI coordinate system, Y-axis is positive upwards, while grid layouts usually arrange from top to bottom.',
    'doc.coords.rotate.title': 'Rotation Transform Formula (Clockwise 90°)',

    // Raycast Filter
    'doc.raycast.title': 'SpriteMeshRaycastFilter',
    'doc.raycast.subtitle': 'Precise Tetris Shape Operation Filter',
    'doc.raycast.desc': 'It makes mouse clicks more precise, responding only to the "real shape" area of the object and ignoring transparent blank areas.',
    'doc.raycast.step1': 'Locate',
    'doc.raycast.step1.desc': ': Determine mouse position within the current Image.',
    'doc.raycast.step2': 'Lookup',
    'doc.raycast.step2.desc': ': Read item "shape data" (L-shape, T-shape, etc.).',
    'doc.raycast.step3': 'Judge',
    'doc.raycast.step3.desc': ': Intercept click if the corresponding cell is occupied, otherwise let it pass.',

    // Nested Inventory
    'doc.nested.title': 'Nested Inventory',
    'doc.nested.subtitle': 'Logical Dependencies Built on GUIDs',
    'doc.nested.desc': 'Inventory nesting (e.g., a backpack containing body armor, which contains a bandage) and restoration are implemented through a combination of GUID indexing and InventoryTreeCache.',
    'doc.nested.guid.title': 'GUID Chained Reference',
    'doc.nested.guid.desc': 'Each container and item has a unique GUID. Each item data contains a containerGuid field pointing to its parent container.',
    'doc.nested.restore.title': 'Data Reconstruction & Restoration',
    'doc.nested.restore.desc': 'The system iterates through all item data, building a ContainerID -> List<Items> mapping in the Cache. The restoration process is recursive on demand, not generating all UI at once.',

    // InventoryTreeCache
    'doc.cache.title': 'InventoryTreeCache',
    'doc.cache.subtitle': 'The Navigator of TetrisItem',
    'doc.cache.desc': 'InventoryTreeCache does not store the tree directly; it stores "relationships". This "lookup table" approach perfectly solves the infinite nesting problem, because no matter how deep the hierarchy, it involves simple Key-Value lookups without complex recursive traversal algorithms.',

    // Language switch
    'language.switch': 'Switch Language',
    'language.zh-CN': '简体中文',
    'language.en-US': 'English',

    // Search
    'search.placeholder': 'Search documentation...',
    'search.noResults': 'No results found for "{query}"',
    'search.tryOther': 'Try other keywords or check spelling',
    'search.navigate': 'Navigate',
    'search.select': 'Select',
    'search.close': 'Close',
    'search.esc': 'Esc',

    // Contributing
    'contributing.title': 'Contributing Guide',
    'contributing.subtitle': 'Thanks for your interest in this project! We welcome contributions in all forms.',
    
    // Overview
    'contributing.overview.title': 'Before You Start',
    'contributing.overview.desc': 'Please read the following guidelines before contributing code:',
    'contributing.overview.item1': 'Ensure you have read the project README',
    'contributing.overview.item2': 'Check existing Issues and Pull Requests',
    'contributing.overview.item3': 'Understand the project code style and conventions',
    'contributing.overview.item4': 'Consider adding test cases',

    // Dev Setup
    'contributing.setup.title': 'Development Setup',
    'contributing.setup.desc': 'Follow these steps to set up your local development environment:',
    'contributing.setup.fork': '1. Fork the repository',
    'contributing.setup.clone': '2. Clone your fork',
    'contributing.setup.unity': '3. Add and open project in Unity Hub',
    'contributing.setup.unityDesc': '(Version: 2022.3.52f1 LTS)',
    'contributing.setup.branch': '4. Create a new branch',
    
    // Submitting
    'contributing.submit.title': 'Submitting Changes',
    'contributing.submit.desc': 'We use the following commit message format:',
    'contributing.submit.types.title': 'Commit Types',
    'contributing.submit.type.feat': 'New feature',
    'contributing.submit.type.fix': 'Bug fix',
    'contributing.submit.type.docs': 'Documentation update',
    'contributing.submit.type.style': 'Code style adjustment',
    'contributing.submit.type.refactor': 'Code refactoring',
    'contributing.submit.type.test': 'Testing related',
    'contributing.submit.type.chore': 'Build process or tool changes',

    // Coming Soon
    'comingSoon.title': 'Coming Soon',
    'comingSoon.desc': 'This module is currently under active development. Complete documentation and examples will be available soon.',
    'comingSoon.back': 'Back to Home',
  }
}

// 创建国际化store
export const useI18n = create<TranslationStore>((set, get) => ({
  currentLanguage: 'zh-CN',
  translations,
  
  setLanguage: (lang: Language) => {
    set({ currentLanguage: lang })
    localStorage.setItem('language', lang)
  },
  
  t: (key: string): string => {
    const state = get()
    return state.translations[state.currentLanguage][key] || key
  }
}))

// 初始化语言
export const initI18n = () => {
  const savedLanguage = localStorage.getItem('language') as Language | null
  if (savedLanguage) {
    useI18n.getState().setLanguage(savedLanguage)
  }
}
