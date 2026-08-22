using Ctis.Core;
using Ctis.Presentation;
using DotPudica.Core.Composition;
using DotPudica.Godot.Views;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using AppContext = DotPudica.Godot.AppContext;

namespace Ctis.Demo;

public partial class MobileControlsOverlay : CanvasLayer
{
    [Inject] private IInventorySession _session = null!;
    [Inject] private IItemDragMediator _mediator = null!;
    [Inject] private MobileSettingsManager _settings = null!;
    [Inject] private TetrisItemGhostVM _ghost = null!;

    private Control _root = null!;
    private HBoxContainer _navBar = null!;
    private Button _inventoryBtn = null!;
    private Button _saveBtn = null!;
    private Button _debugBtn = null!;

    private VBoxContainer _dragBar = null!;
    private Button _rotateBtn = null!;
    private Button _cancelBtn = null!;

    public override void _Ready()
    {
        _session ??= AppContext.Current.Services.GetRequiredService<IInventorySession>();
        _mediator ??= AppContext.Current.Services.GetRequiredService<IItemDragMediator>();
        _settings ??= AppContext.Current.Services.GetRequiredService<MobileSettingsManager>();
        _ghost ??= AppContext.Current.Services.GetRequiredService<TetrisItemGhostVM>();

        _root = GetNode<Control>("Root");
        _navBar = GetNode<HBoxContainer>("Root/NavBar");
        _inventoryBtn = GetNode<Button>("Root/NavBar/BtnInventory");
        _saveBtn = GetNode<Button>("Root/NavBar/BtnSave");
        _debugBtn = GetNode<Button>("Root/NavBar/BtnDebug");

        _dragBar = GetNode<VBoxContainer>("Root/DragBar");
        _rotateBtn = GetNode<Button>("Root/DragBar/BtnRotate");
        _cancelBtn = GetNode<Button>("Root/DragBar/BtnCancel");

        _inventoryBtn.Pressed += OnInventoryPressed;
        _saveBtn.Pressed += OnSavePressed;
        _debugBtn.Pressed += OnDebugPressed;
        _rotateBtn.Pressed += OnRotatePressed;
        _cancelBtn.Pressed += OnCancelPressed;

        _settings.MobileModeChanged += OnMobileModeChanged;
        Visible = _settings.IsMobileMode;
        SetProcess(true);
    }

    public override void _ExitTree()
    {
        if (_inventoryBtn != null && GodotObject.IsInstanceValid(_inventoryBtn))
            _inventoryBtn.Pressed -= OnInventoryPressed;
        if (_saveBtn != null && GodotObject.IsInstanceValid(_saveBtn))
            _saveBtn.Pressed -= OnSavePressed;
        if (_debugBtn != null && GodotObject.IsInstanceValid(_debugBtn))
            _debugBtn.Pressed -= OnDebugPressed;
        if (_rotateBtn != null && GodotObject.IsInstanceValid(_rotateBtn))
            _rotateBtn.Pressed -= OnRotatePressed;
        if (_cancelBtn != null && GodotObject.IsInstanceValid(_cancelBtn))
            _cancelBtn.Pressed -= OnCancelPressed;

        if (_settings != null)
        {
            _settings.MobileModeChanged -= OnMobileModeChanged;
        }
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;

        bool isDragging = _mediator.IsDragging;
        if (_dragBar != null && _dragBar.Visible != isDragging)
        {
            _dragBar.Visible = isDragging;
        }
    }

    private void OnMobileModeChanged(bool isMobile)
    {
        Visible = isMobile;
    }

    private void OnInventoryPressed() => _session.ToggleInventory();
    private void OnSavePressed() => _session.ToggleSavePanel();
    private void OnDebugPressed()
    {
        var wm = AppContext.Current.WindowManager;
        var existing = wm.Find<DebugItemListWindow>();
        if (existing != null && existing.IsWindowVisible)
            wm.Dismiss(existing, ignoreAnimation: true);
        else
            wm.ShowPooled<DebugItemListWindow>(new Bundle());
    }

    private void OnRotatePressed()
    {
        if (_ghost.OnDragging)
            _ghost.Rotate();
    }

    private void OnCancelPressed()
    {
        if (_ghost.OnDragging)
            _ghost.RequestEndDrag();
    }
}
