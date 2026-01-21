import { useEffect } from 'react'
import { Link } from 'lucide-react'

interface HeaderAnchorProps {
  id: string
  level: 1 | 2 | 3 | 4 | 5 | 6
  children: React.ReactNode
  className?: string
}

export function HeaderAnchor({ id, level, children, className = '' }: HeaderAnchorProps) {
  const Tag = `h${level}` as keyof JSX.IntrinsicElements
  
  const scrollToElement = (elementId: string) => {
    const element = document.getElementById(elementId)
    if (element) {
      // 获取固定头部的高度
      const header = document.querySelector('.cholopols-header')
      const headerHeight = header ? header.clientHeight + 20 : 80 // 默认80px + 20px padding
      
      // 计算目标位置（考虑头部高度）
      const elementPosition = element.getBoundingClientRect().top
      const offsetPosition = elementPosition + window.pageYOffset - headerHeight
      
      // 平滑滚动到目标位置
      window.scrollTo({
        top: offsetPosition,
        behavior: 'smooth'
      })
    }
  }
  
  useEffect(() => {
    // 处理页面加载时的锚点跳转
    if (window.location.hash === `#${id}`) {
      setTimeout(() => {
        scrollToElement(id)
      }, 100)
    }
  }, [id])

  const handleAnchorClick = (e: React.MouseEvent) => {
    e.preventDefault()
    scrollToElement(id)
    // 更新URL hash
    window.history.pushState(null, '', `#${id}`)
  }

  return (
    <Tag 
      id={id} 
      className={`group relative flex items-center gap-2 cursor-pointer ${className}`}
      onClick={handleAnchorClick}
    >
      <span className="flex-1">{children}</span>
      <a
        href={`#${id}`}
        onClick={(e) => e.stopPropagation()}
        className="opacity-0 group-hover:opacity-100 transition-opacity text-primary hover:text-primary/80"
        aria-label={`链接到 "${children}"`}
      >
        <Link className="h-4 w-4" />
      </a>
    </Tag>
  )
}

// 辅助函数：生成锚点ID
export function generateHeaderId(title: string): string {
  return title.toLowerCase()
    .replace(/[^\u4e00-\u9fa5a-zA-Z0-9\s]/g, '')
    .replace(/\s+/g, '-')
    .replace(/^-|-$/g, '')
}