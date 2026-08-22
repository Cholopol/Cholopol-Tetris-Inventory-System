using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ctis.Core;

namespace Ctis.Presentation;

public partial class DebugItemListVM : DotPudica.Core.ViewModels.ViewModelBase
{
    private readonly IItemCatalog _catalog;
    private readonly IItemVmRegistry _registry;
    private readonly IInventoryService _inventory;
    private readonly IPointerGridSession _session;

    public System.Collections.ObjectModel.ObservableCollection<ItemDetailsRowVM> Items { get; } = new();

    public DebugItemListVM(
        IItemCatalog catalog,
        IItemVmRegistry registry,
        IInventoryService inventory,
        IPointerGridSession session)
    {
        _catalog = catalog;
        _registry = registry;
        _inventory = inventory;
        _session = session;
        foreach (var item in catalog.All)
            Items.Add(new ItemDetailsRowVM(item));
    }

    [RelayCommand]
    private void Add(ItemDetailsRowVM row)
    {
        using var _ = CtisTrace.Scope("DebugItemList.Add");
        var grid = _session.DepositoryGrid;
        if (grid == null) return;
        var details = _catalog.GetById(row.ItemId);
        if (details == null) return;
        var vm = _registry.GetOrCreate(details, null, grid);
        if (!InventoryLogic.TryFindFreeOrigin(grid, vm, out var origin)
            || !_inventory.PlaceOnGrid(vm, grid, origin, null))
            _registry.Unregister(vm.Guid, true);
    }
}

public partial class ItemDetailsRowVM : DotPudica.Core.ViewModels.ViewModelBase
{
    public ItemDetailsRowVM(ItemDetails details)
    {
        Details = details;
        Name = details.NameText;
        ItemId = details.ItemId;
        IconKey = details.IconKey;
    }

    public ItemDetails Details { get; }
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private int _itemId;
    [ObservableProperty] private string _iconKey = "";
}
