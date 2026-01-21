import { ReactNode, useState, useEffect } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { Book, Code, FileText, Github, Home, Settings, Menu, X, Sun, Moon, Search, Zap } from 'lucide-react'
import TetrisBackground from './TetrisBackground'
import { LanguageSwitcher } from './LanguageSwitcher'
import { SearchModal } from './SearchModal'
import { useI18n, initI18n } from '../i18n'
import lopolLogoLight from '../assets/LOPOL-LOGO.png'
import lopolLogoDark from '../assets/LOPOL-LOGO-White.png'

interface LayoutProps {
  children: ReactNode
}

export default function Layout({ children }: LayoutProps) {
  const location = useLocation()
  const [isSidebarOpen, setIsSidebarOpen] = useState(false)
  const [isSearchOpen, setIsSearchOpen] = useState(false)
  const [isDarkMode, setIsDarkMode] = useState(() => {
    // Check if dark mode is saved in localStorage or if user prefers dark mode
    const savedDarkMode = localStorage.getItem('darkMode')
    if (savedDarkMode !== null) {
      return savedDarkMode === 'true'
    }
    // Check system preference
    return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches
  })
  const { t } = useI18n()

  // Check if we're on homepage
  const isHomepage = location.pathname === '/'

  // Initialize dark mode and i18n on mount
  useEffect(() => {
    initI18n()
    if (isDarkMode) {
      document.documentElement.classList.add('dark')
    } else {
      document.documentElement.classList.remove('dark')
    }
    localStorage.setItem('darkMode', String(isDarkMode))
  }, [isDarkMode])

  // Handle keyboard shortcut for search
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Ctrl/Cmd + K to open search
      if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault()
        setIsSearchOpen(true)
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [])

  const navigation = [
    { name: t('nav.home'), href: '/', icon: Home },
    { name: t('nav.gettingStarted'), href: '/getting-started', icon: Book },
    { name: t('nav.documentation'), href: '/documentation', icon: FileText },
    { name: t('nav.api'), href: '/api', icon: Code },
    { name: t('nav.examples'), href: '/examples', icon: Zap },
    { name: t('nav.contributing'), href: '/contributing', icon: Settings },
  ]

  const isActive = (path: string) => {
    if (path === '/') {
      return location.pathname === '/'
    }
    return location.pathname.startsWith(path)
  }

  return (
    <div className="cholopols-layout relative">
      {/* Tetris Background */}
      <TetrisBackground />
      {/* Header */}
      <header className="cholopols-header sticky top-0 z-50 bg-background/85 backdrop-blur-sm">
        <div className="px-4 sm:px-6 lg:px-8">
          {/* 增加头部高度到20（从14）以适应400px的大LOGO，确保完美对齐 */}
          <div className="flex justify-between items-center h-20">
            {/* Left side */}
            <div className="flex items-center space-x-4">
                {/* 统一的菜单按钮占位符，保持布局一致性 */}
                <div className="w-9 h-9 flex items-center justify-center">
                  {!isHomepage && (
                    <button
                      onClick={() => setIsSidebarOpen(!isSidebarOpen)}
                      className="cholopols-button cholopols-button-ghost lg:hidden p-2"
                    >
                      {isSidebarOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
                    </button>
                  )}
                </div>
                <div className="flex items-center space-x-3">
                  <div className="w-16 h-16 flex items-center justify-center flex-shrink-0 bg-transparent relative">
                    <img 
                      src={isDarkMode ? lopolLogoDark : lopolLogoLight}
                      alt="LOPOL Logo"
                      className="w-full h-full object-contain max-w-full max-h-full transition-opacity duration-300 absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2"
                      style={{ filter: isDarkMode ? 'brightness(1.1)' : 'none' }}
                    />
                  </div>
                  <h1 className="text-2xl font-bold text-foreground leading-none m-0 p-0">Cholopol's Tetris Inventory System Wiki</h1>
                </div>
              </div>

            {/* Right side */}
            <div className="flex items-center space-x-2">
              {/* Search */}
              <button
                onClick={() => setIsSearchOpen(true)}
                className={`hidden md:flex items-center gap-2 px-4 py-2 text-sm rounded-lg border transition-colors ${
                  isDarkMode 
                    ? 'bg-gray-800 border-gray-600 text-gray-200 hover:bg-gray-700' 
                    : 'bg-white border-gray-300 text-gray-700 hover:bg-gray-50'
                }`}
              >
                <Search className="h-4 w-4" />
                <span>{t('search.placeholder')}</span>
                <kbd className={`ml-2 px-2 py-0.5 text-xs border rounded font-medium ${
                  isDarkMode 
                    ? 'border-gray-500 text-gray-300 bg-gray-700' 
                    : 'border-gray-400 text-gray-600 bg-gray-100'
                }`}>
                  ⌘K
                </kbd>
              </button>

              {/* Language Switcher */}
              <LanguageSwitcher />

              {/* Theme toggle */}
              <button
                onClick={() => setIsDarkMode(!isDarkMode)}
                className="cholopols-button cholopols-button-ghost p-2"
              >
                {isDarkMode ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
              </button>

              {/* GitHub link */}
              <a
                href="https://github.com"
                target="_blank"
                rel="noopener noreferrer"
                className="cholopols-button cholopols-button-ghost p-2"
              >
                <Github className="h-4 w-4" />
              </a>
            </div>
          </div>
        </div>
      </header>

      <div className="flex">
        {/* Sidebar - Hidden on homepage */}
        {!isHomepage && (
          <aside className={`cholopols-sidebar w-64 flex-shrink-0 sticky top-20 self-start h-[calc(100vh-5rem)] overflow-y-auto z-40 transform transition-transform duration-200 ease-in-out ${
            isSidebarOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'
          }`}>
            <div className="p-4">
              <nav className="space-y-1">
                {navigation.map((item) => (
                  <Link
                    key={item.name}
                    to={item.href}
                    onClick={() => setIsSidebarOpen(false)}
                    className={`sidebar-link ${
                      isActive(item.href) ? 'active' : ''
                    }`}
                  >
                    <item.icon className="h-4 w-4 mr-3 flex-shrink-0" />
                    {item.name}
                  </Link>
                ))}
              </nav>
            </div>
          </aside>
        )}

        {/* Mobile sidebar overlay - Hidden on homepage */}
        {!isHomepage && isSidebarOpen && (
          <div 
            className="fixed inset-0 bg-overlay z-30 lg:hidden"
            onClick={() => setIsSidebarOpen(false)}
          />
        )}

        {/* Main content */}
        <main className={`flex-1 ${isHomepage ? 'w-full' : ''} relative z-10`}>
          <div className={`cholopols-content p-6 lg:p-8 bg-background/80 backdrop-blur-sm`}>
            {children}
          </div>
        </main>
      </div>

      {/* Search Modal */}
      <SearchModal 
        isOpen={isSearchOpen} 
        onClose={() => setIsSearchOpen(false)} 
        isDarkMode={isDarkMode}
      />
    </div>
  )
}
