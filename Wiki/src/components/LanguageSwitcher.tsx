import { useState, useRef, useEffect } from 'react'
import { Globe, Check } from 'lucide-react'
import { useI18n, type Language } from '../i18n'

export function LanguageSwitcher() {
  const { currentLanguage, setLanguage, t } = useI18n()
  const [isOpen, setIsOpen] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)

  const languages: { code: Language; name: string; flag: string }[] = [
    { code: 'zh-CN', name: t('language.zh-CN'), flag: '🇨🇳' },
    { code: 'en-US', name: t('language.en-US'), flag: '🇺🇸' }
  ]

  const currentLang = languages.find(lang => lang.code === currentLanguage) || languages[0]

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside)
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [isOpen])

  const handleLanguageChange = (langCode: Language) => {
    setLanguage(langCode)
    setIsOpen(false)
  }

  return (
    <div className="relative" ref={dropdownRef}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="cholopols-button cholopols-button-ghost p-2 rounded-md hover:bg-accent hover:text-accent-foreground transition-colors"
        title={t('language.switch')}
      >
        <Globe className="h-4 w-4" />
        <span className="ml-1 text-xs hidden sm:inline font-medium language-code">{currentLang.code === 'zh-CN' ? 'CN' : 'EN'}</span>
      </button>

      {isOpen && (
        <div className="absolute right-0 mt-2 w-32 bg-popover border border-border rounded-md shadow-lg z-50 language-switcher-dropdown">
          <div className="py-1">
            {languages.map((lang) => (
              <button
                key={lang.code}
                onClick={() => handleLanguageChange(lang.code)}
                className={`w-full text-left px-3 py-2 text-sm hover:bg-accent hover:text-accent-foreground transition-colors language-switcher-button flex items-center justify-between ${
                  currentLanguage === lang.code ? 'text-foreground font-medium' : 'text-foreground'
                }`}
              >
                <div className="flex items-center space-x-2">
                  <span>{lang.flag}</span>
                  <span>{lang.name}</span>
                </div>
                {currentLanguage === lang.code && <Check className="h-3 w-3" />}
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
