using TetrisCoordLib.Core.Math;

namespace Ctis.Core;

public enum InventoryCommandKind
{
    Place,
    MoveToSlot,
    Lift,
    Stack,
    Split,
    ResizeContainer,
    PatchOccupancy,
    RemoveOccupancyPatch,
    Exchange,
    Flip,
    OrganizeContainer,
    OrganizeItemGrids
}

public sealed class InventoryCommand
{
    public InventoryCommandKind Kind { get; init; }
    public string ItemGuid { get; init; } = "";
    public string? ContainerId { get; init; }
    public Vec2I Origin { get; init; }
    public Dir Direction { get; init; } = Dir.Down;
    public bool HasDirection { get; init; }
    public int SlotIndex { get; init; } = -1;
    public string? TargetGuid { get; init; }
    public int Amount { get; init; }
    public string? NewItemGuid { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public float TileWidth { get; init; }
    public float TileHeight { get; init; }
    public bool HasExpectedRevision { get; init; }
    public string? PatchKey { get; init; }
    public IReadOnlyList<Vec2I>? Add { get; init; }
    public IReadOnlyList<Vec2I>? Remove { get; init; }
    public string ActorId { get; init; } = InventoryTreeIds.LocalActorId;
    public string? OriginContainerId { get; init; }
    public string CommandId { get; init; } = "";
    public int ExpectedRevision { get; init; }
    public bool FlipH { get; init; }
    public bool FlipV { get; init; }
    public bool HasFlip { get; init; }
    public InventorySortStrategy SortStrategy { get; init; } = InventorySortStrategy.Area;

    /// <summary>Places an existing item onto a grid container.</summary>
    public static InventoryCommand Place(string itemGuid, string containerId, Vec2I origin, Dir? direction = null)
        => new()
        {
            Kind = InventoryCommandKind.Place,
            ItemGuid = itemGuid,
            ContainerId = containerId,
            Origin = origin,
            Direction = direction ?? Dir.Down,
            HasDirection = direction.HasValue
        };

    /// <summary>Moves an existing item onto an equipment slot.</summary>
    public static InventoryCommand MoveToSlot(string itemGuid, int slotIndex)
        => new()
        {
            Kind = InventoryCommandKind.MoveToSlot,
            ItemGuid = itemGuid,
            SlotIndex = slotIndex,
            ContainerId = InventoryTreeIds.Slot(slotIndex)
        };

    /// <summary>Moves an item into the actor's held container.</summary>
    public static InventoryCommand Lift(string itemGuid, string actorId = InventoryTreeIds.LocalActorId)
        => new()
        {
            Kind = InventoryCommandKind.Lift,
            ItemGuid = itemGuid,
            ActorId = actorId,
            ContainerId = InventoryTreeIds.Held(actorId)
        };

    /// <summary>Merges source onto target.</summary>
    public static InventoryCommand Stack(string sourceGuid, string targetGuid)
        => new()
        {
            Kind = InventoryCommandKind.Stack,
            ItemGuid = sourceGuid,
            TargetGuid = targetGuid
        };

    /// <summary>Splits amount off an item; <paramref name="newItemGuid"/> is issued by the authority.</summary>
    public static InventoryCommand Split(string itemGuid, int amount, string newItemGuid)
        => new()
        {
            Kind = InventoryCommandKind.Split,
            ItemGuid = itemGuid,
            Amount = amount,
            NewItemGuid = newItemGuid
        };

    /// <summary>Resizes a container in cells. Tile size 0 keeps the existing (or default) pixels.</summary>
    public static InventoryCommand ResizeContainer(string containerId, int width, int height)
        => ResizeContainer(containerId, width, height, 0f, 0f);

    /// <summary>Resizes a container in cells and pixel tile size.</summary>
    public static InventoryCommand ResizeContainer(
        string containerId,
        int width,
        int height,
        float tileWidth,
        float tileHeight)
        => new()
        {
            Kind = InventoryCommandKind.ResizeContainer,
            ContainerId = containerId,
            Width = width,
            Height = height,
            TileWidth = tileWidth,
            TileHeight = tileHeight
        };

    /// <summary>Applies a named occupancy patch on an item.</summary>
    public static InventoryCommand PatchOccupancy(string itemGuid, string key, IEnumerable<Vec2I>? add, IEnumerable<Vec2I>? remove)
        => new()
        {
            Kind = InventoryCommandKind.PatchOccupancy,
            ItemGuid = itemGuid,
            PatchKey = key,
            Add = add != null ? new List<Vec2I>(add) : null,
            Remove = remove != null ? new List<Vec2I>(remove) : null
        };

    /// <summary>Removes a named occupancy patch from an item.</summary>
    public static InventoryCommand RemoveOccupancyPatch(string itemGuid, string key)
        => new()
        {
            Kind = InventoryCommandKind.RemoveOccupancyPatch,
            ItemGuid = itemGuid,
            PatchKey = key
        };

    /// <summary>Swaps the item with fully covered occupants, then places the mover on dest.</summary>
    public static InventoryCommand Exchange(
        string itemGuid,
        string containerId,
        Vec2I origin,
        Dir direction,
        string? originContainerId = null)
        => new()
        {
            Kind = InventoryCommandKind.Exchange,
            ItemGuid = itemGuid,
            ContainerId = containerId,
            Origin = origin,
            Direction = direction,
            HasDirection = true,
            OriginContainerId = originContainerId
        };

    /// <summary>Sets an item's local horizontal/vertical flip flags in place on its grid.</summary>
    public static InventoryCommand Flip(string itemGuid, bool flipH, bool flipV)
        => new()
        {
            Kind = InventoryCommandKind.Flip,
            ItemGuid = itemGuid,
            FlipH = flipH,
            FlipV = flipV,
            HasFlip = true
        };

    /// <summary>Organizes items in a single container or grid according to the sort strategy.</summary>
    public static InventoryCommand Organize(string containerId, InventorySortStrategy strategy = InventorySortStrategy.Area)
        => new()
        {
            Kind = InventoryCommandKind.OrganizeContainer,
            ContainerId = containerId,
            SortStrategy = strategy
        };

    /// <summary>Organizes all inner grids belonging to an item according to the sort strategy.</summary>
    public static InventoryCommand OrganizeItem(string itemGuid, InventorySortStrategy strategy = InventorySortStrategy.Area)
        => new()
        {
            Kind = InventoryCommandKind.OrganizeItemGrids,
            ItemGuid = itemGuid,
            SortStrategy = strategy
        };

    /// <summary>Copies this command with a replay envelope.</summary>
    public InventoryCommand WithEnvelope(string commandId, int expectedRevision)
        => new()
        {
            Kind = Kind,
            ItemGuid = ItemGuid,
            ContainerId = ContainerId,
            Origin = Origin,
            Direction = Direction,
            HasDirection = HasDirection,
            SlotIndex = SlotIndex,
            TargetGuid = TargetGuid,
            Amount = Amount,
            NewItemGuid = NewItemGuid,
            Width = Width,
            Height = Height,
            TileWidth = TileWidth,
            TileHeight = TileHeight,
            PatchKey = PatchKey,
            Add = Add,
            Remove = Remove,
            ActorId = ActorId,
            OriginContainerId = OriginContainerId,
            FlipH = FlipH,
            FlipV = FlipV,
            HasFlip = HasFlip,
            SortStrategy = SortStrategy,
            CommandId = commandId,
            ExpectedRevision = expectedRevision,
            HasExpectedRevision = true
        };
}

public readonly struct InventoryCommandResult
{
    public bool Ok { get; }
    public InventoryPlacementBlockReason Reason { get; }

    private InventoryCommandResult(bool ok, InventoryPlacementBlockReason reason)
    {
        Ok = ok;
        Reason = reason;
    }

    public static InventoryCommandResult Success() => new(true, InventoryPlacementBlockReason.None);
    public static InventoryCommandResult Fail(InventoryPlacementBlockReason reason) => new(false, reason);
}

/// <summary>One item placement produced by exchange or pack planning.</summary>
public readonly struct InventoryPlacementPlan
{
    public InventoryPlacementPlan(string itemGuid, string containerId, Vec2I origin, Dir direction)
    {
        ItemGuid = itemGuid;
        ContainerId = containerId;
        Origin = origin;
        Direction = direction;
    }

    public string ItemGuid { get; }
    public string ContainerId { get; }
    public Vec2I Origin { get; }
    public Dir Direction { get; }
}

public interface IItemIdFactory
{
    /// <summary>Issues a new item guid. Authority-owned in a networked session.</summary>
    string Next();
}

public sealed class GuidItemIdFactory : IItemIdFactory
{
    public string Next() => Guid.NewGuid().ToString();
}
