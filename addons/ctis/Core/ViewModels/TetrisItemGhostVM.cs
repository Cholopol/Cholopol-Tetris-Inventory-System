using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.Interactivity;
using TetrisCoordLib.Core.Math;

namespace Ctis.Core;

public partial class TetrisItemGhostVM : DotPudica.Core.ViewModels.ViewModelBase
{
    public readonly record struct GhostInitData(float WorldX, float WorldY, float PivotX, float PivotY, Size2 Size, Dir Direction);

    private readonly IInventoryService _inventory;
    private readonly IItemDragMediator _mediator;
    private readonly IPointerGridSession _pointer;
    private readonly PlacementConfig _placement;

    public TetrisItemGhostVM(
        IInventoryService inventory,
        IItemDragMediator mediator,
        IPointerGridSession pointer,
        PlacementConfig? placement = null)
    {
        _inventory = inventory;
        _mediator = mediator;
        _pointer = pointer;
        _placement = placement ?? new PlacementConfig();
        _mediator.Attach(this);
        DraggingGhostColor = Rgba.White.WithAlpha(0f);
    }

    public readonly InteractionRequest<GhostInitData> InitializeFromItemRequest = new();
    public readonly InteractionRequest<Dir> OnRotateRequest = new();

    [ObservableProperty] private int _onGridPositionX;
    [ObservableProperty] private int _onGridPositionY;
    [ObservableProperty] private Size2 _size;
    [ObservableProperty] private Dir _direction = Dir.Down;
    [ObservableProperty] private bool _rotated;
    [ObservableProperty] private bool _flipH;
    [ObservableProperty] private bool _flipV;
    [ObservableProperty] private List<Vec2I> _tetrisCoordinateSet = new();
    [ObservableProperty] private string _iconKey = "";
    [ObservableProperty] private Rgba _draggingGhostColor;
    [ObservableProperty] private TetrisItemVM? _selectedItem;
    [ObservableProperty] private TetrisItemContainerVM? _originContainerOnDrag;
    [ObservableProperty] private TetrisItemContainerVM? _targetContainerOnDrop;
    [ObservableProperty] private ItemDetails? _itemDetails;
    [ObservableProperty] private bool _onDragging;
    [ObservableProperty] private int _width;
    [ObservableProperty] private int _height;

    public InventoryDropPreview? DropPreview { get; private set; }
    private readonly InventoryDropPreview _dropPreview = new();
    private readonly List<InventoryDropPreviewCell> _dropPreviewCells = new();
    private bool _suppressRotationUpdate;

    private TetrisItemContainerVM? _lastTarget;
    private int _lastPosX = int.MinValue;
    private int _lastPosY = int.MinValue;
    private TetrisItemVM? _lastHovered;
    private Dir _lastDir = (Dir)(-1);
    private bool _lastFlipH;
    private bool _lastFlipV;

    /// <summary>Starts a drag if one is not already running.</summary>
    public void RequestBeginDrag()
    {
        if (OnDragging || SelectedItem == null) return;
        CtisTrace.Mark("Ghost.BeginDrag");
        OnDragging = true;
        ResetDirtyCache();
        OnBeginDrag();
    }

    /// <summary>Commits or cancels the current drag.</summary>
    public void RequestEndDrag()
    {
        if (!OnDragging) return;
        CtisTrace.Mark("Ghost.EndDrag");
        OnDragging = false;
        DropPreview = null;
        ResetDirtyCache();
        OnEndDrag();
    }

    private void ResetDirtyCache()
    {
        _lastTarget = null;
        _lastPosX = int.MinValue;
        _lastPosY = int.MinValue;
        _lastHovered = null;
        _lastDir = (Dir)(-1);
        _lastFlipH = false;
        _lastFlipV = false;
    }

    partial void OnDirectionChanged(Dir value)
    {
        if (!_suppressRotationUpdate) UpdateRotation();
    }

    partial void OnRotatedChanged(bool value)
    {
        if (!_suppressRotationUpdate) UpdateRotation();
    }

    partial void OnFlipHChanged(bool value)
    {
        if (!_suppressRotationUpdate) UpdateRotation();
    }

    partial void OnFlipVChanged(bool value)
    {
        if (!_suppressRotationUpdate) UpdateRotation();
    }

    partial void OnItemDetailsChanged(ItemDetails? value)
    {
        IconKey = value?.IconKey ?? "";
        UpdateRotation();
    }

    /// <summary>Updates the ghost origin from the already-refreshed pointer grid.</summary>
    public void TickPointer()
    {
        if (!OnDragging || SelectedItem == null)
        {
            DropPreview = null;
            ResetDirtyCache();
            return;
        }

        var hovered = _pointer.HoveredItem;
        var target = ResolvePointerTarget();
        var grid = target as TetrisGridVM;
        var pos = grid != null ? _pointer.GetGhostTileGridOrigin(Width, Height) : Vec2I.Zero;
        OnGridPositionX = pos.X;
        OnGridPositionY = pos.Y;

        if (ReferenceEquals(target, _lastTarget)
            && pos.X == _lastPosX && pos.Y == _lastPosY
            && ReferenceEquals(hovered, _lastHovered)
            && Direction == _lastDir
            && FlipH == _lastFlipH && FlipV == _lastFlipV)
        {
            return;
        }

        _lastTarget = target;
        _lastPosX = pos.X;
        _lastPosY = pos.Y;
        _lastHovered = hovered;
        _lastDir = Direction;
        _lastFlipH = FlipH;
        _lastFlipV = FlipV;

        var drop = _inventory.EvaluateDrop(
            InventoryPlacementContext.ForGhost(SelectedItem, this, target, pos, hovered),
            this);

        if (InventoryLogic.IsInnerInsertHover(SelectedItem, hovered))
        {
            InventoryLogic.FillInnerInsertPreview(
                _dropPreview,
                _dropPreviewCells,
                hovered!.CurrentTetrisContainer as TetrisGridVM,
                hovered,
                drop,
                _placement);
            DropPreview = _dropPreview;
            var palette = _placement.ResolveHighlightPalette();
            DraggingGhostColor = drop.Kind == InventoryDropKind.InsertIntoInner
                ? palette.ValidEmpty.WithAlpha(0.8f)
                : palette.Invalid.WithAlpha(0.8f);
            return;
        }

        DraggingGhostColor = Rgba.White.WithAlpha(0.8f);
        if (grid == null)
        {
            DropPreview = null;
            return;
        }

        InventoryLogic.FillDropPreview(
            _dropPreview,
            _dropPreviewCells,
            grid,
            SelectedItem,
            TetrisCoordinateSet,
            pos,
            drop,
            _placement);
        DropPreview = _dropPreview;
    }

    /// <summary>Lifts the selected item and shows the translucent ghost.</summary>
    public void OnBeginDrag()
    {
        SelectedItem?.BeginDragDim(0.2f);
        if (SelectedItem != null)
            SelectedItem.IsRaycastTargetEnabled = false;
        DraggingGhostColor = Rgba.White.WithAlpha(0.8f);
        if (string.IsNullOrEmpty(IconKey) && SelectedItem?.ItemDetails != null)
            IconKey = SelectedItem.ItemDetails.IconKey;
        if (SelectedItem != null)
        {
            _mediator.CacheItemState(SelectedItem);
            OriginContainerOnDrag = SelectedItem.CurrentTetrisContainer;
        }
        TargetContainerOnDrop = OriginContainerOnDrag;
        UpdateSizeForContainer(TargetContainerOnDrop);
        _mediator.CacheGhostState(this);
        if (SelectedItem != null)
            _inventory.Lift(SelectedItem);
    }

    /// <summary>Drops onto the current target or returns the item to its origin.</summary>
    public void OnEndDrag()
    {
        SelectedItem?.EndDragDim();
        if (SelectedItem != null)
            SelectedItem.IsRaycastTargetEnabled = true;
        DraggingGhostColor = Rgba.White.WithAlpha(0f);
        IconKey = "";
        if (_pointer.SelectedSlot != null)
            TargetContainerOnDrop = _pointer.SelectedSlot;
        else if (TargetContainerOnDrop is TetrisGridVM && _pointer.SelectedGrid == null)
            TargetContainerOnDrop = null;
        CommitDrop(SelectedItem);
    }

    /// <summary>Rotates the ghost one quarter-turn while dragging.</summary>
    public void Rotate()
    {
        if (!OnDragging || ItemDetails == null) return;
        CtisTrace.Mark("Ghost.Rotate");
        Direction = DirUtil.Next(Direction);
        Rotated = DirUtil.IsRotated(Direction);
        _mediator.CacheGhostState(this);
        UpdateSizeForContainer(TargetContainerOnDrop ?? OriginContainerOnDrag);
        OnRotateRequest.Raise(Direction);
    }

    /// <summary>Sizes the ghost from the target grid tiles or slot rectangle.</summary>
    public void UpdateSizeForContainer(TetrisItemContainerVM? container)
    {
        if (ItemDetails == null) return;
        if (container is TetrisGridVM grid)
        {
            Size = new Size2(Width * grid.LocalGridTileSizeWidth, Height * grid.LocalGridTileSizeHeight);
            return;
        }
        if (container is TetrisSlotVM slot)
        {
            Size = slot.SlotSize;
            return;
        }
        Size = new Size2(Width * CtisSettings.GridTileSizeWidth, Height * CtisSettings.GridTileSizeHeight);
    }

    /// <summary>Copies occupancy from an item and raises the view init request.</summary>
    public void InitializeFromItem(TetrisItemVM item, GhostInitData initData)
    {
        if (OnDragging) return;
        CopyStateFromItem(item);
        Size = initData.Size;
        InitializeFromItemRequest.Raise(initData);
    }

    private void CopyStateFromItem(TetrisItemVM item)
    {
        SelectedItem = item;
        OriginContainerOnDrag = item.CurrentTetrisContainer;
        ItemDetails = item.ItemDetails;
        Rotated = item.Rotated;
        FlipH = item.FlipH;
        FlipV = item.FlipV;
        Direction = item.Direction;
        OnGridPositionX = item.LocalGridCoordinate.X;
        OnGridPositionY = item.LocalGridCoordinate.Y;
        if (!ReferenceEquals(TetrisCoordinateSet, item.TetrisCoordinateSet))
        {
            TetrisCoordinateSet.Clear();
            if (item.TetrisCoordinateSet != null)
                TetrisCoordinateSet.AddRange(item.TetrisCoordinateSet);
        }
        OnPropertyChanged(nameof(TetrisCoordinateSet));
        Width = item.Width;
        Height = item.Height;
        UpdateSizeForContainer(item.CurrentTetrisContainer);
    }

    /// <summary>Restores a cached facing and occupancy without recomputing from catalog data.</summary>
    public void RestoreCachedShape(Dir dir, bool rotated, bool flipH, bool flipV, IReadOnlyList<Vec2I> cells, int width, int height)
    {
        _suppressRotationUpdate = true;
        Direction = dir;
        Rotated = rotated;
        FlipH = flipH;
        FlipV = flipV;
        if (!ReferenceEquals(TetrisCoordinateSet, cells))
        {
            TetrisCoordinateSet.Clear();
            if (cells != null)
                TetrisCoordinateSet.AddRange(cells);
        }
        OnPropertyChanged(nameof(TetrisCoordinateSet));
        Width = width;
        Height = height;
        _suppressRotationUpdate = false;
    }

    private void UpdateRotation()
    {
        if (ItemDetails == null) return;
        var rotated = ItemShape.Resolve(ItemDetails.Occupancy, SelectedItem?.OccupancyPatches, Direction, SelectedItem?.FlipH ?? FlipH, SelectedItem?.FlipV ?? FlipV);
        TetrisCoordinateSet.Clear();
        TetrisCoordinateSet.AddRange(rotated.Cells);
        OnPropertyChanged(nameof(TetrisCoordinateSet));
        Width = rotated.Width;
        Height = rotated.Height;
    }

    private void CommitDrop(TetrisItemVM? selected)
    {
        if (selected == null)
            return;

        using var _ = CtisTrace.Scope("Ghost.CommitDrop");
        var hovered = _pointer.HoveredItem;
        var target = TargetContainerOnDrop;
        if (target == null && !InventoryLogic.IsInnerInsertHover(selected, hovered))
        {
            ReturnToOrigin(selected);
            return;
        }

        var origin = new Vec2I(OnGridPositionX, OnGridPositionY);
        var drop = _inventory.EvaluateDrop(
            InventoryPlacementContext.ForGhost(selected, this, target, origin, hovered),
            this);
        switch (drop.Kind)
        {
            case InventoryDropKind.InsertIntoInner:
                _mediator.ApplyStateToItem(selected);
                if (_inventory.TryPlaceInnerInsert(selected, drop))
                    DestroyOriginView(drop.InnerGrid);
                else
                    ReturnToOrigin(selected);
                TargetContainerOnDrop = null;
                return;
            case InventoryDropKind.Vacant:
                if (target is TetrisGridVM vacantGrid)
                    PlaceOnGrid(selected, vacantGrid, origin);
                else if (target is TetrisSlotVM vacantSlot)
                {
                    _mediator.ApplyStateToItem(selected);
                    if (_inventory.PlaceOnSlot(selected, vacantSlot))
                        DestroyOriginView(vacantSlot);
                    else
                        ReturnToOrigin(selected);
                }
                else
                    ReturnToOrigin(selected);
                return;
            case InventoryDropKind.Stack:
                StackOnto(selected, drop.Overlap!);
                return;
            case InventoryDropKind.Exchange:
                if (target is TetrisGridVM exchangeGrid && _inventory.TryQuickExchange(exchangeGrid, this, origin))
                    DestroyOriginView(exchangeGrid);
                else
                    ReturnToOrigin(selected);
                TargetContainerOnDrop = null;
                return;
            default:
                ReturnToOrigin(selected);
                return;
        }
    }

    private TetrisItemContainerVM? ResolvePointerTarget()
    {
        if (_pointer.PreferSlotTarget && _pointer.SelectedSlot != null)
            return _pointer.SelectedSlot;
        return _pointer.SelectedGrid ?? (TetrisItemContainerVM?)_pointer.SelectedSlot;
    }

    private void PlaceOnGrid(TetrisItemVM selected, TetrisGridVM grid, Vec2I origin)
    {
        _mediator.ApplyStateToItem(selected);
        if (_inventory.PlaceOnGrid(selected, grid, origin, null))
            DestroyOriginView(TargetContainerOnDrop);
        else
            ReturnToOrigin(selected);
    }

    private void StackOnto(TetrisItemVM selected, TetrisItemVM overlap)
    {
        if (!_inventory.TryStack(selected, overlap))
        {
            ReturnToOrigin(selected);
            CopyStateFromItem(overlap);
            TargetContainerOnDrop = null;
            return;
        }
        if (selected.CurrentStack <= 0)
        {
            CopyStateFromItem(overlap);
            TargetContainerOnDrop = null;
            return;
        }
        ReturnToOrigin(selected);
        CopyStateFromItem(overlap);
        TargetContainerOnDrop = null;
    }

    private void DestroyOriginView(TetrisItemContainerVM? target)
    {
        if (SelectedItem == null || target == OriginContainerOnDrag) return;
        if (OriginContainerOnDrag is TetrisGridVM grid)
            grid.RequestRemoveItemView(SelectedItem);
        else if (OriginContainerOnDrag is TetrisSlotVM slot)
            slot.RequestRemoveItemView(SelectedItem);
    }

    private void ReturnToOrigin(TetrisItemVM selected)
    {
        _mediator.ApplyStateToGhost(this);
        if (selected.CurrentTetrisContainer is TetrisGridVM grid)
            _inventory.PlaceOnGrid(selected, grid, selected.LocalGridCoordinate, null);
        else if (selected.CurrentTetrisContainer is TetrisSlotVM slot)
            _inventory.PlaceOnSlot(selected, slot);
    }
}
