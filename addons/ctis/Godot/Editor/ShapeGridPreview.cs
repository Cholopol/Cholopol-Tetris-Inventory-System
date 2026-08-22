using Ctis.Core;
using Godot;
using TetrisCoordLib.Core.Math;

namespace Ctis.Presentation.Editor;

internal sealed partial class ShapeGridPreview : Control
{
    public const int CellSize = 24;
    private readonly HashSet<Vec2I> _occupied = new();
    private int _cols = 4;
    private int _rows = 4;
    private int _boundW = 1;
    private int _boundH = 1;
    private bool _editable = true;
    private Texture2D? _texture;
    private Color _cellTint = Colors.Transparent;

    public event Action<int, int>? CellClicked;

    public ShapeGridPreview()
    {
        MouseFilter = MouseFilterEnum.Stop;
        TextureFilter = TextureFilterEnum.Nearest;
        Resized += QueueRedraw;
    }

    public bool Editable
    {
        get => _editable;
        set
        {
            _editable = value;
            MouseFilter = value ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
        }
    }

    public void SetTexture(Texture2D? texture)
    {
        _texture = texture;
        QueueRedraw();
    }

    public void SetCellTint(Color color)
    {
        _cellTint = color;
        QueueRedraw();
    }

    public void SetCanvas(int cols, int rows, IReadOnlyList<Vec2I> occupied)
    {
        _cols = Math.Max(1, cols);
        _rows = Math.Max(1, rows);
        _occupied.Clear();
        int maxX = 0;
        int maxY = 0;
        bool any = false;
        foreach (var p in occupied)
        {
            if (p.X < 0 || p.Y < 0 || p.X >= _cols || p.Y >= _rows) continue;
            _occupied.Add(p);
            any = true;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }
        _boundW = any ? maxX + 1 : _cols;
        _boundH = any ? maxY + 1 : _rows;
        CustomMinimumSize = GridPixelSize();
        QueueRedraw();
    }

    public void SetPoints(IReadOnlyList<Vec2I> points, int padding = 2, int minSize = 4)
    {
        int maxX = 0;
        int maxY = 0;
        foreach (var p in points)
        {
            if (p.X < 0 || p.Y < 0) continue;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }
        int boundW = Math.Max(1, maxX + 1);
        int boundH = Math.Max(1, maxY + 1);
        SetCanvas(Math.Max(minSize, boundW + padding), Math.Max(minSize, boundH + padding), points);
        _boundW = boundW;
        _boundH = boundH;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!_editable) return;
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse)
            return;
        var local = mouse.Position - GridOrigin() - new Vector2(4, 4);
        int x = (int)Math.Floor(local.X / (CellSize + 2));
        int y = (int)Math.Floor(local.Y / (CellSize + 2));
        if ((uint)x >= (uint)_cols || (uint)y >= (uint)_rows) return;
        CellClicked?.Invoke(x, y);
        AcceptEvent();
    }

    public override void _Draw()
    {
        var draw = ResolveDrawTexture(_texture, out var srcBase);
        bool mapTexture = draw != null && srcBase.Size.X > 0 && srcBase.Size.Y > 0;
        for (int y = 0; y < _rows; y++)
        {
            for (int x = 0; x < _cols; x++)
            {
                var rect = CellRect(x, y);
                bool occupied = _occupied.Contains(new Vec2I(x, y));
                var fill = CtisEditorTheme.GridCell;
                if (occupied)
                    fill = _cellTint.A > 0f
                        ? new Color(_cellTint.R, _cellTint.G, _cellTint.B, Math.Max(_cellTint.A, 0.55f))
                        : CtisEditorTheme.GridCellActive;
                DrawRect(rect, fill, true);
                if (!occupied || !mapTexture) continue;
                var src = new Rect2(
                    srcBase.Position.X + x / (float)_boundW * srcBase.Size.X,
                    srcBase.Position.Y + y / (float)_boundH * srcBase.Size.Y,
                    srcBase.Size.X / _boundW,
                    srcBase.Size.Y / _boundH);
                DrawTextureRectRegion(draw!, rect, src);
            }
        }
    }

    private static Texture2D? ResolveDrawTexture(Texture2D? texture, out Rect2 region)
    {
        if (texture is AtlasTexture atlas && atlas.Atlas != null)
        {
            region = atlas.Region;
            if (region.Size.X <= 0f || region.Size.Y <= 0f)
                region = new Rect2(Vector2.Zero, atlas.Atlas.GetSize());
            return atlas.Atlas;
        }
        region = new Rect2(Vector2.Zero, texture?.GetSize() ?? Vector2.Zero);
        return texture;
    }

    private Vector2 GridPixelSize()
        => new(_cols * (CellSize + 2) + 8, _rows * (CellSize + 2) + 8);

    private Vector2 GridOrigin()
    {
        var grid = GridPixelSize();
        return new Vector2(
            MathF.Max(0f, (Size.X - grid.X) * 0.5f),
            MathF.Max(0f, (Size.Y - grid.Y) * 0.5f));
    }

    private Rect2 CellRect(int x, int y)
    {
        var origin = GridOrigin();
        return new Rect2(origin.X + 4 + x * (CellSize + 2), origin.Y + 4 + y * (CellSize + 2), CellSize, CellSize);
    }
}
