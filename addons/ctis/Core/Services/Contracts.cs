using TetrisCoordLib.Core.Math;

namespace Ctis.Core;

public interface IItemCatalog
{
    /// <summary>Looks up catalog details by item id.</summary>
    ItemDetails? GetById(int itemId);
    IReadOnlyList<ItemDetails> All { get; }
    /// <summary>Shared catalog generation; snapshots with a different non-zero version are rejected.</summary>
    int Version { get; }
    /// <summary>Registers or replaces a catalog entry.</summary>
    void Register(ItemDetails details);
    /// <summary>Replaces the entire catalog.</summary>
    void ReplaceAll(IEnumerable<ItemDetails> details);
}

public interface IItemVmRegistry
{
    /// <summary>Returns an existing VM for the guid or creates one from details and save data.</summary>
    TetrisItemVM GetOrCreate(ItemDetails? details, TetrisItemPersistentData? data, TetrisItemContainerVM? container);
    /// <summary>Looks up a live item VM by guid.</summary>
    bool TryGet(string guid, out TetrisItemVM vm);
    /// <summary>Drops a VM from the registry and optionally disposes it.</summary>
    void Unregister(string guid, bool dispose);
    /// <summary>Disposes every registered item VM.</summary>
    void Clear();
}

public interface IInventoryService
{
    /// <summary>True when placement rules accept this drop target.</summary>
    bool CanPlace(in InventoryPlacementContext context, out InventoryPlacementBlockReason reason);
    /// <summary>Evaluates a drop as inner-insert, vacant, stack, exchange, or blocked.</summary>
    InventoryDropResult EvaluateDrop(in InventoryPlacementContext context, TetrisItemGhostVM? ghost = null);
    /// <summary>Removes an item from its current grid or slot without destroying the VM.</summary>
    void Lift(TetrisItemVM item);
    /// <summary>Swaps the dragged item with fully covered occupants.</summary>
    bool TryQuickExchange(TetrisGridVM grid, TetrisItemGhostVM ghost, Vec2I targetPos);
    /// <summary>Merges source onto target and removes the source when empty.</summary>
    bool TryStack(TetrisItemVM source, TetrisItemVM target);
    /// <summary>Places an item onto a grid origin.</summary>
    bool PlaceOnGrid(TetrisItemVM item, TetrisGridVM grid, Vec2I origin, TetrisSlotVM? fromSlot);
    /// <summary>Places an item onto an equipment slot.</summary>
    bool PlaceOnSlot(TetrisItemVM item, TetrisSlotVM slot);
    /// <summary>Evaluates dropping <paramref name="mover"/> into <paramref name="host"/> inner grids.</summary>
    InventoryDropResult EvaluateInnerInsert(TetrisItemVM mover, TetrisItemVM host);
    /// <summary>Places onto the inner grid chosen by <see cref="EvaluateInnerInsert"/>.</summary>
    bool TryPlaceInnerInsert(TetrisItemVM item, InventoryDropResult insert);
    /// <summary>Splits a stack and places the new stack on the same grid.</summary>
    bool TrySplit(TetrisItemVM item, int amount);
    /// <summary>Resizes a grid and repacks occupants.</summary>
    bool TryResizeGrid(TetrisGridVM grid, int width, int height, float tileWidth, float tileHeight);
    /// <summary>Applies a named occupancy patch and writes the item into the tree.</summary>
    void ApplyOccupancyPatch(TetrisItemVM item, string key, IEnumerable<Vec2I>? add, IEnumerable<Vec2I>? remove);
    /// <summary>Removes a named occupancy patch and writes the restored footprint into the tree.</summary>
    void RemoveOccupancyPatch(TetrisItemVM item, string key);
    /// <summary>Toggles a screen-axis flip for an item sitting on a grid.</summary>
    bool TryFlip(TetrisItemVM item, bool horizontal);
    /// <summary>Organizes items in a single grid.</summary>
    bool TryOrganizeGrid(TetrisGridVM grid, InventorySortStrategy strategy = InventorySortStrategy.Area);
    /// <summary>Organizes items in a named container.</summary>
    bool TryOrganizeContainer(string containerId, InventorySortStrategy strategy = InventorySortStrategy.Area);
    /// <summary>Organizes items across all inner grids belonging to an item.</summary>
    bool TryOrganizeItemGrids(TetrisItemVM item, InventorySortStrategy strategy = InventorySortStrategy.Area);
    /// <summary>Applies a guid/container-id command against the tree; view-models are hydrated when present.</summary>
    InventoryCommandResult Apply(InventoryCommand command);
    /// <summary>Replays a networked command. CommandId and ExpectedRevision are required and never filled in.</summary>
    InventoryCommandResult ApplyRemote(InventoryCommand command);
}

public interface IItemDragMediator
{
    /// <summary>Binds the mediator to the active drag ghost.</summary>
    void Attach(TetrisItemGhostVM ghost);
    /// <summary>Caches the ghost's current facing and occupancy.</summary>
    void CacheGhostState(TetrisItemGhostVM ghost);
    /// <summary>Caches the item's origin container and occupancy before lift.</summary>
    void CacheItemState(TetrisItemVM item);
    /// <summary>Restores the ghost to the cached origin item state.</summary>
    void ApplyStateToGhost(TetrisItemGhostVM ghost);
    /// <summary>Applies the cached ghost facing and occupancy onto the item.</summary>
    void ApplyStateToItem(TetrisItemVM item);
    /// <summary>Sets the drop target to a grid while dragging.</summary>
    void SyncGhostTargetDroppedGrid(TetrisGridVM target);
    /// <summary>Sets the drop target to a slot while dragging.</summary>
    void SyncGhostTargetDroppedSlot(TetrisSlotVM target);
    /// <summary>Initializes the ghost from an item if a drag is not already running.</summary>
    bool TrySyncGhostFromItem(TetrisItemVM item, TetrisItemGhostVM.GhostInitData initData);
    /// <summary>Syncs the ghost from the item and starts a drag.</summary>
    bool TryBeginDragFromItem(TetrisItemVM item, TetrisItemGhostVM.GhostInitData initData);
    /// <summary>True while a drag session is active.</summary>
    bool IsDragging { get; }
    /// <summary>Returns the current hover highlight when a drag is over a grid.</summary>
    bool TryGetDropPreview(out InventoryDropPreview preview);
}

public interface IPointerGridSession
{
    TetrisGridVM? SelectedGrid { get; }
    TetrisSlotVM? SelectedSlot { get; }
    TetrisItemVM? HoveredItem { get; }
    TetrisGridVM? DepositoryGrid { get; set; }
    bool PreferSlotTarget { get; }
    /// <summary>Pins the hovered grid from a view, or clears it.</summary>
    void SetSelectedGrid(TetrisGridVM? grid);
    /// <summary>Re-resolves the hovered grid or slot from the current pointer.</summary>
    void RefreshFromMouse();
    /// <summary>Converts the pointer into a ghost origin that keeps the shape under the cursor.</summary>
    Vec2I GetGhostTileGridOrigin(int ghostWidth, int ghostHeight);
}

public interface ISaveSlotStore
{
    int SlotCount { get; }
    /// <summary>True when the slot file or buffer exists.</summary>
    bool Exists(int index);
    /// <summary>Reads raw JSON for a slot, or null when missing.</summary>
    string? Read(int index);
    /// <summary>Writes raw JSON to a slot.</summary>
    void Write(int index, string json);
    /// <summary>Deletes a slot.</summary>
    void Delete(int index);
}

public interface ISaveLoadService
{
    event Action? Restored;
    int SlotCount { get; }
    /// <summary>Serializes live items and non-derivable grid sizes to JSON.</summary>
    string Serialize();
    /// <summary>Replaces live inventory from JSON and raises <see cref="Restored"/>.</summary>
    void Restore(string json);
    /// <summary>Reads slot metadata without mutating live inventory.</summary>
    SaveSlotInfo GetSlot(int index);
    /// <summary>Writes the current inventory into a numbered slot.</summary>
    void SaveSlot(int index);
    /// <summary>Loads a slot into live inventory; false when missing or corrupt.</summary>
    bool LoadSlot(int index);
    /// <summary>Deletes a numbered slot.</summary>
    void DeleteSlot(int index);
}

public interface IInventoryTreeCache
{
    /// <summary>Returns the container node, creating it when missing.</summary>
    ContainerNode GetOrCreateContainer(string containerId);
    /// <summary>Looks up a container without creating it.</summary>
    bool TryGetContainer(string containerId, out ContainerNode container);
    /// <summary>Records which item owns a nested container.</summary>
    void SetContainerOwner(string containerId, string ownerItemGuid);
    /// <summary>Stores grid size for a container.</summary>
    void SetContainerConfig(string containerId, int w, int h, float tileW, float tileH);
    /// <summary>Inserts or updates item data in the global item index.</summary>
    ItemNode UpsertItem(TetrisItemPersistentData data);
    /// <summary>Snapshot generation; incremented after a successful mutation.</summary>
    int Revision { get; }
    /// <summary>Command id of the last successful commit; used for retry idempotency.</summary>
    string? LastAppliedCommandId { get; }
    /// <summary>Sets the snapshot generation, used when restoring a save.</summary>
    void SetRevision(int revision);
    /// <summary>Increments the snapshot generation.</summary>
    void BumpRevision();
    /// <summary>Records the command id that last mutated the tree.</summary>
    void RememberAppliedCommand(string commandId);
    /// <summary>Places item data into a container and indexes it.</summary>
    void PlaceItem(string containerId, TetrisItemPersistentData data);
    /// <summary>Removes an item from every index.</summary>
    bool RemoveItem(string itemGuid);
    /// <summary>Unlinks an item from one container without deleting the global item index.</summary>
    bool RemoveFromContainer(string containerId, string itemGuid);
    /// <summary>Lists items currently in a container.</summary>
    IEnumerable<TetrisItemPersistentData> GetItems(string containerId);
    /// <summary>Looks up an indexed item node.</summary>
    bool TryGetItem(string itemGuid, out ItemNode node);
    /// <summary>Returns the container currently holding an item.</summary>
    string? GetContainerIdByItem(string itemGuid);
    /// <summary>Clears every container and item index.</summary>
    void Clear();
    /// <summary>Removes all items from one container.</summary>
    void ClearContainerItems(string containerId);
    /// <summary>Enumerates every known container.</summary>
    IEnumerable<ContainerNode> GetAllContainers();
    /// <summary>Enumerates every known container id.</summary>
    IEnumerable<string> GetAllContainerIds();
    /// <summary>True when <paramref name="targetContainerId"/> is nested under the given item.</summary>
    bool IsDescendantContainer(string itemGuid, string targetContainerId);
}

public interface IInnerGridLayout
{
    /// <summary>Inner-grid sizes for an item panel, in placeholder order. Empty when unknown.</summary>
    IReadOnlyList<InnerGridSpec> SpecsFor(ItemDetails? details);
}
