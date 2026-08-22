using Ctis.Core;
using DotPudica.Core.Composition;
using DotPudica.Core.ViewModels;
using DotPudica.Godot.Views;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using AppContext = DotPudica.Godot.AppContext;

namespace Ctis.Demo;

[DotPudicaView(typeof(InventoryPageVM), Pooled = true, Ownership = ViewModelOwnership.External)]
public partial class InventoryWindow : GodotWindow
{
    private EquipmentPanelView _equipment = null!;
    private ContainerPanelView _containers = null!;
    private StashPanelView _stash = null!;

    public override void _Ready() => InitializeView();

    public override void _ExitTree()
    {
        RecycleView();
        base._ExitTree();
    }

    public InventoryWindow()
    {
        WindowType = WindowType.Full;
        SetAnchorsPreset(LayoutPreset.FullRect);
    }

    [ViewModelFactory]
    private InventoryPageVM ResolvePage()
        => AppContext.Current.Services.GetRequiredService<InventoryPageVM>();

    partial void OnViewReady()
    {
        _equipment = GetNode<EquipmentPanelView>("Root/GearPane/Shell/EquipmentPanel");
        _containers = GetNode<ContainerPanelView>("Root/InventoryPane/Shell/ContainerPanel");
        _stash = GetNode<StashPanelView>("Root/StashPane/Shell/StashPanel");
    }

    partial void OnViewModelBound()
    {
        if (ViewModel == null) return;
        _equipment.BindPanel(ViewModel.Equipment);
        _containers.BindPanel(ViewModel.Containers);
        _stash.BindPanel(ViewModel.Stash);
    }
}
