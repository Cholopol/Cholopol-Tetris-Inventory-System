using System.ComponentModel;
using Ctis.Core;
using DotPudica.Core.Binding;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Composition;
using DotPudica.Godot.Views;
using Godot;
using TetrisCoordLib.Core.Math;

namespace Ctis.Presentation;

[DotPudicaView(typeof(TetrisItemVM), AutoInitialize = false, Pooled = true)]
public partial class TetrisItemView : Control
{
    private bool _pressing;
    private Vector2 _pressGlobal;
    private double _pressTime;
    private bool _longPressTriggered;
    private const double LongPressDuration = 0.38;

    [Inject] private ItemViewRegistry _itemViews = null!;
    [Inject] private IFloatingInventoryWindows _windows = null!;
    [Inject] private IItemDragMediator _mediator = null!;

    public TetrisItemVM? BoundViewModel => ViewModel;

    public void BindItem(TetrisItemVM vm) => ActivateViewModel(vm);

    [Export, BindTo(nameof(TetrisItemVM.CurrentStack), Mode = BindingMode.OneWay, Converter = typeof(IntToStackTextConverter))]
    private Label _stack = null!;

    [Export, BindTo(nameof(TetrisItemVM.ItemName), Mode = BindingMode.OneWay)]
    private Label _name = null!;

    [Export, BindTo(nameof(TetrisItemVM.IconKey), Mode = BindingMode.OneWay, Converter = typeof(IconKeyToTextureConverter))]
    private TextureRect _icon = null!;

    [BindTo(nameof(TetrisItemVM.ImageColor), Mode = BindingMode.OneWay, Converter = typeof(RgbaToColorConverter), Target = "Modulate")]
    private Control _modulateHost = null!;

    [BindTo(nameof(TetrisItemVM.IsRaycastTargetEnabled), Mode = BindingMode.OneWay, Converter = typeof(BoolToMouseFilterConverter), Target = "MouseFilter")]
    private Control _hitHost = null!;

    public override void _Ready() => InitializeView();
    public override void _ExitTree() => RecycleView();

    public void BuildTree()
    {
        MouseFilter = MouseFilterEnum.Stop;
        ClipContents = true;
        TextureFilter = TextureFilterEnum.Nearest;
        _modulateHost = this;
        _hitHost = this;
        
        var legacyRarity = GetNodeOrNull<Node>("Rarity");
        if (legacyRarity != null)
        {
            RemoveChild(legacyRarity);
            legacyRarity.QueueFree();
        }

        _icon = GetNodeOrNull<TextureRect>("Content") ?? new TextureRect { Name = "Content" };
        _stack = GetNodeOrNull<Label>("Stack") ?? new Label { Name = "Stack" };
        _name = GetNodeOrNull<Label>("ItemName") ?? new Label { Name = "ItemName" };
        if (_icon.GetParent() == null) AddChild(_icon);
        if (_stack.GetParent() == null) AddChild(_stack);
        if (_name.GetParent() == null) AddChild(_name);

        _icon.MouseFilter = MouseFilterEnum.Ignore;
        _icon.TextureFilter = TextureFilterEnum.Nearest;
        _icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        ConfigureItemName(_name);
        _stack.MouseFilter = MouseFilterEnum.Ignore;
        _stack.HorizontalAlignment = HorizontalAlignment.Right;
        _stack.VerticalAlignment = VerticalAlignment.Bottom;
        _stack.AddThemeFontSizeOverride("font_size", 13);
        CtisUi.ApplyLabelOutline(_stack);
        LayoutLabels();
    }

    partial void OnViewReady()
    {
        ClipContents = true;
        if (_icon == null) BuildTree();
        else
        {
            var legacyRarity = GetNodeOrNull<Node>("Rarity");
            if (legacyRarity != null)
            {
                RemoveChild(legacyRarity);
                legacyRarity.QueueFree();
            }
            ConfigureItemName(_name);
        }
        _modulateHost = this;
        _hitHost = this;
        SetProcess(false);
    }

    partial void OnViewModelBound()
    {
        _itemViews.Register(this);
        ApplyVisual();
        ShowItemName(false);
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        if (ViewModel != null)
            ViewModel.PropertyChanged += OnItemPropertyChanged;
        SetProcess(false);
    }

    partial void OnViewDisposing()
    {
        _pressing = false;
        _longPressTriggered = false;
        _pressTime = 0;
        SetProcess(false);
        ShowItemName(false);
        MouseEntered -= OnMouseEntered;
        MouseExited -= OnMouseExited;
        if (ViewModel != null)
            ViewModel.PropertyChanged -= OnItemPropertyChanged;
        _itemViews.Unregister(this);
    }

    public override void _Process(double delta)
    {
        if (_pressing && !_longPressTriggered && ViewModel != null)
        {
            var mouse = GetGlobalMousePosition();
            if (PointerDrag.ExceedsStart(_pressGlobal.X, _pressGlobal.Y, mouse.X, mouse.Y))
            {
                _pressing = false;
                _longPressTriggered = false;
                _pressTime = 0;
                SetProcess(false);
                CtisTrace.Mark("Item.BeginDrag");
                _mediator.TryBeginDragFromItem(ViewModel, GhostInit());
                return;
            }

            _pressTime += delta;
            if (_pressTime >= LongPressDuration)
            {
                _longPressTriggered = true;
                _pressing = false;
                SetProcess(false);
                UiPick.BringOwningWindowToFront(this);
                OnMouseEntered();
                CtisTrace.Mark("Item.ShowContextMenu.LongPress");
                _windows.ShowContextMenu(ViewModel, _pressGlobal);
            }
        }
        else
        {
            SetProcess(false);
        }
    }

    public override bool _HasPoint(Vector2 point)
    {
        if (ViewModel?.ItemDetails == null) return false;
        var slot = ViewModel.CurrentTetrisContainer is TetrisSlotVM;
        return ShapeHitTest.Contains(ViewModel.TetrisCoordinateSet, ViewModel.Width, ViewModel.Height, point, Size, slot);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized && ViewModel != null)
        {
            LayoutLabels();
            QueueRedraw();
            CenterIcon(ViewModel.Direction);
            ApplyIconFlip();
        }
        base._Notification(what);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (ViewModel == null || @event is not InputEventMouseButton button) return;

        if (button.ButtonIndex == MouseButton.Left)
        {
            if (button.Pressed)
            {
                _windows.DismissContextMenu();
                UiPick.BringOwningWindowToFront(this);
                OnMouseEntered();
                _pressing = true;
                _pressTime = 0;
                _longPressTriggered = false;
                _pressGlobal = GetGlobalMousePosition();
                SetProcess(true);
            }
            else
            {
                _pressing = false;
                _longPressTriggered = false;
                _pressTime = 0;
                SetProcess(false);
            }
            AcceptEvent();
            return;
        }

        if (button.ButtonIndex == MouseButton.Right && button.Pressed)
        {
            _pressing = false;
            _longPressTriggered = false;
            _pressTime = 0;
            SetProcess(false);
            UiPick.BringOwningWindowToFront(this);
            OnMouseEntered();
            CtisTrace.Mark("Item.ShowContextMenu");
            _windows.ShowContextMenu(ViewModel, GetGlobalMousePosition());
            AcceptEvent();
        }
    }

    public void ApplyGridPlacement(int cellX, int cellY, float tileW, float tileH)
    {
        CtisTrace.Mark("ItemView.ApplyGridPlacement");
        SetAnchorsPreset(LayoutPreset.TopLeft);
        ZIndex = 0;
        ZAsRelative = true;
        RotationDegrees = 0;
        Position = new Vector2(cellX * tileW, cellY * tileH);
        RefreshVisual();
    }

    public void ApplySlotPlacement()
    {
        CtisTrace.Mark("ItemView.ApplySlotPlacement");
        Visible = true;
        ZIndex = 0;
        ZAsRelative = true;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        RotationDegrees = 0;
        RefreshVisual();
    }

    private TetrisItemGhostVM.GhostInitData GhostInit()
        => new(GlobalPosition.X, GlobalPosition.Y, 0f, 0f, new Size2(Size.X, Size.Y), ViewModel!.Direction);

    private void OnMouseEntered()
    {
        ShowItemName(true);
    }

    private void OnMouseExited() => ShowItemName(false);

    private void ShowItemName(bool show)
    {
        if (_name != null)
            _name.Visible = show;
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (ViewModel == null) return;
        if (e.PropertyName is nameof(TetrisItemVM.Size)
            or nameof(TetrisItemVM.Direction)
            or nameof(TetrisItemVM.FlipH)
            or nameof(TetrisItemVM.FlipV)
            or nameof(TetrisItemVM.RarityColor)
            or nameof(TetrisItemVM.Width)
            or nameof(TetrisItemVM.Height)
            or nameof(TetrisItemVM.TetrisCoordinateSet)
            or nameof(TetrisItemVM.CurrentTetrisContainer))
            ApplyVisual();
    }

    public void RefreshVisual() => ApplyVisual();

    private void ApplyVisual()
    {
        if (ViewModel == null) return;
        using var _ = CtisTrace.Scope("ItemView.ApplyVisual");
        if (ViewModel.CurrentTetrisContainer is not TetrisSlotVM)
            Size = new Vector2(ViewModel.Size.Width, ViewModel.Size.Height);
        RotationDegrees = 0;
        LayoutLabels();
        QueueRedraw();
        CenterIcon(ViewModel.Direction);
        ApplyIconFlip();
    }

    public override void _Draw()
    {
        if (ViewModel == null) return;
        var color = RgbaToColorConverter.Instance.Convert(ViewModel.RarityColor);
        if (color.A <= 0.001f) return;

        if (ViewModel.CurrentTetrisContainer is TetrisSlotVM)
        {
            DrawRect(new Rect2(Vector2.Zero, Size), color);
            return;
        }

        var cells = ViewModel.TetrisCoordinateSet;
        if (cells == null || cells.Count == 0) return;

        int gridW = Math.Max(1, ViewModel.Width);
        int gridH = Math.Max(1, ViewModel.Height);
        float tw = Size.X / gridW;
        float th = Size.Y / gridH;

        foreach (var cell in cells)
        {
            DrawRect(new Rect2(cell.X * tw, cell.Y * th, tw, th), color);
        }
    }

    private void ApplyIconFlip()
    {
        if (_icon == null || ViewModel == null) return;
        _icon.FlipH = ViewModel.FlipH;
        _icon.FlipV = ViewModel.FlipV;
    }

    private static void ConfigureItemName(Label name)
    {
        if (name == null) return;
        name.MouseFilter = MouseFilterEnum.Ignore;
        name.AutoTranslateMode = AutoTranslateModeEnum.Always;
        name.Visible = false;
        name.HorizontalAlignment = HorizontalAlignment.Center;
        name.VerticalAlignment = VerticalAlignment.Center;
        name.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        name.ClipText = false;
        name.AddThemeFontSizeOverride("font_size", 11);
        CtisUi.ApplyLabelOutline(name);
    }

    /// <summary>Edge inset from the occupancy AABB so wrapped names stay inside the cells.</summary>
    internal static Vector2 ItemNamePadding(Vector2 viewSize)
    {
        const float min = 3f;
        const float max = 8f;
        const float ratio = 0.08f;
        if (viewSize.X < 1f || viewSize.Y < 1f)
            return new Vector2(min, min);
        return new Vector2(
            Math.Clamp(viewSize.X * ratio, min, max),
            Math.Clamp(viewSize.Y * ratio, min, max));
    }

    private void LayoutLabels()
    {
        if (_name != null)
        {
            var pad = ItemNamePadding(Size);
            _name.SetAnchorsPreset(LayoutPreset.FullRect);
            _name.OffsetLeft = pad.X;
            _name.OffsetRight = -pad.X;
            _name.OffsetTop = pad.Y;
            _name.OffsetBottom = -pad.Y;
        }
        if (_stack != null)
        {
            _stack.SetAnchorsPreset(LayoutPreset.BottomRight);
            _stack.OffsetLeft = -36;
            _stack.OffsetTop = -20;
            _stack.OffsetRight = -4;
            _stack.OffsetBottom = -2;
        }
    }

    private void CenterIcon(Dir dir)
    {
        if (_icon == null) return;
        if (ViewModel?.CurrentTetrisContainer is TetrisSlotVM)
        {
            _icon.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _icon.OffsetLeft = 4;
            _icon.OffsetTop = 4;
            _icon.OffsetRight = -4;
            _icon.OffsetBottom = -4;
            _icon.PivotOffset = Vector2.Zero;
            _icon.RotationDegrees = 0;
            _icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            return;
        }

        LayoutGridIcon(_icon, dir, Size);
    }

    internal static void ApplyContentRotation(Control content, Dir dir)
    {
        if (content.GetParent() is not Control host) return;
        LayoutGridIcon(content, dir, host.Size);
    }

    /// <summary>
    /// Sizes the Down-facing icon to the occupancy AABB. Left/Right swap the unrotated rect
    /// so a 90° turn still fills the already-rotated view size.
    /// </summary>
    private static void LayoutGridIcon(Control content, Dir dir, Vector2 aabb)
    {
        const float pad = 2f;
        float innerW = MathF.Max(1f, aabb.X - pad * 2f);
        float innerH = MathF.Max(1f, aabb.Y - pad * 2f);
        var unrotated = DirUtil.IsRotated(dir)
            ? new Vector2(innerH, innerW)
            : new Vector2(innerW, innerH);
        content.SetAnchorsPreset(LayoutPreset.TopLeft);
        content.Size = unrotated;
        content.Position = (aabb - unrotated) / 2f;
        content.PivotOffset = unrotated / 2f;
        content.RotationDegrees = DirUtil.VisualDegrees(dir);
        if (content is TextureRect rect)
            rect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
    }
}
