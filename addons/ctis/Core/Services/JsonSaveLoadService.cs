using System.Text.Json;

namespace Ctis.Core;

public sealed class JsonSaveLoadService : ISaveLoadService
{
    private readonly IInventoryTreeCache _tree;
    private readonly IItemVmRegistry _registry;
    private readonly ISaveSlotStore _slots;
    private readonly IItemCatalog _catalog;

    public JsonSaveLoadService(
        IInventoryTreeCache tree,
        IItemVmRegistry registry,
        ISaveSlotStore slots,
        IItemCatalog catalog)
    {
        _tree = tree;
        _registry = registry;
        _slots = slots;
        _catalog = catalog;
    }

    public event Action? Restored;
    public int SlotCount => _slots.SlotCount;

    /// <summary>Serializes live items and non-derivable grid sizes to JSON.</summary>
    public string Serialize()
    {
        using var _ = CtisTrace.Scope("SaveLoad.Serialize");
        var payload = new GameSaveData
        {
            CatalogVersion = _catalog.Version
        };
        foreach (var container in _tree.GetAllContainers())
        {
            if (InventoryTreeIds.IsHeld(container.ContainerId) && container.ItemsByGuid.Count == 0)
                continue;
            foreach (var item in container.Items)
            {
                var data = item.Clone();
                data.ContainerId = container.ContainerId;
                payload.Items.Add(data);
            }
            if (!ShouldWriteConfig(container))
                continue;
            payload.GridConfigs[container.ContainerId] = new GridContainerConfig
            {
                Width = container.GridSizeWidth,
                Height = container.GridSizeHeight,
                TileWidth = container.LocalGridTileSizeWidth,
                TileHeight = container.LocalGridTileSizeHeight
            };
        }
        var wrapper = new SaveFileWrapper<GameSaveData>
        {
            Version = CtisSettings.CurrentVersion,
            Timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
            Payload = payload
        };
        return JsonSerializer.Serialize(wrapper, CtisJson.Options);
    }

    /// <summary>Replaces live inventory from JSON and raises <see cref="Restored"/>.</summary>
    public void Restore(string json)
    {
        using var _ = CtisTrace.Scope("SaveLoad.Restore");
        if (!string.IsNullOrWhiteSpace(json)
            && TryRead(json, out var pending)
            && pending?.Payload != null
            && !CatalogCompatible(pending.Payload))
            return;

        _registry.Clear();
        _tree.Clear();
        if (string.IsNullOrWhiteSpace(json))
        {
            Restored?.Invoke();
            return;
        }
        var wrapper = JsonSerializer.Deserialize<SaveFileWrapper<GameSaveData>>(json, CtisJson.Options);
        Apply(wrapper?.Payload);
        Restored?.Invoke();
    }

    /// <summary>Reads slot metadata without mutating live inventory.</summary>
    public SaveSlotInfo GetSlot(int index)
    {
        if (!_slots.Exists(index))
            return new SaveSlotInfo { Index = index };
        var json = _slots.Read(index);
        if (string.IsNullOrWhiteSpace(json))
            return new SaveSlotInfo { Index = index, IsCorrupt = true };
        if (!TryRead(json, out var wrapper))
            return new SaveSlotInfo { Index = index, IsCorrupt = true };
        return new SaveSlotInfo
        {
            Index = index,
            HasData = wrapper?.Payload != null,
            IsCorrupt = wrapper?.Payload == null,
            Timestamp = wrapper?.Timestamp ?? ""
        };
    }

    /// <summary>Writes the current inventory into a numbered slot.</summary>
    public void SaveSlot(int index)
    {
        using var _ = CtisTrace.Scope($"SaveLoad.SaveSlot_{index}");
        _slots.Write(index, Serialize());
    }

    /// <summary>Loads a slot into live inventory; false when missing, corrupt, or catalog-mismatched.</summary>
    public bool LoadSlot(int index)
    {
        using var _ = CtisTrace.Scope($"SaveLoad.LoadSlot_{index}");
        if (!_slots.Exists(index)) return false;
        var json = _slots.Read(index);
        if (!TryRead(json, out var wrapper) || wrapper?.Payload == null)
            return false;
        if (!CatalogCompatible(wrapper.Payload))
            return false;
        Restore(json!);
        return true;
    }

    /// <summary>Deletes a numbered slot.</summary>
    public void DeleteSlot(int index) => _slots.Delete(index);

    private bool CatalogCompatible(GameSaveData payload)
        => payload.CatalogVersion == _catalog.Version
            || (payload.CatalogVersion == 0 && _catalog.Version == 1);

    private static bool TryRead(string? json, out SaveFileWrapper<GameSaveData>? wrapper)
    {
        wrapper = null;
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            wrapper = JsonSerializer.Deserialize<SaveFileWrapper<GameSaveData>>(json, CtisJson.Options);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private void Apply(GameSaveData? payload)
    {
        if (payload?.GridConfigs != null)
        {
            foreach (var pair in payload.GridConfigs)
            {
                _tree.SetContainerConfig(
                    pair.Key,
                    Math.Max(1, pair.Value.Width),
                    Math.Max(1, pair.Value.Height),
                    pair.Value.TileWidth > 0 ? pair.Value.TileWidth : CtisSettings.GridTileSizeWidth,
                    pair.Value.TileHeight > 0 ? pair.Value.TileHeight : CtisSettings.GridTileSizeHeight);
            }
        }
        if (payload?.Items != null)
        {
            foreach (var item in payload.Items)
                _tree.PlaceItem(item.ContainerId, item);
        }
    }

    private static bool ShouldWriteConfig(ContainerNode container)
    {
        if (InventoryTreeIds.IsSlot(container.ContainerId) || InventoryTreeIds.IsHeld(container.ContainerId))
            return false;
        return container.ItemsByGuid.Count > 0 || container.ContainerId == InventoryTreeIds.Depository;
    }
}
