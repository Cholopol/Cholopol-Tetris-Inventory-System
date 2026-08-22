using System.ComponentModel;
using Ctis.Core;
using DotPudica.Core.Binding;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Composition;
using DotPudica.Core.Interactivity;
using DotPudica.Core.ViewModels;
using DotPudica.Godot.Views;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using AppContext = DotPudica.Godot.AppContext;

namespace Ctis.Presentation;

[DotPudicaView(typeof(TetrisItemGhostVM), Ownership = ViewModelOwnership.External)]
public partial class TetrisItemGhostView : Control
{
    private bool _rotateHeld;
    private TetrisItemGhostVM Vm => ViewModel!;

    [Inject] private IPointerGridViews _pointerViews = null!;
    [Inject] private IItemDragMediator _mediator = null!;
    private TetrisItemContainerVM? _syncedTarget;

    [Export, BindTo(nameof(TetrisItemGhostVM.IconKey), Mode = BindingMode.OneWay, Converter = typeof(IconKeyToTextureConverter))]
    private TextureRect _content = null!;

    [BindTo(nameof(TetrisItemGhostVM.DraggingGhostColor), Mode = BindingMode.OneWay, Converter = typeof(RgbaToColorConverter), Target = "Modulate")]
    private Control _ghostTint = null!;

    public override void _Ready() => InitializeView();
    public override void _ExitTree() => DisposeView();

    [ViewModelFactory]
    private TetrisItemGhostVM ResolveGhostViewModel()
        => AppContext.Current.Services.GetRequiredService<TetrisItemGhostVM>();

    public void BuildTree()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = TextureFilterEnum.Nearest;
        SetAnchorsPreset(LayoutPreset.TopLeft);
        CustomMinimumSize = new Vector2(32, 32);
        Size = CustomMinimumSize;
        SetProcess(true);
        SetProcessInput(false);
        ProcessPriority = -1000;
        _content = GetNodeOrNull<TextureRect>("Content") ?? new TextureRect { Name = "Content" };
        if (_content.GetParent() == null)
            AddChild(_content);
        _content.SetAnchorsPreset(LayoutPreset.FullRect);
        _content.MouseFilter = MouseFilterEnum.Ignore;
        _content.TextureFilter = TextureFilterEnum.Nearest;
        _content.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _content.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        _ghostTint = _content;
    }

    partial void OnViewReady()
    {
        if (_content == null)
            BuildTree();
        else
            _ghostTint = _content;
        SetProcess(true);
        SetProcessInput(false);
        ProcessPriority = -1000;
        IgnorePointer();
    }

    partial void OnViewModelBound()
    {
        if (ViewModel != null)
            ViewModel.PropertyChanged += OnGhostPropertyChanged;
        ApplyGhostVisual();
    }

    partial void OnViewDisposing()
    {
        if (ViewModel != null)
            ViewModel.PropertyChanged -= OnGhostPropertyChanged;
    }

    [Subscribe("InitializeFromItemRequest.Raised")]
    private void OnInitializeFromItem(object? sender, InteractionEventArgs<TetrisItemGhostVM.GhostInitData> args)
    {
        var data = args.Context;
        MoveToFront();
        GlobalPosition = new Vector2(data.WorldX, data.WorldY) - new Vector2(data.Size.Width * data.PivotX, data.Size.Height * data.PivotY);
        PivotOffset = new Vector2(data.Size.Width * data.PivotX, data.Size.Height * data.PivotY);
        Size = new Vector2(data.Size.Width, data.Size.Height);
        RotationDegrees = 0;
        ApplyGhostVisual();
        IgnorePointer();
    }

    [Subscribe("OnRotateRequest.Raised")]
    private void OnRotate(object? sender, InteractionEventArgs<Dir> args)
    {
        if (Vm.Size.Width > 0 && Vm.Size.Height > 0)
            Size = new Vector2(Vm.Size.Width, Vm.Size.Height);
        RotationDegrees = 0;
        ApplyContentRotation();
    }

    public override void _Process(double delta)
    {
        if (ViewModel == null || !Vm.OnDragging)
        {
            _rotateHeld = false;
            _syncedTarget = null;
            return;
        }
        IgnorePointer();
        SyncDropTarget();
        FollowDrag();
        if (Vm.OnDragging)
            Vm.TickPointer();
    }

    private void SyncDropTarget()
    {
        _pointerViews.RefreshFromMouse();
        TetrisItemContainerVM? target;
        if (_pointerViews.PreferSlotTarget && _pointerViews.SelectedSlot != null)
            target = _pointerViews.SelectedSlot;
        else if (_pointerViews.SelectedGrid != null)
            target = _pointerViews.SelectedGrid;
        else
            target = _pointerViews.SelectedSlot;
        if (ReferenceEquals(target, _syncedTarget))
            return;
        _syncedTarget = target;
        if (target is TetrisSlotVM slot)
            _mediator.SyncGhostTargetDroppedSlot(slot);
        else if (target is TetrisGridVM grid)
            _mediator.SyncGhostTargetDroppedGrid(grid);
    }

    private void EndDrag()
    {
        IgnorePointer();
        SyncDropTarget();
        Vm.RequestEndDrag();
        ApplyGhostVisual();
    }

    private void FollowDrag()
    {
        PivotOffset = Size / 2f;
        GlobalPosition = GetGlobalMousePosition() - PivotOffset;
        if (!Input.IsMouseButtonPressed(MouseButton.Left))
        {
            EndDrag();
            return;
        }

        var rotate = Input.IsPhysicalKeyPressed(Key.R);
        if (rotate && !_rotateHeld)
            Vm.Rotate();
        _rotateHeld = rotate;
    }

    private void IgnorePointer()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        if (_content != null)
            _content.MouseFilter = MouseFilterEnum.Ignore;
    }

    private void OnGhostPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TetrisItemGhostVM.Size)
            or nameof(TetrisItemGhostVM.Direction)
            or nameof(TetrisItemGhostVM.FlipH)
            or nameof(TetrisItemGhostVM.FlipV))
            ApplyGhostVisual();
    }

    private void ApplyGhostVisual()
    {
        if (_content == null || ViewModel == null) return;
        if (Vm.Size.Width > 0 && Vm.Size.Height > 0)
            Size = new Vector2(Vm.Size.Width, Vm.Size.Height);
        RotationDegrees = 0;
        ApplyContentRotation();
    }

    private void ApplyContentRotation()
    {
        if (_content == null || ViewModel == null) return;
        TetrisItemView.ApplyContentRotation(_content, Vm.Direction);
        _content.FlipH = Vm.FlipH;
        _content.FlipV = Vm.FlipV;
    }
}
