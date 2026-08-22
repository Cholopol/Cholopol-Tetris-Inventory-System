using Ctis.Core;
using Godot;
using TetrisCoordLib.Core.Math;

namespace Ctis.Presentation;

public partial class HighlightOverlay : Control
{
    private readonly List<ColorRect> _tiles = new();
    private bool _isCleared = true;
    private TetrisGridVM? _lastGrid;
    private Vec2I _lastOrigin;
    private InventoryDropKind _lastKind;
    private int _lastCellCount = -1;
    private int _lastFingerprint;
    private float _lastTileW = float.NaN;
    private float _lastTileH = float.NaN;

    public HighlightOverlay()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 0;
        ZAsRelative = true;
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 0;
        ZAsRelative = true;
        SetAnchorsPreset(LayoutPreset.TopLeft);
    }

    public void Clear()
    {
        if (_isCleared) return;
        _isCleared = true;
        _lastGrid = null;
        _lastCellCount = -1;
        _lastFingerprint = 0;
        foreach (var tile in _tiles)
            tile.Visible = false;
    }

    public void Release()
    {
        _isCleared = true;
        _lastGrid = null;
        _lastCellCount = -1;
        _lastFingerprint = 0;
        foreach (var tile in _tiles)
        {
            tile.GetParent()?.RemoveChild(tile);
            CtisRuntime.HighlightTiles.Free(tile);
        }
        _tiles.Clear();
    }

    /// <summary>Paints precomputed drop-preview cells. Layout-only; colors come from the VM.</summary>
    public void Show(InventoryDropPreview preview, float tileW, float tileH)
    {
        int fingerprint = Fingerprint(preview);
        if (!_isCleared
            && ReferenceEquals(preview.Grid, _lastGrid)
            && preview.Origin == _lastOrigin
            && preview.Result.Kind == _lastKind
            && preview.Cells.Count == _lastCellCount
            && fingerprint == _lastFingerprint
            && tileW == _lastTileW
            && tileH == _lastTileH)
            return;

        _isCleared = false;
        _lastGrid = preview.Grid;
        _lastOrigin = preview.Origin;
        _lastKind = preview.Result.Kind;
        _lastCellCount = preview.Cells.Count;
        _lastFingerprint = fingerprint;
        _lastTileW = tileW;
        _lastTileH = tileH;
        EnsureTiles(preview.Cells.Count);
        int i = 0;
        foreach (var cell in preview.Cells)
        {
            var tile = _tiles[i++];
            tile.Visible = true;
            tile.Position = new Vector2(cell.Cell.X * tileW, cell.Cell.Y * tileH);
            tile.Size = new Vector2(tileW, tileH);
            tile.Color = new Color(cell.Color.R, cell.Color.G, cell.Color.B, cell.Color.A);
        }
        for (; i < _tiles.Count; i++)
            _tiles[i].Visible = false;
    }

    private static int Fingerprint(InventoryDropPreview preview)
    {
        int hash = preview.Cells.Count;
        foreach (var cell in preview.Cells)
            hash = hash * 31 + cell.Cell.X * 397 + cell.Cell.Y + (int)cell.Kind;
        return hash;
    }

    private void EnsureTiles(int count)
    {
        while (_tiles.Count < count)
        {
            var rect = CtisRuntime.HighlightTiles.Allocate();
            rect.MouseFilter = MouseFilterEnum.Ignore;
            rect.Visible = true;
            if (rect.GetParent() != this)
                AddChild(rect);
            _tiles.Add(rect);
        }
    }
}
