namespace Ctis.Core;

public interface IGridFactory
{
    /// <summary>Creates a grid VM and registers it under a guid when assigned.</summary>
    TetrisGridVM Create(int width, int height);
    /// <summary>Indexes a live grid VM by guid.</summary>
    void RegisterVM(string guid, TetrisGridVM vm);
    /// <summary>Looks up a live grid VM by guid.</summary>
    bool TryGetVM(string guid, out TetrisGridVM vm);
    /// <summary>Drops a live grid VM from the guid index.</summary>
    void UnregisterVM(string guid);
}

public sealed class GridFactory : IGridFactory
{
    private readonly IInventoryTreeCache _tree;
    private readonly IItemVmRegistry _registry;
    private readonly IItemCatalog _catalog;
    private readonly Dictionary<string, TetrisGridVM> _vmRegistry = new();

    public GridFactory(IInventoryTreeCache tree, IItemVmRegistry registry, IItemCatalog catalog)
    {
        _tree = tree;
        _registry = registry;
        _catalog = catalog;
    }

    /// <summary>Creates a grid VM and registers it under a guid when assigned.</summary>
    public TetrisGridVM Create(int width, int height)
        => new(width, height, _tree, _registry, _catalog, this);

    /// <summary>Indexes a live grid VM by guid.</summary>
    public void RegisterVM(string guid, TetrisGridVM vm)
    {
        if (string.IsNullOrEmpty(guid)) return;
        _vmRegistry[guid] = vm;
    }

    /// <summary>Looks up a live grid VM by guid.</summary>
    public bool TryGetVM(string guid, out TetrisGridVM vm)
        => _vmRegistry.TryGetValue(guid, out vm!);

    /// <summary>Drops a live grid VM from the guid index.</summary>
    public void UnregisterVM(string guid)
    {
        if (!string.IsNullOrEmpty(guid))
            _vmRegistry.Remove(guid);
    }
}
