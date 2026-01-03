import { useState, useEffect } from 'react'

interface TetrisBlock {
  id: number
  x: number
  y: number
  color: string
  speed: number
  rotation: number
  shape: TetrisShape
}

interface TetrisShape {
  type: 'I' | 'O' | 'T' | 'S' | 'Z' | 'J' | 'L' | 'I2' | 'O2' | 'T2' | 'S2' | 'Z2' | 'J2' | 'L2' | 'Star' | 'Cross' | 'Plus'
  cells: [number, number][]
  color: string
}

const TETRIS_SHAPES: Record<string, TetrisShape> = {
  I: {
    type: 'I',
    cells: [[0, 0], [1, 0], [2, 0], [3, 0]],
    color: 'bg-cyan-400'
  },
  O: {
    type: 'O',
    cells: [[0, 0], [1, 0], [0, 1], [1, 1]],
    color: 'bg-yellow-400'
  },
  T: {
    type: 'T',
    cells: [[1, 0], [0, 1], [1, 1], [2, 1]],
    color: 'bg-purple-400'
  },
  S: {
    type: 'S',
    cells: [[1, 0], [2, 0], [0, 1], [1, 1]],
    color: 'bg-green-400'
  },
  Z: {
    type: 'Z',
    cells: [[0, 0], [1, 0], [1, 1], [2, 1]],
    color: 'bg-red-400'
  },
  J: {
    type: 'J',
    cells: [[0, 0], [0, 1], [1, 1], [2, 1]],
    color: 'bg-blue-400'
  },
  L: {
    type: 'L',
    cells: [[2, 0], [0, 1], [1, 1], [2, 1]],
    color: 'bg-orange-400'
  },
  // 新增更多颜色和形状 - 使用高亮度颜色
  I2: {
    type: 'I2',
    cells: [[0, 0], [1, 0], [2, 0]],
    color: 'bg-pink-400'
  },
  O2: {
    type: 'O2',
    cells: [[0, 0], [1, 0], [0, 1]],
    color: 'bg-indigo-400'
  },
  T2: {
    type: 'T2',
    cells: [[0, 0], [1, 0], [2, 0], [1, 1]],
    color: 'bg-teal-400'
  },
  S2: {
    type: 'S2',
    cells: [[0, 1], [1, 1], [1, 0], [2, 0]],
    color: 'bg-lime-400'
  },
  Z2: {
    type: 'Z2',
    cells: [[0, 0], [1, 0], [1, 1], [2, 1]],
    color: 'bg-rose-400'
  },
  J2: {
    type: 'J2',
    cells: [[1, 0], [1, 1], [0, 1], [0, 2]],
    color: 'bg-emerald-400'
  },
  L2: {
    type: 'L2',
    cells: [[0, 0], [0, 1], [0, 2], [1, 2]],
    color: 'bg-sky-400'
  },
  Star: {
    type: 'Star',
    cells: [[1, 0], [0, 1], [1, 1], [2, 1], [1, 2]],
    color: 'bg-amber-300'
  },
  Cross: {
    type: 'Cross',
    cells: [[1, 0], [0, 1], [1, 1], [2, 1], [1, 2]],
    color: 'bg-violet-400'
  },
  Plus: {
    type: 'Plus',
    cells: [[0, 1], [1, 0], [1, 1], [1, 2], [2, 1]],
    color: 'bg-fuchsia-400'
  }
}

const SHAPE_TYPES: (keyof typeof TETRIS_SHAPES)[] = [
  'I', 'O', 'T', 'S', 'Z', 'J', 'L',
  'I2', 'O2', 'T2', 'S2', 'Z2', 'J2', 'L2',
  'Star', 'Cross', 'Plus'
]

export default function TetrisBackground() {
  const [blocks, setBlocks] = useState<TetrisBlock[]>([])
  const TARGET_BLOCKS = 10 // 目标方块数量，保持连续生成而不是硬限制

  useEffect(() => {
    // 连续生成的随机算法 - 解决视觉不连续问题
    const staggeredInit = () => {
      // 增加初始生成数量，确保进入站点时有足够的方块
      for (let i = 0; i < 16; i++) { // 从12个增加到16个，确保初始视觉效果
        setTimeout(() => {
          const shapeType = SHAPE_TYPES[Math.floor(Math.random() * SHAPE_TYPES.length)]
          
          // 完全随机的水平位置 - 整个屏幕宽度
          const xPosition = Math.random() * 100 // 0-100% 完全随机
          
          // 更大的垂直范围，确保分散出现
          const randomY = -200 - Math.random() * 1000 - (i * 80) // 增加初始分散度
          
          // 适度的随机延迟范围
          const randomDelay = Math.random() * 600 // 0-0.6秒的随机延迟
          
          setTimeout(() => {
            // 智能生成策略 - 保持连续性而不是硬限制
            setBlocks(prev => {
              // 如果方块数量远超过目标，减少生成概率
              if (prev.length > TARGET_BLOCKS + 4) {
                // 50%概率跳过生成，避免过度累积
                if (Math.random() < 0.5) return prev
              }
              
              // 检查是否与现有方块过于接近
              const tooClose = prev.some(existingBlock => {
                const horizontalDistance = Math.abs(existingBlock.x - xPosition)
                const verticalDistance = Math.abs(existingBlock.y - randomY)
                return horizontalDistance < 10 && verticalDistance < 120 // 稍微收紧距离限制
              })
              
              if (tooClose) {
                // 如果太接近，50%概率仍然生成，保持连续性
                if (Math.random() < 0.5) return prev
              }
              
              const newBlock: TetrisBlock = {
                id: Date.now() + Math.random() * 10000,
                x: xPosition,
                y: randomY,
                color: TETRIS_SHAPES[shapeType].color,
                speed: 0.6 + Math.random() * 2.0, // 增加速度变化范围
                rotation: Math.floor(Math.random() * 4) * 90,
                shape: TETRIS_SHAPES[shapeType]
              }
              return [...prev, newBlock]
            })
          }, randomDelay)
        }, i * 700) // 稍微加快生成间隔，从1000ms改为700ms
      }
    }
    
    staggeredInit()

    // 连续生成机制 - 确保视觉效果持续
    let generationInterval: ReturnType<typeof setInterval> | null = null
    
    const continuousGeneration = () => {
      generationInterval = setInterval(() => {
        setBlocks(prev => {
          // 如果方块数量低于目标数量，补充生成
          if (prev.length < TARGET_BLOCKS - 2) {
            const shapeType = SHAPE_TYPES[Math.floor(Math.random() * SHAPE_TYPES.length)]
            const xPosition = Math.random() * 100
            const randomY = -200 - Math.random() * 500
            
            // 检查是否太接近现有方块
            const tooClose = prev.some(existingBlock => {
              const horizontalDistance = Math.abs(existingBlock.x - xPosition)
              const verticalDistance = Math.abs(existingBlock.y - randomY)
              return horizontalDistance < 8 && verticalDistance < 100
            })
            
            if (!tooClose) {
              const newBlock: TetrisBlock = {
                id: Date.now() + Math.random() * 10000,
                x: xPosition,
                y: randomY,
                color: TETRIS_SHAPES[shapeType].color,
                speed: 0.6 + Math.random() * 2.0,
                rotation: Math.floor(Math.random() * 4) * 90,
                shape: TETRIS_SHAPES[shapeType]
              }
              return [...prev, newBlock]
            }
          }
          return prev
        })
      }, 1500) // 每1.5秒检查一次，补充生成
      
      return generationInterval
    }

    // 延迟启动连续生成，等初始生成完成后再开始
    const continuousTimeout = setTimeout(continuousGeneration, 12000) // 12秒后开始

    // 动画循环
    const interval = setInterval(() => {
      setBlocks(prevBlocks => {
        if (prevBlocks.length === 0) return prevBlocks
        
        const newBlocks = prevBlocks.map(block => ({
          ...block,
          y: block.y + block.speed
        }))

        // 重置落到底部的方块，保持自然分布
        const resetBlocks = newBlocks.map(block => {
          if (block.y > (window.innerHeight || 800) + 200) {
            const shapeType = SHAPE_TYPES[Math.floor(Math.random() * SHAPE_TYPES.length)]
            
            // 完全随机的重置位置
            const newX = Math.random() * 100 // 0-100% 完全随机
            
            return {
              ...block,
              y: Math.random() * -800 - 100, // 完全随机的垂直位置
              x: newX,
              color: TETRIS_SHAPES[shapeType].color,
              speed: 0.6 + Math.random() * 2.2, // 更随机的速度
              rotation: Math.floor(Math.random() * 4) * 90,
              shape: TETRIS_SHAPES[shapeType]
            }
          }
          return block
        })

        return resetBlocks
      })
    }, 30)

    return () => {
      clearInterval(interval)
      clearTimeout(continuousTimeout)
      if (generationInterval) {
        clearInterval(generationInterval)
      }
    }
  }, [])

  const renderTetrisShape = (block: TetrisBlock) => {
    const { shape, rotation } = block
    const cellSize = 80 // 每个小方块的尺寸 - 放大5倍
    
    return (
      <div className="relative" style={{ width: '400px', height: '400px' }}>
        {shape.cells.map((cell, index) => {
          const [cx, cy] = cell
          // 应用旋转变换
          let rotatedX = cx
          let rotatedY = cy
          
          // 根据旋转角度调整坐标（适应4x4网格）
          switch (rotation) {
            case 90:
              rotatedX = 3 - cy
              rotatedY = cx
              break
            case 180:
              rotatedX = 3 - cx
              rotatedY = 3 - cy
              break
            case 270:
              rotatedX = cy
              rotatedY = 3 - cx
              break
          }
          
          return (
            <div
              key={index}
              className={`absolute ${shape.color} border-2 border-white/60 shadow-lg tetris-block`}
              style={{
                left: `${rotatedX * cellSize}px`,
                top: `${rotatedY * cellSize}px`,
                width: `${cellSize}px`,
                height: `${cellSize}px`,
                borderRadius: '2px',
                filter: 'drop-shadow(0 0 12px rgba(255, 255, 255, 0.5))',
                '--tw-shadow-color': 'rgba(255, 255, 255, 0.3)',
                '--tw-shadow': '0 0 12px var(--tw-shadow-color)'
              } as React.CSSProperties}
            >
              {/* 内部高光效果 - 增强对比度 */}
              <div className="absolute inset-2 bg-white/30 rounded-md"></div>
              <div className="absolute inset-1 border border-white/50 rounded-md"></div>
              {/* 额外发光效果 - 暗色模式下更明显 */}
              <div className="absolute -inset-1 bg-white/15 rounded-md blur-sm"></div>
            </div>
          )
        })}
      </div>
    )
  }

  return (
    <div className="fixed inset-0 overflow-hidden pointer-events-none z-0">
      {/* 主俄罗斯方块 - 固定大小，无缩放动画 */}
      {blocks.map(block => (
        <div
          key={block.id}
          className="absolute opacity-90"
          style={{
            left: `${block.x}%`,
            top: `${block.y}px`
          }}
        >
          {renderTetrisShape(block)}
        </div>
      ))}
    </div>
  )
}