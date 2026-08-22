using Ctis.Presentation;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Godot.Views;
using Godot;

namespace Ctis.Demo;

[DotPudicaView(typeof(DebugItemListVM), Pooled = true)]
public partial class DebugItemListWindow : GodotWindow
{
    [ItemsSource(nameof(DebugItemListVM.Items), "res://CTIS_Demo/demo/DebugItemRow.tscn",
        ItemCommand = nameof(DebugItemListVM.AddCommand))]
    private VBoxContainer _list = null!;

    public override void _Ready() => InitializeView();

    public override void _ExitTree()
    {
        RecycleView();
        base._ExitTree();
    }

    public DebugItemListWindow()
    {
        WindowType = WindowType.Popup;
        MouseFilter = MouseFilterEnum.Stop;
        CustomMinimumSize = new Vector2(340, 420);
        Size = CustomMinimumSize;
        ZIndex = 1000;
        ZAsRelative = false;
    }

    protected override void OnShow()
    {
        base.OnShow();
        ZIndex = 1000;
        ZAsRelative = false;
        MoveToFront();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true })
            MoveToFront();
        base._GuiInput(@event);
    }

    partial void OnViewReady()
    {
        _list = GetNode<VBoxContainer>("Box/Scroll/List");
    }
}
