import { Language } from '../i18n'

export interface SearchResult {
  id: string
  title: string
  content: string
  category: string
  section: string
  path: string
  anchor?: string
  parentTitle?: string
  keywords: string[]
}

export interface SearchDocument {
  path: string
  title: string
  category: string
  sections: SearchSection[]
}

export interface SearchSection {
  id: string
  title: string
  content: string
  anchor: string
  parentTitle?: string
  keywords: string[]
}

const zhData: SearchDocument[] = [
  {
    path: '/getting-started',
    title: '快速开始',
    category: '快速开始',
    sections: [
      {
        id: 'requirements',
        title: '环境要求',
        content: 'Unity 2022.3.52f1 或更高版本。',
        anchor: '#requirements',
        keywords: ['环境', '要求', 'Unity', '版本']
      },
      {
        id: 'import-steps',
        title: '导入步骤',
        content: '1. 克隆仓库 git clone ... 2. 在 Unity Hub 中打开项目。 3. 等待 Unity 导入资源与依赖包。',
        anchor: '#import-steps',
        keywords: ['导入', '克隆', 'git', 'Unity Hub']
      },
      {
        id: 'basic-usage',
        title: '基础使用',
        content: '打开 Assets/Scenes/EFT Like UI 场景，点击播放(Play)。控制说明：B键打开/关闭背包，R键旋转物品，鼠标左键拖动，右键打开菜单。',
        anchor: '#basic-usage',
        keywords: ['基础', '使用', '场景', '控制', '按键', 'B键', 'R键', '鼠标']
      },
      {
        id: 'third-party',
        title: '第三方依赖',
        content: 'Loxodon Framework (MVVM框架), Unity Localization (本地化), Newtonsoft-json (JSON序列化)。',
        anchor: '#third-party',
        keywords: ['依赖', '第三方', 'Loxodon', 'MVVM', 'Localization', 'JSON']
      }
    ]
  },
  {
    path: '/documentation',
    title: '开发文档',
    category: '文档',
    sections: [
      {
        id: 'core-features',
        title: '核心功能亮点',
        content: '完美的 Tetris 瓦片设计(旋转、网格放置)，MVVM 架构(View/ViewModel/Model解耦)，存档系统(序列化)，自定义编辑器，本地化支持。',
        anchor: '#core-features',
        keywords: ['核心', '功能', 'Tetris', 'MVVM', '存档', '编辑器', '本地化']
      },
      {
        id: 'core-components',
        title: '核心组件',
        content: 'TetrisGridVM (系统大脑，维护二维数组 _tetrisItemOccupiedCells)，InventoryManager (全局交互，输入处理，射线检测)，InventorySaveLoadService (数据持久化，GUID映射)。',
        anchor: '#core-components',
        keywords: ['组件', 'TetrisGridVM', 'InventoryManager', 'SaveLoadService', 'GUID']
      },
      {
        id: 'loxodon-mvvm',
        title: 'Loxodon MVVM 架构',
        content: 'Model (数据形状, ScriptableObject), ViewModel (状态与逻辑桥梁), View (可视化展示, 数据绑定)。',
        anchor: '#loxodon-mvvm',
        keywords: ['Loxodon', 'MVVM', 'Model', 'ViewModel', 'View', '绑定']
      },
      {
        id: 'tetris-coords',
        title: 'Tetris 坐标系统',
        content: '基于笛卡尔坐标系，左上角为原点(0,0)，X轴向右，Y轴向下。包含网格坐标定义、像素位置计算公式、旋转变换公式。',
        anchor: '#tetris-coords',
        keywords: ['坐标', 'Tetris', '笛卡尔', '公式', '旋转']
      },
      {
        id: 'raycast-filter',
        title: 'SpriteMeshRaycastFilter',
        content: '精确的 Tetris 形状操作过滤器。只响应物体“真实形状”的区域，忽略透明空白。步骤：定位 -> 查表 -> 判定。',
        anchor: '#raycast-filter',
        keywords: ['射线', '检测', 'Filter', 'Raycast', '形状', '点击']
      },
      {
        id: 'nested-inventory',
        title: '背包套娃',
        content: '基于 GUID 的无限嵌套系统。每个容器和物品都有唯一 GUID。通过 InventoryTreeCache 建立 ContainerID -> List<Items> 的映射关系实现数据重组。',
        anchor: '#nested-inventory',
        keywords: ['嵌套', '背包', '套娃', 'GUID', 'TreeCache', '递归']
      }
    ]
  },
  {
    path: '/contributing',
    title: '贡献指南',
    category: '贡献',
    sections: [
      {
        id: 'overview',
        title: '开始之前',
        content: '阅读 README，检查 Issues，了解代码风格，添加测试用例。',
        anchor: '#getting-started',
        keywords: ['贡献', 'README', 'Issue', 'PR', '测试']
      },
      {
        id: 'setup',
        title: '开发环境设置',
        content: '1. Fork 仓库。 2. Clone 到本地。 3. Unity Hub 添加项目 (2022.3.52f1)。 4. 创建分支。',
        anchor: '#development-setup',
        keywords: ['环境', '设置', 'Fork', 'Clone', 'Unity', '分支']
      },
      {
        id: 'submit',
        title: '提交更改',
        content: '使用规范的提交信息 (feat, fix, docs 等)。提交 PR。',
        anchor: '#submitting-changes',
        keywords: ['提交', 'PR', 'commit', 'feat', 'fix']
      }
    ]
  },
  {
    path: '/api',
    title: 'API 文档',
    category: 'API',
    sections: [
      {
        id: 'coming-soon',
        title: '敬请期待',
        content: 'API 文档正在开发中...',
        anchor: '',
        keywords: ['API', '文档', '接口']
      }
    ]
  },
  {
    path: '/examples',
    title: '示例',
    category: '示例',
    sections: [
      {
        id: 'coming-soon',
        title: '敬请期待',
        content: '示例代码正在准备中...',
        anchor: '',
        keywords: ['示例', 'Example', '代码']
      }
    ]
  }
]

const enData: SearchDocument[] = [
  {
    path: '/getting-started',
    title: 'Getting Started',
    category: 'Getting Started',
    sections: [
      {
        id: 'requirements',
        title: 'Requirements',
        content: 'Unity 2022.3.52f1 or higher.',
        anchor: '#requirements',
        keywords: ['requirements', 'unity', 'version']
      },
      {
        id: 'import-steps',
        title: 'Import Steps',
        content: '1. Clone repository. 2. Open in Unity Hub. 3. Wait for Unity to import assets and packages.',
        anchor: '#import-steps',
        keywords: ['import', 'clone', 'git', 'unity hub']
      },
      {
        id: 'basic-usage',
        title: 'Basic Usage',
        content: 'Open Assets/Scenes/EFT Like UI scene and click Play. Controls: B to toggle inventory, R to rotate, Left Mouse to drag, Right Mouse for menu.',
        anchor: '#basic-usage',
        keywords: ['basic', 'usage', 'controls', 'play', 'B key', 'R key', 'mouse']
      },
      {
        id: 'third-party',
        title: 'Third Party Dependencies',
        content: 'Loxodon Framework (MVVM), Unity Localization, Newtonsoft-json.',
        anchor: '#third-party',
        keywords: ['dependencies', 'third party', 'loxodon', 'mvvm', 'localization', 'json']
      }
    ]
  },
  {
    path: '/documentation',
    title: 'Documentation',
    category: 'Documentation',
    sections: [
      {
        id: 'core-features',
        title: 'Core Features',
        content: 'Perfect Tetris Tile Design (rotation, grid placement), MVVM Architecture (decoupling), Save System (serialization), Custom Editor, Localization.',
        anchor: '#core-features',
        keywords: ['core', 'features', 'tetris', 'mvvm', 'save', 'editor', 'localization']
      },
      {
        id: 'core-components',
        title: 'Core Components',
        content: 'TetrisGridVM (Brain, maintains 2D array _tetrisItemOccupiedCells), InventoryManager (Global interaction, input, raycast), InventorySaveLoadService (Persistence, GUID mapping).',
        anchor: '#core-components',
        keywords: ['components', 'TetrisGridVM', 'InventoryManager', 'SaveLoadService', 'GUID']
      },
      {
        id: 'loxodon-mvvm',
        title: 'Loxodon MVVM Architecture',
        content: 'Model (Data shape, ScriptableObject), ViewModel (Bridge for state & logic), View (Visuals, Data Binding).',
        anchor: '#loxodon-mvvm',
        keywords: ['loxodon', 'mvvm', 'model', 'viewmodel', 'view', 'binding']
      },
      {
        id: 'tetris-coords',
        title: 'Tetris Coordinate System',
        content: 'Based on Cartesian system, top-left origin (0,0). Includes grid definitions, pixel position formulas, rotation formulas.',
        anchor: '#tetris-coords',
        keywords: ['coordinates', 'tetris', 'cartesian', 'formula', 'rotation']
      },
      {
        id: 'raycast-filter',
        title: 'SpriteMeshRaycastFilter',
        content: 'Precise Tetris shape operation filter. Responds only to real shape area, ignoring transparent voids. Steps: Locate -> Lookup -> Decide.',
        anchor: '#raycast-filter',
        keywords: ['raycast', 'filter', 'detection', 'shape', 'click']
      },
      {
        id: 'nested-inventory',
        title: 'Nested Inventory',
        content: 'Infinite nesting based on GUID. Unique GUIDs for items/containers. Uses InventoryTreeCache to map ContainerID -> List<Items> for reconstruction.',
        anchor: '#nested-inventory',
        keywords: ['nested', 'inventory', 'guid', 'treecache', 'recursive']
      }
    ]
  },
  {
    path: '/contributing',
    title: 'Contributing',
    category: 'Contributing',
    sections: [
      {
        id: 'overview',
        title: 'Before You Start',
        content: 'Read README, check Issues, understand code style, add tests.',
        anchor: '#getting-started',
        keywords: ['contributing', 'readme', 'issue', 'pr', 'test']
      },
      {
        id: 'setup',
        title: 'Development Setup',
        content: '1. Fork repo. 2. Clone fork. 3. Add to Unity Hub (2022.3.52f1). 4. Create branch.',
        anchor: '#development-setup',
        keywords: ['setup', 'fork', 'clone', 'unity', 'branch']
      },
      {
        id: 'submit',
        title: 'Submitting Changes',
        content: 'Use standard commit messages (feat, fix, docs). Submit PR.',
        anchor: '#submitting-changes',
        keywords: ['submit', 'pr', 'commit', 'feat', 'fix']
      }
    ]
  },
  {
    path: '/api',
    title: 'API Reference',
    category: 'API',
    sections: [
      {
        id: 'coming-soon',
        title: 'Coming Soon',
        content: 'API documentation is under development...',
        anchor: '',
        keywords: ['api', 'docs', 'reference']
      }
    ]
  },
  {
    path: '/examples',
    title: 'Examples',
    category: 'Examples',
    sections: [
      {
        id: 'coming-soon',
        title: 'Coming Soon',
        content: 'Examples are being prepared...',
        anchor: '',
        keywords: ['examples', 'code', 'sample']
      }
    ]
  }
]

const searchData: Record<string, SearchDocument[]> = {
  'zh-CN': zhData,
  'en-US': enData
}

export function searchDocuments(query: string, lang: Language = 'zh-CN'): SearchResult[] {
  if (!query.trim()) return []
  
  const results: SearchResult[] = []
  const lowerQuery = query.toLowerCase()
  const data = searchData[lang] || searchData['zh-CN']
  
  data.forEach(doc => {
    doc.sections.forEach(section => {
      const titleMatch = section.title.toLowerCase().includes(lowerQuery)
      const contentMatch = section.content.toLowerCase().includes(lowerQuery)
      const keywordMatch = section.keywords.some(keyword => 
        keyword.toLowerCase().includes(lowerQuery)
      )
      
      if (titleMatch || contentMatch || keywordMatch) {
        results.push({
          id: `${doc.path}-${section.id}`,
          title: section.title,
          content: section.content,
          category: doc.category,
          section: section.title,
          path: doc.path,
          anchor: section.anchor,
          parentTitle: doc.title,
          keywords: section.keywords
        })
      }
    })
  })
  
  return results
}

export function highlightText(text: string, query: string): string {
  if (!query.trim()) return text
  
  const regex = new RegExp(`(${query})`, 'gi')
  return text.replace(regex, '<mark class="search-highlight">$1</mark>')
}

export function generateAnchorId(title: string): string {
  return title.toLowerCase()
    .replace(/[^\u4e00-\u9fa5a-zA-Z0-9\s]/g, '')
    .replace(/\s+/g, '-')
    .replace(/^-|-$/g, '')
}
