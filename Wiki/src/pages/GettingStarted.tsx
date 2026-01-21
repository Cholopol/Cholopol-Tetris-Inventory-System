import { CodeBlock } from '../components/CodeBlock'
import { HeaderAnchor } from '../components/HeaderAnchor'
import { useI18n } from '../i18n'

export default function GettingStarted() {
  const { t } = useI18n()
  const cloneCode = `git clone https://github.com/Cholopol/Cholopol-Tetris-Inventory-System.git`

  const loxodonInstallCode = `{
  "dependencies": {
    // ...
    "com.unity.modules.xr": "1.0.0",
    "com.vovgou.loxodon-framework": "2.6.7"
  },
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.vovgou"
      ]
    }
  ]
}`

  return (
    <div className="space-y-8">
      <div>
        <HeaderAnchor level={1} id="getting-started" className="text-3xl font-bold text-foreground mb-4">
          {t('gettingStarted.title')}
        </HeaderAnchor>
        <p className="text-lg text-muted-foreground">
          {t('gettingStarted.subtitle')}
        </p>
      </div>

      <div className="space-y-8">
        <section id="requirements" className="cholopols-card p-6">
          <HeaderAnchor level={2} id="requirements" className="text-2xl font-semibold text-foreground mb-4">
            {t('gettingStarted.requirements.title')}
          </HeaderAnchor>
          <ul className="list-disc list-inside space-y-2 text-muted-foreground">
            <li>{t('gettingStarted.requirements.item1')}</li>
          </ul>
        </section>

        <section id="import-steps" className="cholopols-card p-6">
          <HeaderAnchor level={2} id="import-steps" className="text-2xl font-semibold text-foreground mb-4">
            {t('gettingStarted.import.title')}
          </HeaderAnchor>
          <div className="space-y-4">
            <div>
              <h3 className="text-lg font-medium text-foreground mb-2">{t('gettingStarted.import.step1.title')}</h3>
              <CodeBlock code={cloneCode} language="bash" />
            </div>
            <ul className="list-disc list-inside space-y-2 text-muted-foreground">
              <li>{t('gettingStarted.import.step2.item1')}</li>
              <li>{t('gettingStarted.import.step2.item2')}</li>
            </ul>
          </div>
        </section>

        <section id="basic-usage" className="cholopols-card p-6">
          <HeaderAnchor level={2} id="basic-usage" className="text-2xl font-semibold text-foreground mb-4">
            {t('gettingStarted.basicUsage.title')}
          </HeaderAnchor>
          <div className="space-y-4">
            <ul className="list-disc list-inside space-y-2 text-muted-foreground">
              <li>{t('gettingStarted.basicUsage.item1')} <code className="bg-muted px-1 py-0.5 rounded">Assets/Scenes/</code>。</li>
              <li>{t('gettingStarted.basicUsage.item2')} <strong>EFT Like UI</strong> 场景。</li>
              <li>{t('gettingStarted.basicUsage.item3')}</li>
            </ul>
            
            <div className="mt-4">
              <h3 className="text-lg font-medium text-foreground mb-2">{t('gettingStarted.basicUsage.controls.title')}</h3>
              <ul className="list-disc list-inside space-y-2 text-muted-foreground">
                <li><strong>{t('gettingStarted.basicUsage.controls.b')}</strong>{t('gettingStarted.basicUsage.controls.b.desc')}</li>
                <li><strong>{t('gettingStarted.basicUsage.controls.r')}</strong>{t('gettingStarted.basicUsage.controls.r.desc')}</li>
                <li><strong>{t('gettingStarted.basicUsage.controls.lmb')}</strong>{t('gettingStarted.basicUsage.controls.lmb.desc')}</li>
                <li><strong>{t('gettingStarted.basicUsage.controls.rmb')}</strong>{t('gettingStarted.basicUsage.controls.rmb.desc')}</li>
              </ul>
            </div>
          </div>
        </section>

        <section id="third-party" className="cholopols-card p-6">
          <HeaderAnchor level={2} id="third-party" className="text-2xl font-semibold text-foreground mb-4">
            {t('gettingStarted.thirdParty.title')}
          </HeaderAnchor>
          <p className="text-muted-foreground mb-4">
            {t('gettingStarted.thirdParty.subtitle')}
          </p>

          <div className="space-y-6">
            <div>
              <h3 className="text-xl font-medium text-foreground mb-2">{t('gettingStarted.thirdParty.loxodon.title')}</h3>
              <p className="text-muted-foreground mb-2">
                {t('gettingStarted.thirdParty.loxodon.desc')} <code className="bg-muted px-1 py-0.5 rounded">Packages/manifest.json</code> {t('gettingStarted.thirdParty.loxodon.desc2')}
              </p>
              <CodeBlock code={loxodonInstallCode} language="json" />
              <p className="text-muted-foreground mt-2">
                {t('gettingStarted.thirdParty.loxodon.link')}
              </p>
            </div>

            <div>
              <h3 className="text-xl font-medium text-foreground mb-2">{t('gettingStarted.thirdParty.localization.title')}</h3>
              <p className="text-muted-foreground">
                {t('gettingStarted.thirdParty.localization.desc')} <code className="bg-muted px-1 py-0.5 rounded">com.unity.localization</code>。
              </p>
            </div>

            <div>
              <h3 className="text-xl font-medium text-foreground mb-2">{t('gettingStarted.thirdParty.json.title')}</h3>
              <p className="text-muted-foreground">
                {t('gettingStarted.thirdParty.json.desc')} <code className="bg-muted px-1 py-0.5 rounded">com.unity.nuget.newtonsoft-json</code>。
              </p>
            </div>
          </div>
        </section>
      </div>
    </div>
  )
}