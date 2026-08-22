using Ctis.Core;
using Godot;

namespace Ctis.Presentation;

public partial class ItemGridPanelHost : VBoxContainer
{
    private bool _showTitle;
    private Label _title = null!;
    private Control _banner = null!;
    private Control _canvas = null!;
    private readonly List<TetrisGridView> _views = new();
    private Control? _layoutRoot;
    private TetrisItemVM? _item;
    private Func<int, int, int, TetrisGridVM>? _ensure;

    public TetrisItemVM? Item => _item;
    public Vector2 PanelPixelSize => _canvas?.CustomMinimumSize ?? Vector2.Zero;

    public override void _Ready()
    {
        EnsureNodes();
    }

    public ItemGridPanelHost() : this(true)
    {
    }

    public ItemGridPanelHost(bool showTitle)
    {
        _showTitle = showTitle;
        EnsureNodes();
    }

    private void EnsureNodes()
    {
        if (_canvas != null && GodotObject.IsInstanceValid(_canvas)) return;

        Name = "ItemGridPanelHost";
        MouseFilter = MouseFilterEnum.Stop;
        SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        SizeFlagsVertical = SizeFlags.ShrinkBegin;

        _banner = GetNodeOrNull<Control>("Banner") ?? new ColorRect
        {
            Name = "Banner",
            Color = new Color(0.16f, 0.18f, 0.24f, 1f),
            CustomMinimumSize = new Vector2(0, 22),
            MouseFilter = MouseFilterEnum.Ignore
        };
        if (_banner.GetParent() == null)
            AddChild(_banner);
        _banner.Visible = _showTitle;

        _title = GetNodeOrNull<Label>("Banner/Title") ?? new Label
        {
            Name = "Title",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
            AutoTranslateMode = AutoTranslateModeEnum.Always
        };
        _title.AddThemeFontSizeOverride("font_size", 13);
        _title.SetAnchorsPreset(LayoutPreset.FullRect);
        if (_title.GetParent() == null)
            _banner.AddChild(_title);

        _canvas = GetNodeOrNull<Control>("Canvas") ?? new Control
        {
            Name = "Canvas",
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            SizeFlagsVertical = SizeFlags.ShrinkBegin
        };
        if (_canvas.GetParent() == null)
            AddChild(_canvas);
    }

    /// <summary>Binds an item's inner grids from its panel scene.</summary>
    public void BindItem(TetrisItemVM item, Func<int, int, int, TetrisGridVM> ensure)
    {
        Unbind();
        _item = item;
        _ensure = ensure;
        _title.Text = item.ItemName;
        _banner.Visible = _showTitle;
        SpawnFromScene(item.ItemDetails?.GridPanelSceneKey, BindItemGrid);
    }

    /// <summary>Binds persistent pocket/coffer grids from a layout scene.</summary>
    public void BindPersistent(string scenePath, Func<int, int, int, TetrisGridVM> ensure)
    {
        Unbind();
        _ensure = ensure;
        _banner.Visible = false;
        SpawnFromScene(scenePath, BindPersistentGrid);
    }

    private void BindPersistentGrid(int index, int width, int height, TetrisGridView view)
    {
        if (_ensure != null)
            view.BindGrid(_ensure(index, width, height));
    }

    /// <summary>Clears bound grids and recycles views.</summary>
    public void Unbind()
    {
        _item = null;
        _ensure = null;
        foreach (var view in _views)
            CtisRuntime.Recycle(view);
        _views.Clear();
        _layoutRoot = null;
        var children = _canvas.GetChildren();
        for (int i = children.Count - 1; i >= 0; i--)
        {
            var child = children[i];
            _canvas.RemoveChild(child);
            child.QueueFree();
        }
        _canvas.CustomMinimumSize = Vector2.Zero;
        CustomMinimumSize = Vector2.Zero;
    }

    private void BindItemGrid(int index, int width, int height, TetrisGridView view)
    {
        if (_ensure == null) return;
        view.BindGrid(_ensure(index, width, height));
    }

    private bool SpawnFromScene(string? path, Action<int, int, int, TetrisGridView> bind)
    {
        var layout = GridPanelLayout.Instantiate(path ?? "");
        if (layout == null) return false;
        _layoutRoot = layout;
        layout.MouseFilter = MouseFilterEnum.Ignore;
        _canvas.AddChild(layout);
        layout.Position = Vector2.Zero;

        var placeholders = GridPanelLayout.Collect(layout);
        if (placeholders.Count == 0)
        {
            layout.QueueFree();
            _layoutRoot = null;
            return false;
        }

        var bounds = Vector2.Zero;
        for (int i = 0; i < placeholders.Count; i++)
        {
            var spec = placeholders[i];
            if (spec.Node != null)
            {
                spec.Node.Visible = false;
                spec.Node.MouseFilter = MouseFilterEnum.Ignore;
            }
            SpawnView(layout, spec.Position, spec.Width, spec.Height, i, bind);
            bounds = MaxBounds(bounds, spec.Position, spec.Width, spec.Height);
        }

        var layoutSize = layout.CustomMinimumSize;
        if (layoutSize.X < bounds.X || layoutSize.Y < bounds.Y)
            layout.CustomMinimumSize = bounds;
        _canvas.CustomMinimumSize = layout.CustomMinimumSize.Max(bounds);
        CustomMinimumSize = new Vector2(
            _canvas.CustomMinimumSize.X,
            _canvas.CustomMinimumSize.Y + (_banner.Visible ? 26f : 0f));
        return true;
    }

    private void SpawnView(
        Control parent,
        Vector2 position,
        int width,
        int height,
        int index,
        Action<int, int, int, TetrisGridView> bind)
    {
        var view = CtisRuntime.CreateGridView();
        view.Name = "Grid_" + index;
        view.AutoFitTiles = false;
        parent.AddChild(view);
        view.Position = position;
        bind(index, width, height, view);
        _views.Add(view);
    }

    private static Vector2 MaxBounds(Vector2 current, Vector2 position, int width, int height)
    {
        var size = position + new Vector2(
            width * CtisSettings.GridTileSizeWidth,
            height * CtisSettings.GridTileSizeHeight);
        return new Vector2(MathF.Max(current.X, size.X), MathF.Max(current.Y, size.Y));
    }
}
