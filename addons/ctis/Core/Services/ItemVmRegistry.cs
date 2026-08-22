namespace Ctis.Core;

public sealed class ItemVmRegistry : IItemVmRegistry
{
    private readonly Dictionary<string, TetrisItemVM> _vms = new();
    private readonly PlacementConfig _placement;

    public ItemVmRegistry(PlacementConfig? placement = null)
    {
        _placement = placement ?? new PlacementConfig();
    }

    /// <summary>Returns an existing VM for the guid or creates one from details and save data.</summary>
    public TetrisItemVM GetOrCreate(ItemDetails? details, TetrisItemPersistentData? data, TetrisItemContainerVM? container)
    {
        var guid = data != null && !string.IsNullOrEmpty(data.ItemGuid)
            ? data.ItemGuid
            : System.Guid.NewGuid().ToString();

        if (_vms.TryGetValue(guid, out var existing))
        {
            if (details != null) existing.ItemDetails = details;
            if (container != null) existing.CurrentTetrisContainer = container;
            return existing;
        }

        var vm = new TetrisItemVM(details, data, container, _placement);
        if (string.IsNullOrEmpty(vm.Guid))
            vm.Guid = guid;
        _vms[vm.Guid] = vm;
        return vm;
    }

    /// <summary>Looks up a live item VM by guid.</summary>
    public bool TryGet(string guid, out TetrisItemVM vm)
        => _vms.TryGetValue(guid, out vm!);

    /// <summary>Drops a VM from the registry and optionally disposes it.</summary>
    public void Unregister(string guid, bool dispose)
    {
        if (!_vms.Remove(guid, out var vm)) return;
        if (dispose) vm.Dispose();
    }

    /// <summary>Disposes every registered item VM.</summary>
    public void Clear()
    {
        foreach (var vm in _vms.Values)
            vm.Dispose();
        _vms.Clear();
    }
}
