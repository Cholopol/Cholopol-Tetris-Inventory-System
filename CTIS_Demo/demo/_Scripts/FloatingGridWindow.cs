using Ctis.Core;
using Ctis.Presentation;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Godot.Views;
using Godot;
using AppContext = DotPudica.Godot.AppContext;

namespace Ctis.Demo;

[DotPudicaView(typeof(FloatingGridVM), Pooled = true)]
public partial class FloatingGridWindow : GodotWindow
{
    private ItemGridPanelHost _host = null!;
    private IBundle? _pending;

    [Export, BindTo(nameof(FloatingGridVM.Title))]
    private Label _title = null!;

    public override void _Ready() => InitializeView();

    public override void _ExitTree()
    {
        RecycleView();
        base._ExitTree();
    }

    public FloatingGridWindow()
    {
        WindowType = WindowType.Popup;
        MouseFilter = MouseFilterEnum.Stop;
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        CustomMinimumSize = new Vector2(160, 80);
        Size = CustomMinimumSize;
    }

    partial void OnViewReady()
    {
        _title = GetNode<Label>("Panel/Box/Banner/Title");
        _title.OffsetRight = -62f;
        var closeBtn = GetNode<TextureButton>("Panel/Box/Banner/Close");
        closeBtn.Pressed += OnClosePressed;

        var sortBtn = GetNodeOrNull<MenuButton>("Panel/Box/Banner/SortButton");
        if (sortBtn != null)
        {
            var popup = sortBtn.GetPopup();
            popup.Clear();
            popup.AddItem("CTIS_SORT_AREA", (int)InventorySortStrategy.Area);
            popup.AddItem("CTIS_SORT_TYPE", (int)InventorySortStrategy.SlotType);
            popup.AddItem("CTIS_SORT_RARITY", (int)InventorySortStrategy.Rarity);
            popup.AddItem("CTIS_SORT_ITEM_ID", (int)InventorySortStrategy.ItemId);
            popup.AutoTranslateMode = AutoTranslateModeEnum.Always;
            popup.IdPressed += OnSortStrategyPressed;
        }

        var bodyNode = GetNode<MarginContainer>("Panel/Box/Body");
        if (_host == null)
        {
            _host = new ItemGridPanelHost(false);
            _host.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            _host.SizeFlagsVertical = SizeFlags.ShrinkBegin;
            bodyNode.AddChild(_host);
        }
    }

    private void OnClosePressed() => AppContext.Current.WindowManager.Dismiss(this);

    private void OnSortStrategyPressed(long id)
    {
        ViewModel?.OrganizeWithStrategy((InventorySortStrategy)id);
    }

    protected override void OnCreate(IBundle? bundle)
    {
        _pending = bundle;
        ApplyBundle();
    }

    partial void OnViewModelBound() => ApplyBundle();

    partial void OnViewDisposing()
    {
        var closeBtn = GetNodeOrNull<TextureButton>("Panel/Box/Banner/Close");
        if (closeBtn != null && GodotObject.IsInstanceValid(closeBtn))
            closeBtn.Pressed -= OnClosePressed;

        var sortBtn = GetNodeOrNull<MenuButton>("Panel/Box/Banner/SortButton");
        if (sortBtn != null && GodotObject.IsInstanceValid(sortBtn))
            sortBtn.GetPopup().IdPressed -= OnSortStrategyPressed;

        _host?.Unbind();
    }

    private TetrisItemVM? _currentItem;

    private TetrisGridVM CreateInnerGrid(int index, int width, int height)
        => ViewModel!.EnsureInnerGrid(_currentItem!, index, width, height);

    private const float FloatHeaderHeight = 22f;

    private void ApplyBundle()
    {
        if (_pending == null || ViewModel == null || _host == null) return;
        var item = _pending.Get<TetrisItemVM>("item");
        _currentItem = item;
        ViewModel.BindItem(item);
        _host.BindItem(item, CreateInnerGrid);
        var gridSize = _host.PanelPixelSize;
        CustomMinimumSize = new Vector2(
            MathF.Max(120f, gridSize.X + 8f),
            gridSize.Y + FloatHeaderHeight + 8f);
        Size = CustomMinimumSize;
        _pending = null;
        CallDeferred(nameof(CenterOnScreen));
    }

    private void CenterOnScreen()
    {
        CtisUi.CenterWindowOnScreen(this);
    }
}
