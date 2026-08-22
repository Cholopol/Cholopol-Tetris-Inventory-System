using TetrisCoordLib.Core.Math;

namespace Ctis.Core;

public static class InventoryLogic
{
    /// <summary>True when the drop target is this item or a grid nested inside it.</summary>
    public static bool IsPlacingIntoSelfOwnedContainer(
        TetrisItemVM item,
        TetrisItemContainerVM targetContainer,
        IInventoryTreeCache tree)
    {
        var owner = targetContainer.RelatedTetrisItem;
        if (owner != null && (ReferenceEquals(owner, item) || owner.Guid == item.Guid))
            return true;

        return targetContainer is TetrisGridVM grid
            && IsPlacingIntoSelfOwnedContainer(item.Guid, grid.GridGuid, tree);
    }

    /// <summary>True when the target container id is this item or nested under it.</summary>
    public static bool IsPlacingIntoSelfOwnedContainer(string itemGuid, string? targetContainerId, IInventoryTreeCache tree)
        => !string.IsNullOrEmpty(itemGuid)
            && !string.IsNullOrEmpty(targetContainerId)
            && tree.IsDescendantContainer(itemGuid, targetContainerId);

    /// <summary>True when every cell of the shape at origin is empty and in bounds.</summary>
    public static bool CanPlaceAt(OccupancyBoard board, IReadOnlyList<Vec2I> shape, Vec2I origin)
        => board.CanPlace(shape, origin);

    /// <summary>True when the shape fits the container occupancy rebuilt from Tree.</summary>
    public static bool CanPlaceAt(
        IInventoryTreeCache tree,
        IItemCatalog catalog,
        string containerId,
        IReadOnlyList<Vec2I> shape,
        Vec2I origin,
        string? excludeGuid = null)
    {
        return OccupancyBoard.For(tree, catalog, containerId).CanPlace(shape, origin, excludeGuid);
    }

    /// <summary>True when every cell of <paramref name="item"/> at the origin is empty and in bounds.</summary>
    public static bool CanPlaceAt(TetrisGridVM grid, TetrisItemVM item, int posX, int posY)
        => CanPlaceAt(grid.ResolveBoard(), item.TetrisCoordinateSet, new Vec2I(posX, posY));

    /// <summary>Finds the first origin where <paramref name="item"/> fits, trying other facings when needed.</summary>
    public static bool TryFindFreeOrigin(TetrisGridVM grid, TetrisItemVM item, out Vec2I origin)
    {
        if (!TryFindFreeOrigin(grid, item, out origin, out var direction))
            return false;
        if (direction != item.Direction)
            item.Direction = direction;
        return true;
    }

    /// <summary>Finds a free origin without mutating <paramref name="item"/> facing.</summary>
    public static bool TryFindFreeOrigin(TetrisGridVM grid, TetrisItemVM item, out Vec2I origin, out Dir direction)
    {
        return grid.ResolveBoard().TryFindFreeOrigin(
            item.ItemDetails?.Occupancy,
            item.OccupancyPatches,
            item.Direction,
            item.FlipH,
            item.FlipV,
            out origin,
            out direction,
            item.Guid);
    }

    /// <summary>True when the pointer is over another item that owns inner grids.</summary>
    public static bool IsInnerInsertHover(TetrisItemVM? mover, TetrisItemVM? hovered)
        => mover != null
            && hovered != null
            && hovered.Guid != mover.Guid
            && hovered.ItemDetails?.HasInnerGrid == true;

    /// <summary>
    /// First-fit search across <paramref name="inners"/> in list order.
    /// Does not change <paramref name="mover"/> facing.
    /// </summary>
    public static bool TryFindInnerInsert(
        IInventoryTreeCache tree,
        IItemCatalog catalog,
        TetrisItemVM mover,
        IReadOnlyList<TetrisGridVM> inners,
        out TetrisGridVM grid,
        out Vec2I origin,
        out Dir direction,
        out InventoryPlacementBlockReason reason)
    {
        using var traceScope = CtisTrace.Scope("InventoryLogic.TryFindInnerInsert");
        grid = null!;
        origin = Vec2I.Zero;
        direction = mover.Direction;
        reason = InventoryPlacementBlockReason.UnknownContainer;
        if (inners.Count == 0)
            return false;

        var exclude = new HashSet<string>(StringComparer.Ordinal) { mover.Guid };
        reason = InventoryPlacementBlockReason.Occupied;
        for (int i = 0; i < inners.Count; i++)
        {
            var inner = inners[i];
            if (string.IsNullOrEmpty(inner.GridGuid))
                continue;
            if (IsPlacingIntoSelfOwnedContainer(mover.Guid, inner.GridGuid, tree))
            {
                reason = InventoryPlacementBlockReason.SelfOwnedContainer;
                return false;
            }

            var board = OccupancyBoard.For(tree, catalog, inner.GridGuid);
            if (!board.TryFindFreeOrigin(
                    mover.ItemDetails?.Occupancy,
                    mover.OccupancyPatches,
                    mover.Direction,
                    mover.FlipH,
                    mover.FlipV,
                    out origin,
                    out direction,
                    exclude))
                continue;
            grid = inner;
            reason = InventoryPlacementBlockReason.None;
            return true;
        }

        return false;
    }

    /// <summary>Repacks items into a new width/height, failing if any item cannot fit.</summary>
    /// <summary>Repacks items into a new width/height, failing if any item cannot fit.</summary>
    public static bool TryPlanPack(
        int width,
        int height,
        IReadOnlyList<(TetrisItemVM Item, Vec2I Origin)> items,
        out List<(TetrisItemVM Item, Vec2I Origin)> packed)
        => TryPlanPack(width, height, items, InventorySortStrategy.Area, out packed);

    /// <summary>Repacks items into a new width/height according to sort strategy.</summary>
    public static bool TryPlanPack(
        int width,
        int height,
        IReadOnlyList<(TetrisItemVM Item, Vec2I Origin)> items,
        InventorySortStrategy strategy,
        out List<(TetrisItemVM Item, Vec2I Origin)> packed)
    {
        packed = new List<(TetrisItemVM, Vec2I)>(items.Count);
        var shaped = new List<(string Guid, int ItemId, int Rarity, int SlotType, ItemOccupancy? Occ, IReadOnlyList<OccupancyPatch>? Patches, Vec2I Origin, Dir Preferred, bool FlipH, bool FlipV)>(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            var entry = items[i];
            var details = entry.Item.ItemDetails;
            shaped.Add((
                entry.Item.Guid,
                details?.ItemId ?? 0,
                (int)(details?.Rarity ?? ItemRarity.Common),
                (int)(details?.SlotType ?? InventorySlotType.Pocket),
                details?.Occupancy,
                entry.Item.OccupancyPatches,
                entry.Origin,
                entry.Item.Direction,
                entry.Item.FlipH,
                entry.Item.FlipV));
        }

        if (!TryPlanPack(width, height, shaped, strategy, out var packedShapes))
            return false;

        for (int i = 0; i < packedShapes.Count; i++)
        {
            var shape = packedShapes[i];
            for (int j = 0; j < items.Count; j++)
            {
                var entry = items[j];
                if (entry.Item.Guid == shape.Guid)
                {
                    if (shape.Direction != entry.Item.Direction)
                        entry.Item.Direction = shape.Direction;
                    packed.Add((entry.Item, shape.Origin));
                    break;
                }
            }
        }
        return true;
    }

    /// <summary>Repacks persisted shapes into a new width/height, rotating when the current facing cannot fit.</summary>
    public static bool TryPlanPack(
        int width,
        int height,
        IReadOnlyList<(string Guid, ItemOccupancy? Occ, IReadOnlyList<OccupancyPatch>? Patches, Vec2I Origin, Dir Preferred, bool FlipH, bool FlipV)> items,
        out List<(string Guid, Vec2I Origin, Dir Direction)> packed)
    {
        var shaped = new List<(string Guid, int ItemId, int Rarity, int SlotType, ItemOccupancy? Occ, IReadOnlyList<OccupancyPatch>? Patches, Vec2I Origin, Dir Preferred, bool FlipH, bool FlipV)>(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            var e = items[i];
            shaped.Add((e.Guid, 0, 0, 0, e.Occ, e.Patches, e.Origin, e.Preferred, e.FlipH, e.FlipV));
        }
        return TryPlanPack(width, height, shaped, InventorySortStrategy.Area, out packed, true);
    }

    /// <summary>Repacks persisted shapes into a new width/height with a sort strategy.</summary>
    public static bool TryPlanPack(
        int width,
        int height,
        IReadOnlyList<(string Guid, int ItemId, int Rarity, int SlotType, ItemOccupancy? Occ, IReadOnlyList<OccupancyPatch>? Patches, Vec2I Origin, Dir Preferred, bool FlipH, bool FlipV)> items,
        InventorySortStrategy strategy,
        out List<(string Guid, Vec2I Origin, Dir Direction)> packed,
        bool keepInPlace = false)
    {
        packed = new List<(string, Vec2I, Dir)>(items.Count);
        int w = Math.Max(1, width);
        int h = Math.Max(1, height);
        var board = new OccupancyBoard(w, h);
        var ranked = RankByStrategy(items, strategy);
        for (int i = 0; i < ranked.Count; i++)
        {
            var entry = ranked[i];
            var preferred = ItemShape.Resolve(entry.Occ, entry.Patches, entry.Preferred, entry.FlipH, entry.FlipV);
            var origin = entry.Origin;
            var direction = entry.Preferred;

            bool placed = false;
            if (keepInPlace && board.CanPlace(preferred.Cells, origin))
            {
                placed = true;
            }
            else if (board.TryFindFreeOrigin(
                entry.Occ,
                entry.Patches,
                entry.Preferred,
                entry.FlipH,
                entry.FlipV,
                out origin,
                out direction))
            {
                placed = true;
            }

            if (!placed)
            {
                packed.Clear();
                return false;
            }

            var fitted = direction == entry.Preferred
                ? preferred
                : ItemShape.Resolve(entry.Occ, entry.Patches, direction, entry.FlipH, entry.FlipV);
            board.TryOccupy(entry.Guid, fitted.Cells, origin);
            packed.Add((entry.Guid, origin, direction));
        }
        return true;
    }

    /// <summary>Packs items across a group of multiple inner grids using a multi-grid first-fit decreasing heuristic.</summary>
    public static bool TryPlanMultiGridPack(
        IReadOnlyList<(string GridGuid, int Width, int Height)> grids,
        IReadOnlyList<TetrisItemPersistentData> items,
        IItemCatalog catalog,
        InventorySortStrategy strategy,
        out List<InventoryPlacementPlan> placements)
    {
        placements = new List<InventoryPlacementPlan>(items.Count);
        if (grids.Count == 0 || items.Count == 0)
            return true;

        var boards = new OccupancyBoard[grids.Count];
        for (int g = 0; g < grids.Count; g++)
            boards[g] = new OccupancyBoard(Math.Max(1, grids[g].Width), Math.Max(1, grids[g].Height));

        var shaped = new List<(string Guid, int ItemId, int Rarity, int SlotType, ItemOccupancy? Occ, IReadOnlyList<OccupancyPatch>? Patches, Vec2I Origin, Dir Preferred, bool FlipH, bool FlipV)>(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            var data = items[i];
            var details = catalog.GetById(data.ItemId);
            shaped.Add((
                data.ItemGuid,
                data.ItemId,
                (int)(details?.Rarity ?? ItemRarity.Common),
                (int)(details?.SlotType ?? InventorySlotType.Pocket),
                details?.Occupancy,
                data.OccupancyPatches,
                data.OriginPosition,
                data.Direction,
                data.FlipH,
                data.FlipV));
        }

        var ranked = RankByStrategy(shaped, strategy);

        // Try placing each item into the first available grid in the group
        for (int i = 0; i < ranked.Count; i++)
        {
            var entry = ranked[i];
            bool placed = false;
            for (int g = 0; g < grids.Count; g++)
            {
                var board = boards[g];
                var gridGuid = grids[g].GridGuid;
                if (!board.TryFindFreeOrigin(
                    entry.Occ,
                    entry.Patches,
                    entry.Preferred,
                    entry.FlipH,
                    entry.FlipV,
                    out var origin,
                    out var direction))
                {
                    continue;
                }

                var fitted = direction == entry.Preferred
                    ? ItemShape.Resolve(entry.Occ, entry.Patches, entry.Preferred, entry.FlipH, entry.FlipV)
                    : ItemShape.Resolve(entry.Occ, entry.Patches, direction, entry.FlipH, entry.FlipV);
                board.TryOccupy(entry.Guid, fitted.Cells, origin);
                placements.Add(new InventoryPlacementPlan(entry.Guid, gridGuid, origin, direction));
                placed = true;
                break;
            }

            if (!placed)
            {
                placements.Clear();
                return false;
            }
        }

        return true;
    }

    /// <summary>Repacks persisted shapes into a new width/height.</summary>
    public static bool TryPlanPack(
        int width,
        int height,
        IReadOnlyList<(string Guid, IReadOnlyList<Vec2I> Shape, Vec2I Origin)> items,
        out List<(string Guid, Vec2I Origin)> packed)
    {
        packed = new List<(string, Vec2I)>(items.Count);
        int w = Math.Max(1, width);
        int h = Math.Max(1, height);
        var board = new OccupancyBoard(w, h);
        var ranked = RankByBoundsArea(items);
        for (int i = 0; i < ranked.Count; i++)
        {
            var entry = ranked[i];
            var origin = entry.Origin;
            if (!board.CanPlace(entry.Shape, origin)
                && !board.TryFindFreeOrigin(entry.Shape, out origin))
            {
                packed.Clear();
                return false;
            }
            board.TryOccupy(entry.Guid, entry.Shape, origin);
            packed.Add((entry.Guid, origin));
        }
        return true;
    }

    private static int BoundsArea(IReadOnlyList<Vec2I> shape)
    {
        int width = 0;
        int height = 0;
        for (int i = 0; i < shape.Count; i++)
        {
            width = Math.Max(width, shape[i].X + 1);
            height = Math.Max(height, shape[i].Y + 1);
        }
        return width * height;
    }

    /// <summary>True when the two items share an id and the target still has stack room.</summary>
    public static bool CanMergeStack(TetrisItemVM target, TetrisItemVM donor)
    {
        if (target.ItemDetails == null || donor.ItemDetails == null) return false;
        if (target.ItemDetails.ItemId != donor.ItemDetails.ItemId) return false;
        if (target.MaxStack <= 1) return false;
        if (target.CurrentStack >= target.MaxStack) return false;
        return donor.CurrentStack > 0;
    }

    /// <summary>True when persisted stacks share an id and the target still has room.</summary>
    public static bool CanMergeStack(TetrisItemPersistentData target, TetrisItemPersistentData source, ItemDetails? details)
    {
        if (details == null || details.MaxStack <= 1) return false;
        if (target.ItemId != source.ItemId) return false;
        if (target.Stack >= details.MaxStack) return false;
        return source.Stack > 0;
    }

    /// <summary>True when a block reason applies to the whole drop, not individual cells.</summary>
    public static bool BlocksEntireDrop(InventoryPlacementBlockReason reason)
        => reason is InventoryPlacementBlockReason.SelfOwnedContainer
            or InventoryPlacementBlockReason.OutOfBounds
            or InventoryPlacementBlockReason.SlotOccupied
            or InventoryPlacementBlockReason.SlotTypeMismatch
            or InventoryPlacementBlockReason.UnknownItem
            or InventoryPlacementBlockReason.UnknownContainer
            or InventoryPlacementBlockReason.InvalidCommand
            or InventoryPlacementBlockReason.DuplicateGuid
            or InventoryPlacementBlockReason.RevisionMismatch;

    /// <summary>Per-cell drop tint: occupancy and stack/exchange are local; only whole-drop rules paint every cell.</summary>
    public static InventoryDropCellKind ResolveDropCell(
        InventoryPlacementBlockReason reason,
        bool occupiedByOther,
        bool canStackOnOccupant,
        bool canExchange)
    {
        if (BlocksEntireDrop(reason))
            return InventoryDropCellKind.Blocked;
        if (!occupiedByOther)
            return InventoryDropCellKind.Empty;
        if (canStackOnOccupant)
            return InventoryDropCellKind.Stack;
        return canExchange ? InventoryDropCellKind.Exchange : InventoryDropCellKind.Occupied;
    }

    /// <summary>Highlight color for a resolved drop cell.</summary>
    public static Rgba ColorForDropCell(
        InventoryDropCellKind kind,
        InventoryPlacementBlockReason reason,
        PlacementConfig placement)
    {
        var palette = placement.ResolveHighlightPalette();
        return kind switch
        {
            InventoryDropCellKind.Blocked => placement.GetInvalidColor(reason),
            InventoryDropCellKind.Empty => palette.ValidEmpty,
            InventoryDropCellKind.Stack => palette.CanStack,
            InventoryDropCellKind.Exchange => palette.CanQuickExchange,
            _ => palette.Invalid
        };
    }

    /// <summary>Builds per-cell highlight data for a ghost hovering a grid.</summary>
    public static InventoryDropPreview BuildDropPreview(
        TetrisGridVM grid,
        TetrisItemVM? selected,
        IReadOnlyList<Vec2I> shape,
        Vec2I origin,
        InventoryDropResult drop,
        PlacementConfig placement)
    {
        var preview = new InventoryDropPreview();
        var cells = new List<InventoryDropPreviewCell>(shape.Count);
        FillDropPreview(preview, cells, grid, selected, shape, origin, drop, placement);
        return preview;
    }

    /// <summary>Fills an existing preview and cell buffer so drag hover can reuse the same instances.</summary>
    public static void FillDropPreview(
        InventoryDropPreview preview,
        List<InventoryDropPreviewCell> cells,
        TetrisGridVM grid,
        TetrisItemVM? selected,
        IReadOnlyList<Vec2I> shape,
        Vec2I origin,
        InventoryDropResult drop,
        PlacementConfig placement)
    {
        cells.Clear();
        bool canExchange = drop.Kind == InventoryDropKind.Exchange;
        foreach (var local in shape)
        {
            var cell = origin + local;
            if (!grid.PositionCheck(cell.X, cell.Y))
                continue;
            var occ = grid.GetTetrisItemVM(cell.X, cell.Y);
            var kind = ResolveDropCell(
                drop.Reason,
                occupiedByOther: occ != null && occ != selected,
                canStackOnOccupant: selected != null && occ != null && occ != selected && CanMergeStack(occ, selected),
                canExchange);
            cells.Add(new InventoryDropPreviewCell(cell, kind, ColorForDropCell(kind, drop.Reason, placement)));
        }

        preview.Grid = grid;
        preview.Origin = origin;
        preview.Result = drop;
        preview.Cells = cells;
    }

    /// <summary>Paints the host item's footprint on its parent grid for an inner-insert hover.</summary>
    public static InventoryDropPreview BuildInnerInsertPreview(
        TetrisGridVM? parentGrid,
        TetrisItemVM host,
        InventoryDropResult drop,
        PlacementConfig placement)
    {
        var preview = new InventoryDropPreview();
        var cells = new List<InventoryDropPreviewCell>(host.TetrisCoordinateSet.Count);
        FillInnerInsertPreview(preview, cells, parentGrid, host, drop, placement);
        return preview;
    }

    /// <summary>Fills an existing preview and cell buffer for an inner-insert hover.</summary>
    public static void FillInnerInsertPreview(
        InventoryDropPreview preview,
        List<InventoryDropPreviewCell> cells,
        TetrisGridVM? parentGrid,
        TetrisItemVM host,
        InventoryDropResult drop,
        PlacementConfig placement)
    {
        cells.Clear();
        var kind = drop.Kind == InventoryDropKind.InsertIntoInner
            ? InventoryDropCellKind.Empty
            : InventoryDropCellKind.Blocked;
        var color = ColorForDropCell(kind, drop.Reason, placement);
        if (parentGrid != null)
        {
            var origin = host.LocalGridCoordinate;
            foreach (var local in host.TetrisCoordinateSet)
            {
                var cell = origin + local;
                if (!parentGrid.PositionCheck(cell.X, cell.Y))
                    continue;
                cells.Add(new InventoryDropPreviewCell(cell, kind, color));
            }
        }

        preview.Grid = parentGrid;
        preview.Origin = host.LocalGridCoordinate;
        preview.Result = drop;
        preview.Cells = cells;
    }

    /// <summary>Classifies a grid drop as vacant, stack, multi-item exchange, or blocked.</summary>
    public static InventoryDropResult EvaluateGridDrop(
        IInventoryTreeCache tree,
        IItemCatalog catalog,
        IItemVmRegistry registry,
        string containerId,
        TetrisItemVM item,
        IReadOnlyList<Vec2I> shape,
        Vec2I origin,
        Dir destDirection,
        string? originContainerId)
    {
        using var traceScope = CtisTrace.Scope("InventoryLogic.EvaluateGridDrop");
        var board = OccupancyBoard.For(tree, catalog, containerId);
        var coverage = board.ScanCoverage(shape, origin, item.Guid);
        if (coverage.OutOfBounds)
            return InventoryDropResult.Invalid(InventoryPlacementBlockReason.OutOfBounds);
        if (coverage.UniqueCount == 0)
            return InventoryDropResult.Vacant();

        if (coverage.UniqueCount == 1
            && coverage.FirstGuid != null
            && tree.TryGetItem(coverage.FirstGuid, out var targetNode)
            && tree.TryGetItem(item.Guid, out var sourceNode)
            && CanMergeStack(targetNode.Data, sourceNode.Data, catalog.GetById(targetNode.Data.ItemId))
            && registry.TryGet(coverage.FirstGuid, out var stackTarget))
            return InventoryDropResult.Stack(stackTarget);

        if (CanQuickExchange(
                tree,
                catalog,
                item.Guid,
                containerId,
                origin,
                destDirection,
                originContainerId)
            && coverage.FirstGuid != null
            && registry.TryGet(coverage.FirstGuid, out var overlap))
            return InventoryDropResult.Exchange(overlap);

        return InventoryDropResult.Invalid(InventoryPlacementBlockReason.Occupied);
    }

    [ThreadStatic]
    private static List<InventoryPlacementPlan>? _scratchPlacements;

    /// <summary>True when Tree occupancy allows swapping fully covered occupants with the mover.</summary>
    public static bool CanQuickExchange(
        IInventoryTreeCache tree,
        IItemCatalog catalog,
        string moverGuid,
        string destContainerId,
        Vec2I destOrigin,
        Dir destDirection,
        string? originContainerId)
    {
        var scratch = _scratchPlacements ??= new List<InventoryPlacementPlan>(8);
        scratch.Clear();
        return TryPlanExchange(
            tree,
            catalog,
            moverGuid,
            destContainerId,
            destOrigin,
            destDirection,
            originContainerId,
            scratch,
            out _);
    }

    /// <summary>
    /// Plans swapping fully covered occupants, then placing the mover on dest.
    /// Grid origins map occupants into the mover hole; slot/held origins search dest from the top-left.
    /// </summary>
    public static bool TryPlanExchange(
        IInventoryTreeCache tree,
        IItemCatalog catalog,
        string moverGuid,
        string destContainerId,
        Vec2I destOrigin,
        Dir destDirection,
        string? originContainerId,
        out List<InventoryPlacementPlan> placements,
        out InventoryPlacementBlockReason reason)
    {
        placements = new List<InventoryPlacementPlan>();
        return TryPlanExchange(
            tree,
            catalog,
            moverGuid,
            destContainerId,
            destOrigin,
            destDirection,
            originContainerId,
            placements,
            out reason);
    }

    public static bool TryPlanExchange(
        IInventoryTreeCache tree,
        IItemCatalog catalog,
        string moverGuid,
        string destContainerId,
        Vec2I destOrigin,
        Dir destDirection,
        string? originContainerId,
        List<InventoryPlacementPlan> placements,
        out InventoryPlacementBlockReason reason)
    {
        placements.Clear();
        reason = InventoryPlacementBlockReason.Occupied;
        if (string.IsNullOrEmpty(moverGuid) || !InventoryTreeIds.IsGridContainer(destContainerId))
        {
            reason = InventoryPlacementBlockReason.InvalidCommand;
            return false;
        }
        if (!tree.TryGetItem(moverGuid, out var moverNode))
        {
            reason = InventoryPlacementBlockReason.UnknownItem;
            return false;
        }
        if (!tree.TryGetContainer(destContainerId, out _))
        {
            reason = InventoryPlacementBlockReason.UnknownContainer;
            return false;
        }

        var mover = moverNode.Data;
        var moverDetails = catalog.GetById(mover.ItemId);
        if (moverDetails == null)
        {
            reason = InventoryPlacementBlockReason.UnknownItem;
            return false;
        }

        var destShape = ItemShape.Resolve(moverDetails.Occupancy, mover.OccupancyPatches, destDirection, mover.FlipH, mover.FlipV);
        var destBoard = OccupancyBoard.For(tree, catalog, destContainerId);
        var excludeMover = new HashSet<string>(StringComparer.Ordinal) { moverGuid };
        var overlappedGuids = destBoard.CollectCoverageGuids(destShape.Cells, destOrigin, excludeMover);
        if (overlappedGuids == null)
        {
            reason = InventoryPlacementBlockReason.OutOfBounds;
            return false;
        }
        if (overlappedGuids.Count == 0)
            return false;

        var destCoverage = Occupancy.CellSet(destOrigin, destShape.Cells);
        if (!TryCollectFullyCovered(
                tree,
                catalog,
                overlappedGuids,
                destCoverage,
                out var overlapped,
                out reason))
            return false;

        bool originIsGrid = !string.IsNullOrEmpty(originContainerId)
            && InventoryTreeIds.IsGridContainer(originContainerId);
        if (originIsGrid && !tree.TryGetContainer(originContainerId!, out _))
        {
            reason = InventoryPlacementBlockReason.UnknownContainer;
            return false;
        }

        if (originIsGrid)
        {
            if (!TryPlanHoleExchange(
                    tree,
                    catalog,
                    mover,
                    moverDetails,
                    originContainerId!,
                    destShape.Cells,
                    destOrigin,
                    overlapped,
                    excludeMover,
                    placements))
            {
                reason = InventoryPlacementBlockReason.Occupied;
                placements.Clear();
                return false;
            }
        }
        else if (!TryPlanDestRelocate(destContainerId, destBoard, moverGuid, destCoverage, overlapped, placements))
        {
            reason = InventoryPlacementBlockReason.Occupied;
            placements.Clear();
            return false;
        }

        if (PlannedOccupiesDest(tree, catalog, destContainerId, destCoverage, placements))
        {
            reason = InventoryPlacementBlockReason.Occupied;
            placements.Clear();
            return false;
        }

        var exclude = new HashSet<string>(overlappedGuids, StringComparer.Ordinal) { moverGuid };
        var destBlocked = destBoard.BlockReason(destShape.Cells, destOrigin, exclude);
        if (destBlocked.HasValue)
        {
            reason = destBlocked.Value;
            placements.Clear();
            return false;
        }

        placements.Add(new InventoryPlacementPlan(moverGuid, destContainerId, destOrigin, destDirection));
        reason = InventoryPlacementBlockReason.None;
        return true;
    }

    private static readonly List<InventoryPlacementPlan> CanExchangeScratch = new(8);

    private static bool TryCollectFullyCovered(
        IInventoryTreeCache tree,
        IItemCatalog catalog,
        HashSet<string> overlappedGuids,
        HashSet<Vec2I> destCoverage,
        out List<(string Guid, TetrisItemPersistentData Data, ItemDetails Details)> overlapped,
        out InventoryPlacementBlockReason reason)
    {
        overlapped = new List<(string, TetrisItemPersistentData, ItemDetails)>(overlappedGuids.Count);
        reason = InventoryPlacementBlockReason.Occupied;
        foreach (var guid in overlappedGuids)
        {
            if (!tree.TryGetItem(guid, out var occupantNode))
            {
                reason = InventoryPlacementBlockReason.UnknownItem;
                return false;
            }
            var occupant = occupantNode.Data;
            var details = catalog.GetById(occupant.ItemId);
            if (details == null)
            {
                reason = InventoryPlacementBlockReason.UnknownItem;
                return false;
            }
            var occupantShape = ItemShape.Resolve(details.Occupancy, occupant.OccupancyPatches, occupant);
            foreach (var cell in Occupancy.Cells(occupant.OriginPosition, occupantShape.Cells))
            {
                if (!destCoverage.Contains(cell))
                    return false;
            }
            overlapped.Add((guid, occupant, details));
        }
        reason = InventoryPlacementBlockReason.None;
        return true;
    }

    private static bool TryPlanHoleExchange(
        IInventoryTreeCache tree,
        IItemCatalog catalog,
        TetrisItemPersistentData mover,
        ItemDetails moverDetails,
        string originId,
        IReadOnlyList<Vec2I> destShape,
        Vec2I destOrigin,
        List<(string Guid, TetrisItemPersistentData Data, ItemDetails Details)> overlapped,
        ISet<string> excludeMover,
        List<InventoryPlacementPlan> placements)
    {
        var holeShape = ItemShape.Resolve(moverDetails.Occupancy, mover.OccupancyPatches, mover);
        var holeOrigin = mover.OriginPosition;
        var hole = Occupancy.CellSet(holeOrigin, holeShape.Cells);
        var originBoard = OccupancyBoard.For(tree, catalog, originId);
        var remainingHole = new HashSet<Vec2I>(hole);
        var leftover = new List<(string Guid, ItemOccupancy? Occ, IReadOnlyList<OccupancyPatch>? Patches, Dir Preferred, bool FlipH, bool FlipV)>();
        foreach (var occupant in overlapped)
        {
            var occupantShape = ItemShape.Resolve(
                occupant.Details.Occupancy,
                occupant.Data.OccupancyPatches,
                occupant.Data);
            var occupantCells = Occupancy.Cells(occupant.Data.OriginPosition, occupantShape.Cells);
            var mapping = MapCoveredCellsToHole(destShape, destOrigin, holeShape.Cells, holeOrigin, occupantCells);
            if (TryFitMappedShape(
                    new HashSet<Vec2I>(mapping.Values),
                    hole,
                    originBoard,
                    remainingHole,
                    occupant.Details.Occupancy,
                    occupant.Data.OccupancyPatches,
                    occupant.Data.Direction,
                    occupant.Data.FlipH,
                    occupant.Data.FlipV,
                    excludeMover,
                    out var fittedDir,
                    out var fittedOrigin))
            {
                var fitted = ItemShape.Resolve(
                    occupant.Details.Occupancy,
                    occupant.Data.OccupancyPatches,
                    fittedDir,
                    occupant.Data.FlipH,
                    occupant.Data.FlipV);
                foreach (var cell in Occupancy.Cells(fittedOrigin, fitted.Cells))
                    remainingHole.Remove(cell);
                placements.Add(new InventoryPlacementPlan(occupant.Guid, originId, fittedOrigin, fittedDir));
            }
            else
            {
                leftover.Add((
                    occupant.Guid,
                    occupant.Details.Occupancy,
                    occupant.Data.OccupancyPatches,
                    occupant.Data.Direction,
                    occupant.Data.FlipH,
                    occupant.Data.FlipV));
            }
        }

        return leftover.Count == 0
            || TryPackInCells(
                leftover,
                remainingHole,
                OrderCellsByOffset(hole, originBoard.Width),
                originId,
                originBoard,
                excludeMover,
                placements);
    }

    private static bool TryPlanDestRelocate(
        string destContainerId,
        OccupancyBoard destBoard,
        string moverGuid,
        HashSet<Vec2I> destCoverage,
        List<(string Guid, TetrisItemPersistentData Data, ItemDetails Details)> overlapped,
        List<InventoryPlacementPlan> placements)
    {
        var exclude = new HashSet<string>(overlapped.Count + 1, StringComparer.Ordinal) { moverGuid };
        for (int i = 0; i < overlapped.Count; i++)
            exclude.Add(overlapped[i].Guid);
        var leftover = new List<(string Guid, ItemOccupancy? Occ, IReadOnlyList<OccupancyPatch>? Patches, Dir Preferred, bool FlipH, bool FlipV)>(overlapped.Count);
        foreach (var occupant in overlapped)
            leftover.Add((occupant.Guid, occupant.Details.Occupancy, occupant.Data.OccupancyPatches, occupant.Data.Direction, occupant.Data.FlipH, occupant.Data.FlipV));
        var available = CollectFreeCells(destBoard, destCoverage, exclude);
        return leftover.Count == 0
            || TryPackInCells(leftover, new HashSet<Vec2I>(available), available, destContainerId, destBoard, exclude, placements);
    }

    private static List<Vec2I> CollectFreeCells(OccupancyBoard board, HashSet<Vec2I> reserved, ISet<string>? exclude)
    {
        var cells = new List<Vec2I>();
        for (int row = 0; row < board.Height; row++)
        {
            for (int column = 0; column < board.Width; column++)
            {
                if (board.GetOccupant(column, row, exclude) != null) continue;
                var cell = new Vec2I(column, row);
                if (reserved.Contains(cell)) continue;
                cells.Add(cell);
            }
        }
        return cells;
    }

    /// <summary>
    /// Same-grid slide: after overlapped items move into the mover hole, dest cells must still be empty.
    /// Matches the post-place HasItem check from the VM exchange.
    /// </summary>
    private static bool PlannedOccupiesDest(
        IInventoryTreeCache tree,
        IItemCatalog catalog,
        string destContainerId,
        HashSet<Vec2I> destCoverage,
        List<InventoryPlacementPlan> placements)
    {
        for (int i = 0; i < placements.Count; i++)
        {
            var plan = placements[i];
            if (plan.ContainerId != destContainerId) continue;
            if (!tree.TryGetItem(plan.ItemGuid, out var node)) continue;
            var details = catalog.GetById(node.Data.ItemId);
            var shape = ItemShape.Resolve(details?.Occupancy, node.Data.OccupancyPatches, plan.Direction, node.Data.FlipH, node.Data.FlipV);
            foreach (var cell in Occupancy.Cells(plan.Origin, shape.Cells))
            {
                if (destCoverage.Contains(cell))
                    return true;
            }
        }
        return false;
    }

    private static Dictionary<Vec2I, Vec2I> MapCoveredCellsToHole(
        IReadOnlyList<Vec2I> destShape,
        Vec2I destOrigin,
        IReadOnlyList<Vec2I> holeShape,
        Vec2I holeOrigin,
        IReadOnlyList<Vec2I> occupantWorldCells)
    {
        var mapping = new Dictionary<Vec2I, Vec2I>();
        foreach (var cell in occupantWorldCells)
        {
            var relative = cell - destOrigin;
            int index = -1;
            for (int k = 0; k < destShape.Count; k++)
            {
                if (destShape[k] == relative)
                {
                    index = k;
                    break;
                }
            }
            if (index >= 0 && index < holeShape.Count)
                mapping[cell] = holeOrigin + holeShape[index];
        }
        return mapping;
    }

    private static bool TryFitMappedShape(
        HashSet<Vec2I> targetCells,
        HashSet<Vec2I> hole,
        OccupancyBoard originBoard,
        HashSet<Vec2I> remainingHole,
        ItemOccupancy? occupancy,
        IReadOnlyList<OccupancyPatch>? patches,
        Dir preferred,
        bool flipH,
        bool flipV,
        ISet<string>? exclude,
        out Dir fittedDir,
        out Vec2I fittedOrigin)
    {
        fittedDir = preferred;
        fittedOrigin = Vec2I.Zero;
        if (targetCells.Count == 0) return false;
        foreach (var cell in targetCells)
        {
            if (!hole.Contains(cell) || !remainingHole.Contains(cell)) return false;
            if (!originBoard.PositionCheck(cell.X, cell.Y)) return false;
            if (originBoard.GetOccupant(cell.X, cell.Y, exclude) != null) return false;
        }

        var t0 = MinCellByOffset(targetCells, originBoard.Width);
        foreach (Dir dir in DirUtil.PreferThenOthers(preferred))
        {
            var resolved = ItemShape.Resolve(occupancy, patches, dir, flipH, flipV);
            if (resolved.CellCount != targetCells.Count) continue;
            for (int i = 0; i < resolved.CellCount; i++)
            {
                var anchor = t0 - resolved.Cells[i];
                bool match = true;
                foreach (var point in resolved.Cells)
                {
                    if (!targetCells.Contains(anchor + point))
                    {
                        match = false;
                        break;
                    }
                }
                if (match && originBoard.CanPlace(resolved.Cells, anchor, exclude))
                {
                    fittedDir = dir;
                    fittedOrigin = anchor;
                    return true;
                }
            }
        }
        return false;
    }

    private static bool TryPackInCells(
        List<(string Guid, ItemOccupancy? Occ, IReadOnlyList<OccupancyPatch>? Patches, Dir Preferred, bool FlipH, bool FlipV)> items,
        HashSet<Vec2I> remaining,
        List<Vec2I> availableOrdered,
        string containerId,
        OccupancyBoard board,
        ISet<string>? exclude,
        List<InventoryPlacementPlan> dest)
    {
        SortByCellCountDescending(items);
        foreach (var item in items)
        {
            bool placed = false;
            foreach (Dir dir in DirUtil.PreferThenOthers(item.Preferred))
            {
                var resolved = ItemShape.Resolve(item.Occ, item.Patches, dir, item.FlipH, item.FlipV);
                foreach (var cell in availableOrdered)
                {
                    if (!remaining.Contains(cell)) continue;
                    if (!board.CanPlace(resolved.Cells, cell, exclude)) continue;
                    var coverage = Occupancy.Cells(cell, resolved.Cells);
                    bool hasOutOfBounds = false;
                    for (int ci = 0; ci < coverage.Count; ci++)
                    {
                        if (!remaining.Contains(coverage[ci]))
                        {
                            hasOutOfBounds = true;
                            break;
                        }
                    }
                    if (hasOutOfBounds) continue;
                    dest.Add(new InventoryPlacementPlan(item.Guid, containerId, cell, dir));
                    for (int ci = 0; ci < coverage.Count; ci++)
                        remaining.Remove(coverage[ci]);
                    placed = true;
                    break;
                }
                if (placed) break;
            }
            if (!placed) return false;
        }
        return true;
    }

    private static int CellCount(ItemOccupancy? occupancy, IReadOnlyList<OccupancyPatch>? patches, Dir dir, bool flipH, bool flipV)
        => ItemShape.Resolve(occupancy, patches, dir, flipH, flipV).CellCount;

    private static List<(string Guid, ItemOccupancy? Occ, IReadOnlyList<OccupancyPatch>? Patches, Vec2I Origin, Dir Preferred, bool FlipH, bool FlipV)> RankByCellCount(
        IReadOnlyList<(string Guid, ItemOccupancy? Occ, IReadOnlyList<OccupancyPatch>? Patches, Vec2I Origin, Dir Preferred, bool FlipH, bool FlipV)> items)
    {
        var ranked = new List<(int Count, int Order, (string Guid, ItemOccupancy? Occ, IReadOnlyList<OccupancyPatch>? Patches, Vec2I Origin, Dir Preferred, bool FlipH, bool FlipV) Entry)>(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            ranked.Add((CellCount(item.Occ, item.Patches, item.Preferred, item.FlipH, item.FlipV), i, item));
        }
        ranked.Sort(CompareCountThenOrder);
        var result = new List<(string Guid, ItemOccupancy? Occ, IReadOnlyList<OccupancyPatch>? Patches, Vec2I Origin, Dir Preferred, bool FlipH, bool FlipV)>(ranked.Count);
        for (int i = 0; i < ranked.Count; i++)
            result.Add(ranked[i].Entry);
        return result;
    }

    private static List<(string Guid, IReadOnlyList<Vec2I> Shape, Vec2I Origin)> RankByBoundsArea(
        IReadOnlyList<(string Guid, IReadOnlyList<Vec2I> Shape, Vec2I Origin)> items)
    {
        var ranked = new List<(int Count, int Order, (string Guid, IReadOnlyList<Vec2I> Shape, Vec2I Origin) Entry)>(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            ranked.Add((BoundsArea(item.Shape), i, item));
        }
        ranked.Sort(CompareCountThenOrder);
        var result = new List<(string Guid, IReadOnlyList<Vec2I> Shape, Vec2I Origin)>(ranked.Count);
        for (int i = 0; i < ranked.Count; i++)
            result.Add(ranked[i].Entry);
        return result;
    }

    private static void SortByCellCountDescending(
        List<(string Guid, ItemOccupancy? Occ, IReadOnlyList<OccupancyPatch>? Patches, Dir Preferred, bool FlipH, bool FlipV)> items)
    {
        if (items.Count <= 1) return;
        var ranked = new List<(int Count, int Order, (string Guid, ItemOccupancy? Occ, IReadOnlyList<OccupancyPatch>? Patches, Dir Preferred, bool FlipH, bool FlipV) Entry)>(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            ranked.Add((CellCount(item.Occ, item.Patches, item.Preferred, item.FlipH, item.FlipV), i, item));
        }
        ranked.Sort(CompareCountThenOrder);
        items.Clear();
        for (int i = 0; i < ranked.Count; i++)
            items.Add(ranked[i].Entry);
    }

    private static List<(string Guid, int ItemId, int Rarity, int SlotType, ItemOccupancy? Occ, IReadOnlyList<OccupancyPatch>? Patches, Vec2I Origin, Dir Preferred, bool FlipH, bool FlipV)> RankByStrategy(
        IReadOnlyList<(string Guid, int ItemId, int Rarity, int SlotType, ItemOccupancy? Occ, IReadOnlyList<OccupancyPatch>? Patches, Vec2I Origin, Dir Preferred, bool FlipH, bool FlipV)> items,
        InventorySortStrategy strategy)
    {
        var ranked = new List<(int Count, int Rarity, int SlotType, int ItemId, int Order, (string Guid, int ItemId, int Rarity, int SlotType, ItemOccupancy? Occ, IReadOnlyList<OccupancyPatch>? Patches, Vec2I Origin, Dir Preferred, bool FlipH, bool FlipV) Entry)>(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            int count = CellCount(item.Occ, item.Patches, item.Preferred, item.FlipH, item.FlipV);
            ranked.Add((count, item.Rarity, item.SlotType, item.ItemId, i, item));
        }

        switch (strategy)
        {
            case InventorySortStrategy.SlotType:
                ranked.Sort(CompareSlotType);
                break;
            case InventorySortStrategy.Rarity:
                ranked.Sort(CompareRarity);
                break;
            case InventorySortStrategy.ItemId:
                ranked.Sort(CompareItemId);
                break;
            default:
                ranked.Sort(CompareArea);
                break;
        }

        var result = new List<(string Guid, int ItemId, int Rarity, int SlotType, ItemOccupancy? Occ, IReadOnlyList<OccupancyPatch>? Patches, Vec2I Origin, Dir Preferred, bool FlipH, bool FlipV)>(ranked.Count);
        for (int i = 0; i < ranked.Count; i++)
            result.Add(ranked[i].Entry);
        return result;
    }

    private static int CompareArea<T>(
        (int Count, int Rarity, int SlotType, int ItemId, int Order, T Entry) left,
        (int Count, int Rarity, int SlotType, int ItemId, int Order, T Entry) right)
    {
        int byCount = right.Count.CompareTo(left.Count);
        return byCount != 0 ? byCount : left.Order.CompareTo(right.Order);
    }

    private static int CompareSlotType<T>(
        (int Count, int Rarity, int SlotType, int ItemId, int Order, T Entry) left,
        (int Count, int Rarity, int SlotType, int ItemId, int Order, T Entry) right)
    {
        int byType = left.SlotType.CompareTo(right.SlotType);
        if (byType != 0) return byType;
        int byCount = right.Count.CompareTo(left.Count);
        return byCount != 0 ? byCount : left.Order.CompareTo(right.Order);
    }

    private static int CompareRarity<T>(
        (int Count, int Rarity, int SlotType, int ItemId, int Order, T Entry) left,
        (int Count, int Rarity, int SlotType, int ItemId, int Order, T Entry) right)
    {
        int byRarity = right.Rarity.CompareTo(left.Rarity);
        if (byRarity != 0) return byRarity;
        int byCount = right.Count.CompareTo(left.Count);
        return byCount != 0 ? byCount : left.Order.CompareTo(right.Order);
    }

    private static int CompareItemId<T>(
        (int Count, int Rarity, int SlotType, int ItemId, int Order, T Entry) left,
        (int Count, int Rarity, int SlotType, int ItemId, int Order, T Entry) right)
    {
        int byId = left.ItemId.CompareTo(right.ItemId);
        if (byId != 0) return byId;
        int byCount = right.Count.CompareTo(left.Count);
        return byCount != 0 ? byCount : left.Order.CompareTo(right.Order);
    }

    private static int CompareCountThenOrder<T>((int Count, int Order, T Entry) left, (int Count, int Order, T Entry) right)
    {
        int byCount = right.Count.CompareTo(left.Count);
        return byCount != 0 ? byCount : left.Order.CompareTo(right.Order);
    }

    private static List<Vec2I> OrderCellsByOffset(HashSet<Vec2I> cells, int width)
    {
        var ordered = new List<Vec2I>(cells.Count);
        if (cells.Count == 0) return ordered;
        var offsets = new int[cells.Count];
        int n = 0;
        foreach (var cell in cells)
            offsets[n++] = cell.Y * width + cell.X;
        Array.Sort(offsets);
        for (int i = 0; i < offsets.Length; i++)
        {
            int offset = offsets[i];
            ordered.Add(new Vec2I(offset % width, offset / width));
        }
        return ordered;
    }

    private static Vec2I MinCellByOffset(HashSet<Vec2I> cells, int width)
    {
        int minOffset = int.MaxValue;
        foreach (var cell in cells)
        {
            int offset = cell.Y * width + cell.X;
            if (offset < minOffset)
                minOffset = offset;
        }
        return new Vec2I(minOffset % width, minOffset / width);
    }
}
