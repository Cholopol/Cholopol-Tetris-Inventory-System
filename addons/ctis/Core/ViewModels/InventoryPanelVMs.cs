using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.ViewModels;

namespace Ctis.Core;

public partial class EquipmentPanelVM : ViewModelBase
{
    private readonly IInventoryTreeCache _tree;
    private readonly IItemCatalog _catalog;
    private readonly IItemVmRegistry _registry;

    public EquipmentPanelVM(
        IReadOnlyList<TetrisSlotVM> characterSlots,
        IReadOnlyList<TetrisSlotVM> weaponSlots,
        IInventoryTreeCache tree,
        IItemCatalog catalog,
        IItemVmRegistry registry)
    {
        CharacterSlots = characterSlots;
        WeaponSlots = weaponSlots;
        _tree = tree;
        _catalog = catalog;
        _registry = registry;
    }

    public IReadOnlyList<TetrisSlotVM> CharacterSlots { get; }
    public IReadOnlyList<TetrisSlotVM> WeaponSlots { get; }

    public void RebuildFromCache()
    {
        foreach (var slot in CharacterSlots)
            InventoryPageVM.RestoreSlot(slot, _tree, _catalog, _registry);
        foreach (var slot in WeaponSlots)
            InventoryPageVM.RestoreSlot(slot, _tree, _catalog, _registry);
    }

    public void DetachItems()
    {
        InventoryPageVM.DetachSlots(CharacterSlots);
        InventoryPageVM.DetachSlots(WeaponSlots);
    }
}

public partial class ContainerPanelVM : ViewModelBase
{
    private readonly IInventoryTreeCache _tree;
    private readonly IItemCatalog _catalog;
    private readonly IItemVmRegistry _registry;
    private readonly IGridFactory _grids;
    private readonly IInventoryService _inventory;
    private readonly Dictionary<string, TetrisGridVM> _persistentGrids = new();
    private readonly List<TetrisGridVM> _persistentGridsList = new();

    public ContainerPanelVM(
        IReadOnlyList<TetrisSlotVM> containerSlots,
        IInventoryTreeCache tree,
        IItemCatalog catalog,
        IItemVmRegistry registry,
        IGridFactory grids,
        IInventoryService inventory)
    {
        ContainerSlots = containerSlots;
        _tree = tree;
        _catalog = catalog;
        _registry = registry;
        _grids = grids;
        _inventory = inventory;
        foreach (var slot in ContainerSlots)
        {
            slot.PlaceItemViewRequested += OnSlotPlaced;
            slot.RemoveItemViewRequested += OnSlotRemoved;
        }
    }

    public IReadOnlyList<TetrisSlotVM> ContainerSlots { get; }
    public IReadOnlyList<TetrisGridVM> PersistentGrids => _persistentGridsList;

    public event Action<TetrisSlotVM>? ContainerItemChanged;
    public event Action<TetrisItemVM>? ContainerItemCleared;

    public TetrisGridVM GetOrCreatePersistentGrid(int index, int width, int height)
        => GetOrCreatePersistentGrid(InventoryTreeIds.Pocket(index), width, height);

    public TetrisGridVM GetOrCreatePersistentGrid(string gridGuid, int width, int height)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        if (!_persistentGrids.TryGetValue(gridGuid, out var grid))
        {
            grid = _grids.Create(width, height);
            grid.GridGuid = gridGuid;
            _persistentGrids[gridGuid] = grid;
            _persistentGridsList.Add(grid);
        }

        float tileWidth = CtisSettings.GridTileSizeWidth;
        float tileHeight = CtisSettings.GridTileSizeHeight;
        if (_tree.TryGetContainer(gridGuid, out var node)
            && node.GridSizeWidth > 0
            && node.GridSizeHeight > 0)
        {
            tileWidth = node.LocalGridTileSizeWidth;
            tileHeight = node.LocalGridTileSizeHeight;
        }

        _inventory.Apply(InventoryCommand.ResizeContainer(gridGuid, width, height, tileWidth, tileHeight));
        InventoryPageVM.ApplyCachedConfig(grid, _tree);
        return grid;
    }

    public TetrisGridVM EnsureInnerGrid(TetrisItemVM item, int index, int width, int height)
        => ItemInnerGrid.Configure(item, index, _tree, _grids, _inventory, width, height);

    public void RebuildFromCache()
    {
        foreach (var grid in _persistentGrids.Values)
            InventoryPageVM.ApplyCachedConfig(grid, _tree);
        foreach (var slot in ContainerSlots)
            InventoryPageVM.RestoreSlot(slot, _tree, _catalog, _registry);
    }

    public void DetachItems()
    {
        InventoryPageVM.DetachSlots(ContainerSlots);
        foreach (var grid in _persistentGrids.Values)
            InventoryPageVM.ClearGridItems(grid);
    }

    [RelayCommand]
    public void OrganizeSlot(TetrisSlotVM slot) => OrganizeSlotWithStrategy(slot, InventorySortStrategy.Area);

    public void OrganizeSlotWithStrategy(TetrisSlotVM slot, InventorySortStrategy strategy)
    {
        var equipped = slot.RelatedTetrisItem;
        if (equipped?.ItemDetails?.HasInnerGrid == true)
            _inventory.TryOrganizeItemGrids(equipped, strategy);
    }

    [RelayCommand]
    public void OrganizePersistentGrid(string gridGuid) => OrganizePersistentGridWithStrategy(gridGuid, InventorySortStrategy.Area);

    public void OrganizePersistentGridWithStrategy(string gridGuid, InventorySortStrategy strategy)
    {
        if (_persistentGrids.TryGetValue(gridGuid, out var grid))
            _inventory.TryOrganizeGrid(grid, strategy);
        else
            _inventory.TryOrganizeContainer(gridGuid, strategy);
    }

    private void OnSlotPlaced(TetrisItemVM item)
    {
        if (item.CurrentTetrisContainer is TetrisSlotVM slot)
            ContainerItemChanged?.Invoke(slot);
    }

    private void OnSlotRemoved(TetrisItemVM item)
        => ContainerItemCleared?.Invoke(item);
}

public partial class StashPanelVM : ViewModelBase
{
    private readonly IInventoryService _inventory;
    private readonly IInventoryTreeCache _tree;
    private bool _syncing;

    public StashPanelVM(TetrisGridVM depository, IInventoryService inventory, IInventoryTreeCache tree)
    {
        Depository = depository;
        _inventory = inventory;
        _tree = tree;
        SyncFromDepository();
    }

    public TetrisGridVM Depository { get; }

    [ObservableProperty] private int _columns = CtisSettings.DepositoryColumns;
    [ObservableProperty] private int _rows = CtisSettings.DepositoryRows;
    [ObservableProperty] private float _cellSize = CtisSettings.GridTileSizeWidth;

    [RelayCommand]
    public void Organize() => OrganizeWithStrategy(InventorySortStrategy.Area);

    public void OrganizeWithStrategy(InventorySortStrategy strategy)
        => _inventory.TryOrganizeGrid(Depository, strategy);

    public bool TryConfigure(int columns, int rows, float tileSize)
        => _inventory.TryResizeGrid(Depository, columns, rows, tileSize, tileSize);

    public void RebuildFromCache()
    {
        InventoryPageVM.ApplyCachedConfig(Depository, _tree);
        SyncFromDepository();
    }

    public void DetachItems() => InventoryPageVM.ClearGridItems(Depository);

    public void SyncFromDepository()
    {
        _syncing = true;
        Columns = Depository.GridSizeWidth;
        Rows = Depository.GridSizeHeight;
        CellSize = Depository.LocalGridTileSizeWidth;
        _syncing = false;
    }

    partial void OnColumnsChanged(int value) => TryApply();
    partial void OnRowsChanged(int value) => TryApply();
    partial void OnCellSizeChanged(float value) => TryApply();

    private void TryApply()
    {
        if (_syncing) return;
        if (!TryConfigure(Columns, Rows, CellSize))
            SyncFromDepository();
    }
}
