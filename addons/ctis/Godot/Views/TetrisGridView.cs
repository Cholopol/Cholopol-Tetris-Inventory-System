using System.ComponentModel;
using Ctis.Core;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Composition;
using DotPudica.Godot.Views;
using Godot;
using TetrisCoordLib.Core.Math;

namespace Ctis.Presentation;

[DotPudicaView(typeof(TetrisGridVM), AutoInitialize = false, Pooled = true)]
public partial class TetrisGridView : Control
{
    private readonly Dictionary<string, TetrisItemView> _itemViews = new();
    private HighlightOverlay _highlight = null!;
    private string _boundGuid = "";
    private Texture2D? _cellTexture;

    [Inject] private IPointerGridViews _pointerViews = null!;
    [Inject] private GridViewRegistry _gridViews = null!;
    [Inject] private IItemDragMediator _mediator = null!;
    [Inject] private ItemViewRegistry _itemViewRegistry = null!;

    [Export] public int EditorWidth { get; set; } = 10;
    [Export] public int EditorHeight { get; set; } = 8;
    [Export] public string EditorGridGuid { get; set; } = "depository";
    [Export] public bool AutoFitTiles { get; set; } = true;
    [Export] public bool FitToWidthOnly { get; set; }

    public TetrisGridVM? BoundViewModel => ViewModel;
    public GodotWindow? OwningWindow { get; private set; }

    public Vector2 GridPixelSize => ViewModel == null
        ? Size
        : new Vector2(
            ViewModel.GridSizeWidth * ViewModel.LocalGridTileSizeWidth,
            ViewModel.GridSizeHeight * ViewModel.LocalGridTileSizeHeight);

    public Rect2 GetGridGlobalRect() => new(GlobalPosition, GridPixelSize);

    public override void _Ready() => InitializeView();
    public override void _ExitTree() => RecycleView();

    partial void OnViewReady()
    {
        OwningWindow = UiPick.FindAncestor<GodotWindow>(this);
        MouseFilter = MouseFilterEnum.Stop;
        ClipContents = true;
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        ApplySizeFlags();
        _cellTexture = CtisArt.Load(CtisArt.SlotFallback);
        _highlight = GetNodeOrNull<HighlightOverlay>("Highlight");
        if (_highlight == null)
            _highlight = new HighlightOverlay { Name = "Highlight" };
        if (_highlight.GetParent() == null)
            AddChild(_highlight);
        BringHighlightToFront();
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        SetProcess(true);
        QueueRedraw();
    }

    partial void OnViewModelBound()
    {
        if (ViewModel == null) return;
        if (OwningWindow == null)
            OwningWindow = UiPick.FindAncestor<GodotWindow>(this);
        if (string.IsNullOrEmpty(ViewModel.GridGuid) && !string.IsNullOrEmpty(EditorGridGuid))
            ViewModel.GridGuid = EditorGridGuid;
        BringHighlightToFront();

        _pointerViews.RegisterGrid(this);
        if (!string.IsNullOrEmpty(ViewModel.GridGuid))
        {
            _boundGuid = ViewModel.GridGuid;
            _gridViews.RegisterView(_boundGuid, this);
        }

        ViewModel.PropertyChanged += OnGridPropertyChanged;
        foreach (var item in ViewModel.OwnerItemsDic.Values)
            OnPlaceItem(item, item.LocalGridCoordinate.X, item.LocalGridCoordinate.Y);
        ApplySizeFlags();
        if (AutoFitTiles)
        {
            CustomMinimumSize = new Vector2(ViewModel.GridSizeWidth * 24f, ViewModel.GridSizeHeight * 24f);
            CallDeferred(nameof(FitTiles));
        }
        else
        {
            ApplyFixedPixelSize();
        }
        QueueRedraw();
    }

    partial void OnViewDisposing()
    {
        OwningWindow = null;
        MouseEntered -= OnMouseEntered;
        MouseExited -= OnMouseExited;
        _pointerViews.UnregisterGrid(this);
        if (!string.IsNullOrEmpty(_boundGuid))
            _gridViews.UnregisterView(_boundGuid, this);
        _boundGuid = "";
        if (ViewModel != null)
            ViewModel.PropertyChanged -= OnGridPropertyChanged;
        foreach (var view in _itemViews.Values)
            CtisRuntime.Recycle(view);
        _itemViews.Clear();
        _highlight?.Release();
    }

    public void BindGrid(TetrisGridVM vm) => ActivateViewModel(vm);

    public override void _Notification(int what)
    {
        if (what == NotificationResized && AutoFitTiles)
            CallDeferred(nameof(FitTiles));
        base._Notification(what);
    }

    private void OnGridPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (ViewModel == null) return;
        if (e.PropertyName is not (
            nameof(TetrisGridVM.GridSizeWidth)
            or nameof(TetrisGridVM.GridSizeHeight)
            or nameof(TetrisGridVM.LocalGridTileSizeWidth)
            or nameof(TetrisGridVM.LocalGridTileSizeHeight)
            or nameof(TetrisGridVM.Size)))
            return;
        if (AutoFitTiles)
            CallDeferred(nameof(FitTiles));
        else
            ApplyFixedPixelSize();
    }

    public override bool _HasPoint(Vector2 point)
    {
        var size = GridPixelSize;
        return point.X >= 0 && point.Y >= 0 && point.X < size.X && point.Y < size.Y;
    }

    public Vec2I ScreenToCell(Vector2 screen)
    {
        if (ViewModel == null) return Vec2I.Zero;
        return ControlCoordSystem.ScreenToCell(this, screen, ViewModel.LocalGridTileSizeWidth, ViewModel.LocalGridTileSizeHeight);
    }

    public Vec2I CellUnderMouse()
    {
        if (ViewModel == null) return Vec2I.Zero;
        return ControlCoordSystem.LocalToCell(
            ControlCoordSystem.CanvasMouseToLocal(this),
            ViewModel.LocalGridTileSizeWidth,
            ViewModel.LocalGridTileSizeHeight);
    }

    public bool ContainsMouse()
        => ControlCoordSystem.ContainsMouse(this, GridPixelSize);

    public override void _Process(double delta)
    {
        if (ViewModel == null) return;
        if (!_mediator.IsDragging || !_mediator.TryGetDropPreview(out var preview) || preview.Grid != ViewModel)
        {
            _highlight.Clear();
            return;
        }
        var size = GridPixelSize;
        if (_highlight.Size != size)
            _highlight.Size = size;
        _highlight.Show(preview, ViewModel.LocalGridTileSizeWidth, ViewModel.LocalGridTileSizeHeight);
    }

    public override void _Draw()
    {
        if (ViewModel == null || _cellTexture == null) return;
        using var _ = CtisTrace.Scope("GridView.Draw");
        int gw = ViewModel.GridSizeWidth;
        int gh = ViewModel.GridSizeHeight;
        float tw = ViewModel.LocalGridTileSizeWidth;
        float th = ViewModel.LocalGridTileSizeHeight;
        for (int y = 0; y < gh; y++)
        {
            for (int x = 0; x < gw; x++)
                DrawTextureRect(_cellTexture, new Rect2(x * tw, y * th, tw, th), false);
        }
    }

    private void ApplySizeFlags()
    {
        if (AutoFitTiles)
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SizeFlagsVertical = FitToWidthOnly ? SizeFlags.ShrinkBegin : SizeFlags.ExpandFill;
        }
        else
        {
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            SizeFlagsVertical = SizeFlags.ShrinkBegin;
        }
    }

    private void FitTiles()
    {
        if (!AutoFitTiles || ViewModel == null || !IsInsideTree()) return;
        using var _ = CtisTrace.Scope("GridView.FitTiles");
        int gw = ViewModel.GridSizeWidth;
        int gh = ViewModel.GridSizeHeight;
        if (gw <= 0 || gh <= 0) return;
        if (Size.X < 8) return;
        if (!FitToWidthOnly && Size.Y < 8) return;
        float tile = FitToWidthOnly
            ? MathF.Floor(Size.X / gw)
            : MathF.Floor(MathF.Min(Size.X / gw, Size.Y / gh));
        tile = Math.Clamp(tile, 24f, FitToWidthOnly ? 48f : 128f);
        if (Math.Abs(tile - ViewModel.LocalGridTileSizeWidth) >= 0.5f
            || Math.Abs(tile - ViewModel.LocalGridTileSizeHeight) >= 0.5f)
            ViewModel.SetLocalTileSize(tile, tile);
        if (FitToWidthOnly)
            CustomMinimumSize = new Vector2(gw * 24f, tile * gh);
        if (_highlight != null)
            _highlight.Size = GridPixelSize;
        LayoutItems();
        QueueRedraw();
    }

    private void ApplyFixedPixelSize()
    {
        if (ViewModel == null) return;
        var size = GridPixelSize;
        CustomMinimumSize = size;
        Size = size;
        if (_highlight != null)
            _highlight.Size = size;
        LayoutItems();
        QueueRedraw();
    }

    private void LayoutItems()
    {
        if (ViewModel == null) return;
        float tw = ViewModel.LocalGridTileSizeWidth;
        float th = ViewModel.LocalGridTileSizeHeight;
        foreach (var view in _itemViews.Values)
        {
            var item = view.BoundViewModel;
            if (item == null) continue;
            view.ApplyGridPlacement(
                item.LocalGridCoordinate.X,
                item.LocalGridCoordinate.Y,
                tw,
                th);
        }
        BringHighlightToFront();
    }

    private void BringHighlightToFront()
    {
        if (_highlight == null) return;
        _highlight.MouseFilter = MouseFilterEnum.Ignore;
        _highlight.ZIndex = 0;
        _highlight.ZAsRelative = true;
        if (_highlight.GetParent() == this)
            MoveChild(_highlight, GetChildCount() - 1);
    }

    private void OnMouseEntered()
    {
        _pointerViews.SetHoveredView(this);
        if (ViewModel != null)
            _mediator.SyncGhostTargetDroppedGrid(ViewModel);
    }

    private void OnMouseExited()
    {
        if (_mediator.IsDragging) return;
        if (_pointerViews.SelectedGrid == ViewModel)
            _pointerViews.SetHoveredView(null);
    }

    [Subscribe(nameof(TetrisGridVM.PlaceItemViewRequested))]
    private void OnPlaceItem(TetrisItemVM item, int x, int y)
    {
        CtisTrace.Mark("GridView.PlaceItem");
        var view = _itemViewRegistry.FindUnderParent(item.Guid, this);
        if (view == null && !_itemViews.TryGetValue(item.Guid, out view))
        {
            view = CtisRuntime.CreateItemView();
            if (view.GetParent() != this)
                AddChild(view);
            view.BindItem(item);
        }
        else
        {
            if (view.GetParent() != this)
                AddChild(view);
            if (view.BoundViewModel != item)
                view.BindItem(item);
            else
                view.RefreshVisual();
        }

        _itemViews[item.Guid] = view;
        view.ApplyGridPlacement(x, y, ViewModel!.LocalGridTileSizeWidth, ViewModel.LocalGridTileSizeHeight);
        BringHighlightToFront();
    }

    [Subscribe(nameof(TetrisGridVM.RemoveItemViewRequested))]
    private void OnRemoveItem(TetrisItemVM item)
    {
        CtisTrace.Mark("GridView.RemoveItem");
        if (!_itemViews.Remove(item.Guid, out var view))
        {
            view = _itemViewRegistry.FindUnderParent(item.Guid, this);
            if (view == null) return;
        }
        CtisRuntime.Recycle(view);
    }
}
