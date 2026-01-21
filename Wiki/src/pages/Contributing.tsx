import { CodeBlock } from '../components/CodeBlock'
import { HeaderAnchor } from '../components/HeaderAnchor'
import { useI18n } from '../i18n'

export default function Contributing() {
  const { t } = useI18n()

  const setupCode = `# ${t('contributing.setup.fork')}
# ${t('contributing.setup.clone')}
git clone https://github.com/your-username/Cholopol-Tetris-Inventory-System.git
cd Cholopol-Tetris-Inventory-System

# ${t('contributing.setup.unity')}
# ${t('contributing.setup.unityDesc')}

# ${t('contributing.setup.branch')}
git checkout -b feature/your-feature-name`

  const commitCode = `# ${t('contributing.submit.title')}
git add .
git commit -m "feat: ${t('contributing.submit.type.feat')}"

# Push to your fork
git push origin feature/your-feature-name

# Create Pull Request
# Visit GitHub and create Pull Request`

  return (
    <div className="space-y-8">
      <div>
        <HeaderAnchor id="contributing-overview" level={1}>
          {t('contributing.title')}
        </HeaderAnchor>
        <p className="text-lg text-muted-foreground">
          {t('contributing.subtitle')}
        </p>
      </div>

      <div className="space-y-8">
        <section>
          <HeaderAnchor id="getting-started" level={2}>
            {t('contributing.overview.title')}
          </HeaderAnchor>
          <p className="text-muted-foreground mb-4">
            {t('contributing.overview.desc')}
          </p>
          <div className="cholopols-card p-6">
            <ul className="space-y-2 text-muted-foreground">
              <li className="flex items-start">
                <span className="text-blue-500 mr-2">•</span>
                <span>{t('contributing.overview.item1')}</span>
              </li>
              <li className="flex items-start">
                <span className="text-blue-500 mr-2">•</span>
                <span>{t('contributing.overview.item2')}</span>
              </li>
              <li className="flex items-start">
                <span className="text-blue-500 mr-2">•</span>
                <span>{t('contributing.overview.item3')}</span>
              </li>
              <li className="flex items-start">
                <span className="text-blue-500 mr-2">•</span>
                <span>{t('contributing.overview.item4')}</span>
              </li>
            </ul>
          </div>
        </section>

        <section>
          <HeaderAnchor id="development-setup" level={2}>
            {t('contributing.setup.title')}
          </HeaderAnchor>
          <p className="text-muted-foreground mb-4">
            {t('contributing.setup.desc')}
          </p>
          <CodeBlock code={setupCode} language="bash" />
        </section>

        <section>
          <HeaderAnchor id="submitting-changes" level={2}>
            {t('contributing.submit.title')}
          </HeaderAnchor>
          <p className="text-muted-foreground mb-4">
            {t('contributing.submit.desc')}
          </p>
          <div className="cholopols-card p-6 mb-4">
            <HeaderAnchor id="commit-types" level={3}>
              {t('contributing.submit.types.title')}
            </HeaderAnchor>
            <ul className="space-y-2 text-muted-foreground">
              <li><code className="bg-muted px-2 py-1 rounded">feat:</code> {t('contributing.submit.type.feat')}</li>
              <li><code className="bg-muted px-2 py-1 rounded">fix:</code> {t('contributing.submit.type.fix')}</li>
              <li><code className="bg-muted px-2 py-1 rounded">docs:</code> {t('contributing.submit.type.docs')}</li>
              <li><code className="bg-muted px-2 py-1 rounded">style:</code> {t('contributing.submit.type.style')}</li>
              <li><code className="bg-muted px-2 py-1 rounded">refactor:</code> {t('contributing.submit.type.refactor')}</li>
              <li><code className="bg-muted px-2 py-1 rounded">test:</code> {t('contributing.submit.type.test')}</li>
              <li><code className="bg-muted px-2 py-1 rounded">chore:</code> {t('contributing.submit.type.chore')}</li>
            </ul>
          </div>
          <CodeBlock code={commitCode} language="bash" />
        </section>
      </div>
    </div>
  )
}
