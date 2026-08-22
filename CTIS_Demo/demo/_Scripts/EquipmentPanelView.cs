using Ctis.Core;
using Ctis.Presentation;
using DotPudica.Core.ViewModels;
using DotPudica.Godot.Views;
using Godot;

namespace Ctis.Demo;

[DotPudicaView(typeof(EquipmentPanelVM), AutoInitialize = false, Pooled = true, Ownership = ViewModelOwnership.External)]
public partial class EquipmentPanelView : VBoxContainer
{
    private readonly Dictionary<InventorySlotType, Control> _characterSockets = new();
    private readonly List<Control> _weaponSockets = new();
    private readonly Dictionary<InventorySlotType, TetrisSlotView> _characterSlotViews = new();
    private readonly List<TetrisSlotView> _weaponSlotViews = new();

    public override void _Ready() => InitializeView();
    public override void _ExitTree() => RecycleView();

    public void BindPanel(EquipmentPanelVM vm) => ActivateViewModel(vm);

    partial void OnViewReady()
    {
        _characterSockets[InventorySlotType.Helmet] = GetNode<Control>("GearHost/Gear/Paperdoll/Helmet");
        _characterSockets[InventorySlotType.HeadMountedEquipment] = GetNode<Control>("GearHost/Gear/Paperdoll/HeadMounted");
        _characterSockets[InventorySlotType.Melee] = GetNode<Control>("GearHost/Gear/Paperdoll/Melee");
        _characterSockets[InventorySlotType.ShortWeapon] = GetNode<Control>("GearHost/Gear/Paperdoll/ShortWeapon");
        _characterSockets[InventorySlotType.Pants] = GetNode<Control>("GearHost/Gear/Paperdoll/Pants");
        _characterSockets[InventorySlotType.Shoes] = GetNode<Control>("GearHost/Gear/Paperdoll/Shoes");

        _weaponSockets.Clear();
        _weaponSockets.Add(GetNode<Control>("GearHost/Gear/Weapons/PrimaryWeapon"));
        _weaponSockets.Add(GetNode<Control>("GearHost/Gear/Weapons/SecondaryWeapon"));

        foreach (var (type, socket) in _characterSockets)
        {
            if (!_characterSlotViews.TryGetValue(type, out var view) || !GodotObject.IsInstanceValid(view))
            {
                ClearStaticPreviews(socket);
                view = CtisRuntime.CreateSlotView();
                view.SetAnchorsPreset(LayoutPreset.FullRect);
                socket.AddChild(view);
                _characterSlotViews[type] = view;
            }
        }

        for (int i = 0; i < _weaponSockets.Count; i++)
        {
            if (_weaponSlotViews.Count <= i || !GodotObject.IsInstanceValid(_weaponSlotViews[i]))
            {
                ClearStaticPreviews(_weaponSockets[i]);
                var view = CtisRuntime.CreateSlotView();
                view.SetAnchorsPreset(LayoutPreset.FullRect);
                _weaponSockets[i].AddChild(view);
                if (_weaponSlotViews.Count <= i)
                    _weaponSlotViews.Add(view);
                else
                    _weaponSlotViews[i] = view;
            }
        }
    }

    private static void ClearStaticPreviews(Node parent)
    {
        foreach (var child in parent.GetChildren())
        {
            if (child is not TetrisSlotView)
                child.QueueFree();
        }
    }

    partial void OnViewModelBound()
    {
        if (ViewModel == null) return;

        foreach (var slot in ViewModel.CharacterSlots)
        {
            if (_characterSlotViews.TryGetValue(slot.SlotType, out var view))
                view.BindSlot(slot);
        }

        for (int i = 0; i < ViewModel.WeaponSlots.Count && i < _weaponSlotViews.Count; i++)
        {
            _weaponSlotViews[i].BindSlot(ViewModel.WeaponSlots[i]);
        }
    }

    partial void OnViewDisposing()
    {
    }
}
