using System.Windows.Input;
using Ctis.Presentation;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Godot.Binding.ControlProxies;
using DotPudica.Godot.Views;
using Godot;

namespace Ctis.Demo;

[DotPudicaView(typeof(ItemDetailsRowVM), AutoInitialize = false, Pooled = true)]
public partial class DebugItemRowView : HBoxContainer, IItemsControlItem, IItemsControlItemCommand
{
    [Export, BindTo(nameof(ItemDetailsRowVM.Name))]
    private Label _name = null!;

    [Export, BindTo(nameof(ItemDetailsRowVM.IconKey), Converter = typeof(IconKeyToTextureConverter))]
    private TextureRect _icon = null!;

    [Export]
    private Button _add = null!;

    public override void _Ready() => InitializeView();
    public override void _ExitTree() => RecycleView();

    public ICommand? ItemCommand { get; set; }

    public object? DataContext
    {
        get => ViewModel;
        set
        {
            if (value is ItemDetailsRowVM vm)
                ActivateViewModel(vm);
        }
    }

    partial void OnViewReady()
    {
        _icon = GetNode<TextureRect>("Icon");
        _name = GetNode<Label>("Name");
        _add = GetNode<Button>("Add");
        _add.Pressed += OnAddPressed;
    }

    partial void OnViewDisposing()
    {
        if (_add != null && GodotObject.IsInstanceValid(_add))
            _add.Pressed -= OnAddPressed;
    }

    private void OnAddPressed()
    {
        if (ItemCommand?.CanExecute(ViewModel) == true)
            ItemCommand.Execute(ViewModel);
    }
}
