using Ctis.Core;
using Ctis.Presentation;
using DotPudica.Core.Binding;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Composition;
using DotPudica.Core.Interactivity;
using DotPudica.Godot.Views;
using Godot;

namespace Ctis.Demo;

[DotPudicaView(typeof(SaveSlotRowVM), AutoInitialize = false, Pooled = true)]
public partial class SaveSlotRowView : PanelContainer
{
    [Inject] private IInventorySession _session = null!;

    [Export, BindTo(nameof(SaveSlotRowVM.StatusKey), Mode = BindingMode.OneWay)]
    private Label _status = null!;

    [Export, BindTo(nameof(SaveSlotRowVM.StatusText), Mode = BindingMode.OneWay)]
    private Label _time = null!;

    [Export, BindCommand(nameof(SaveSlotRowVM.SaveCommand))]
    private Button _save = null!;

    [Export, BindCommand(nameof(SaveSlotRowVM.LoadCommand))]
    private Button _load = null!;

    [Export, BindCommand(nameof(SaveSlotRowVM.DeleteCommand))]
    [BindTo(nameof(SaveSlotRowVM.CanDelete), Mode = BindingMode.OneWay, Target = "Visible")]
    private Button _delete = null!;

    [BindTo(nameof(SaveSlotRowVM.CanDelete), Mode = BindingMode.OneWay, Converter = typeof(BoolToMouseFilterConverter), Target = "MouseFilter")]
    private Button _deleteHit = null!;

    public override void _Ready() => InitializeView();
    public override void _ExitTree() => RecycleView();

    public void Bind(SaveSlotRowVM vm) => ActivateViewModel(vm);

    partial void OnViewReady()
    {
        _status = GetNode<Label>("Box/Status");
        _time = GetNode<Label>("Box/Time");
        _load = GetNode<Button>("Box/Load");
        _save = GetNode<Button>("Box/Save");
        _delete = GetNode<Button>("Box/Delete");
        _deleteHit = _delete;
    }

    [Subscribe("SaveRequest.Raised")]
    private void OnSaveRequested(object? sender, InteractionEventArgs<int> args)
        => _session.SaveSlot(args.Context);

    [Subscribe("LoadRequest.Raised")]
    private void OnLoadRequested(object? sender, InteractionEventArgs<int> args)
        => _session.LoadOrStart(args.Context);

    [Subscribe("DeleteRequest.Raised")]
    private void OnDeleteRequested(object? sender, InteractionEventArgs<int> args)
        => _session.DeleteSlot(args.Context);
}
