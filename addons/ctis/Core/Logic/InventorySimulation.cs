using TetrisCoordLib.Core.Math;

namespace Ctis.Core;

/// <summary>Validates a command against a Tree snapshot and commits once on success.</summary>
public static class InventorySimulation
{
    /// <summary>Mutates the tree for one command and bumps revision on success.</summary>
    public static InventoryCommandResult Apply(
        InventoryCommand command,
        IInventoryTreeCache tree,
        IItemCatalog catalog,
        EquipmentLayout layout)
    {
        if (!string.IsNullOrEmpty(command.CommandId)
            && command.CommandId == tree.LastAppliedCommandId)
            return InventoryCommandResult.Success();
        if (command.ExpectedRevision != tree.Revision)
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.RevisionMismatch);

        var result = command.Kind switch
        {
            InventoryCommandKind.Place => Place(command, tree, catalog),
            InventoryCommandKind.MoveToSlot => MoveToSlot(command, tree, catalog, layout),
            InventoryCommandKind.Lift => Lift(command, tree),
            InventoryCommandKind.Stack => Stack(command, tree, catalog),
            InventoryCommandKind.Split => Split(command, tree, catalog),
            InventoryCommandKind.ResizeContainer => Resize(command, tree, catalog),
            InventoryCommandKind.PatchOccupancy => Patch(command, tree, catalog),
            InventoryCommandKind.RemoveOccupancyPatch => RemovePatch(command, tree, catalog),
            InventoryCommandKind.Exchange => Exchange(command, tree, catalog),
            InventoryCommandKind.Flip => Flip(command, tree, catalog),
            InventoryCommandKind.OrganizeContainer => OrganizeContainer(command, tree, catalog),
            InventoryCommandKind.OrganizeItemGrids => OrganizeItemGrids(command, tree, catalog),
            _ => InventoryCommandResult.Fail(InventoryPlacementBlockReason.InvalidCommand)
        };
        return result;
    }

    /// <summary>Catalog occupancy for a persisted item at its stored facing.</summary>
    public static IReadOnlyList<Vec2I> ResolveCells(IItemCatalog catalog, TetrisItemPersistentData data)
        => ItemShape.Resolve(catalog.GetById(data.ItemId)?.Occupancy, data.OccupancyPatches, data).Cells;

    private static InventoryCommandResult Place(
        InventoryCommand command,
        IInventoryTreeCache tree,
        IItemCatalog catalog)
    {
        using var traceScope = CtisTrace.Scope("Simulation.Place");
        if (string.IsNullOrEmpty(command.ItemGuid) || string.IsNullOrEmpty(command.ContainerId))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.InvalidCommand);
        if (!InventoryTreeIds.IsGridContainer(command.ContainerId) || !tree.TryGetContainer(command.ContainerId, out _))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.UnknownContainer);
        if (!tree.TryGetItem(command.ItemGuid, out var node))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.UnknownItem);
        if (catalog.GetById(node.Data.ItemId) == null)
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.UnknownItem);
        if (InventoryLogic.IsPlacingIntoSelfOwnedContainer(command.ItemGuid, command.ContainerId, tree))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.SelfOwnedContainer);

        var data = node.Data.Clone();
        if (command.HasDirection)
            data.Direction = command.Direction;
        data.OriginPosition = command.Origin;
        data.IsOnSlot = false;
        data.SlotIndex = -1;

        var shape = ItemShape.Resolve(catalog.GetById(data.ItemId)?.Occupancy, data.OccupancyPatches, data);
        var exclude = new HashSet<string>(StringComparer.Ordinal) { data.ItemGuid };
        var blocked = OccupancyBoard.For(tree, catalog, command.ContainerId)
            .BlockReason(shape.Cells, command.Origin, exclude);
        if (blocked.HasValue)
            return InventoryCommandResult.Fail(blocked.Value);

        tree.PlaceItem(command.ContainerId, data);
        return CommitSuccess(tree, command.CommandId);
    }

    private static InventoryCommandResult MoveToSlot(
        InventoryCommand command,
        IInventoryTreeCache tree,
        IItemCatalog catalog,
        EquipmentLayout layout)
    {
        using var traceScope = CtisTrace.Scope("Simulation.MoveToSlot");
        if (string.IsNullOrEmpty(command.ItemGuid) || command.SlotIndex < 0)
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.InvalidCommand);
        if (!tree.TryGetItem(command.ItemGuid, out var node))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.UnknownItem);

        var spec = layout.Find(command.SlotIndex);
        if (spec == null)
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.UnknownContainer);

        var details = catalog.GetById(node.Data.ItemId);
        if (details == null)
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.UnknownItem);
        if (details.SlotType != spec.SlotType)
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.SlotTypeMismatch);

        var slotId = InventoryTreeIds.Slot(command.SlotIndex);
        if (InventoryLogic.IsPlacingIntoSelfOwnedContainer(command.ItemGuid, slotId, tree))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.SelfOwnedContainer);

        foreach (var occupant in tree.GetItems(slotId))
        {
            if (occupant.ItemGuid != command.ItemGuid)
                return InventoryCommandResult.Fail(InventoryPlacementBlockReason.SlotOccupied);
        }

        var data = node.Data.Clone();
        data.IsOnSlot = true;
        data.SlotIndex = command.SlotIndex;
        data.OriginPosition = Vec2I.Zero;
        tree.PlaceItem(slotId, data);
        return CommitSuccess(tree, command.CommandId);
    }

    private static InventoryCommandResult Lift(InventoryCommand command, IInventoryTreeCache tree)
    {
        using var traceScope = CtisTrace.Scope("Simulation.Lift");
        if (string.IsNullOrEmpty(command.ItemGuid))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.InvalidCommand);
        if (!tree.TryGetItem(command.ItemGuid, out var node))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.UnknownItem);

        var data = node.Data.Clone();
        data.IsOnSlot = false;
        data.SlotIndex = -1;
        tree.PlaceItem(InventoryTreeIds.Held(command.ActorId), data);
        return CommitSuccess(tree, command.CommandId);
    }

    private static InventoryCommandResult Stack(
        InventoryCommand command,
        IInventoryTreeCache tree,
        IItemCatalog catalog)
    {
        using var traceScope = CtisTrace.Scope("Simulation.Stack");
        if (string.IsNullOrEmpty(command.ItemGuid) || string.IsNullOrEmpty(command.TargetGuid))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.InvalidCommand);
        if (!tree.TryGetItem(command.ItemGuid, out var sourceNode)
            || !tree.TryGetItem(command.TargetGuid, out var targetNode))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.UnknownItem);

        var source = sourceNode.Data.Clone();
        var target = targetNode.Data.Clone();
        var details = catalog.GetById(target.ItemId);
        if (!InventoryLogic.CanMergeStack(target, source, details))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.Occupied);

        int transfer = Math.Min(source.Stack, details!.MaxStack - target.Stack);
        target.Stack += transfer;
        source.Stack -= transfer;

        PersistInPlace(tree, command.TargetGuid, target);
        if (source.Stack <= 0)
            tree.RemoveItem(command.ItemGuid);
        else
            PersistInPlace(tree, command.ItemGuid, source);
        return CommitSuccess(tree, command.CommandId);
    }

    private static InventoryCommandResult Split(
        InventoryCommand command,
        IInventoryTreeCache tree,
        IItemCatalog catalog)
    {
        using var traceScope = CtisTrace.Scope("Simulation.Split");
        if (string.IsNullOrEmpty(command.ItemGuid) || command.Amount <= 0)
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.InvalidCommand);
        if (string.IsNullOrEmpty(command.NewItemGuid) || tree.TryGetItem(command.NewItemGuid, out _))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.DuplicateGuid);
        if (!tree.TryGetItem(command.ItemGuid, out var node))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.UnknownItem);

        var source = node.Data.Clone();
        if (source.Stack <= 1 || command.Amount >= source.Stack)
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.Occupied);

        var containerId = tree.GetContainerIdByItem(command.ItemGuid);
        if (string.IsNullOrEmpty(containerId) || !InventoryTreeIds.IsGridContainer(containerId))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.Occupied);

        var details = catalog.GetById(source.ItemId);
        var shape = ItemShape.Resolve(details?.Occupancy, source.OccupancyPatches, source);
        var board = OccupancyBoard.For(tree, catalog, containerId);
        Dir splitDir = source.Direction;
        if (!board.TryFindAdjacentOrigin(
                shape.Cells,
                shape.Width,
                shape.Height,
                source.OriginPosition,
                shape.Width,
                shape.Height,
                out var origin)
            && !board.TryFindFreeOrigin(
                details?.Occupancy,
                source.OccupancyPatches,
                source.Direction,
                source.FlipH,
                source.FlipV,
                out origin,
                out splitDir))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.Occupied);

        var split = source.Clone();
        split.ItemGuid = command.NewItemGuid;
        split.OriginPosition = origin;
        split.Direction = splitDir;
        split.Stack = command.Amount;
        source.Stack -= command.Amount;
        tree.PlaceItem(containerId, source);
        tree.PlaceItem(containerId, split);
        return CommitSuccess(tree, command.CommandId);
    }

    private static InventoryCommandResult Resize(
        InventoryCommand command,
        IInventoryTreeCache tree,
        IItemCatalog catalog)
    {
        using var traceScope = CtisTrace.Scope("Simulation.Resize");
        if (string.IsNullOrEmpty(command.ContainerId) || !InventoryTreeIds.IsGridContainer(command.ContainerId))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.InvalidCommand);

        int width = Math.Clamp(command.Width, CtisSettings.GridMinCells, CtisSettings.GridMaxColumns);
        int height = Math.Clamp(command.Height, CtisSettings.GridMinCells, CtisSettings.GridMaxRows);
        if (!tree.TryGetContainer(command.ContainerId, out var node))
        {
            tree.SetContainerConfig(
                command.ContainerId,
                width,
                height,
                ResolveTile(command.TileWidth, CtisSettings.GridTileSizeWidth),
                ResolveTile(command.TileHeight, CtisSettings.GridTileSizeHeight));
            return CommitSuccess(tree, command.CommandId);
        }

        float tileWidth = ResolveTile(command.TileWidth, node.LocalGridTileSizeWidth);
        float tileHeight = ResolveTile(command.TileHeight, node.LocalGridTileSizeHeight);
        bool cellsChanged = width != node.GridSizeWidth || height != node.GridSizeHeight;
        bool tilesChanged = tileWidth != node.LocalGridTileSizeWidth || tileHeight != node.LocalGridTileSizeHeight;
        if (!cellsChanged && !tilesChanged)
            return SucceedIdempotent(tree, command.CommandId);

        if (!cellsChanged)
        {
            tree.SetContainerConfig(
                command.ContainerId, width, height, tileWidth, tileHeight);
            return CommitSuccess(tree, command.CommandId);
        }

        var sourceItems = tree.GetItems(command.ContainerId);
        var shaped = new List<(string Guid, ItemOccupancy? Occ, IReadOnlyList<OccupancyPatch>? Patches, Vec2I Origin, Dir Preferred, bool FlipH, bool FlipV)>();
        var byGuid = new Dictionary<string, TetrisItemPersistentData>(StringComparer.Ordinal);
        foreach (var source in sourceItems)
        {
            var data = source.Clone();
            byGuid[data.ItemGuid] = data;
            shaped.Add((
                data.ItemGuid,
                catalog.GetById(data.ItemId)?.Occupancy,
                data.OccupancyPatches,
                data.OriginPosition,
                data.Direction,
                data.FlipH,
                data.FlipV));
        }
        if (!InventoryLogic.TryPlanPack(width, height, shaped, out var packed))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.Occupied);

        tree.SetContainerConfig(command.ContainerId, width, height, tileWidth, tileHeight);
        foreach (var entry in packed)
        {
            var data = byGuid[entry.Guid];
            data.OriginPosition = entry.Origin;
            data.Direction = entry.Direction;
            tree.PlaceItem(command.ContainerId, data);
        }
        return CommitSuccess(tree, command.CommandId);
    }

    private static float ResolveTile(float requested, float fallback)
        => requested <= 0f
            ? fallback
            : Math.Clamp(requested, CtisSettings.GridMinTileSize, CtisSettings.GridMaxTileSize);

    private static InventoryCommandResult Patch(
        InventoryCommand command,
        IInventoryTreeCache tree,
        IItemCatalog catalog)
    {
        using var traceScope = CtisTrace.Scope("Simulation.Patch");
        if (string.IsNullOrEmpty(command.ItemGuid) || string.IsNullOrEmpty(command.PatchKey))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.InvalidCommand);
        if (!tree.TryGetItem(command.ItemGuid, out var node))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.UnknownItem);

        var data = node.Data.Clone();
        var patches = new List<OccupancyPatch>();
        if (data.OccupancyPatches != null)
        {
            for (int i = 0; i < data.OccupancyPatches.Count; i++)
            {
                var p = data.OccupancyPatches[i];
                if (p.Key != command.PatchKey)
                    patches.Add(p.Clone());
            }
        }
        patches.Add(new OccupancyPatch
        {
            Key = command.PatchKey,
            Add = command.Add != null ? new List<Vec2I>(command.Add) : new List<Vec2I>(),
            Remove = command.Remove != null ? new List<Vec2I>(command.Remove) : new List<Vec2I>()
        });
        data.OccupancyPatches = patches;
        var blocked = OccupancyBlockedByShape(tree, catalog, command.ItemGuid, data);
        if (blocked.HasValue)
            return InventoryCommandResult.Fail(blocked.Value);

        PersistInPlace(tree, command.ItemGuid, data);
        return CommitSuccess(tree, command.CommandId);
    }

    private static InventoryCommandResult RemovePatch(
        InventoryCommand command,
        IInventoryTreeCache tree,
        IItemCatalog catalog)
    {
        using var traceScope = CtisTrace.Scope("Simulation.Patch");
        if (string.IsNullOrEmpty(command.ItemGuid) || string.IsNullOrEmpty(command.PatchKey))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.InvalidCommand);
        if (!tree.TryGetItem(command.ItemGuid, out var node) || node.Data.OccupancyPatches == null)
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.UnknownItem);

        var data = node.Data.Clone();
        if (data.OccupancyPatches == null)
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.Occupied);

        int removed = 0;
        for (int i = data.OccupancyPatches.Count - 1; i >= 0; i--)
        {
            if (data.OccupancyPatches[i].Key == command.PatchKey)
            {
                data.OccupancyPatches.RemoveAt(i);
                removed++;
            }
        }
        if (removed == 0)
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.Occupied);
        if (data.OccupancyPatches.Count == 0)
            data.OccupancyPatches = null;
        var blocked = OccupancyBlockedByShape(tree, catalog, command.ItemGuid, data);
        if (blocked.HasValue)
            return InventoryCommandResult.Fail(blocked.Value);

        PersistInPlace(tree, command.ItemGuid, data);
        return CommitSuccess(tree, command.CommandId);
    }

    private static InventoryCommandResult Flip(
        InventoryCommand command,
        IInventoryTreeCache tree,
        IItemCatalog catalog)
    {
        using var traceScope = CtisTrace.Scope("Simulation.Flip");
        if (string.IsNullOrEmpty(command.ItemGuid))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.InvalidCommand);
        if (!tree.TryGetItem(command.ItemGuid, out var node))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.UnknownItem);

        var containerId = tree.GetContainerIdByItem(command.ItemGuid);
        if (string.IsNullOrEmpty(containerId) || !InventoryTreeIds.IsGridContainer(containerId))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.InvalidCommand);

        var data = node.Data.Clone();
        data.FlipH = command.FlipH;
        data.FlipV = command.FlipV;
        var blocked = OccupancyBlockedByShape(tree, catalog, command.ItemGuid, data);
        if (blocked.HasValue)
            return InventoryCommandResult.Fail(blocked.Value);

        PersistInPlace(tree, command.ItemGuid, data);
        return CommitSuccess(tree, command.CommandId);
    }

    private static InventoryCommandResult Exchange(
        InventoryCommand command,
        IInventoryTreeCache tree,
        IItemCatalog catalog)
    {
        using var traceScope = CtisTrace.Scope("Simulation.Exchange");
        if (string.IsNullOrEmpty(command.ItemGuid) || string.IsNullOrEmpty(command.ContainerId))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.InvalidCommand);

        if (!InventoryLogic.TryPlanExchange(
                tree,
                catalog,
                command.ItemGuid,
                command.ContainerId,
                command.Origin,
                command.HasDirection ? command.Direction : Dir.Down,
                command.OriginContainerId,
                out var plans,
                out var reason))
            return InventoryCommandResult.Fail(reason);

        var writes = new List<(string ContainerId, TetrisItemPersistentData Data)>(plans.Count);
        foreach (var plan in plans)
        {
            if (!tree.TryGetItem(plan.ItemGuid, out var node))
                return InventoryCommandResult.Fail(InventoryPlacementBlockReason.UnknownItem);
            var data = node.Data.Clone();
            data.OriginPosition = plan.Origin;
            data.Direction = plan.Direction;
            data.IsOnSlot = false;
            data.SlotIndex = -1;
            writes.Add((plan.ContainerId, data));
        }

        foreach (var write in writes)
            tree.PlaceItem(write.ContainerId, write.Data);
        return CommitSuccess(tree, command.CommandId);
    }

    private static InventoryPlacementBlockReason? OccupancyBlockedByShape(
        IInventoryTreeCache tree,
        IItemCatalog catalog,
        string itemGuid,
        TetrisItemPersistentData data)
    {
        var containerId = tree.GetContainerIdByItem(itemGuid);
        if (string.IsNullOrEmpty(containerId) || !InventoryTreeIds.IsGridContainer(containerId))
            return null;
        var shape = ItemShape.Resolve(catalog.GetById(data.ItemId)?.Occupancy, data.OccupancyPatches, data);
        var exclude = new HashSet<string>(StringComparer.Ordinal) { itemGuid };
        return OccupancyBoard.For(tree, catalog, containerId)
            .BlockReason(shape.Cells, data.OriginPosition, exclude);
    }

    private static void PersistInPlace(IInventoryTreeCache tree, string itemGuid, TetrisItemPersistentData data)
    {
        var containerId = tree.GetContainerIdByItem(itemGuid);
        if (!string.IsNullOrEmpty(containerId))
            tree.PlaceItem(containerId, data);
        else
            tree.UpsertItem(data);
    }

    private static InventoryCommandResult OrganizeContainer(
        InventoryCommand command,
        IInventoryTreeCache tree,
        IItemCatalog catalog)
    {
        using var traceScope = CtisTrace.Scope("Simulation.Organize");
        if (string.IsNullOrEmpty(command.ContainerId) || !InventoryTreeIds.IsGridContainer(command.ContainerId))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.InvalidCommand);
        if (!tree.TryGetContainer(command.ContainerId, out var node))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.UnknownContainer);

        var rawItems = tree.GetItems(command.ContainerId);
        var items = new List<TetrisItemPersistentData>();
        foreach (var it in rawItems)
            items.Add(it.Clone());

        if (items.Count <= 1)
            return CommitSuccess(tree, command.CommandId);

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

        if (!InventoryLogic.TryPlanPack(node.GridSizeWidth, node.GridSizeHeight, shaped, command.SortStrategy, out var packed))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.Occupied);

        for (int i = 0; i < packed.Count; i++)
        {
            var entry = packed[i];
            for (int j = 0; j < items.Count; j++)
            {
                var data = items[j];
                if (data.ItemGuid == entry.Guid)
                {
                    data.OriginPosition = entry.Origin;
                    data.Direction = entry.Direction;
                    tree.PlaceItem(command.ContainerId, data);
                    break;
                }
            }
        }

        return CommitSuccess(tree, command.CommandId);
    }

    private static InventoryCommandResult OrganizeItemGrids(
        InventoryCommand command,
        IInventoryTreeCache tree,
        IItemCatalog catalog)
    {
        using var traceScope = CtisTrace.Scope("Simulation.Organize");
        if (string.IsNullOrEmpty(command.ItemGuid))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.InvalidCommand);

        var prefix = command.ItemGuid + ":";
        var subGridContainers = new List<(string GridGuid, int Width, int Height)>();
        var allItems = new List<TetrisItemPersistentData>();

        foreach (var containerId in tree.GetAllContainerIds())
        {
            if (containerId.StartsWith(prefix, StringComparison.Ordinal)
                && tree.TryGetContainer(containerId, out var containerNode))
            {
                subGridContainers.Add((containerId, containerNode.GridSizeWidth, containerNode.GridSizeHeight));
                foreach (var it in tree.GetItems(containerId))
                    allItems.Add(it.Clone());
            }
        }

        if (subGridContainers.Count == 0)
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.UnknownContainer);
        if (allItems.Count == 0)
            return CommitSuccess(tree, command.CommandId);

        if (!InventoryLogic.TryPlanMultiGridPack(subGridContainers, allItems, catalog, command.SortStrategy, out var placements))
            return InventoryCommandResult.Fail(InventoryPlacementBlockReason.Occupied);

        for (int i = 0; i < subGridContainers.Count; i++)
            tree.ClearContainerItems(subGridContainers[i].GridGuid);

        for (int i = 0; i < placements.Count; i++)
        {
            var plan = placements[i];
            for (int j = 0; j < allItems.Count; j++)
            {
                var data = allItems[j];
                if (data.ItemGuid == plan.ItemGuid)
                {
                    data.ContainerId = plan.ContainerId;
                    data.OriginPosition = plan.Origin;
                    data.Direction = plan.Direction;
                    tree.PlaceItem(plan.ContainerId, data);
                    break;
                }
            }
        }

        return CommitSuccess(tree, command.CommandId);
    }

    private static InventoryCommandResult CommitSuccess(IInventoryTreeCache tree, string commandId)
    {
        tree.BumpRevision();
        tree.RememberAppliedCommand(commandId);
        return InventoryCommandResult.Success();
    }

    private static InventoryCommandResult SucceedIdempotent(IInventoryTreeCache tree, string commandId)
    {
        tree.RememberAppliedCommand(commandId);
        return InventoryCommandResult.Success();
    }
}
