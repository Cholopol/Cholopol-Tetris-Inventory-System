using Godot;

namespace Ctis.Presentation.Editor;

internal sealed partial class IconSheetPreview : Control
{
    private Texture2D? _texture;
    private int _cols = 1;
    private int _rows = 1;
    private int _cellX;
    private int _cellY;

    public event Action<int, int>? CellPicked;

    public IconSheetPreview()
    {
        MouseFilter = MouseFilterEnum.Stop;
        TextureFilter = TextureFilterEnum.Nearest;
        CustomMinimumSize = new Vector2(80, 80);
        Resized += QueueRedraw;
    }

    public void SetSheet(Texture2D? texture, int cols, int rows, int cellX, int cellY)
    {
        _texture = texture;
        _cols = Math.Max(1, cols);
        _rows = Math.Max(1, rows);
        _cellX = Math.Clamp(cellX, 0, _cols - 1);
        _cellY = Math.Clamp(cellY, 0, _rows - 1);
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (_cols <= 1 && _rows <= 1) return;
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse)
            return;
        var fitted = FittedRect();
        if (!fitted.HasArea() || !fitted.HasPoint(mouse.Position)) return;
        var local = mouse.Position - fitted.Position;
        int x = (int)Math.Floor(local.X / fitted.Size.X * _cols);
        int y = (int)Math.Floor(local.Y / fitted.Size.Y * _rows);
        x = Math.Clamp(x, 0, _cols - 1);
        y = Math.Clamp(y, 0, _rows - 1);
        CellPicked?.Invoke(x, y);
        AcceptEvent();
    }

    public override void _Draw()
    {
        var fitted = FittedRect();
        if (_texture != null && fitted.HasArea())
            DrawTextureRect(_texture, fitted, false);
        if (_cols <= 1 && _rows <= 1) return;
        float cw = fitted.Size.X / _cols;
        float ch = fitted.Size.Y / _rows;
        for (int x = 1; x < _cols; x++)
        {
            float px = fitted.Position.X + x * cw;
            DrawLine(new Vector2(px, fitted.Position.Y), new Vector2(px, fitted.End.Y), CtisEditorTheme.BorderStrong);
        }
        for (int y = 1; y < _rows; y++)
        {
            float py = fitted.Position.Y + y * ch;
            DrawLine(new Vector2(fitted.Position.X, py), new Vector2(fitted.End.X, py), CtisEditorTheme.BorderStrong);
        }
        var selected = new Rect2(
            fitted.Position.X + _cellX * cw,
            fitted.Position.Y + _cellY * ch,
            cw,
            ch);
        DrawRect(selected, CtisEditorTheme.Accent, false, 2);
    }

    private Rect2 FittedRect()
    {
        var tex = _texture?.GetSize() ?? Vector2.Zero;
        if (tex.X <= 0f || tex.Y <= 0f || Size.X <= 0f || Size.Y <= 0f)
            return new Rect2(Vector2.Zero, Size);
        float scale = Math.Min(Size.X / tex.X, Size.Y / tex.Y);
        var size = tex * scale;
        return new Rect2((Size - size) * 0.5f, size);
    }
}
