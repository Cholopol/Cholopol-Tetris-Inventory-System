using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.Interactivity;

namespace Ctis.Core;

public partial class ContextMenuVM : DotPudica.Core.ViewModels.ViewModelBase
{
    private readonly IInventoryService _inventory;

    public ContextMenuVM(IInventoryService inventory)
    {
        _inventory = inventory;
    }

    [ObservableProperty] private TetrisItemVM? _currentItem;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SplitCommand))]
    private bool _canSplit;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenCommand))]
    private bool _canOpen;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FlipHorizontalCommand))]
    [NotifyCanExecuteChangedFor(nameof(FlipVerticalCommand))]
    private bool _canFlip;

    public readonly InteractionRequest<ItemDetails> ShowInfoRequest = new();
    public readonly InteractionRequest<TetrisItemVM> OpenPanelRequest = new();
    public readonly InteractionRequest CloseRequest = new();

    partial void OnCurrentItemChanged(TetrisItemVM? value)
    {
        CanSplit = value is { IsStackable: true, CurrentStack: > 1 };
        CanOpen = value?.ItemDetails?.HasInnerGrid == true;
        CanFlip = value?.CurrentTetrisContainer is TetrisGridVM;
    }

    [RelayCommand]
    private void Check()
    {
        if (CurrentItem?.ItemDetails == null) return;
        ShowInfoRequest.Raise(CurrentItem.ItemDetails);
        CloseRequest.Raise();
    }

    [RelayCommand(CanExecute = nameof(CanSplit))]
    private void Split()
    {
        if (CurrentItem == null) return;
        _inventory.TrySplit(CurrentItem, CurrentItem.CurrentStack / 2);
        CloseRequest.Raise();
    }

    [RelayCommand(CanExecute = nameof(CanOpen))]
    private void Open()
    {
        if (CurrentItem == null) return;
        OpenPanelRequest.Raise(CurrentItem);
        CloseRequest.Raise();
    }

    [RelayCommand(CanExecute = nameof(CanFlip))]
    private void FlipHorizontal()
    {
        if (CurrentItem == null) return;
        _inventory.TryFlip(CurrentItem, true);
        CloseRequest.Raise();
    }

    [RelayCommand(CanExecute = nameof(CanFlip))]
    private void FlipVertical()
    {
        if (CurrentItem == null) return;
        _inventory.TryFlip(CurrentItem, false);
        CloseRequest.Raise();
    }
}

public partial class ItemInformationVM : DotPudica.Core.ViewModels.ViewModelBase
{
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _iconKey = "";
    [ObservableProperty] private string _stackText = "";

    /// <summary>Fills the info panel from catalog details and current stack.</summary>
    public void Bind(ItemDetails? details, int stack)
    {
        Title = details?.NameText ?? "";
        Description = details?.DescriptionKey ?? "";
        IconKey = details?.IconKey ?? "";
        StackText = stack > 1 ? stack.ToString() : "";
    }
}

public partial class InventoryPageVM : DotPudica.Core.ViewModels.ViewModelBase
{
    [ObservableProperty] private string _title = "Inventory";
    [ObservableProperty] private bool _isOpen;

    public EquipmentLayout Layout { get; }
    public TetrisGridVM Depository => Stash.Depository;
    public IReadOnlyList<TetrisSlotVM> Slots { get; }
    public IReadOnlyList<TetrisGridVM> PersistentGrids => Containers.PersistentGrids;
    public EquipmentPanelVM Equipment { get; }
    public ContainerPanelVM Containers { get; }
    public StashPanelVM Stash { get; }

    public InventoryPageVM(
        IGridFactory grids,
        IPointerGridSession session,
        IInventoryTreeCache tree,
        IItemCatalog catalog,
        IItemVmRegistry registry,
        IInventoryService inventory,
        EquipmentLayout? layout = null)
    {
        Layout = layout ?? new EquipmentLayout();
        var depository = grids.Create(CtisSettings.DepositoryColumns, CtisSettings.DepositoryRows);
        depository.GridGuid = InventoryTreeIds.Depository;
        session.DepositoryGrid = depository;
        inventory.Apply(InventoryCommand.ResizeContainer(
            depository.GridGuid,
            depository.GridSizeWidth,
            depository.GridSizeHeight,
            depository.LocalGridTileSizeWidth,
            depository.LocalGridTileSizeHeight));
        CharacterSlots = CreateGroup(EquipmentSlotGroup.Character);
        ContainerSlots = CreateGroup(EquipmentSlotGroup.Container);
        WeaponSlots = CreateGroup(EquipmentSlotGroup.Weapon);
        Slots = CharacterSlots.Concat(ContainerSlots).Concat(WeaponSlots).ToArray();
        Equipment = new EquipmentPanelVM(CharacterSlots, WeaponSlots, tree, catalog, registry);
        Containers = new ContainerPanelVM(ContainerSlots, tree, catalog, registry, grids, inventory);
        Stash = new StashPanelVM(depository, inventory, tree);
    }

    public IReadOnlyList<TetrisSlotVM> CharacterSlots { get; }
    public IReadOnlyList<TetrisSlotVM> ContainerSlots { get; }
    public IReadOnlyList<TetrisSlotVM> WeaponSlots { get; }

    /// <summary>Returns an existing pocket/coffer grid or creates one with the given size.</summary>
    public TetrisGridVM GetOrCreatePersistentGrid(int index, int width, int height)
        => Containers.GetOrCreatePersistentGrid(index, width, height);

    /// <summary>Returns an existing persistent grid by id or creates one with the given size.</summary>
    public TetrisGridVM GetOrCreatePersistentGrid(string gridGuid, int width, int height)
        => Containers.GetOrCreatePersistentGrid(gridGuid, width, height);

    /// <summary>Resizes the stash grid, packing current items into the new bounds.</summary>
    public bool TryConfigureDepository(int columns, int rows, float tileSize)
        => Stash.TryConfigure(columns, rows, tileSize);

    /// <summary>Finds the first equipment slot of the given type.</summary>
    public TetrisSlotVM? FindSlot(InventorySlotType type)
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            if (Slots[i].SlotType == type)
                return Slots[i];
        }
        return null;
    }

    private TetrisSlotVM[] CreateGroup(EquipmentSlotGroup group)
    {
        var specs = Layout.OfGroup(group);
        var slots = new TetrisSlotVM[specs.Count];
        for (int i = 0; i < specs.Count; i++)
            slots[i] = CreateSlot(specs[i]);
        return slots;
    }

    private static TetrisSlotVM CreateSlot(EquipmentSlotSpec spec)
        => new()
        {
            SlotType = spec.SlotType,
            SlotIndex = spec.SlotIndex,
            TitleKey = spec.TitleKey,
            SlotSize = new Size2(
                CtisSettings.GridTileSizeWidth * spec.CellsWidth,
                CtisSettings.GridTileSizeHeight * spec.CellsHeight)
        };

    /// <summary>Rebinds grids and slots from the inventory tree after a load.</summary>
    public void RebuildFromCache()
    {
        using var _ = CtisTrace.Scope("Inventory.RebuildFromCache");
        Stash.RebuildFromCache();
        Containers.RebuildFromCache();
        Equipment.RebuildFromCache();
    }

    /// <summary>Detaches live item views without clearing persisted tree data.</summary>
    public void DetachItems()
    {
        using var _ = CtisTrace.Scope("Inventory.DetachItems");
        Equipment.DetachItems();
        Containers.DetachItems();
        Stash.DetachItems();
    }

    internal static void ApplyCachedConfig(TetrisGridVM grid, IInventoryTreeCache tree)
    {
        if (!string.IsNullOrEmpty(grid.GridGuid)
            && tree.TryGetContainer(grid.GridGuid, out var node)
            && node.GridSizeWidth > 0
            && node.GridSizeHeight > 0)
        {
            grid.ApplyConfig(
                node.GridSizeWidth,
                node.GridSizeHeight,
                node.LocalGridTileSizeWidth,
                node.LocalGridTileSizeHeight);
            return;
        }
        grid.ApplyConfig(
            grid.GridSizeWidth,
            grid.GridSizeHeight,
            grid.LocalGridTileSizeWidth,
            grid.LocalGridTileSizeHeight);
    }

    internal static void ClearGridItems(TetrisGridVM grid)
    {
        foreach (var item in grid.OwnerItemsDic.Values)
            grid.RequestRemoveItemView(item);
        grid.OwnerItemsDic = new Dictionary<string, TetrisItemVM>();
    }

    internal static void DetachSlots(IEnumerable<TetrisSlotVM> slots)
    {
        foreach (var slot in slots)
        {
            if (slot.RelatedTetrisItem != null)
                slot.RemoveTetrisItem(false);
        }
    }

    internal static void RestoreSlot(
        TetrisSlotVM slot,
        IInventoryTreeCache tree,
        IItemCatalog catalog,
        IItemVmRegistry registry)
    {
        var current = slot.RelatedTetrisItem;
        if (current != null)
            slot.RemoveTetrisItem(true);
        var items = tree.GetItems(InventoryTreeIds.Slot(slot.SlotIndex));
        TetrisItemPersistentData? data = null;
        foreach (var item in items)
        {
            data = item;
            break;
        }
        if (data == null) return;
        var details = catalog.GetById(data.ItemId);
        var vm = registry.GetOrCreate(details, data, slot);
        slot.TryPlaceTetrisItem(vm);
    }
}

public partial class FloatingGridVM : DotPudica.Core.ViewModels.ViewModelBase
{
    private readonly IInventoryTreeCache _tree;
    private readonly IGridFactory _grids;
    private readonly IInventoryService _inventory;

    public FloatingGridVM(IInventoryTreeCache tree, IGridFactory grids, IInventoryService inventory)
    {
        _tree = tree;
        _grids = grids;
        _inventory = inventory;
    }

    [ObservableProperty] private string _title = "";
    public TetrisItemVM? Item { get; private set; }

    public void BindItem(TetrisItemVM item)
    {
        Item = item;
        Title = item.ItemName;
    }

    public TetrisGridVM EnsureInnerGrid(TetrisItemVM item, int index, int width, int height)
        => ItemInnerGrid.Configure(item, index, _tree, _grids, _inventory, width, height);

    [RelayCommand]
    public void Organize() => OrganizeWithStrategy(InventorySortStrategy.Area);

    public void OrganizeWithStrategy(InventorySortStrategy strategy)
    {
        if (Item != null)
            _inventory.TryOrganizeItemGrids(Item, strategy);
    }
}

public partial class SaveSlotListVM : DotPudica.Core.ViewModels.ViewModelBase
{
    public System.Collections.ObjectModel.ObservableCollection<SaveSlotRowVM> Slots { get; } = new();

    public SaveSlotListVM(ISaveLoadService save)
    {
        for (int i = 0; i < save.SlotCount; i++)
            Slots.Add(new SaveSlotRowVM(save, i));
        Refresh();
    }

    /// <summary>Reloads every save-slot row from the store.</summary>
    public void Refresh()
    {
        foreach (var slot in Slots)
            slot.Refresh();
    }
}

public partial class SaveSlotRowVM : DotPudica.Core.ViewModels.ViewModelBase
{
    private readonly ISaveLoadService _save;

    public SaveSlotRowVM(ISaveLoadService save, int index)
    {
        _save = save;
        Index = index;
        Refresh();
    }

    public readonly InteractionRequest<int> SaveRequest = new();
    public readonly InteractionRequest<int> LoadRequest = new();
    public readonly InteractionRequest<int> DeleteRequest = new();

    [ObservableProperty] private int _index;
    [ObservableProperty] private string _statusKey = "CTIS_NULL";
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private bool _hasData;
    [ObservableProperty] private bool _isCorrupt;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private bool _canDelete;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    private bool _canLoad = true;

    /// <summary>Reads slot metadata into this row's bindable fields.</summary>
    public void Refresh()
    {
        var info = _save.GetSlot(Index);
        IsCorrupt = info.IsCorrupt;
        HasData = info.HasData;
        StatusText = info.HasData ? info.Timestamp : "";
        StatusKey = info.IsCorrupt ? "CTIS_SAVE_CORRUPT" : info.HasData ? "CTIS_SAVE_TIME" : "CTIS_NULL";
        CanDelete = info.HasData || info.IsCorrupt;
        CanLoad = !info.IsCorrupt;
    }

    [RelayCommand]
    private void Save()
    {
        SaveRequest.Raise(Index);
        Refresh();
    }

    [RelayCommand(CanExecute = nameof(CanLoad))]
    private void Load()
    {
        LoadRequest.Raise(Index);
        Refresh();
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private void Delete()
    {
        DeleteRequest.Raise(Index);
        Refresh();
    }
}
