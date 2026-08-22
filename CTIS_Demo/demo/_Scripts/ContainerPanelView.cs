using Ctis.Core;
using Ctis.Presentation;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Composition;
using DotPudica.Core.ViewModels;
using DotPudica.Godot.Views;
using Godot;

namespace Ctis.Demo;

[DotPudicaView(typeof(ContainerPanelVM), AutoInitialize = false, Pooled = true, Ownership = ViewModelOwnership.External)]
public partial class ContainerPanelView : VBoxContainer
{
    private ItemGridPanelHost _pocketHost = null!;
    private ItemGridPanelHost _cofferHost = null!;
    private VBoxContainer _containerHost = null!;
    private readonly List<TetrisSlotView> _slotViews = new();
    private readonly Dictionary<InventorySlotType, ItemGridPanelHost> _gearHosts = new();
    private bool _slotsCreated;
    private bool _gearPanelsCreated;

    public override void _Ready() => InitializeView();
    public override void _ExitTree() => RecycleView();

    public void BindPanel(ContainerPanelVM vm) => ActivateViewModel(vm);

    partial void OnViewReady()
    {
        if (_containerHost != null && GodotObject.IsInstanceValid(_containerHost))
            return;

        var host = GetNode<VBoxContainer>("Scroll/Host");
        _containerHost = GetNode<VBoxContainer>("Scroll/Host/ContainerHost");

        if (_pocketHost == null || !GodotObject.IsInstanceValid(_pocketHost))
        {
            var pocketWrap = BuildPersistentPanel("CTIS_POCKET", InventorySlotType.Pocket, out _pocketHost, strategy => ViewModel?.OrganizePersistentGridWithStrategy(InventoryTreeIds.Pocket(0), strategy));
            host.AddChild(pocketWrap);
            host.MoveChild(pocketWrap, 0);
        }

        if (_cofferHost == null || !GodotObject.IsInstanceValid(_cofferHost))
        {
            var cofferWrap = BuildPersistentPanel("CTIS_COFFER", InventorySlotType.Coffer, out _cofferHost, strategy => ViewModel?.OrganizePersistentGridWithStrategy(InventoryTreeIds.Coffer(0), strategy));
            host.AddChild(cofferWrap);
        }
    }

    partial void OnViewModelBound()
    {
        if (ViewModel == null) return;
        ClearGearGrids();
        EnsureGearPanels();
        EnsureSlotLayout();
        BindPocketHost();
        BindCofferHost();
        foreach (var slot in ViewModel.ContainerSlots)
            BindGearGrid(slot);
    }

    partial void OnViewDisposing()
    {
        ClearGearGrids();
        _pocketHost?.Unbind();
        _cofferHost?.Unbind();
    }

    [Subscribe(nameof(ContainerPanelVM.ContainerItemChanged))]
    private void OnContainerItemChanged(TetrisSlotVM slot) => BindGearGrid(slot);

    [Subscribe(nameof(ContainerPanelVM.ContainerItemCleared))]
    private void OnContainerItemCleared(TetrisItemVM item)
    {
        foreach (var pair in _gearHosts)
        {
            if (pair.Value.Item == item)
                pair.Value.Unbind();
        }
    }

    private Control BuildPersistentPanel(string titleKey, InventorySlotType type, out ItemGridPanelHost host, Action<InventorySortStrategy>? onOrganize = null)
    {
        var body = new HBoxContainer();
        body.AddThemeConstantOverride("separation", 8);
        body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        body.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        body.AddChild(CtisArt.CreateSlotFace(CtisArt.SlotBackground(type)));
        host = new ItemGridPanelHost(false);
        body.AddChild(host);
        return InventoryChrome.WrapEmbeddedBag(titleKey, body, onOrganize);
    }

    private Control BuildGearPanel(TetrisSlotVM slot)
    {
        var type = slot.SlotType;
        var titleKey = string.IsNullOrEmpty(slot.TitleKey) ? slot.SlotType.ToString() : slot.TitleKey;
        var body = new HBoxContainer();
        body.AddThemeConstantOverride("separation", 8);
        body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        body.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        body.Name = type.ToString();
        var host = new ItemGridPanelHost(false);
        _gearHosts[type] = host;
        body.AddChild(host);
        return InventoryChrome.WrapEmbeddedBag(titleKey, body, strategy => ViewModel?.OrganizeSlotWithStrategy(slot, strategy));
    }

    private void EnsureGearPanels()
    {
        if (ViewModel == null || _gearPanelsCreated || _containerHost == null) return;
        foreach (var slot in ViewModel.ContainerSlots)
        {
            _containerHost.AddChild(BuildGearPanel(slot));
        }
        _gearPanelsCreated = true;
    }

    private void EnsureSlotLayout()
    {
        if (ViewModel == null) return;
        if (!_slotsCreated)
        {
            foreach (var slot in ViewModel.ContainerSlots)
            {
                if (!_gearHosts.TryGetValue(slot.SlotType, out var host)) continue;
                var body = host.GetParent();
                if (body == null) continue;
                var view = CtisRuntime.CreateSlotView();
                view.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
                body.AddChild(view);
                body.MoveChild(view, 0);
                view.BindSlot(slot);
                _slotViews.Add(view);
            }
            _slotsCreated = true;
            return;
        }

        for (int i = 0; i < _slotViews.Count && i < ViewModel.ContainerSlots.Count; i++)
            _slotViews[i].BindSlot(ViewModel.ContainerSlots[i]);
    }

    private TetrisGridVM CreatePocketGrid(int index, int width, int height)
        => ViewModel!.GetOrCreatePersistentGrid(InventoryTreeIds.PocketPrefix + index, width, height);

    private TetrisGridVM CreateCofferGrid(int index, int width, int height)
        => ViewModel!.GetOrCreatePersistentGrid(InventoryTreeIds.CofferPrefix + index, width, height);

    private void BindPocketHost()
    {
        if (_pocketHost == null || ViewModel == null) return;
        _pocketHost.Unbind();
        _pocketHost.Visible = !string.IsNullOrWhiteSpace(CtisRuntime.PersistentPocketScenePath);
        if (_pocketHost.Visible)
            _pocketHost.BindPersistent(CtisRuntime.PersistentPocketScenePath, CreatePocketGrid);
    }

    private void BindCofferHost()
    {
        if (_cofferHost == null || ViewModel == null) return;
        _cofferHost.Unbind();
        _cofferHost.Visible = !string.IsNullOrWhiteSpace(CtisRuntime.PersistentCofferScenePath);
        if (_cofferHost.Visible)
            _cofferHost.BindPersistent(CtisRuntime.PersistentCofferScenePath, CreateCofferGrid);
    }

    private TetrisItemVM? _currentEquippedItem;

    private TetrisGridVM CreateGearInnerGrid(int index, int width, int height)
        => ViewModel!.EnsureInnerGrid(_currentEquippedItem!, index, width, height);

    private void BindGearGrid(TetrisSlotVM slot)
    {
        if (!_gearHosts.TryGetValue(slot.SlotType, out var host) || ViewModel == null) return;
        host.Unbind();
        var equipped = slot.RelatedTetrisItem;
        if (equipped?.ItemDetails?.HasInnerGrid == true)
        {
            _currentEquippedItem = equipped;
            host.BindItem(equipped, CreateGearInnerGrid);
        }
    }

    private void ClearGearGrids()
    {
        foreach (var host in _gearHosts.Values)
            host.Unbind();
    }
}
