using Ctis.Core;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Composition;
using DotPudica.Godot.Views;
using Godot;

namespace Ctis.Presentation;

[DotPudicaView(typeof(TetrisSlotVM), AutoInitialize = false, Pooled = true)]
public partial class TetrisSlotView : Control
{
    private TetrisItemView? _itemView;
    private NinePatchRect _plate = null!;
    private TextureRect _pattern = null!;

    [Inject] private IPointerGridViews _pointerViews = null!;
    [Inject] private IItemDragMediator _mediator = null!;
    [Inject] private ItemViewRegistry _itemViews = null!;

    public TetrisSlotVM? BoundViewModel => ViewModel;

    public override void _Ready() => InitializeView();
    public override void _ExitTree() => RecycleView();

    partial void OnViewReady()
    {
        MouseFilter = MouseFilterEnum.Stop;
        ClipContents = true;
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        SizeFlagsVertical = SizeFlags.ShrinkBegin;
        CustomMinimumSize = new Vector2(CtisSettings.GridTileSizeWidth, CtisSettings.GridTileSizeHeight);
        _plate = EnsurePlate();
        _pattern = BindPattern();
        MoveChild(_plate, 0);
        MoveChild(_pattern, 1);
        MouseEntered += OnMouseEntered;
        QueueRedraw();
    }

    partial void OnViewModelBound()
    {
        if (ViewModel == null) return;
        CustomMinimumSize = GridSize(ViewModel.SlotSize);
        _pattern.Texture = CtisArt.Load(CtisArt.SlotBackground(ViewModel.SlotType)) ?? CtisArt.Load(CtisArt.SlotFallback);
        _pointerViews.RegisterSlot(this);
        if (ViewModel.RelatedTetrisItem != null)
            OnPlace(ViewModel.RelatedTetrisItem);
        else
            SetOccupied(false);
    }

    partial void OnViewDisposing()
    {
        MouseEntered -= OnMouseEntered;
        _pointerViews.UnregisterSlot(this);
        if (_itemView != null)
        {
            CtisRuntime.Recycle(_itemView);
            _itemView = null;
        }
        SetOccupied(false);
    }

    private TextureRect BindPattern()
    {
        var layer = GetNodeOrNull<TextureRect>("SlotPattern") ?? new TextureRect { Name = "SlotPattern" };
        layer.SetAnchorsPreset(LayoutPreset.FullRect);
        layer.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        layer.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        layer.MouseFilter = MouseFilterEnum.Ignore;
        layer.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        if (layer.GetParent() == null)
            AddChild(layer);
        return layer;
    }

    private NinePatchRect EnsurePlate()
    {
        GetNodeOrNull("SlotBg")?.Free();
        if (GetNodeOrNull<NinePatchRect>("SlotPlate") is { } plate)
        {
            plate.AxisStretchHorizontal = NinePatchRect.AxisStretchMode.Stretch;
            plate.AxisStretchVertical = NinePatchRect.AxisStretchMode.Stretch;
            plate.Texture = CtisArt.Load(CtisArt.SlotPlate);
            return plate;
        }
        GetNodeOrNull("SlotPlate")?.Free();
        plate = CtisArt.CreateSlotPlate();
        AddChild(plate);
        return plate;
    }

    private static Vector2 GridSize(Size2 size)
    {
        float tw = CtisSettings.GridTileSizeWidth;
        float th = CtisSettings.GridTileSizeHeight;
        return new Vector2(
            MathF.Max(tw, MathF.Round(size.Width / tw) * tw),
            MathF.Max(th, MathF.Round(size.Height / th) * th));
    }

    private void SetOccupied(bool occupied)
    {
        if (_pattern != null)
            _pattern.Visible = !occupied;
    }

    public override bool _HasPoint(Vector2 point)
    {
        return point.X >= 0f && point.Y >= 0f && point.X < Size.X && point.Y < Size.Y;
    }

    public void BindSlot(TetrisSlotVM vm) => ActivateViewModel(vm);

    private void OnMouseEntered()
    {
        if (ViewModel == null) return;
        _mediator.SyncGhostTargetDroppedSlot(ViewModel);
    }

    [Subscribe(nameof(TetrisSlotVM.PlaceItemViewRequested))]
    private void OnPlace(TetrisItemVM item)
    {
        var existing = _itemViews.FindUnderParent(item.Guid, this);
        if (existing != null && existing == _itemView)
        {
            existing.ApplySlotPlacement();
            existing.MoveToFront();
            SetOccupied(true);
            return;
        }
        OnRemove(item);
        _itemView = existing ?? CtisRuntime.CreateItemView();
        if (_itemView.GetParent() != this)
            AddChild(_itemView);
        _itemView.BindItem(item);
        _itemView.ApplySlotPlacement();
        _itemView.MoveToFront();
        SetOccupied(true);
    }

    [Subscribe(nameof(TetrisSlotVM.RemoveItemViewRequested))]
    private void OnRemove(TetrisItemVM item)
    {
        if (_itemView == null) return;
        CtisRuntime.Recycle(_itemView);
        _itemView = null;
        SetOccupied(false);
    }
}
