using TetrisCoordLib.Core.Math;

namespace Ctis.Core;

public sealed class TetrisItemMediator : IItemDragMediator
{
    private TetrisItemGhostVM? _ghost;
    private Dir _cachedDir;
    private bool _cachedRotated;
    private bool _cachedFlipH;
    private bool _cachedFlipV;
    private readonly List<Vec2I> _cachedShapePos = new(16);
    private int _cachedWidth;
    private int _cachedHeight;
    private TetrisItemVM? _cachedOriginItem;
    private TetrisItemContainerVM? _cachedOrigin;
    private Dir _cachedItemDir;
    private bool _cachedItemRotated;
    private bool _cachedItemFlipH;
    private bool _cachedItemFlipV;
    private readonly List<Vec2I> _cachedItemShapePos = new(16);
    private int _cachedItemWidth;
    private int _cachedItemHeight;

    /// <summary>Binds the mediator to the active drag ghost.</summary>
    public void Attach(TetrisItemGhostVM ghost) => _ghost = ghost;

    public bool IsDragging => _ghost is { OnDragging: true };

    /// <summary>Returns the current hover highlight when a drag is over a grid.</summary>
    public bool TryGetDropPreview(out InventoryDropPreview preview)
    {
        preview = _ghost?.DropPreview!;
        return _ghost is { OnDragging: true, DropPreview: not null };
    }

    /// <summary>Caches the ghost's current facing and occupancy.</summary>
    public void CacheGhostState(TetrisItemGhostVM ghost)
    {
        _cachedDir = ghost.Direction;
        _cachedRotated = ghost.Rotated;
        _cachedFlipH = ghost.FlipH;
        _cachedFlipV = ghost.FlipV;
        _cachedShapePos.Clear();
        if (ghost.TetrisCoordinateSet != null)
            _cachedShapePos.AddRange(ghost.TetrisCoordinateSet);
        _cachedWidth = ghost.Width;
        _cachedHeight = ghost.Height;
    }

    /// <summary>Caches the item's origin container and occupancy before lift.</summary>
    public void CacheItemState(TetrisItemVM item)
    {
        _cachedOriginItem = item;
        _cachedOrigin = item.CurrentTetrisContainer;
        _cachedItemDir = item.Direction;
        _cachedItemRotated = item.Rotated;
        _cachedItemFlipH = item.FlipH;
        _cachedItemFlipV = item.FlipV;
        _cachedItemShapePos.Clear();
        if (item.TetrisCoordinateSet != null)
            _cachedItemShapePos.AddRange(item.TetrisCoordinateSet);
        _cachedItemWidth = item.Width;
        _cachedItemHeight = item.Height;
    }

    /// <summary>Restores the ghost to the cached origin item state.</summary>
    public void ApplyStateToGhost(TetrisItemGhostVM ghost)
    {
        ghost.RestoreCachedShape(_cachedItemDir, _cachedItemRotated, _cachedItemFlipH, _cachedItemFlipV, _cachedItemShapePos, _cachedItemWidth, _cachedItemHeight);
        ghost.SelectedItem = _cachedOriginItem;
        ghost.OriginContainerOnDrag = _cachedOrigin;
        ghost.UpdateSizeForContainer(_cachedOrigin);
    }

    /// <summary>Applies the cached ghost facing and occupancy onto the item.</summary>
    public void ApplyStateToItem(TetrisItemVM item)
    {
        item.RestoreCachedShape(_cachedDir, _cachedRotated, _cachedFlipH, _cachedFlipV, _cachedShapePos, _cachedWidth, _cachedHeight);
    }

    /// <summary>Sets the drop target to a grid while dragging.</summary>
    public void SyncGhostTargetDroppedGrid(TetrisGridVM target)
    {
        if (_ghost == null || !_ghost.OnDragging) return;
        _ghost.TargetContainerOnDrop = target;
        _ghost.UpdateSizeForContainer(target);
    }

    /// <summary>Sets the drop target to a slot while dragging.</summary>
    public void SyncGhostTargetDroppedSlot(TetrisSlotVM target)
    {
        if (_ghost == null || !_ghost.OnDragging) return;
        _ghost.TargetContainerOnDrop = target;
        _ghost.UpdateSizeForContainer(target);
    }

    /// <summary>Initializes the ghost from an item if a drag is not already running.</summary>
    public bool TrySyncGhostFromItem(TetrisItemVM item, TetrisItemGhostVM.GhostInitData initData)
    {
        if (_ghost == null || _ghost.OnDragging) return false;
        _ghost.InitializeFromItem(item, initData);
        return true;
    }

    /// <summary>Syncs the ghost from the item and starts a drag.</summary>
    public bool TryBeginDragFromItem(TetrisItemVM item, TetrisItemGhostVM.GhostInitData initData)
    {
        if (!TrySyncGhostFromItem(item, initData) || _ghost == null) return false;
        _ghost.RequestBeginDrag();
        return _ghost.OnDragging;
    }
}
