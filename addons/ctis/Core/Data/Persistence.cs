using System.Text.Json.Serialization;
using TetrisCoordLib.Core.Math;

namespace Ctis.Core;

public sealed class TetrisItemPersistentData
{
    public int ItemId { get; set; }
    public string ItemGuid { get; set; } = "";
    public string ContainerId { get; set; } = "";
    public Vec2I OriginPosition { get; set; }
    public Dir Direction { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool FlipH { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool FlipV { get; set; }

    public int Stack { get; set; }
    public bool IsOnSlot { get; set; }
    public int SlotIndex { get; set; } = -1;
    public Dictionary<string, string> CustomData { get; set; } = new();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<OccupancyPatch>? OccupancyPatches { get; set; }

    /// <summary>Copies instance fields from a live item VM, leaving <see cref="CustomData"/> unchanged.</summary>
    public void CopyFrom(TetrisItemVM item)
    {
        ItemId = item.ItemDetails?.ItemId ?? 0;
        ItemGuid = item.Guid;
        ContainerId = InventoryTreeIds.Of(item.CurrentTetrisContainer);
        IsOnSlot = item.CurrentTetrisContainer is TetrisSlotVM;
        SlotIndex = item.CurrentTetrisContainer is TetrisSlotVM slot ? slot.SlotIndex : -1;
        OriginPosition = item.LocalGridCoordinate;
        Direction = item.Direction;
        FlipH = item.FlipH;
        FlipV = item.FlipV;
        Stack = item.CurrentStack;
        if (item.OccupancyPatches.Count > 0)
        {
            var patches = new List<OccupancyPatch>(item.OccupancyPatches.Count);
            for (int i = 0; i < item.OccupancyPatches.Count; i++)
                patches.Add(item.OccupancyPatches[i].Clone());
            OccupancyPatches = patches;
        }
        else
        {
            OccupancyPatches = null;
        }
    }

    /// <summary>Deep-copies identity, placement, stack, patches, and custom data.</summary>
    public TetrisItemPersistentData Clone()
    {
        List<OccupancyPatch>? patches = null;
        if (OccupancyPatches != null)
        {
            patches = new List<OccupancyPatch>(OccupancyPatches.Count);
            for (int i = 0; i < OccupancyPatches.Count; i++)
                patches.Add(OccupancyPatches[i].Clone());
        }

        return new TetrisItemPersistentData
        {
            ItemId = ItemId,
            ItemGuid = ItemGuid,
            ContainerId = ContainerId,
            OriginPosition = OriginPosition,
            Direction = Direction,
            FlipH = FlipH,
            FlipV = FlipV,
            Stack = Stack,
            IsOnSlot = IsOnSlot,
            SlotIndex = SlotIndex,
            CustomData = CustomData == null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(CustomData),
            OccupancyPatches = patches
        };
    }
}

public sealed class GameSaveData
{
    public int CatalogVersion { get; set; }
    public List<TetrisItemPersistentData> Items { get; set; } = new();
    public Dictionary<string, GridContainerConfig> GridConfigs { get; set; } = new();
}

public sealed class GridContainerConfig
{
    public int Width { get; set; }
    public int Height { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float TileWidth { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float TileHeight { get; set; }
}

public sealed class SaveSlotInfo
{
    public int Index { get; init; }
    public bool HasData { get; init; }
    public bool IsCorrupt { get; init; }
    public string Timestamp { get; init; } = "";
}

public static class InventoryTreeIds
{
    public const string Depository = "depository";
    public const string SlotPrefix = "slot:";
    public const string PocketPrefix = "pocket:";
    public const string CofferPrefix = "coffer:";
    public const string HeldPrefix = "held:";
    public const string LocalActorId = "local";
    public const int SaveSlotCount = 3;

    /// <summary>Builds the equipment-slot container id for a slot index.</summary>
    public static string Slot(int index) => SlotPrefix + index;

    /// <summary>True when the container id belongs to an equipment slot.</summary>
    public static bool IsSlot(string containerId)
        => !string.IsNullOrEmpty(containerId) && containerId.StartsWith(SlotPrefix, StringComparison.Ordinal);

    /// <summary>Builds the persistent pocket container id.</summary>
    public static string Pocket(int index) => PocketPrefix + index;

    /// <summary>True when the container id belongs to a pocket grid.</summary>
    public static bool IsPocket(string containerId)
        => !string.IsNullOrEmpty(containerId) && containerId.StartsWith(PocketPrefix, StringComparison.Ordinal);

    /// <summary>Builds the persistent coffer container id.</summary>
    public static string Coffer(int index) => CofferPrefix + index;

    /// <summary>True when the container id belongs to a coffer grid.</summary>
    public static bool IsCoffer(string containerId)
        => !string.IsNullOrEmpty(containerId) && containerId.StartsWith(CofferPrefix, StringComparison.Ordinal);

    /// <summary>Builds the in-transit held container id for an actor.</summary>
    public static string Held(string actorId)
        => HeldPrefix + (string.IsNullOrEmpty(actorId) ? LocalActorId : actorId);

    /// <summary>True when the container id is an in-transit held bucket.</summary>
    public static bool IsHeld(string containerId)
        => !string.IsNullOrEmpty(containerId) && containerId.StartsWith(HeldPrefix, StringComparison.Ordinal);

    /// <summary>True when the id is a grid container rather than a slot or held bucket.</summary>
    public static bool IsGridContainer(string containerId)
        => !string.IsNullOrEmpty(containerId) && !IsSlot(containerId) && !IsHeld(containerId);

    /// <summary>Resolves the tree container id for a live grid or slot.</summary>
    public static string Of(TetrisItemContainerVM? container)
        => container switch
        {
            TetrisGridVM grid when !string.IsNullOrEmpty(grid.GridGuid) => grid.GridGuid,
            TetrisSlotVM slot => Slot(slot.SlotIndex),
            _ => ""
        };
}

public sealed class SaveFileWrapper<T>
{
    public int Version { get; set; }
    public string Timestamp { get; set; } = "";
    public T? Payload { get; set; }
}
