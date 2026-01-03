import { Construction } from 'lucide-react'
import { Link } from 'react-router-dom'
import { useI18n } from '../i18n'

export default function ApiReference() {
  const { t } = useI18n()

  return (
    <div className="flex flex-col items-center justify-center h-[calc(100vh-4rem)] text-center px-4">
      <div className="w-24 h-24 bg-primary/10 rounded-full flex items-center justify-center mb-6">
        <Construction className="w-12 h-12 text-primary" />
      </div>
      <h1 className="text-3xl font-bold mb-4 bg-gradient-to-r from-blue-600 to-indigo-600 bg-clip-text text-transparent">
        {t('comingSoon.title')}
      </h1>
      <p className="text-xl text-muted-foreground mb-8 max-w-md">
        {t('comingSoon.desc')}
      </p>
      <Link 
        to="/" 
        className="inline-flex items-center justify-center px-6 py-3 border border-transparent text-base font-medium rounded-md text-white bg-primary hover:bg-primary/90 transition-colors"
      >
        {t('comingSoon.back')}
      </Link>
    </div>
  )
}
