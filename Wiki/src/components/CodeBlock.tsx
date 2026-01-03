import { useState } from 'react'
import Prism from 'prismjs'
import 'prismjs/components/prism-typescript'
import 'prismjs/components/prism-javascript'
import 'prismjs/components/prism-jsx'
import 'prismjs/components/prism-tsx'
import 'prismjs/components/prism-bash'
import 'prismjs/components/prism-shell-session'
import 'prismjs/components/prism-json'
import 'prismjs/components/prism-markup'
import 'prismjs/components/prism-css'
import 'prismjs/themes/prism-tomorrow.css'
import { Copy, Check } from 'lucide-react'

interface CodeBlockProps {
  code: string
  language?: string
  filename?: string
}

export function CodeBlock({ code, language = 'typescript', filename }: CodeBlockProps) {
  const [copied, setCopied] = useState(false)

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(code)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch (err) {
      console.error('Failed to copy code:', err)
    }
  }

  const getLanguageDisplayName = (lang: string) => {
    const languageMap: Record<string, string> = {
      typescript: 'TypeScript',
      javascript: 'JavaScript',
      tsx: 'TSX',
      jsx: 'JSX',
      bash: 'Bash',
      'shell-session': 'Bash',
      shell: 'Bash',
      json: 'JSON',
      markup: 'HTML',
      html: 'HTML',
      css: 'CSS',
      plaintext: 'Plain Text',
      text: 'Plain Text',
    }
    return languageMap[lang] || lang.toUpperCase()
  }

  const highlightedCode = (() => {
    try {
      // Validate language parameter
      let validLanguage = language || 'plaintext'
      
      // Map language aliases
      const languageAliases: Record<string, string> = {
        'shell-session': 'bash',
        'shell': 'bash',
        'html': 'markup',
        'plaintext': 'text'
      }
      
      if (languageAliases[validLanguage]) {
        validLanguage = languageAliases[validLanguage]
      }
      
      // Check if the language exists in Prism
      if (!Prism.languages[validLanguage]) {
        console.warn(`Language '${validLanguage}' not supported by Prism, falling back to plain text`)
        // Return plain text with basic HTML escaping
        return code.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
      }
      
      // Ensure code is a string
      const codeString = String(code || '')
      return Prism.highlight(codeString, Prism.languages[validLanguage], validLanguage)
    } catch (error) {
      console.error('Prism highlighting error:', error)
      // Fallback to plain text with basic HTML escaping
      return code.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
    }
  })()

  return (
    <div className="code-block my-6">
      <div className="code-header">
        <div className="flex items-center space-x-3">
          <div className="flex items-center space-x-2">
            <div className="flex space-x-1.5">
              <div className="w-3 h-3 rounded-full bg-red-500"></div>
              <div className="w-3 h-3 rounded-full bg-yellow-500"></div>
              <div className="w-3 h-3 rounded-full bg-green-500"></div>
            </div>
            {filename && (
              <span className="text-sm font-medium text-muted-foreground">{filename}</span>
            )}
          </div>
        </div>
        <div className="flex items-center space-x-2">
          <span className="text-xs text-muted-foreground font-mono">
            {getLanguageDisplayName(language)}
          </span>
          <button
            onClick={handleCopy}
            className="copy-button"
            aria-label="Copy code"
          >
            {copied ? (
              <>
                <Check className="h-3 w-3 mr-1" />
                已复制
              </>
            ) : (
              <>
                <Copy className="h-3 w-3 mr-1" />
                复制
              </>
            )}
          </button>
        </div>
      </div>
      <pre className="p-4 overflow-x-auto text-sm">
        <code
          className={`language-${language}`}
          dangerouslySetInnerHTML={{ __html: highlightedCode }}
        />
      </pre>
    </div>
  )
}