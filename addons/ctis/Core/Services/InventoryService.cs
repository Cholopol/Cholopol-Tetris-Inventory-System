using TetrisCoordLib.Core.Math;

namespace Ctis.Core;

public sealed class InventoryService : IInventoryService
{
    private readonly IInventoryTreeCache _tree;
    private readonly IItemVmRegistry _registry;
    private readonly PlacementConfig _placement;
    private readonly IItemCatalog _catalog;
    private readonly IGridFactory? _grids;
    private readonly IItemIdFactory _ids;
    private readonly EquipmentLayout _layout;
    private readonly IInnerGridLayout _innerLayout;

    public InventoryService(
        IInventoryTreeCache tree,
        IItemVmRegistry registry,
        PlacementConfig placement,
        IItemCatalog catalog,
        IGridFactory? grids = null,
        IItemIdFactory? ids = null,
        EquipmentLayout? layout = null,
        IInnerGridLayout? innerLayout = null)
    {
        _tree = tree;
        _registry = registry;
        _placement = placement;
        _catalog = catalog;
        _grids = grids;
        _ids = ids ?? new GuidItemIdFactory();
        _layout = layout ?? new EquipmentLayout();
        _innerLayout = innerLayout ?? EmptyInnerGridLayout.Instance;
    }

    /// <summary>True when placement rules accept this drop target.</summary>
    public bool CanPlace(in InventoryPlacementContext context, out InventoryPlacementBlockReason reason)
        => _placement.Evaluate(context, _tree, out reason);

    /// <summary>Evaluates a drop as inner-insert, vacant, stack, exchange, or blocked.</summary>
    public InventoryDropResult EvaluateDrop(in InventoryPlacementContext context, TetrisItemGhostVM? ghost = null)
    {
        using var traceScope = CtisTrace.Scope("Inventory.EvaluateDrop");
        if (InventoryLogic.IsInnerInsertHover(context.Item, context.HoveredItem))
            return EvaluateInnerInsert(context.Item!, context.HoveredItem!);
        if (!CanPlace(context, out var reason))
            return InventoryDropResult.Invalid(reason);
        if (context.TargetSlot != null)
            return InventoryDropResult.Vacant();
        if (context.TargetGrid == null || context.Item == null)
            return InventoryDropResult.Invalid(InventoryPlacementBlockReason.Occupied);
        var originId = OriginContainerId(context.Item.CurrentTetrisContainer ?? ghost?.OriginContainerOnDrag);
        var direction = ghost?.Direction ?? context.Item.Direction;
        return InventoryLogic.EvaluateGridDrop(
            _tree,
            _catalog,
            _registry,
            context.TargetGrid.GridGuid,
            context.Item,
            context.ShapeCoordinates,
            context.Origin,
            direction,
            originId);
    }

    /// <summary>Applies a guid/container-id command against the tree, then projects live VMs.</summary>
    public InventoryCommandResult Apply(InventoryCommand command)
    {
        if (string.IsNullOrEmpty(command.CommandId))
            command = command.WithEnvelope(_ids.Next(), _tree.Revision);
        return Dispatch(command);
    }

    /// <summary>Replays a command that already carries a network envelope.</summary>
    public InventoryCommandResult ApplyRemote(InventoryCommand command)
    {
        if (string.IsNullOrEmpty(command.CommandId) || !command.HasExpectedRevision)
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.InvalidCommand);
        return Dispatch(command);
    }

    /// <summary>Swaps the dragged item with fully covered occupants.</summary>
    public bool TryQuickExchange(TetrisGridVM grid, TetrisItemGhostVM ghost, Vec2I targetPos)
    {
        var item = ghost.SelectedItem;
        if (item == null || string.IsNullOrEmpty(grid.GridGuid)) return false;
        var originContainer = item.CurrentTetrisContainer ?? ghost.OriginContainerOnDrag;
        var originId = OriginContainerId(originContainer);
        return Apply(InventoryCommand.Exchange(item.Guid, grid.GridGuid, targetPos, ghost.Direction, originId)).Ok;
    }

    /// <summary>Merges source onto target and removes the source when its stack hits zero.</summary>
    public bool TryStack(TetrisItemVM source, TetrisItemVM target)
        => Apply(InventoryCommand.Stack(source.Guid, target.Guid)).Ok;

    /// <summary>Removes an item from its current grid or slot without destroying the VM.</summary>
    public void Lift(TetrisItemVM item)
        => Lift(item, InventoryTreeIds.LocalActorId);

    /// <summary>Moves an item into the actor's held container and clears live occupancy.</summary>
    public void Lift(TetrisItemVM item, string actorId)
        => Apply(InventoryCommand.Lift(item.Guid, actorId));

    /// <summary>Places an item onto a grid origin after occupancy and rule checks.</summary>
    public bool PlaceOnGrid(TetrisItemVM item, TetrisGridVM grid, Vec2I origin, TetrisSlotVM? fromSlot)
    {
        if (string.IsNullOrEmpty(grid.GridGuid)) return false;
        EnsureSpawned(item);
        if (!TryEnsureContainer(grid)) return false;
        return Apply(InventoryCommand.Place(item.Guid, grid.GridGuid, origin, item.Direction)).Ok;
    }

    /// <summary>Places an item onto an equipment slot after type and occupancy checks.</summary>
    public bool PlaceOnSlot(TetrisItemVM item, TetrisSlotVM slot)
    {
        EnsureSpawned(item);
        var result = Apply(InventoryCommand.MoveToSlot(item.Guid, slot.SlotIndex));
        if (!result.Ok) return false;
        return slot.TryPlaceTetrisItem(item);
    }

    /// <summary>Evaluates dropping <paramref name="mover"/> into <paramref name="host"/> inner grids without opening a panel.</summary>
    public InventoryDropResult EvaluateInnerInsert(TetrisItemVM mover, TetrisItemVM host)
    {
        using var traceScope = CtisTrace.Scope("Inventory.EvaluateInnerInsert");
        if (host.ItemDetails?.HasInnerGrid != true)
            return InventoryDropResult.Invalid(InventoryPlacementBlockReason.Occupied);
        if (mover.Guid == host.Guid)
            return InventoryDropResult.Invalid(InventoryPlacementBlockReason.SelfOwnedContainer);
        if (_grids == null)
            return InventoryDropResult.Invalid(InventoryPlacementBlockReason.UnknownContainer);

        var specs = _innerLayout.SpecsFor(host.ItemDetails);
        if (specs.Count == 0)
            return InventoryDropResult.Invalid(InventoryPlacementBlockReason.UnknownContainer);

        var inners = ItemInnerGrid.EnsureAll(host, specs, _tree, _grids, this);
        if (!InventoryLogic.TryFindInnerInsert(
                _tree,
                _catalog,
                mover,
                inners,
                out var grid,
                out var origin,
                out var direction,
                out var reason))
            return InventoryDropResult.Invalid(reason);
        return InventoryDropResult.InsertIntoInner(grid, origin, direction, host);
    }

    /// <summary>Places onto the inner grid chosen by <see cref="EvaluateInnerInsert"/>.</summary>
    public bool TryPlaceInnerInsert(TetrisItemVM item, InventoryDropResult insert)
    {
        if (insert.Kind != InventoryDropKind.InsertIntoInner || insert.InnerGrid == null)
            return false;
        if (insert.InnerDirection != item.Direction)
            item.Direction = insert.InnerDirection;
        return PlaceOnGrid(item, insert.InnerGrid, insert.InnerOrigin, null);
    }

    /// <summary>Splits a stack and places the new stack beside or elsewhere on the same grid.</summary>
    public bool TrySplit(TetrisItemVM item, int amount)
    {
        if (item.CurrentTetrisContainer is not TetrisGridVM) return false;
        var newGuid = _ids.Next();
        return Apply(InventoryCommand.Split(item.Guid, amount, newGuid)).Ok;
    }

    /// <summary>Resizes a grid and repacks occupants; returns false if they cannot fit.</summary>
    public bool TryResizeGrid(TetrisGridVM grid, int width, int height, float tileWidth, float tileHeight)
    {
        width = Math.Clamp(width, CtisSettings.GridMinCells, CtisSettings.GridMaxColumns);
        height = Math.Clamp(height, CtisSettings.GridMinCells, CtisSettings.GridMaxRows);
        tileWidth = Math.Clamp(tileWidth, CtisSettings.GridMinTileSize, CtisSettings.GridMaxTileSize);
        tileHeight = Math.Clamp(tileHeight, CtisSettings.GridMinTileSize, CtisSettings.GridMaxTileSize);

        if (string.IsNullOrEmpty(grid.GridGuid)) return false;
        return Apply(InventoryCommand.ResizeContainer(grid.GridGuid, width, height, tileWidth, tileHeight)).Ok;
    }

    /// <summary>Applies a named occupancy patch and writes the item into the tree.</summary>
    public void ApplyOccupancyPatch(TetrisItemVM item, string key, IEnumerable<Vec2I>? add, IEnumerable<Vec2I>? remove)
        => Apply(InventoryCommand.PatchOccupancy(item.Guid, key, add, remove));

    /// <summary>Removes a named occupancy patch and writes the restored footprint into the tree.</summary>
    public void RemoveOccupancyPatch(TetrisItemVM item, string key)
        => Apply(InventoryCommand.RemoveOccupancyPatch(item.Guid, key));

    /// <summary>Toggles a screen-axis flip for an item sitting on a grid.</summary>
    public bool TryFlip(TetrisItemVM item, bool horizontal)
    {
        if (item.CurrentTetrisContainer is not TetrisGridVM)
            return false;
        bool flipH = item.FlipH;
        bool flipV = item.FlipV;
        DirUtil.ToggleVisualFlip(item.Direction, horizontal, ref flipH, ref flipV);
        return Apply(InventoryCommand.Flip(item.Guid, flipH, flipV)).Ok;
    }

    /// <summary>Organizes items in a single grid.</summary>
    public bool TryOrganizeGrid(TetrisGridVM grid, InventorySortStrategy strategy = InventorySortStrategy.Area)
    {
        if (string.IsNullOrEmpty(grid.GridGuid)) return false;
        using var traceScope = CtisTrace.Scope("Inventory.Organize");
        return Apply(InventoryCommand.Organize(grid.GridGuid, strategy)).Ok;
    }

    /// <summary>Organizes items in a named container.</summary>
    public bool TryOrganizeContainer(string containerId, InventorySortStrategy strategy = InventorySortStrategy.Area)
    {
        if (string.IsNullOrEmpty(containerId)) return false;
        using var traceScope = CtisTrace.Scope("Inventory.Organize");
        return Apply(InventoryCommand.Organize(containerId, strategy)).Ok;
    }

    /// <summary>Organizes items across all inner grids belonging to an item.</summary>
    public bool TryOrganizeItemGrids(TetrisItemVM item, InventorySortStrategy strategy = InventorySortStrategy.Area)
    {
        if (string.IsNullOrEmpty(item.Guid)) return false;
        using var traceScope = CtisTrace.Scope("Inventory.Organize");
        return Apply(InventoryCommand.OrganizeItem(item.Guid, strategy)).Ok;
    }

    private InventoryCommandResult Dispatch(InventoryCommand command)
    {
        using var traceScope = CtisTrace.Scope($"Inventory.Dispatch.{command.Kind}");
        var previous = !string.IsNullOrEmpty(command.ItemGuid)
            ? _tree.GetContainerIdByItem(command.ItemGuid)
            : null;
        var result = InventorySimulation.Apply(command, _tree, _catalog, _layout);
        if (result.Ok)
            Project(command, previous);
        return result;
    }

    private void Project(InventoryCommand command, string? previousContainerId)
    {
        switch (command.Kind)
        {
            case InventoryCommandKind.Place:
                if (_registry.TryGet(command.ItemGuid, out var placed)
                    && !string.IsNullOrEmpty(command.ContainerId)
                    && _grids?.TryGetVM(command.ContainerId, out var dest) == true
                    && _tree.TryGetItem(command.ItemGuid, out var placedNode))
                {
                    placed.ProjectFrom(placedNode.Data);
                    MoveVmOntoGrid(placed, dest, placedNode.Data.OriginPosition);
                }
                break;
            case InventoryCommandKind.Lift:
                if (_registry.TryGet(command.ItemGuid, out var lifted))
                    DetachVmOccupancy(lifted);
                break;
            case InventoryCommandKind.MoveToSlot:
                if (_registry.TryGet(command.ItemGuid, out var slotted))
                    DetachVmOccupancy(slotted);
                break;
            case InventoryCommandKind.Stack:
                ProjectStack(command);
                break;
            case InventoryCommandKind.Split:
                ProjectSplit(command, previousContainerId);
                break;
            case InventoryCommandKind.ResizeContainer:
            case InventoryCommandKind.OrganizeContainer:
                ProjectGridById(command.ContainerId);
                break;
            case InventoryCommandKind.OrganizeItemGrids:
                if (!string.IsNullOrEmpty(command.ItemGuid))
                {
                    var prefix = command.ItemGuid + ":";
                    foreach (var id in _tree.GetAllContainerIds())
                    {
                        if (id.StartsWith(prefix, StringComparison.Ordinal))
                            ProjectGridById(id);
                    }
                }
                break;
            case InventoryCommandKind.Exchange:
                ProjectGridById(command.ContainerId);
                if (!string.IsNullOrEmpty(command.OriginContainerId)
                    && command.OriginContainerId != command.ContainerId
                    && InventoryTreeIds.IsGridContainer(command.OriginContainerId))
                    ProjectGridById(command.OriginContainerId);
                break;
            case InventoryCommandKind.PatchOccupancy:
            case InventoryCommandKind.RemoveOccupancyPatch:
            case InventoryCommandKind.Flip:
                ProjectItemShape(command.ItemGuid);
                break;
        }
    }

    private void ProjectStack(InventoryCommand command)
    {
        if (_registry.TryGet(command.ItemGuid, out var source))
        {
            if (!_tree.TryGetItem(command.ItemGuid, out _))
            {
                source.CurrentStack = 0;
                DetachVmOccupancy(source);
                if (source.CurrentTetrisContainer is TetrisGridVM grid)
                    grid.RequestRemoveItemView(source);
                _registry.Unregister(command.ItemGuid, true);
            }
            else if (_tree.TryGetItem(command.ItemGuid, out var remaining))
                source.CurrentStack = remaining.Data.Stack;
        }

        if (!string.IsNullOrEmpty(command.TargetGuid)
            && _registry.TryGet(command.TargetGuid, out var target)
            && _tree.TryGetItem(command.TargetGuid, out var targetNode))
            target.CurrentStack = targetNode.Data.Stack;
    }

    private void ProjectSplit(InventoryCommand command, string? previousContainerId)
    {
        var containerId = previousContainerId ?? _tree.GetContainerIdByItem(command.ItemGuid);
        if (_registry.TryGet(command.ItemGuid, out var original)
            && _tree.TryGetItem(command.ItemGuid, out var originalNode))
            original.CurrentStack = originalNode.Data.Stack;

        if (string.IsNullOrEmpty(command.NewItemGuid) || !_tree.TryGetItem(command.NewItemGuid, out var splitNode))
            return;

        TetrisGridVM? grid = null;
        if (!string.IsNullOrEmpty(containerId))
            _grids?.TryGetVM(containerId, out grid);
        if (grid == null && original?.CurrentTetrisContainer is TetrisGridVM current)
            grid = current;
        if (grid == null)
            return;

        var details = _catalog.GetById(splitNode.Data.ItemId);
        var split = _registry.GetOrCreate(details, splitNode.Data, grid);
        split.ProjectFrom(splitNode.Data);
        if (!grid.OwnerItemsDic.ContainsKey(split.Guid))
            grid.PlaceTetrisItem(split, splitNode.Data.OriginPosition.X, splitNode.Data.OriginPosition.Y);
    }

    private void ProjectItemShape(string itemGuid)
    {
        if (!_tree.TryGetItem(itemGuid, out var node))
            return;
        if (!_registry.TryGet(itemGuid, out var vm))
            return;
        if (vm.CurrentTetrisContainer is TetrisGridVM occupied
            && occupied.OwnerItemsDic.ContainsKey(vm.Guid))
        {
            occupied.RemoveTetrisItem(
                vm,
                vm.LocalGridCoordinate.X,
                vm.LocalGridCoordinate.Y,
                vm.TetrisCoordinateSet,
                false);
        }
        vm.ProjectFrom(node.Data);
        if (vm.CurrentTetrisContainer is TetrisGridVM grid)
            grid.PlaceTetrisItem(vm, node.Data.OriginPosition.X, node.Data.OriginPosition.Y);
    }

    private void ProjectGridById(string? containerId)
    {
        if (string.IsNullOrEmpty(containerId)) return;
        if (_grids?.TryGetVM(containerId, out var grid) == true)
            grid.RefreshFromTree();
    }

    private void EnsureSpawned(TetrisItemVM item)
    {
        if (_tree.TryGetItem(item.Guid, out _)) return;
        var data = new TetrisItemPersistentData { ItemGuid = item.Guid };
        data.CopyFrom(item);
        _tree.UpsertItem(data);
    }

    private bool TryEnsureContainer(TetrisGridVM grid)
    {
        if (string.IsNullOrEmpty(grid.GridGuid)) return false;
        if (_tree.TryGetContainer(grid.GridGuid, out _)) return true;
        return Apply(InventoryCommand.ResizeContainer(
            grid.GridGuid,
            grid.GridSizeWidth,
            grid.GridSizeHeight,
            grid.LocalGridTileSizeWidth,
            grid.LocalGridTileSizeHeight)).Ok;
    }

    private void MoveVmOntoGrid(TetrisItemVM item, TetrisGridVM grid, Vec2I origin)
    {
        var oldGrid = item.CurrentTetrisContainer as TetrisGridVM;
        var oldSlot = item.CurrentTetrisContainer as TetrisSlotVM;
        var oldPos = item.LocalGridCoordinate;
        var oldShape = item.TetrisCoordinateSet;
        if (oldGrid != null && oldGrid.OwnerItemsDic.ContainsKey(item.Guid))
            oldGrid.RemoveTetrisItem(item, oldPos.X, oldPos.Y, oldShape, oldGrid != grid);
        else if (grid.OwnerItemsDic.ContainsKey(item.Guid))
            grid.RemoveTetrisItem(item, oldPos.X, oldPos.Y, oldShape, false);
        oldSlot?.RemoveTetrisItem(false);
        grid.PlaceTetrisItem(item, origin.X, origin.Y);
    }

    private static void DetachVmOccupancy(TetrisItemVM item)
    {
        if (item.CurrentTetrisContainer is TetrisGridVM grid && grid.OwnerItemsDic.ContainsKey(item.Guid))
        {
            grid.RemoveTetrisItem(item, item.LocalGridCoordinate.X, item.LocalGridCoordinate.Y, item.TetrisCoordinateSet, false);
            return;
        }
        if (item.CurrentTetrisContainer is TetrisSlotVM slot)
            slot.RemoveTetrisItem(false);
    }

    private static string? OriginContainerId(TetrisItemContainerVM? container)
    {
        var id = InventoryTreeIds.Of(container);
        return string.IsNullOrEmpty(id) ? null : id;
    }
}
