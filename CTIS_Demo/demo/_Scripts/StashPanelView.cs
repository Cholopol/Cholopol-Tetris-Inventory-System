using Ctis.Core;
using Ctis.Presentation;
using DotPudica.Core.Binding;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.ViewModels;
using DotPudica.Godot.Views;
using Godot;

namespace Ctis.Demo;

[DotPudicaView(typeof(StashPanelVM), AutoInitialize = false, Pooled = true, Ownership = ViewModelOwnership.External)]
public partial class StashPanelView : VBoxContainer
{
    private TetrisGridView _grid = null!;

    [Export, BindTo(nameof(StashPanelVM.Columns), Mode = BindingMode.TwoWay, Converter = typeof(IntToDoubleConverter))]
    private SpinBox _stashColumns = null!;

    [Export, BindTo(nameof(StashPanelVM.Rows), Mode = BindingMode.TwoWay, Converter = typeof(IntToDoubleConverter))]
    private SpinBox _stashRows = null!;

    [Export, BindTo(nameof(StashPanelVM.CellSize), Mode = BindingMode.TwoWay, Converter = typeof(FloatToDoubleConverter))]
    private SpinBox _stashCell = null!;

    [Export]
    private MenuButton _sortBtn = null!;

    public override void _Ready() => InitializeView();
    public override void _ExitTree() => RecycleView();

    public void BindPanel(StashPanelVM vm) => ActivateViewModel(vm);

    partial void OnViewReady()
    {
        _stashColumns ??= GetNode<SpinBox>("ConfigRow/ColsField/ColsSpin");
        _stashRows ??= GetNode<SpinBox>("ConfigRow/RowsField/RowsSpin");
        _stashCell ??= GetNode<SpinBox>("ConfigRow/CellField/CellSpin");
        _sortBtn ??= GetNode<MenuButton>("ConfigRow/SortBtn");

        var popup = _sortBtn.GetPopup();
        popup.Clear();
        popup.AddItem("CTIS_SORT_AREA", (int)InventorySortStrategy.Area);
        popup.AddItem("CTIS_SORT_TYPE", (int)InventorySortStrategy.SlotType);
        popup.AddItem("CTIS_SORT_RARITY", (int)InventorySortStrategy.Rarity);
        popup.AddItem("CTIS_SORT_ITEM_ID", (int)InventorySortStrategy.ItemId);
        popup.AutoTranslateMode = AutoTranslateModeEnum.Always;
        popup.IdPressed += OnSortStrategyPressed;

        var scroll = GetNode<ScrollContainer>("Scroll");
        if (_grid == null || !GodotObject.IsInstanceValid(_grid))
        {
            _grid = CtisRuntime.CreateGridView();
            _grid.Name = "Depository";
            _grid.AutoFitTiles = false;
            _grid.FitToWidthOnly = false;
            scroll.AddChild(_grid);
        }
    }

    partial void OnViewModelBound()
    {
        if (ViewModel == null) return;
        _grid.BindGrid(ViewModel.Depository);
    }

    partial void OnViewDisposing()
    {
        if (_sortBtn != null && GodotObject.IsInstanceValid(_sortBtn))
        {
            _sortBtn.GetPopup().IdPressed -= OnSortStrategyPressed;
        }
    }

    private void OnSortStrategyPressed(long id)
    {
        ViewModel?.OrganizeWithStrategy((InventorySortStrategy)id);
    }
}
