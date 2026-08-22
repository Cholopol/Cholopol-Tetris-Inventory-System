using CommunityToolkit.Mvvm.ComponentModel;
using TetrisCoordLib.Core.Math;

namespace Ctis.Core;

public partial class TetrisItemVM : DotPudica.Core.ViewModels.ViewModelBase
{
    private readonly PlacementConfig _placement;
    private readonly List<OccupancyPatch> _patches = new();
    private int _dragDimRefCount;
    private Rgba _dragDimOriginal = Rgba.White;
    private bool _hasDragDimOriginal;
    private readonly List<TetrisGridVM?> _ownedGrids = new();
    private readonly List<TetrisGridVM> _activeOwnedGrids = new();

    public TetrisItemVM(
        ItemDetails? details,
        TetrisItemPersistentData? data,
        TetrisItemContainerVM? container,
        PlacementConfig? placement = null)
    {
        _placement = placement ?? new PlacementConfig();
        CurrentTetrisContainer = container;
        if (data?.OccupancyPatches != null)
        {
            foreach (var patch in data.OccupancyPatches)
            {
                if (patch != null)
                    _patches.Add(patch.Clone());
            }
        }
        ItemDetails = details;
        MaxStack = details?.MaxStack ?? 0;
        _suppressRotationUpdate = true;
        if (data != null)
        {
            Direction = data.Direction;
            LocalGridCoordinate = data.OriginPosition;
            Rotated = DirUtil.IsRotated(Direction);
            FlipH = data.FlipH;
            FlipV = data.FlipV;
            Guid = !string.IsNullOrEmpty(data.ItemGuid) ? data.ItemGuid : System.Guid.NewGuid().ToString();
            CurrentStack = data.Stack > 0 ? data.Stack : 1;
        }
        else
        {
            Direction = details?.DefaultDirection ?? Dir.Down;
            Rotated = DirUtil.IsRotated(Direction);
            CurrentStack = MaxStack > 0 ? MaxStack : 1;
            Guid = System.Guid.NewGuid().ToString();
        }
        _suppressRotationUpdate = false;
        RefreshFromDetails();
        UpdateRotation();
    }

    public int MaxStack { get; private set; }
    public bool IsStackable => MaxStack > 1;
    public string Guid { get; set; } = "";
    private bool _suppressRotationUpdate;

    [ObservableProperty] private int _width;
    [ObservableProperty] private int _height;
    [ObservableProperty] private TetrisItemContainerVM? _currentTetrisContainer;
    [ObservableProperty] private ItemDetails? _itemDetails;
    [ObservableProperty] private Dir _direction = Dir.Down;
    [ObservableProperty] private bool _rotated;
    [ObservableProperty] private bool _flipH;
    [ObservableProperty] private bool _flipV;
    [ObservableProperty] private Vec2I _localGridCoordinate;
    [ObservableProperty] private List<Vec2I> _tetrisCoordinateSet = new();
    [ObservableProperty] private bool _isRaycastTargetEnabled = true;
    [ObservableProperty] private string _iconKey = "";
    [ObservableProperty] private InventorySlotType _slotType = InventorySlotType.Pocket;
    [ObservableProperty] private Rgba _imageColor = Rgba.White;
    [ObservableProperty] private Rgba _rarityColor = Rgba.Clear;
    [ObservableProperty] private int _currentStack;
    [ObservableProperty] private string _itemName = "";
    [ObservableProperty] private Size2 _size;

    public IReadOnlyList<OccupancyPatch> OccupancyPatches => _patches;
    public IReadOnlyList<TetrisGridVM> OwnedTetrisGridsVM => _activeOwnedGrids;

    partial void OnItemDetailsChanged(ItemDetails? value) => RefreshFromDetails();
    partial void OnDirectionChanged(Dir value)
    {
        if (!_suppressRotationUpdate) UpdateRotation();
    }
    partial void OnRotatedChanged(bool value)
    {
        if (!_suppressRotationUpdate) UpdateRotation();
    }
    partial void OnFlipHChanged(bool value)
    {
        if (!_suppressRotationUpdate) UpdateRotation();
    }
    partial void OnFlipVChanged(bool value)
    {
        if (!_suppressRotationUpdate) UpdateRotation();
    }
    partial void OnCurrentTetrisContainerChanged(TetrisItemContainerVM? value) => UpdateSize(value);

    private void RefreshFromDetails()
    {
        if (ItemDetails == null) return;
        IconKey = ItemDetails.IconKey;
        SlotType = ItemDetails.SlotType;
        RarityColor = _placement.GetRarityColor(ItemDetails.Rarity);
        ItemName = ItemDetails.NameText;
        MaxStack = ItemDetails.MaxStack;
        UpdateRotation();
    }

    /// <summary>Projects persisted identity, stack, facing, and occupancy onto this VM.</summary>
    public void ProjectFrom(TetrisItemPersistentData data)
    {
        _patches.Clear();
        if (data.OccupancyPatches != null)
        {
            foreach (var patch in data.OccupancyPatches)
            {
                if (patch != null)
                    _patches.Add(patch.Clone());
            }
        }
        _suppressRotationUpdate = true;
        Direction = data.Direction;
        Rotated = DirUtil.IsRotated(data.Direction);
        FlipH = data.FlipH;
        FlipV = data.FlipV;
        LocalGridCoordinate = data.OriginPosition;
        _suppressRotationUpdate = false;
        if (data.Stack > 0)
            CurrentStack = data.Stack;
        UpdateRotation();
    }

    /// <summary>Rotates the item one quarter-turn clockwise.</summary>
    public void Rotate()
    {
        Direction = DirUtil.Next(Direction);
        Rotated = DirUtil.IsRotated(Direction);
    }

    /// <summary>Applies a named occupancy patch and recomputes the footprint.</summary>
    public bool ApplyOccupancyPatch(string key, IEnumerable<Vec2I>? add, IEnumerable<Vec2I>? remove)
    {
        if (string.IsNullOrEmpty(key)) return false;
        RemovePatchesByKey(key);
        _patches.Add(new OccupancyPatch
        {
            Key = key,
            Add = add != null ? new List<Vec2I>(add) : new List<Vec2I>(),
            Remove = remove != null ? new List<Vec2I>(remove) : new List<Vec2I>()
        });
        UpdateRotation();
        return true;
    }

    /// <summary>Removes a named occupancy patch and recomputes the footprint.</summary>
    public bool RemoveOccupancyPatch(string key)
    {
        if (RemovePatchesByKey(key) == 0) return false;
        UpdateRotation();
        return true;
    }

    private int RemovePatchesByKey(string key)
    {
        int removed = 0;
        for (int i = _patches.Count - 1; i >= 0; i--)
        {
            if (string.Equals(_patches[i].Key, key, StringComparison.Ordinal))
            {
                _patches.RemoveAt(i);
                removed++;
            }
        }
        return removed;
    }

    private void UpdateRotation()
    {
        if (ItemDetails == null) return;
        var rotated = ItemShape.Resolve(ItemDetails.Occupancy, _patches, Direction, FlipH, FlipV);
        TetrisCoordinateSet = new List<Vec2I>(rotated.Cells);
        Width = rotated.Width;
        Height = rotated.Height;
        UpdateSize(CurrentTetrisContainer);
    }

    /// <summary>Restores a cached facing and occupancy without recomputing from catalog data.</summary>
    public void RestoreCachedShape(Dir dir, bool rotated, bool flipH, bool flipV, IReadOnlyList<Vec2I> cells, int width, int height)
    {
        _suppressRotationUpdate = true;
        Direction = dir;
        Rotated = rotated;
        FlipH = flipH;
        FlipV = flipV;
        if (!ReferenceEquals(TetrisCoordinateSet, cells))
        {
            TetrisCoordinateSet.Clear();
            if (cells != null)
                TetrisCoordinateSet.AddRange(cells);
        }
        Width = width;
        Height = height;
        _suppressRotationUpdate = false;
        UpdateSize(CurrentTetrisContainer);
        OnPropertyChanged(nameof(TetrisCoordinateSet));
    }

    /// <summary>Updates pixel size from the current container's tile or slot size.</summary>
    public void UpdateSize(TetrisItemContainerVM? container)
    {
        if (container is TetrisGridVM grid)
        {
            Size = new Size2(Width * grid.LocalGridTileSizeWidth, Height * grid.LocalGridTileSizeHeight);
            return;
        }
        if (container is TetrisSlotVM slot)
        {
            Size = slot.SlotSize;
            return;
        }
        Size = new Size2(Width * CtisSettings.GridTileSizeWidth, Height * CtisSettings.GridTileSizeHeight);
    }

    /// <summary>Darkens the icon while a drag is active; nested calls are counted.</summary>
    public void BeginDragDim(float darkenFactor = 0.2f)
    {
        if (_dragDimRefCount == 0)
        {
            _dragDimOriginal = ImageColor;
            _hasDragDimOriginal = true;
            ImageColor = _dragDimOriginal.Darken(darkenFactor);
        }
        _dragDimRefCount++;
    }

    /// <summary>Restores the icon color when the last drag dim is released.</summary>
    public void EndDragDim()
    {
        if (_dragDimRefCount <= 0)
        {
            _dragDimRefCount = 0;
            return;
        }
        _dragDimRefCount--;
        if (_dragDimRefCount == 0 && _hasDragDimOriginal)
        {
            ImageColor = _dragDimOriginal;
            _hasDragDimOriginal = false;
        }
    }

    /// <summary>Looks up an already-created inner grid by guid.</summary>
    public bool TryGetOwnedGridVM(string guid, out TetrisGridVM vm)
    {
        vm = null!;
        foreach (var grid in _ownedGrids)
        {
            if (grid != null && grid.GridGuid == guid)
            {
                vm = grid;
                return true;
            }
        }
        return false;
    }

    /// <summary>Returns the inner grid at <paramref name="index"/>, creating it as <c>guid:index</c>.</summary>
    public TetrisGridVM GetOrCreateGridVM(int index, IGridFactory grids)
    {
        while (_ownedGrids.Count <= index)
            _ownedGrids.Add(null!);

        var containerId = Guid + ":" + index;
        var existing = _ownedGrids[index];
        if (existing != null)
        {
            existing.GridGuid = containerId;
            existing.RelatedTetrisItem = this;
            return existing;
        }

        var created = grids.Create(1, 1);
        created.GridGuid = containerId;
        created.RelatedTetrisItem = this;
        _ownedGrids[index] = created;
        if (!_activeOwnedGrids.Contains(created))
            _activeOwnedGrids.Add(created);
        return created;
    }
}
