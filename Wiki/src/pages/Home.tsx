import { useNavigate } from 'react-router-dom'
import { ArrowRight, Github, Rocket, FileText, BookOpen, Code, Users, ExternalLink } from 'lucide-react'
import { useI18n } from '../i18n'

interface CardItem {
  icon: React.ComponentType<{ className?: string }>
  title: string
  description: string
  linkText: string
  linkTo: string
}

export default function Home() {
  const navigate = useNavigate()
  const { t } = useI18n()

  const cardItems: CardItem[] = [
    {
      icon: Github,
      title: t('card.github.title'),
      description: t('card.github.description'),
      linkText: t('card.github.link'),
      linkTo: "https://github.com/Cholopol/Cholopol-Tetris-Inventory-System.git"
    },
    {
      icon: Rocket,
      title: t('card.quickstart.title'),
      description: t('card.quickstart.description'),
      linkText: t('card.quickstart.link'),
      linkTo: "/getting-started"
    },
    {
      icon: FileText,
      title: t('card.documentation.title'),
      description: t('card.documentation.description'),
      linkText: t('card.documentation.link'),
      linkTo: "/documentation"
    },
    {
      icon: BookOpen,
      title: t('card.api.title'),
      description: t('card.api.description'),
      linkText: t('card.api.link'),
      linkTo: "/api"
    },
    {
      icon: Code,
      title: t('card.examples.title'),
      description: t('card.examples.description'),
      linkText: t('card.examples.link'),
      linkTo: "/examples"
    },
    {
      icon: Users,
      title: t('card.contributing.title'),
      description: t('card.contributing.description'),
      linkText: t('card.contributing.link'),
      linkTo: "/contributing"
    },
    {
      icon: ExternalLink,
      title: t('card.external.title.bilibili'),
      description: t('card.external.description.bilibili'),
      linkText: t('card.external.link'),
      linkTo: "https://space.bilibili.com/88797367"
    },
    {
      icon: ExternalLink,
      title: t('card.external.title.youtube'),
      description: t('card.external.description.youtube'),
      linkText: t('card.external.link'),
      linkTo: "https://www.youtube.com/@Cholopol-IndieGame"
    },
    {
      icon: ExternalLink,
      title: t('card.external.title.xiaohongshu'),
      description: t('card.external.description.xiaohongshu'),
      linkText: t('card.external.link'),
      linkTo: "https://www.xiaohongshu.com/user/profile/5db2ad3c0000000001004849"
    }
  ]

  const handleCardClick = (item: CardItem) => {
    if (item.linkTo.startsWith('http')) {
      window.open(item.linkTo, '_blank', 'noopener,noreferrer')
    } else {
      navigate(item.linkTo)
    }
  }

  return (
    <div className="min-h-screen">
      {/* Hero Section */}
      <div className="relative overflow-hidden">
        <div className="px-6 py-16 mx-auto max-w-7xl lg:px-8">
          <div className="mx-auto max-w-10xl text-center">
            <h1 className="text-4xl font-bold tracking-tight text-foreground sm:text-6xl dark:text-foreground">
              {t('home.title')}
            </h1>
            <p className="mt-6 text-lg leading-8 text-muted-foreground dark:text-muted-foreground">
              {t('home.subtitle')}
            </p>
          </div>
        </div>
      </div>

      {/* Card Grid */}
      <div className="px-6 py-12 mx-auto max-w-7xl lg:px-8">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {cardItems.map((item, index) => (
            <div
              key={index}
              className="cholopols-card home-card group relative p-6 hover:shadow-lg transition-all duration-200 cursor-pointer hover:-translate-y-1"
              onClick={() => handleCardClick(item)}
            >
              {/* Card Icon */}
              <div className="mb-4">
                <item.icon className="h-8 w-8 text-primary" />
              </div>

              {/* Card Title */}
              <h3 className="text-lg font-semibold text-foreground mb-3 dark:text-foreground">
                {item.title}
              </h3>

              {/* Card Description */}
              <p className="text-sm text-muted-foreground mb-4 leading-relaxed dark:text-muted-foreground">
                {item.description}
              </p>

              {/* Card Link */}
              <div className="flex items-center text-sm font-medium text-primary hover:text-primary/80 transition-colors dark:text-primary">
                {item.linkText}
                <ArrowRight className="ml-1 h-4 w-4 group-hover:translate-x-1 transition-transform" />
              </div>
            </div>
          ))}
        </div>
      </div>

      <footer className="px-6 pb-10 mx-auto max-w-7xl lg:px-8">
        <div className="text-center text-sm text-muted-foreground">
          Copyright (c) 2026 Cholopol. All rights reserved.
        </div>
      </footer>
    </div>
  )
}
