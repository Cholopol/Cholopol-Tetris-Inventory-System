import { HeaderAnchor } from '../components/HeaderAnchor'
import { CodeBlock } from '../components/CodeBlock'
import { Star, Cpu, Layers, Grid, MousePointerClick, Box, Network } from 'lucide-react'
import { useI18n } from '../i18n'

export default function Documentation() {
  const { t } = useI18n()
  const tetrisGridVMCode = `// TetrisGridVM.cs
private TetrisItemVM[,] _tetrisItemOccupiedCells; // [width, height]`

  const highlightCode = `// InventoryHighlight.cs
Vector2 tilePos = new Vector2(
    (point.x + ghost.RotationOffset.x) * tileW,
    -((point.y + ghost.RotationOffset.y) * tileH) // Y轴取负
);`

  const rotateCode = `// TetrisUtilities.cs
public static List<Vector2Int> RotatePointsClockwise(List<Vector2Int> points)
{
    List<Vector2Int> rotatedPoints = new();
    foreach (var point in points)
    {
        rotatedPoints.Add(new Vector2Int(-point.y, point.x)); // 核心旋转公式
    }
    return rotatedPoints;}`

  const itemVMCode = `// TetrisItemVM.cs
public bool Rotated { get => _rotated; set => Set(ref _rotated, value); }`

  return (
    <div className="max-w-4xl mx-auto space-y-8">
      {/* Page Header */}
      <div className="space-y-4">
        <HeaderAnchor id="documentation-overview" level={1} className="text-4xl font-extrabold tracking-tight lg:text-5xl mb-6">
          {t('doc.title')}
        </HeaderAnchor>
        <p className="text-xl text-muted-foreground leading-relaxed">
          {t('doc.subtitle')}
        </p>
      </div>

      {/* Core Features */}
      <section className="space-y-6">
        <HeaderAnchor id="core-features" level={2} className="text-2xl font-bold tracking-tight border-b pb-2 mt-8 mb-4">
          {t('doc.core.title')}
        </HeaderAnchor>
        <div className="grid gap-6 md:grid-cols-2">
          <div className="cholopols-card p-6">
            <div className="flex items-center space-x-2 mb-4">
              <Grid className="h-5 w-5 text-primary" />
              <h3 className="text-xl font-semibold text-primary">{t('doc.core.feature1.title')}</h3>
            </div>
            <p className="text-muted-foreground">
              {t('doc.core.feature1.desc')}
            </p>
          </div>
          <div className="cholopols-card p-6">
            <div className="flex items-center space-x-2 mb-4">
              <Layers className="h-5 w-5 text-primary" />
              <h3 className="text-xl font-semibold text-primary">{t('doc.core.feature2.title')}</h3>
            </div>
            <p className="text-muted-foreground">
              {t('doc.core.feature2.desc')}
            </p>
          </div>
          <div className="cholopols-card p-6">
            <div className="flex items-center space-x-2 mb-4">
              <Box className="h-5 w-5 text-primary" />
              <h3 className="text-xl font-semibold text-primary">{t('doc.core.feature3.title')}</h3>
            </div>
            <p className="text-muted-foreground">
              {t('doc.core.feature3.desc')}
            </p>
          </div>
          <div className="cholopols-card p-6">
            <div className="flex items-center space-x-2 mb-4">
              <Star className="h-5 w-5 text-primary" />
              <h3 className="text-xl font-semibold text-primary">{t('doc.core.feature4.title')}</h3>
            </div>
            <ul className="text-sm text-muted-foreground space-y-1">
              <li>• {t('doc.core.feature4.item1')}</li>
              <li>• {t('doc.core.feature4.item2')}</li>
            </ul>
          </div>
        </div>
      </section>

      {/* Core Components */}
      <section className="space-y-6">
        <HeaderAnchor id="core-components" level={2}>
          {t('doc.components.title')}
        </HeaderAnchor>
        
        <div className="space-y-6">
          <div className="cholopols-card p-6">
            <div className="flex items-center space-x-2 mb-4">
              <Layers className="h-5 w-5 text-primary" />
              <h3 className="text-xl font-semibold text-primary">{t('doc.components.mvvm.title')}</h3>
            </div>
            <p className="text-muted-foreground mb-4">
              {t('doc.components.mvvm.desc')}
            </p>
            <div className="space-y-4">
              <div>
                <h4 className="text-lg font-semibold mb-3 border-l-4 border-primary pl-3 bg-muted/30 py-1 rounded-r">{t('doc.components.mvvm.vm.title')}</h4>
                <p className="text-sm text-muted-foreground">
                  {t('doc.components.mvvm.vm.desc')}
                </p>
              </div>
              <div>
                <h4 className="text-lg font-semibold mb-3 border-l-4 border-primary pl-3 bg-muted/30 py-1 rounded-r">{t('doc.components.mvvm.view.title')}</h4>
                <p className="text-sm text-muted-foreground">
                  {t('doc.components.mvvm.view.desc')}
                </p>
              </div>
            </div>
          </div>

          <div className="cholopols-card p-6">
            <div className="flex items-center space-x-2 mb-4">
              <Cpu className="h-5 w-5 text-primary" />
              <h3 className="text-xl font-semibold text-primary">{t('doc.components.interaction.title')}</h3>
            </div>
            <p className="text-muted-foreground mb-2">
              {t('doc.components.interaction.desc')}
            </p>
            <ul className="list-disc list-inside space-y-2 text-muted-foreground text-sm">
              <li><strong>{t('doc.components.interaction.item1')}</strong>{t('doc.components.interaction.item1.desc')}</li>
              <li><strong>{t('doc.components.interaction.item2')}</strong>{t('doc.components.interaction.item2.desc')}</li>
              <li><strong>{t('doc.components.interaction.item3')}</strong>{t('doc.components.interaction.item3.desc')}</li>
            </ul>
          </div>

          <div className="cholopols-card p-6">
            <div className="flex items-center space-x-2 mb-4">
              <Box className="h-5 w-5 text-primary" />
              <h3 className="text-xl font-semibold text-primary">{t('doc.components.persistence.title')}</h3>
            </div>
            <p className="text-muted-foreground mb-2">
              {t('doc.components.persistence.desc')}
            </p>
            <ul className="list-disc list-inside space-y-2 text-muted-foreground text-sm">
              <li><strong>{t('doc.components.persistence.item1')}</strong>{t('doc.components.persistence.item1.desc')}</li>
              <li><strong>{t('doc.components.persistence.item2')}</strong>{t('doc.components.persistence.item2.desc')}</li>
            </ul>
          </div>
        </div>
      </section>

      {/* Loxodon MVVM */}
      <section className="space-y-6">
        <HeaderAnchor id="loxodon-mvvm" level={2} className="text-2xl font-bold tracking-tight border-b pb-2 mt-8 mb-4">
          {t('doc.loxodon.title')}
        </HeaderAnchor>
        <p className="text-muted-foreground">
          {t('doc.loxodon.desc')}
        </p>

        <div className="space-y-4">
          <div>
            <HeaderAnchor id="model-layer" level={3} className="text-xl font-semibold text-primary mb-4">
              {t('doc.loxodon.model.title')}
            </HeaderAnchor>
            <p className="text-muted-foreground mb-2">{t('doc.loxodon.model.desc')}</p>
            <ul className="list-disc list-inside space-y-1 text-muted-foreground text-sm">
              <li><strong>{t('doc.loxodon.model.item1')}</strong>{t('doc.loxodon.model.item1.desc')}</li>
              <li><strong>{t('doc.loxodon.model.item2')}</strong>{t('doc.loxodon.model.item2.desc')}</li>
            </ul>
          </div>

          <div>
            <HeaderAnchor id="viewmodel-layer" level={3} className="text-xl font-semibold text-primary mb-4">
              {t('doc.loxodon.vm.title')}
            </HeaderAnchor>
            <p className="text-muted-foreground mb-2">{t('doc.loxodon.vm.desc')}</p>
            <CodeBlock code={itemVMCode} language="csharp" filename="TetrisItemVM.cs" />
          </div>

          <div>
            <HeaderAnchor id="view-layer" level={3} className="text-xl font-semibold text-primary mb-4">
              {t('doc.loxodon.view.title')}
            </HeaderAnchor>
            <p className="text-muted-foreground mb-2">{t('doc.loxodon.view.desc')}</p>
            <p className="text-muted-foreground text-sm">
              {t('doc.loxodon.view.desc2')}
            </p>
          </div>
        </div>
      </section>

      {/* Tetris Coordinate System */}
      <section className="space-y-6">
        <HeaderAnchor id="tetris-coordinate-system" level={2} className="text-2xl font-bold tracking-tight border-b pb-2 mt-8 mb-4">
          {t('doc.coords.title')}
        </HeaderAnchor>
        
        <div className="space-y-4">
          <p className="text-muted-foreground">
            {t('doc.coords.desc')}
          </p>

          <div className="cholopols-card p-6">
            <HeaderAnchor id="coordinate-definition" level={3} className="text-xl font-semibold text-primary mb-4">
              {t('doc.coords.def.title')}
            </HeaderAnchor>
            <div className="space-y-4">
              <div>
                <h4 className="text-lg font-semibold mb-3 border-l-4 border-primary pl-3 bg-muted/30 py-1 rounded-r">{t('doc.coords.grid.title')}</h4>
                <p className="text-sm text-muted-foreground mb-2">
                  {t('doc.coords.grid.desc')}
                </p>
                <CodeBlock code={tetrisGridVMCode} language="csharp" />
              </div>

              <div>
                <h4 className="text-lg font-semibold mb-3 border-l-4 border-primary pl-3 bg-muted/30 py-1 rounded-r">{t('doc.coords.pixel.title')}</h4>
                <div className="bg-muted p-4 rounded-md font-mono text-sm space-y-2">
                  <p>P_x = x * W_unit</p>
                  <p>P_y = - (y * H_unit)</p>
                </div>
                <p className="text-sm text-muted-foreground mt-2">
                  {t('doc.coords.pixel.note')}
                </p>
                <CodeBlock code={highlightCode} language="csharp" />
              </div>

              <div>
                <h4 className="text-lg font-semibold mb-3 border-l-4 border-primary pl-3 bg-muted/30 py-1 rounded-r">{t('doc.coords.rotate.title')}</h4>
                <div className="bg-muted p-4 rounded-md font-mono text-sm space-y-2">
                  <p>x' = -y</p>
                  <p>y' = x</p>
                </div>
                <CodeBlock code={rotateCode} language="csharp" />
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* SpriteMeshRaycastFilter */}
      <section className="space-y-6">
        <HeaderAnchor id="raycast-filter" level={2}>
          {t('doc.raycast.title')}
        </HeaderAnchor>
        <div className="cholopols-card p-6">
          <div className="flex items-center space-x-2 mb-4">
            <MousePointerClick className="h-5 w-5 text-primary" />
            <h3 className="text-xl font-semibold text-primary">{t('doc.raycast.subtitle')}</h3>
          </div>
          <p className="text-muted-foreground mb-4">
            {t('doc.raycast.desc')}
          </p>
          <div className="space-y-2 text-sm text-muted-foreground">
            <p>1. <strong>{t('doc.raycast.step1')}</strong>{t('doc.raycast.step1.desc')}</p>
            <p>2. <strong>{t('doc.raycast.step2')}</strong>{t('doc.raycast.step2.desc')}</p>
            <p>3. <strong>{t('doc.raycast.step3')}</strong>{t('doc.raycast.step3.desc')}</p>
          </div>
        </div>
      </section>

      {/* Nested Inventory */}
      <section className="space-y-6">
        <HeaderAnchor id="nested-inventory" level={2}>
          {t('doc.nested.title')}
        </HeaderAnchor>
        <div className="cholopols-card p-6">
          <div className="flex items-center space-x-2 mb-4">
            <Box className="h-5 w-5 text-primary" />
            <h3 className="text-xl font-semibold text-primary">{t('doc.nested.subtitle')}</h3>
          </div>
          <p className="text-muted-foreground mb-4">
            {t('doc.nested.desc')}
          </p>
          <div className="space-y-4">
            <div>
              <h4 className="text-lg font-semibold mb-3 border-l-4 border-primary pl-3 bg-muted/30 py-1 rounded-r">{t('doc.nested.guid.title')}</h4>
              <p className="text-sm text-muted-foreground">
                {t('doc.nested.guid.desc')}
              </p>
            </div>
            <div>
              <h4 className="text-lg font-semibold mb-3 border-l-4 border-primary pl-3 bg-muted/30 py-1 rounded-r">{t('doc.nested.restore.title')}</h4>
              <p className="text-sm text-muted-foreground">
                {t('doc.nested.restore.desc')}
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* InventoryTreeCache */}
      <section className="space-y-6">
        <HeaderAnchor id="inventory-tree-cache" level={2} className="text-2xl font-bold tracking-tight border-b pb-2 mt-8 mb-4">
          {t('doc.cache.title')}
        </HeaderAnchor>
        <div className="cholopols-card p-6">
          <div className="flex items-center space-x-2 mb-4">
            <Network className="h-5 w-5 text-primary" />
            <h3 className="text-xl font-semibold text-primary">{t('doc.cache.subtitle')}</h3>
          </div>
          <p className="text-muted-foreground">
            {t('doc.cache.desc')}
          </p>
        </div>
      </section>
    </div>
  )
}
