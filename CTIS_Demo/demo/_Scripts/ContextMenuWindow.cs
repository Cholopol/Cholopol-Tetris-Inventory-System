using Ctis.Core;
using Ctis.Presentation;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Composition;
using DotPudica.Core.Interactivity;
using DotPudica.Godot.Views;
using Godot;

namespace Ctis.Demo;

[DotPudicaView(typeof(ContextMenuVM), Pooled = true)]
public partial class ContextMenuWindow : ContextMenuWindowBase
{
    private const float MenuWidth = 140f;
    private const float HeaderHeight = 18f;
    private const float ButtonHeight = 22f;
    private const float Separation = 2f;
    private const float PaddingVertical = 6f;
    private const float DividerHeight = 1f;

    private IBundle? _pending;

    [Inject] private IFloatingInventoryWindows _windows = null!;

    [Export, BindCommand(nameof(ContextMenuVM.CheckCommand))]
    private Button _check = null!;

    [Export, BindCommand(nameof(ContextMenuVM.OpenCommand))]
    [BindTo(nameof(ContextMenuVM.CanOpen), Target = "Visible")]
    private Button _open = null!;

    [Export, BindCommand(nameof(ContextMenuVM.SplitCommand))]
    [BindTo(nameof(ContextMenuVM.CanSplit), Target = "Visible")]
    private Button _split = null!;

    [Export, BindCommand(nameof(ContextMenuVM.FlipHorizontalCommand))]
    [BindTo(nameof(ContextMenuVM.CanFlip), Target = "Visible")]
    private Button _flipH = null!;

    [Export, BindCommand(nameof(ContextMenuVM.FlipVerticalCommand))]
    [BindTo(nameof(ContextMenuVM.CanFlip), Target = "Visible")]
    private Button _flipV = null!;

    private Control _menuRoot = null!;
    private PanelContainer _menuPanel = null!;
    private Control? _divider;
    private Label? _headerTitle;

    public override void _Ready() => InitializeView();

    public override void _ExitTree()
    {
        RecycleView();
        base._ExitTree();
    }

    public ContextMenuWindow()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
    }

    partial void OnViewReady()
    {
        _menuRoot = GetNode<Control>("MenuRoot");
        _menuPanel = GetNode<PanelContainer>("MenuRoot/MenuPanel");
        _check = GetNode<Button>("MenuRoot/MenuPanel/Box/Check");
        _open = GetNode<Button>("MenuRoot/MenuPanel/Box/Open");
        _split = GetNode<Button>("MenuRoot/MenuPanel/Box/Split");
        _divider = GetNodeOrNull<Control>("MenuRoot/MenuPanel/Box/Divider");
        _flipH = GetNode<Button>("MenuRoot/MenuPanel/Box/FlipH");
        _flipV = GetNode<Button>("MenuRoot/MenuPanel/Box/FlipV");
        _headerTitle = GetNodeOrNull<Label>("MenuRoot/MenuPanel/Box/Header/Title");

        ApplyActionVisibility();
    }

    protected override void OnCreate(IBundle? bundle)
    {
        _pending = bundle;
        ApplyBundle();
    }

    partial void OnViewModelBound() => ApplyBundle();

    private void ApplyBundle()
    {
        if (_pending == null || ViewModel == null || _menuRoot == null) return;
        var item = _pending.Get<TetrisItemVM>("item");
        ViewModel.CurrentItem = item;

        if (_headerTitle != null && item != null)
        {
            _headerTitle.AutoTranslateMode = AutoTranslateModeEnum.Disabled;
            var stack = item.CurrentStack;
            var localizedName = Tr(item.ItemName);
            _headerTitle.Text = stack > 1 ? $"{localizedName} x{stack}" : localizedName;
        }

        ApplyActionVisibility();

        Vector2? pos = null;
        if (_pending.ContainsKey("pos"))
        {
            pos = _pending.Get<Vector2>("pos");
        }

        PrepareMenuOpen(pos, _menuRoot);
        _pending = null;
    }

    private void ApplyActionVisibility()
    {
        bool canSplit = ViewModel?.CanSplit == true;
        bool canOpen = ViewModel?.CanOpen == true;
        bool canFlip = ViewModel?.CanFlip == true;

        if (_divider != null)
        {
            _divider.Visible = canFlip;
        }

        int visibleButtons = 1
            + (canOpen ? 1 : 0)
            + (canSplit ? 1 : 0)
            + (canFlip ? 2 : 0);

        float height = PaddingVertical + HeaderHeight + (visibleButtons * ButtonHeight) + (visibleButtons * Separation);
        if (_divider != null && _divider.Visible)
        {
            height += DividerHeight + Separation;
        }

        var minSize = new Vector2(MenuWidth, height);
        if (_menuRoot != null)
        {
            _menuRoot.CustomMinimumSize = minSize;
            _menuRoot.Size = minSize;
        }
        if (_menuPanel != null)
        {
            _menuPanel.CustomMinimumSize = minSize;
            _menuPanel.Size = minSize;
        }
    }

    [Subscribe("ShowInfoRequest.Raised")]
    private void OnShowInfo(object? sender, InteractionEventArgs<ItemDetails> args)
    {
        var item = ViewModel?.CurrentItem;
        _windows.ShowItemInfo(args.Context, item?.CurrentStack ?? 1);
    }

    [Subscribe("OpenPanelRequest.Raised")]
    private void OnOpenPanel(object? sender, InteractionEventArgs<TetrisItemVM> args)
        => _windows.ShowItemGrid(args.Context);

    [Subscribe("CloseRequest.Raised")]
    private void OnClose(object? sender, EventArgs args) => CloseMenu();
}
