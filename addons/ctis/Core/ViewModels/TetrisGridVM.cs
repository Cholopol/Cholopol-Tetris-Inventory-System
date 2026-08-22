using CommunityToolkit.Mvvm.ComponentModel;
using TetrisCoordLib.Core.Math;

namespace Ctis.Core;

public partial class TetrisGridVM : TetrisItemContainerVM
{
	private TetrisItemVM?[] _occupied = new TetrisItemVM?[1];
	private Dictionary<string, TetrisItemVM> _ownerItems = new();
	private readonly IInventoryTreeCache _tree;
	private readonly IItemVmRegistry _registry;
	private readonly IItemCatalog _catalog;
	private readonly IGridFactory? _grids;
	private string _registeredGuid = "";

	public TetrisGridVM(int width, int height, IInventoryTreeCache tree, IItemVmRegistry registry, IItemCatalog catalog, IGridFactory? grids = null)
	{
		_tree = tree;
		_registry = registry;
		_catalog = catalog;
		_grids = grids;
		ApplyConfig(width, height, CtisSettings.GridTileSizeWidth, CtisSettings.GridTileSizeHeight);
	}

	[ObservableProperty] private int _gridSizeWidth = 1;
	[ObservableProperty] private int _gridSizeHeight = 1;
	[ObservableProperty] private float _localGridTileSizeWidth = CtisSettings.GridTileSizeWidth;
	[ObservableProperty] private float _localGridTileSizeHeight = CtisSettings.GridTileSizeHeight;
	[ObservableProperty] private Size2 _size;
	[ObservableProperty] private string _gridGuid = "";

	public override Dictionary<string, TetrisItemVM> OwnerItemsDic
	{
		get => _ownerItems;
		set
		{
			_ownerItems = value;
			foreach (var item in _ownerItems.Values)
				item.CurrentTetrisContainer = this;
			RebuildOccupiedCells();
			OnPropertyChanged();
		}
	}

	public event Action<TetrisItemVM, int, int>? PlaceItemViewRequested;
	public event Action<TetrisItemVM>? RemoveItemViewRequested;

	partial void OnGridGuidChanged(string value)
	{
		if (!string.IsNullOrEmpty(_registeredGuid) && _registeredGuid != value)
			_grids?.UnregisterVM(_registeredGuid);
		if (!string.IsNullOrEmpty(value))
		{
			_grids?.RegisterVM(value, this);
			_registeredGuid = value;
		}
		else
			_registeredGuid = "";
		RebindFromTree();
	}

	/// <summary>Rebuilds cell size and occupancy from a new grid configuration.</summary>
	public void ApplyConfig(int width, int height, float unitWidth, float unitHeight)
	{
		GridSizeWidth = Math.Max(1, width);
		GridSizeHeight = Math.Max(1, height);
		LocalGridTileSizeWidth = unitWidth;
		LocalGridTileSizeHeight = unitHeight;
		Size = new Size2(GridSizeWidth * LocalGridTileSizeWidth, GridSizeHeight * LocalGridTileSizeHeight);
		_occupied = new TetrisItemVM?[GridSizeWidth * GridSizeHeight];
		RebindFromTree(applyTreeConfig: false);
	}

	/// <summary>Changes cell extent without rebinding occupants from Tree.</summary>
	public void SetCellExtent(int width, int height)
	{
		GridSizeWidth = Math.Max(1, width);
		GridSizeHeight = Math.Max(1, height);
		Size = new Size2(GridSizeWidth * LocalGridTileSizeWidth, GridSizeHeight * LocalGridTileSizeHeight);
		RebuildOccupiedCells();
	}

	/// <summary>Rebinds occupants from the inventory tree.</summary>
	public void RefreshFromTree() => RebindFromTree();

	/// <summary>True when cell (x, y) is occupied.</summary>
	public bool HasItem(int x, int y)
		=> PositionCheck(x, y) && _occupied[OccupiedOffset(x, y)] != null;

	/// <summary>Returns the item covering cell (x, y), or null.</summary>
	public TetrisItemVM? GetTetrisItemVM(int x, int y)
		=> PositionCheck(x, y) ? _occupied[OccupiedOffset(x, y)] : null;

	/// <summary>Converts a grid origin into local pixel position.</summary>
	public Size2 CalculatePositionOnGrid(TetrisItemVM item, int posX, int posY)
		=> new(posX * LocalGridTileSizeWidth, posY * LocalGridTileSizeHeight);

	/// <summary>Updates tile size and resizes every occupant to match.</summary>
	public void SetLocalTileSize(float unitWidth, float unitHeight)
	{
		LocalGridTileSizeWidth = Math.Max(1f, unitWidth);
		LocalGridTileSizeHeight = Math.Max(1f, unitHeight);
		Size = new Size2(GridSizeWidth * LocalGridTileSizeWidth, GridSizeHeight * LocalGridTileSizeHeight);
		foreach (var item in _ownerItems.Values)
			item.UpdateSize(this);
	}

	/// <summary>Tree occupancy when this grid is bound to a container; otherwise a VM snapshot.</summary>
	public OccupancyBoard ResolveBoard()
	{
		if (!string.IsNullOrEmpty(GridGuid) && _tree.TryGetContainer(GridGuid, out _))
			return OccupancyBoard.For(_tree, _catalog, GridGuid);
		return OccupancyBoard.FromGrid(this);
	}

	public override bool TryPlaceTetrisItem(TetrisItemVM tetrisItem, int posX = 0, int posY = 0)
	{
		if (!InventoryLogic.CanPlaceAt(this, tetrisItem, posX, posY)) return false;
		PlaceTetrisItem(tetrisItem, posX, posY);
		return true;
	}

	public override void PlaceTetrisItem(TetrisItemVM tetrisItem, int posX = 0, int posY = 0)
	{
		tetrisItem.LocalGridCoordinate = new Vec2I(posX, posY);
		tetrisItem.CurrentTetrisContainer = this;
		tetrisItem.UpdateSize(this);
		_ownerItems[tetrisItem.Guid] = tetrisItem;
		var cells = tetrisItem.TetrisCoordinateSet;
		var origin = new Vec2I(posX, posY);
		for (int i = 0; i < cells.Count; i++)
		{
			var c = origin + cells[i];
			if (PositionCheck(c.X, c.Y))
				_occupied[OccupiedOffset(c.X, c.Y)] = tetrisItem;
		}
		PlaceItemViewRequested?.Invoke(tetrisItem, posX, posY);
	}

	/// <summary>Clears this grid's occupancy for an item by the given origin and shape.</summary>
	public TetrisItemVM? RemoveTetrisItem(TetrisItemVM toReturn, int x, int y, IReadOnlyList<Vec2I> shape, bool destroyView = true)
	{
		var originRm = new Vec2I(x, y);
		for (int i = 0; i < shape.Count; i++)
		{
			var c = originRm + shape[i];
			if (PositionCheck(c.X, c.Y) && _occupied[OccupiedOffset(c.X, c.Y)] == toReturn)
				_occupied[OccupiedOffset(c.X, c.Y)] = null;
		}
		_ownerItems.Remove(toReturn.Guid);
		if (destroyView)
			RemoveItemViewRequested?.Invoke(toReturn);
		return toReturn;
	}

	/// <summary>Asks the bound view to despawn an item without changing occupancy.</summary>
	public void RequestRemoveItemView(TetrisItemVM item)
		=> RemoveItemViewRequested?.Invoke(item);

	/// <summary>True when the axis-aligned footprint stays inside the grid.</summary>
	public bool BoundryCheck(int posX, int posY, int width, int height)
		=> posX >= 0 && posY >= 0 && posX + width <= GridSizeWidth && posY + height <= GridSizeHeight;

	/// <summary>True when a single cell is inside the grid.</summary>
	public bool PositionCheck(int posX, int posY)
		=> posX >= 0 && posY >= 0 && posX < GridSizeWidth && posY < GridSizeHeight;

	private void RebuildOccupiedCells()
	{
		_occupied = new TetrisItemVM?[GridSizeWidth * GridSizeHeight];
		foreach (var item in _ownerItems.Values)
		{
			var itemCells = item.TetrisCoordinateSet;
			var itemOrigin = item.LocalGridCoordinate;
			for (int i = 0; i < itemCells.Count; i++)
			{
				var c = itemOrigin + itemCells[i];
				if (PositionCheck(c.X, c.Y))
					_occupied[OccupiedOffset(c.X, c.Y)] = item;
			}
		}
	}

	private int OccupiedOffset(int x, int y) => y * GridSizeWidth + x;

	private void RebindFromTree(bool applyTreeConfig = true)
	{
		if (string.IsNullOrEmpty(GridGuid)) return;
		bool hasConfig = _tree.TryGetContainer(GridGuid, out var node);
		var treeItems = _tree.GetItems(GridGuid);
		var treeGuids = new HashSet<string>(StringComparer.Ordinal);
		foreach (var data in treeItems)
			treeGuids.Add(data.ItemGuid);

		if (_ownerItems.Count > 0)
		{
			var currentItems = new List<TetrisItemVM>(_ownerItems.Values);
			for (int i = 0; i < currentItems.Count; i++)
			{
				var item = currentItems[i];
				RemoveTetrisItem(
					item,
					item.LocalGridCoordinate.X,
					item.LocalGridCoordinate.Y,
					item.TetrisCoordinateSet,
					!treeGuids.Contains(item.Guid));
			}
		}

		if (applyTreeConfig && hasConfig)
		{
			SetLocalTileSize(node.LocalGridTileSizeWidth, node.LocalGridTileSizeHeight);
			SetCellExtent(node.GridSizeWidth, node.GridSizeHeight);
		}

		foreach (var data in treeItems)
		{
			if (!_registry.TryGet(data.ItemGuid, out var vm))
			{
				var details = _catalog.GetById(data.ItemId);
				vm = _registry.GetOrCreate(details, data, this);
			}
			vm.ProjectFrom(data);
			PlaceTetrisItem(vm, data.OriginPosition.X, data.OriginPosition.Y);
		}
	}
}
