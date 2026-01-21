import { useState, useEffect, useRef, useCallback } from 'react'
import { Search, X, ArrowRight, FileText, Code, Book, Settings, Zap, Home } from 'lucide-react'
import { searchDocuments, type SearchResult } from '../utils/search'
import { useNavigate } from 'react-router-dom'
import { useI18n } from '../i18n'

interface SearchModalProps {
  isOpen: boolean
  onClose: () => void
  isDarkMode: boolean
}

export function SearchModal({ isOpen, onClose, isDarkMode }: SearchModalProps) {
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<SearchResult[]>([])
  const [selectedIndex, setSelectedIndex] = useState(0)
  const inputRef = useRef<HTMLInputElement>(null)
  const resultsRef = useRef<HTMLDivElement>(null)
  const navigate = useNavigate()
  const { t, currentLanguage } = useI18n()

  const handleResultClick = useCallback((result: SearchResult) => {
    const url = result.anchor ? `${result.path}${result.anchor}` : result.path
    navigate(url)
    onClose()
    setQuery('')
  }, [navigate, onClose])

  useEffect(() => {
    if (isOpen && inputRef.current) {
      inputRef.current.focus()
    }
  }, [isOpen])

  // 滚动到当前选中的结果项
  useEffect(() => {
    if (resultsRef.current && results.length > 0) {
      const selectedElement = resultsRef.current.children[selectedIndex] as HTMLElement
      if (selectedElement) {
        selectedElement.scrollIntoView({
          behavior: 'smooth',
          block: 'nearest',
          inline: 'nearest'
        })
      }
    }
  }, [selectedIndex, results.length])

  useEffect(() => {
    if (query.trim()) {
      const searchResults = searchDocuments(query, currentLanguage)
      setResults(searchResults)
      setSelectedIndex(0)
    } else {
      setResults([])
      setSelectedIndex(0)
    }
  }, [query, currentLanguage])

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (!isOpen) return

      switch (e.key) {
        case 'ArrowDown':
          e.preventDefault()
          setSelectedIndex(prev => 
            prev < results.length - 1 ? prev + 1 : prev
          )
          break
        case 'ArrowUp':
          e.preventDefault()
          setSelectedIndex(prev => prev > 0 ? prev - 1 : 0)
          break
        case 'Enter':
          e.preventDefault()
          if (results[selectedIndex]) {
            handleResultClick(results[selectedIndex])
          }
          break
        case 'Escape':
          e.preventDefault()
          onClose()
          break
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [isOpen, results, selectedIndex, onClose, handleResultClick])

  const highlightText = (text: string, query: string) => {
    if (!query.trim()) return text
    
    const regex = new RegExp(`(${query})`, 'gi')
    const parts = text.split(regex)
    
    return parts.map((part, index) => 
      regex.test(part) ? (
        <mark key={index} className="search-highlight bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 text-inherit">
          {part}
        </mark>
      ) : part
    )
  }

  const getIcon = (category: string) => {
    switch (category) {
      case '快速开始': return <Book className="h-4 w-4" />
      case '文档': return <FileText className="h-4 w-4" />
      case 'API': return <Code className="h-4 w-4" />
      case '示例': return <Zap className="h-4 w-4" />
      case '贡献': return <Settings className="h-4 w-4" />
      default: return <Home className="h-4 w-4" />
    }
  }

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 z-50">
      {/* Backdrop */}
      <div 
        className="absolute inset-0 bg-black/50 backdrop-blur-sm" 
        onClick={onClose}
      />
      
      {/* Search Container */}
      <div className="relative mx-auto max-w-2xl mt-20">
        {/* Search Input */}
        <div className={`search-modal-container relative rounded-xl border-2 shadow-2xl overflow-hidden ${
          isDarkMode 
            ? 'bg-gray-900 border-gray-600 shadow-blue-500/10' 
            : 'bg-white border-gray-300 shadow-blue-500/20'
        }`}>
          <div className="flex items-center px-6 py-4">
            <Search className="h-5 w-5 text-muted-foreground mr-4" />
            <input
              ref={inputRef}
              type="text"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder={t('search.placeholder')}
              className={`search-modal-input flex-1 bg-transparent outline-none text-lg font-medium transition-colors ${
                isDarkMode 
                  ? 'text-gray-100 placeholder-gray-400 focus:placeholder-gray-300' 
                  : 'text-gray-900 placeholder-gray-500 focus:placeholder-gray-400'
              }`}
            />
            {query && (
              <button
                onClick={() => setQuery('')}
                className="ml-3 p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
              >
                <X className="h-4 w-4" />
              </button>
            )}
            <button
              onClick={onClose}
              className={`ml-3 px-3 py-1.5 text-xs rounded-lg border font-medium ${
                isDarkMode 
                  ? 'border-gray-600 text-gray-400 hover:bg-gray-800 hover:text-gray-200' 
                  : 'border-gray-300 text-gray-600 hover:bg-gray-100 hover:text-gray-800'
              }`}
            >
              {t('search.esc')}
            </button>
          </div>
        </div>

        {/* Search Results */}
        {results.length > 0 && (
          <div 
            ref={resultsRef}
            className={`mt-3 rounded-xl border-2 shadow-xl max-h-96 overflow-y-auto ${
              isDarkMode 
                ? 'bg-gray-900 border-gray-600 shadow-blue-500/10' 
                : 'bg-white border-gray-300 shadow-blue-500/20'
            }`}
          >
            {results.map((result, index) => (
              <div
                key={result.id}
                onClick={() => handleResultClick(result)}
                className={`p-5 cursor-pointer border-b last:border-b-0 transition-all duration-200 ${
                  isDarkMode ? 'border-gray-700' : 'border-gray-200'
                } ${
                  index === selectedIndex
                    ? isDarkMode
                      ? 'bg-blue-900/20 border-blue-400'
                      : 'bg-blue-50 border-blue-200'
                    : isDarkMode
                      ? 'hover:bg-gray-800'
                      : 'hover:bg-gray-50'
                }`}>
                {/* Category and Section */}
                <div className="flex items-center gap-3 mb-3">
                  <div className={`flex items-center gap-2 px-3 py-1.5 rounded-lg text-xs font-medium ${
                    isDarkMode ? 'bg-gray-800 text-gray-300' : 'bg-gray-100 text-gray-700'
                  }`}>
                    {getIcon(result.category)}
                    <span>{result.category}</span>
                  </div>
                  {result.section !== result.title && (
                    <div className={`px-3 py-1.5 rounded-lg text-xs font-medium ${
                      isDarkMode ? 'bg-blue-900/20 text-blue-300 border border-blue-800/30' : 'bg-blue-100 text-blue-700 border border-blue-200'
                    }`}>
                      {result.section}
                    </div>
                  )}
                </div>

                {/* Title */}
                <h3 className={`font-semibold mb-2 text-lg ${
                  isDarkMode ? 'text-white' : 'text-gray-900'
                }`}>
                  {result.parentTitle && result.parentTitle !== result.title && (
                    <span className="text-sm text-muted-foreground mr-2 font-normal">
                      {result.parentTitle} /
                    </span>
                  )}
                  {highlightText(result.title, query)}
                </h3>

                {/* Content Preview */}
                <p className={`text-sm leading-relaxed line-clamp-2 ${
                  isDarkMode ? 'text-gray-300' : 'text-gray-600'
                }`}>
                  {highlightText(result.content.slice(0, 150) + '...', query)}
                </p>

                {/* Path */}
                <div className="flex items-center gap-2 mt-3 text-xs text-muted-foreground font-mono">
                  <ArrowRight className="h-3 w-3" />
                  <span className="opacity-75">{result.path}{result.anchor}</span>
                </div>
              </div>
            ))}
          </div>
        )}

        {/* No Results */}
        {query.trim() && results.length === 0 && (
          <div className={`mt-3 p-8 text-center rounded-xl border-2 shadow-xl ${
            isDarkMode 
              ? 'bg-gray-900 border-gray-600 text-gray-400 shadow-blue-500/10' 
              : 'bg-white border-gray-300 text-gray-500 shadow-blue-500/20'
          }`}>
            <Search className="h-10 w-10 mx-auto mb-4 opacity-50" />
            <p className="text-lg font-medium mb-2">{t('search.noResults').replace('{query}', query)}</p>
            <p className="text-sm">{t('search.tryOther')}</p>
          </div>
        )}

        {/* Help Text */}
        <div className="flex items-center justify-center gap-6 mt-6 text-sm">
          <div className={`flex items-center gap-2 ${
            isDarkMode ? 'text-gray-400' : 'text-gray-600'
          }`}>
            <span className={`px-2 py-1 border rounded-lg text-xs font-medium ${
              isDarkMode ? 'border-gray-500 text-gray-300 bg-gray-700' : 'border-gray-400 text-gray-600 bg-gray-100'
            }`}>↑↓</span>
            <span className={isDarkMode ? 'text-gray-300' : 'text-gray-600'}>{t('search.navigate')}</span>
          </div>
          <div className={`flex items-center gap-2 ${
            isDarkMode ? 'text-gray-400' : 'text-gray-600'
          }`}>
            <span className={`px-2 py-1 border rounded-lg text-xs font-medium ${
              isDarkMode ? 'border-gray-500 text-gray-300 bg-gray-700' : 'border-gray-400 text-gray-600 bg-gray-100'
            }`}>↵</span>
            <span className={isDarkMode ? 'text-gray-300' : 'text-gray-600'}>{t('search.select')}</span>
          </div>
          <div className={`flex items-center gap-2 ${
            isDarkMode ? 'text-gray-400' : 'text-gray-600'
          }`}>
            <span className={`px-2 py-1 border rounded-lg text-xs font-medium ${
              isDarkMode ? 'border-gray-500 text-gray-300 bg-gray-700' : 'border-gray-400 text-gray-600 bg-gray-100'
            }`}>{t('search.esc')}</span>
            <span className={isDarkMode ? 'text-gray-300' : 'text-gray-600'}>{t('search.close')}</span>
          </div>
        </div>
      </div>
    </div>
  )
}
