namespace Ctis.Core;

public sealed class ItemNode
{
    public TetrisItemPersistentData Data { get; private set; }
    public string ItemGuid => Data.ItemGuid;
    public int ItemId => Data.ItemId;

    public ItemNode(TetrisItemPersistentData data) => Data = data;

    public void Update(TetrisItemPersistentData data) => Data = data;
}

public sealed class ContainerNode
{
    private readonly Dictionary<string, TetrisItemPersistentData> _items = new();
    public string ContainerId { get; internal set; } = "";
    public int GridSizeWidth { get; internal set; } = 1;
    public int GridSizeHeight { get; internal set; } = 1;
    public float LocalGridTileSizeWidth { get; internal set; } = CtisSettings.GridTileSizeWidth;
    public float LocalGridTileSizeHeight { get; internal set; } = CtisSettings.GridTileSizeHeight;
    public string OwnerItemGuid { get; internal set; } = "";
    public IReadOnlyDictionary<string, TetrisItemPersistentData> ItemsByGuid => _items;
    public IEnumerable<TetrisItemPersistentData> Items => _items.Values;

    internal OccupancyBoard? Occupancy { get; set; }
    private HashSet<string>? _dirtyOccupants;

    public void SetConfig(int w, int h, float tileW, float tileH)
    {
        bool sizeChanged = GridSizeWidth != w || GridSizeHeight != h;
        GridSizeWidth = w;
        GridSizeHeight = h;
        LocalGridTileSizeWidth = tileW;
        LocalGridTileSizeHeight = tileH;
        if (sizeChanged)
            InvalidateOccupancy();
    }

    public void Upsert(TetrisItemPersistentData data)
    {
        _items[data.ItemGuid] = data;
        TouchOccupancy(data.ItemGuid);
    }

    public bool TryGet(string itemGuid, out TetrisItemPersistentData data) => _items.TryGetValue(itemGuid, out data!);

    public bool Remove(string itemGuid)
    {
        bool removed = _items.Remove(itemGuid);
        if (removed)
            TouchOccupancy(itemGuid);
        return removed;
    }

    public void Clear()
    {
        _items.Clear();
        Occupancy?.Clear();
        _dirtyOccupants = null;
    }

    internal void InvalidateOccupancy()
    {
        Occupancy = null;
        _dirtyOccupants = null;
    }

    internal OccupancyBoard EnsureOccupancy(IItemCatalog catalog)
    {
        int width = Math.Max(1, GridSizeWidth);
        int height = Math.Max(1, GridSizeHeight);
        if (Occupancy == null || Occupancy.Width != width || Occupancy.Height != height)
        {
            Occupancy = new OccupancyBoard(width, height);
            Occupancy.Rebuild(Items, catalog);
            _dirtyOccupants = null;
            return Occupancy;
        }

        if (_dirtyOccupants is { Count: > 0 })
            ApplyDirtyOccupancy(catalog);

        return Occupancy;
    }

    private void TouchOccupancy(string guid)
    {
        if (Occupancy == null) return;
        _dirtyOccupants ??= new HashSet<string>(StringComparer.Ordinal);
        _dirtyOccupants.Add(guid);
    }

    private void ApplyDirtyOccupancy(IItemCatalog catalog)
    {
        var board = Occupancy!;
        foreach (var guid in _dirtyOccupants!)
            board.Unmark(guid);
        foreach (var guid in _dirtyOccupants)
        {
            if (!_items.TryGetValue(guid, out var data)) continue;
            var details = catalog.GetById(data.ItemId);
            var shape = ItemShape.Resolve(details?.Occupancy, data.OccupancyPatches, data);
            board.Mark(data.ItemGuid, data.OriginPosition, shape.Cells);
        }
        _dirtyOccupants = null;
    }
}

public sealed class InventoryTreeCache : IInventoryTreeCache
{
    private readonly Dictionary<string, ContainerNode> _containers = new();
    private readonly Dictionary<string, ItemNode> _items = new();
    private readonly Dictionary<string, string> _itemToContainer = new();

    public int Revision { get; private set; }
    public string? LastAppliedCommandId { get; private set; }

    public void SetRevision(int revision) => Revision = Math.Max(0, revision);

    public void BumpRevision() => Revision++;

    public void RememberAppliedCommand(string commandId)
        => LastAppliedCommandId = string.IsNullOrEmpty(commandId) ? LastAppliedCommandId : commandId;

    public ContainerNode GetOrCreateContainer(string containerId)
    {
        if (string.IsNullOrEmpty(containerId))
            throw new ArgumentException("Container id is required.", nameof(containerId));
        if (_containers.TryGetValue(containerId, out var existing)) return existing;
        var created = new ContainerNode { ContainerId = containerId };
        _containers[containerId] = created;
        return created;
    }

    public bool TryGetContainer(string containerId, out ContainerNode container)
        => _containers.TryGetValue(containerId, out container!);

    public void SetContainerOwner(string containerId, string ownerItemGuid)
        => GetOrCreateContainer(containerId).OwnerItemGuid = ownerItemGuid;

    public void SetContainerConfig(string containerId, int w, int h, float tileW, float tileH)
        => GetOrCreateContainer(containerId).SetConfig(w, h, tileW, tileH);

    public ItemNode UpsertItem(TetrisItemPersistentData data)
    {
        if (string.IsNullOrEmpty(data.ItemGuid))
            throw new ArgumentException("Item guid is required.");
        if (_items.TryGetValue(data.ItemGuid, out var node))
        {
            node.Update(data);
            return node;
        }
        node = new ItemNode(data);
        _items[data.ItemGuid] = node;
        return node;
    }

    public void PlaceItem(string containerId, TetrisItemPersistentData data)
    {
        if (_itemToContainer.TryGetValue(data.ItemGuid, out var previous) && previous != containerId
            && _containers.TryGetValue(previous, out var previousContainer))
        {
            previousContainer.Remove(data.ItemGuid);
        }

        var container = GetOrCreateContainer(containerId);
        data.ContainerId = containerId;
        UpsertItem(data);
        container.Upsert(data);
        _itemToContainer[data.ItemGuid] = containerId;
    }

    public bool RemoveItem(string itemGuid)
    {
        if (_itemToContainer.TryGetValue(itemGuid, out var containerId) &&
            _containers.TryGetValue(containerId, out var container))
        {
            container.Remove(itemGuid);
            _itemToContainer.Remove(itemGuid);
        }
        return _items.Remove(itemGuid);
    }

    public bool RemoveFromContainer(string containerId, string itemGuid)
    {
        if (!_containers.TryGetValue(containerId, out var container)) return false;
        var removed = container.Remove(itemGuid);
        if (removed) _itemToContainer.Remove(itemGuid);
        return removed;
    }

    public IEnumerable<TetrisItemPersistentData> GetItems(string containerId)
        => _containers.TryGetValue(containerId, out var container)
            ? container.Items
            : Array.Empty<TetrisItemPersistentData>();

    public bool TryGetItem(string itemGuid, out ItemNode node)
        => _items.TryGetValue(itemGuid, out node!);

    public string? GetContainerIdByItem(string itemGuid)
        => _itemToContainer.TryGetValue(itemGuid, out var id) ? id : null;

    public void Clear()
    {
        _containers.Clear();
        _items.Clear();
        _itemToContainer.Clear();
        Revision = 0;
        LastAppliedCommandId = null;
    }

    public void ClearContainerItems(string containerId)
    {
        if (!_containers.TryGetValue(containerId, out var container)) return;
        List<string>? toRemove = null;
        foreach (var kv in _itemToContainer)
        {
            if (string.Equals(kv.Value, containerId, StringComparison.Ordinal))
            {
                toRemove ??= new List<string>();
                toRemove.Add(kv.Key);
            }
        }
        if (toRemove != null)
        {
            for (int i = 0; i < toRemove.Count; i++)
                _itemToContainer.Remove(toRemove[i]);
        }
        container.Clear();
    }

    public IEnumerable<ContainerNode> GetAllContainers() => _containers.Values;
    public IEnumerable<string> GetAllContainerIds() => _containers.Keys;

    public bool IsDescendantContainer(string itemGuid, string targetContainerId)
    {
        if (string.IsNullOrEmpty(itemGuid) || string.IsNullOrEmpty(targetContainerId))
            return false;

        if (targetContainerId.Length > itemGuid.Length
            && targetContainerId.StartsWith(itemGuid, StringComparison.Ordinal)
            && targetContainerId[itemGuid.Length] == ':')
        {
            return true;
        }

        // Floyd's cycle-finding (tortoise and hare) algorithm for infinite depth and zero allocations.
        string? slow = targetContainerId;
        string? fast = targetContainerId;

        while (!string.IsNullOrEmpty(fast))
        {
            slow = StepParentContainer(slow, itemGuid, out bool slowMatched);
            if (slowMatched) return true;
            if (slow == null) return false;

            fast = StepParentContainer(fast, itemGuid, out bool fastMatched1);
            if (fastMatched1) return true;
            if (fast == null) return false;

            fast = StepParentContainer(fast, itemGuid, out bool fastMatched2);
            if (fastMatched2) return true;
            if (fast == null) return false;

            if (string.Equals(slow, fast, StringComparison.Ordinal))
                return false;
        }

        return false;
    }

    private string? StepParentContainer(string containerId, string targetItemGuid, out bool matched)
    {
        matched = false;
        string? ownerGuid = null;
        if (_containers.TryGetValue(containerId, out var container))
            ownerGuid = container.OwnerItemGuid;

        if (string.IsNullOrEmpty(ownerGuid))
        {
            int colon = containerId.IndexOf(':');
            if (colon > 0)
            {
                if (colon == targetItemGuid.Length && containerId.StartsWith(targetItemGuid, StringComparison.Ordinal))
                {
                    matched = true;
                    return null;
                }
                ownerGuid = containerId[..colon];
            }
        }

        if (string.IsNullOrEmpty(ownerGuid))
            return null;

        if (string.Equals(ownerGuid, targetItemGuid, StringComparison.Ordinal))
        {
            matched = true;
            return null;
        }

        if (_itemToContainer.TryGetValue(ownerGuid, out var parentId))
            return parentId;

        return null;
    }
}
