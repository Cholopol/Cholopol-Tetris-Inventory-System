using TetrisCoordLib.Core.Math;

namespace Ctis.Core;

/// <summary>Unique occupants covering a shape. <see cref="UniqueCount"/> of 2 means two or more.</summary>
public readonly struct OccupancyCoverage
{
    public OccupancyCoverage(bool outOfBounds, int uniqueCount, string? firstGuid)
    {
        OutOfBounds = outOfBounds;
        UniqueCount = uniqueCount;
        FirstGuid = firstGuid;
    }

    public bool OutOfBounds { get; }
    public int UniqueCount { get; }
    public string? FirstGuid { get; }
    public static OccupancyCoverage Oob { get; } = new(true, 0, null);
}

/// <summary>
/// Cell occupancy for a container. Cells store a 1-based index into
/// <see cref="Occupants"/>; 0 is empty. Storage is a row-major <c>int[]</c>
/// (<c>index = y * Width + x</c>). The live board lives on
/// <see cref="ContainerNode"/> and is patched when items change.
/// </summary>
public sealed class OccupancyBoard
{
    private readonly int[] _cells;
    private readonly List<string> _occupants = new();
    private readonly Dictionary<string, int> _indexByGuid = new(StringComparer.Ordinal);
    private readonly Dictionary<string, OccupantFootprint> _footprints = new(StringComparer.Ordinal);

    public OccupancyBoard(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
        _cells = new int[Width * Height];
    }

    public int Width { get; }
    public int Height { get; }

    /// <summary>Guids occupying this board, in 1-based cell-index order.</summary>
    public IReadOnlyList<string> Occupants => _occupants;

    /// <summary>Returns the cached board for a container, applying pending occupancy patches.</summary>
    public static OccupancyBoard For(IInventoryTreeCache tree, IItemCatalog catalog, string containerId)
    {
        if (tree.TryGetContainer(containerId, out var node))
            return node.EnsureOccupancy(catalog);
        return new OccupancyBoard(1, 1);
    }

    /// <summary>Builds occupancy from a live grid's current occupants.</summary>
    public static OccupancyBoard FromGrid(TetrisGridVM grid)
    {
        var board = new OccupancyBoard(grid.GridSizeWidth, grid.GridSizeHeight);
        foreach (var item in grid.OwnerItemsDic.Values)
            board.Mark(item.Guid, item.LocalGridCoordinate, item.TetrisCoordinateSet);
        return board;
    }

    /// <summary>True when a single cell is inside the board.</summary>
    public bool PositionCheck(int x, int y)
        => x >= 0 && y >= 0 && x < Width && y < Height;

    /// <summary>True when an axis-aligned footprint stays inside the board.</summary>
    public bool BoundryCheck(int posX, int posY, int width, int height)
        => posX >= 0 && posY >= 0 && posX + width <= Width && posY + height <= Height;

    /// <summary>True when every shape cell at origin is inside the board.</summary>
    public bool ContainsShape(IReadOnlyList<Vec2I> shape, Vec2I origin)
    {
        if (shape.Count == 0) return false;
        for (int i = 0; i < shape.Count; i++)
        {
            int x = origin.X + shape[i].X;
            int y = origin.Y + shape[i].Y;
            if (!PositionCheck(x, y)) return false;
        }
        return true;
    }

    /// <summary>Marks the shape as occupied when every cell is empty and in bounds.</summary>
    public bool TryOccupy(string guid, IReadOnlyList<Vec2I> shape, Vec2I origin)
    {
        if (!CanPlace(shape, origin)) return false;
        Mark(guid, origin, shape);
        return true;
    }

    /// <summary>Returns the occupant guid covering a cell, or null.</summary>
    public string? GetOccupant(int x, int y)
    {
        if (!PositionCheck(x, y)) return null;
        return OccupantAt(Offset(x, y));
    }

    /// <summary>Occupant guid covering a cell, treating <paramref name="excludeGuid"/> as empty.</summary>
    public string? GetOccupant(int x, int y, string? excludeGuid)
    {
        if (!PositionCheck(x, y)) return null;
        return OccupantAt(Offset(x, y), excludeGuid);
    }

    /// <summary>Occupant guid covering a cell, treating <paramref name="exclude"/> as empty.</summary>
    public string? GetOccupant(int x, int y, ISet<string>? exclude)
    {
        if (!PositionCheck(x, y)) return null;
        return OccupantAt(Offset(x, y), exclude);
    }

    /// <summary>True when every shape cell at origin is in bounds and empty.</summary>
    public bool CanPlace(IReadOnlyList<Vec2I> shape, Vec2I origin)
        => CanPlace(shape, origin, (string?)null);

    /// <summary>True when every shape cell at origin is in bounds and empty after excluding a single guid.</summary>
    public bool CanPlace(IReadOnlyList<Vec2I> shape, Vec2I origin, string? excludeGuid)
    {
        if (shape.Count == 0) return false;
        for (int i = 0; i < shape.Count; i++)
        {
            int x = origin.X + shape[i].X;
            int y = origin.Y + shape[i].Y;
            if (!PositionCheck(x, y)) return false;
            if (OccupantAt(Offset(x, y), excludeGuid) != null) return false;
        }
        return true;
    }

    /// <summary>True when every shape cell at origin is in bounds and empty after excludes.</summary>
    public bool CanPlace(IReadOnlyList<Vec2I> shape, Vec2I origin, ISet<string>? exclude)
    {
        if (shape.Count == 0) return false;
        for (int i = 0; i < shape.Count; i++)
        {
            int x = origin.X + shape[i].X;
            int y = origin.Y + shape[i].Y;
            if (!PositionCheck(x, y)) return false;
            if (OccupantAt(Offset(x, y), exclude) != null) return false;
        }
        return true;
    }

    /// <summary>
    /// Unique occupants covering a shape without allocating a set.
    /// <see cref="OccupancyCoverage.UniqueCount"/> is 0, 1, or 2 meaning two-or-more.
    /// </summary>
    public OccupancyCoverage ScanCoverage(IReadOnlyList<Vec2I> shape, Vec2I origin, string? excludeGuid)
    {
        using var traceScope = CtisTrace.Scope("Occupancy.ScanCoverage");
        string? first = null;
        int unique = 0;
        for (int i = 0; i < shape.Count; i++)
        {
            int x = origin.X + shape[i].X;
            int y = origin.Y + shape[i].Y;
            if (!PositionCheck(x, y))
                return OccupancyCoverage.Oob;
            var guid = OccupantAt(Offset(x, y));
            if (guid == null) continue;
            if (excludeGuid != null && string.Equals(guid, excludeGuid, StringComparison.Ordinal))
                continue;
            if (unique == 0)
            {
                first = guid;
                unique = 1;
            }
            else if (unique == 1 && !string.Equals(guid, first, StringComparison.Ordinal))
                unique = 2;
        }
        return new OccupancyCoverage(false, unique, first);
    }

    /// <summary>Occupant guids covered by the shape, or null when any cell is out of bounds.</summary>
    public HashSet<string>? CollectCoverageGuids(IReadOnlyList<Vec2I> shape, Vec2I origin)
        => CollectCoverageGuids(shape, origin, (string?)null);

    /// <summary>Occupant guids covered by the shape, skipping a single <paramref name="excludeGuid"/>.</summary>
    public HashSet<string>? CollectCoverageGuids(IReadOnlyList<Vec2I> shape, Vec2I origin, string? excludeGuid)
    {
        var overlapped = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < shape.Count; i++)
        {
            int x = origin.X + shape[i].X;
            int y = origin.Y + shape[i].Y;
            if (!PositionCheck(x, y)) return null;
            var guid = OccupantAt(Offset(x, y));
            if (guid == null) continue;
            if (excludeGuid != null && string.Equals(guid, excludeGuid, StringComparison.Ordinal)) continue;
            overlapped.Add(guid);
        }
        return overlapped;
    }

    /// <summary>Occupant guids covered by the shape, skipping <paramref name="exclude"/>.</summary>
    public HashSet<string>? CollectCoverageGuids(IReadOnlyList<Vec2I> shape, Vec2I origin, ISet<string>? exclude)
    {
        var overlapped = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < shape.Count; i++)
        {
            int x = origin.X + shape[i].X;
            int y = origin.Y + shape[i].Y;
            if (!PositionCheck(x, y)) return null;
            var guid = OccupantAt(Offset(x, y), exclude);
            if (guid != null)
                overlapped.Add(guid);
        }
        return overlapped;
    }

    /// <summary>Out of bounds or occupied; null when the shape fits.</summary>
    public InventoryPlacementBlockReason? BlockReason(IReadOnlyList<Vec2I> shape, Vec2I origin)
        => BlockReason(shape, origin, null);

    /// <summary>Out of bounds or occupied after excludes; null when the shape fits.</summary>
    public InventoryPlacementBlockReason? BlockReason(IReadOnlyList<Vec2I> shape, Vec2I origin, ISet<string>? exclude)
    {
        using var traceScope = CtisTrace.Scope("Occupancy.BlockReason");
        bool occupied = false;
        for (int i = 0; i < shape.Count; i++)
        {
            int x = origin.X + shape[i].X;
            int y = origin.Y + shape[i].Y;
            if (!PositionCheck(x, y)) return InventoryPlacementBlockReason.OutOfBounds;
            if (OccupantAt(Offset(x, y), exclude) != null)
                occupied = true;
        }
        return occupied ? InventoryPlacementBlockReason.Occupied : null;
    }

    /// <summary>Finds an origin in the ring of cells around a reference footprint.</summary>
    public bool TryFindAdjacentOrigin(
        IReadOnlyList<Vec2I> shape,
        int newWidth,
        int newHeight,
        Vec2I referenceOrigin,
        int referenceWidth,
        int referenceHeight,
        out Vec2I origin)
        => TryFindAdjacentOrigin(
            shape, newWidth, newHeight, referenceOrigin, referenceWidth, referenceHeight, null, out origin);

    /// <summary>Finds an origin in the ring of cells around a reference footprint.</summary>
    public bool TryFindAdjacentOrigin(
        IReadOnlyList<Vec2I> shape,
        int newWidth,
        int newHeight,
        Vec2I referenceOrigin,
        int referenceWidth,
        int referenceHeight,
        ISet<string>? exclude,
        out Vec2I origin)
    {
        int refX = referenceOrigin.X;
        int refY = referenceOrigin.Y;
        int vStart = refY - (newHeight - 1);
        int vEnd = refY + referenceHeight - 1;
        int hStart = refX - (newWidth - 1);
        int hEnd = refX + referenceWidth - 1;

        for (int y = vStart; y <= vEnd; y++)
        {
            var candidate = new Vec2I(refX - newWidth, y);
            if (CanPlace(shape, candidate, exclude))
            {
                origin = candidate;
                return true;
            }
        }
        for (int y = vStart; y <= vEnd; y++)
        {
            var candidate = new Vec2I(refX + referenceWidth, y);
            if (CanPlace(shape, candidate, exclude))
            {
                origin = candidate;
                return true;
            }
        }
        for (int x = hStart; x <= hEnd; x++)
        {
            var candidate = new Vec2I(x, refY - newHeight);
            if (CanPlace(shape, candidate, exclude))
            {
                origin = candidate;
                return true;
            }
        }
        for (int x = hStart; x <= hEnd; x++)
        {
            var candidate = new Vec2I(x, refY + referenceHeight);
            if (CanPlace(shape, candidate, exclude))
            {
                origin = candidate;
                return true;
            }
        }
        origin = Vec2I.Zero;
        return false;
    }

    /// <summary>Finds the first origin where <paramref name="shape"/> fits, skipping full rows.</summary>
    public bool TryFindFreeOrigin(IReadOnlyList<Vec2I> shape, out Vec2I origin)
        => TryFindFreeOrigin(shape, (string?)null, out origin);

    /// <summary>Finds the first origin where <paramref name="shape"/> fits, skipping full rows and a single excludeGuid.</summary>
    public bool TryFindFreeOrigin(IReadOnlyList<Vec2I> shape, string? excludeGuid, out Vec2I origin)
    {
        for (int row = 0; row < Height; row++)
        {
            if (RowHasNoHole(row, excludeGuid)) continue;
            for (int column = 0; column < Width; column++)
            {
                var candidate = new Vec2I(column, row);
                if (!CanPlace(shape, candidate, excludeGuid)) continue;
                origin = candidate;
                return true;
            }
        }
        origin = Vec2I.Zero;
        return false;
    }

    /// <summary>Finds the first origin where <paramref name="shape"/> fits, skipping full rows.</summary>
    public bool TryFindFreeOrigin(IReadOnlyList<Vec2I> shape, ISet<string>? exclude, out Vec2I origin)
    {
        for (int row = 0; row < Height; row++)
        {
            if (RowHasNoHole(row, exclude)) continue;
            for (int column = 0; column < Width; column++)
            {
                var candidate = new Vec2I(column, row);
                if (!CanPlace(shape, candidate, exclude)) continue;
                origin = candidate;
                return true;
            }
        }
        origin = Vec2I.Zero;
        return false;
    }

    /// <summary>Finds a free origin, trying <paramref name="preferred"/> then the other facings, skipping a single excludeGuid.</summary>
    public bool TryFindFreeOrigin(
        ItemOccupancy? occupancy,
        IReadOnlyList<OccupancyPatch>? patches,
        Dir preferred,
        bool flipH,
        bool flipV,
        out Vec2I origin,
        out Dir direction,
        string? excludeGuid)
    {
        origin = Vec2I.Zero;
        direction = preferred;
        var preferredShape = ItemShape.Resolve(occupancy, patches, preferred, flipH, flipV);
        if (preferredShape.Width == 1 && preferredShape.Height == 1)
        {
            if (TryFindFreeOrigin(preferredShape.Cells, excludeGuid, out origin))
                return true;
            return false;
        }

        foreach (Dir dir in DirUtil.PreferThenOthers(preferred))
        {
            var resolved = dir == preferred
                ? preferredShape
                : ItemShape.Resolve(occupancy, patches, dir, flipH, flipV);
            if (!TryFindFreeOrigin(resolved.Cells, excludeGuid, out origin)) continue;
            direction = dir;
            return true;
        }
        return false;
    }

    /// <summary>Finds a free origin, trying <paramref name="preferred"/> then the other facings.</summary>
    public bool TryFindFreeOrigin(
        ItemOccupancy? occupancy,
        IReadOnlyList<OccupancyPatch>? patches,
        Dir preferred,
        bool flipH,
        bool flipV,
        out Vec2I origin,
        out Dir direction,
        ISet<string>? exclude = null)
    {
        origin = Vec2I.Zero;
        direction = preferred;
        var preferredShape = ItemShape.Resolve(occupancy, patches, preferred, flipH, flipV);
        if (preferredShape.Width == 1 && preferredShape.Height == 1)
        {
            if (TryFindFreeOrigin(preferredShape.Cells, exclude, out origin))
                return true;
            return false;
        }

        foreach (Dir dir in DirUtil.PreferThenOthers(preferred))
        {
            var resolved = dir == preferred
                ? preferredShape
                : ItemShape.Resolve(occupancy, patches, dir, flipH, flipV);
            if (!TryFindFreeOrigin(resolved.Cells, exclude, out origin)) continue;
            direction = dir;
            return true;
        }
        return false;
    }

    /// <summary>Clears cells and occupant indices so this board can be reused at the same size.</summary>
    public void Clear()
    {
        Array.Clear(_cells);
        _occupants.Clear();
        _indexByGuid.Clear();
        _footprints.Clear();
    }

    /// <summary>Fills this board from persisted items. Caller owns size.</summary>
    public void Rebuild(IEnumerable<TetrisItemPersistentData> items, IItemCatalog catalog)
    {
        Clear();
        foreach (var data in items)
        {
            var details = catalog.GetById(data.ItemId);
            var shape = ItemShape.Resolve(details?.Occupancy, data.OccupancyPatches, data);
            Mark(data.ItemGuid, data.OriginPosition, shape.Cells);
        }
    }

    /// <summary>Deep-copies cells, occupant indices, and footprints.</summary>
    public OccupancyBoard Clone()
    {
        var copy = new OccupancyBoard(Width, Height);
        Array.Copy(_cells, copy._cells, _cells.Length);
        copy._occupants.AddRange(_occupants);
        foreach (var pair in _indexByGuid)
            copy._indexByGuid[pair.Key] = pair.Value;
        foreach (var pair in _footprints)
            copy._footprints[pair.Key] = pair.Value.Clone();
        return copy;
    }

    /// <summary>Clone with the given guids unmarked. Does not mutate this board.</summary>
    public OccupancyBoard Without(IEnumerable<string> guids)
    {
        var copy = Clone();
        foreach (var guid in guids)
            copy.Unmark(guid);
        return copy;
    }

    /// <summary>Writes <paramref name="guid"/> onto every in-bounds shape cell, replacing any previous footprint.</summary>
    public void Mark(string guid, Vec2I origin, IReadOnlyList<Vec2I> shape)
    {
        if (_footprints.TryGetValue(guid, out var previous))
            ClearFootprint(previous, IndexOf(guid));
        int index = IndexOf(guid);
        var cells = new Vec2I[shape.Count];
        for (int i = 0; i < shape.Count; i++)
        {
            cells[i] = shape[i];
            int x = origin.X + shape[i].X;
            int y = origin.Y + shape[i].Y;
            if (PositionCheck(x, y))
                _cells[Offset(x, y)] = index;
        }
        _footprints[guid] = new OccupantFootprint(origin, cells);
    }

    /// <summary>Clears the stored shape for <paramref name="guid"/> without scanning the whole board.</summary>
    public void Unmark(string guid)
    {
        if (!_indexByGuid.TryGetValue(guid, out int index)) return;
        if (!_footprints.TryGetValue(guid, out var footprint)) return;
        ClearFootprint(footprint, index);
        _footprints.Remove(guid);
    }

    private int Offset(int x, int y) => y * Width + x;

    private string? OccupantAt(int offset)
    {
        int index = _cells[offset];
        if (index <= 0 || index > _occupants.Count) return null;
        return _occupants[index - 1];
    }

    private string? OccupantAt(int offset, string? excludeGuid)
    {
        var guid = OccupantAt(offset);
        if (guid != null && excludeGuid != null && string.Equals(guid, excludeGuid, StringComparison.Ordinal)) return null;
        return guid;
    }

    private string? OccupantAt(int offset, ISet<string>? exclude)
    {
        var guid = OccupantAt(offset);
        if (guid != null && exclude != null && exclude.Contains(guid)) return null;
        return guid;
    }

    private void ClearFootprint(OccupantFootprint footprint, int index)
    {
        var origin = footprint.Origin;
        var cells = footprint.Cells;
        for (int i = 0; i < cells.Length; i++)
        {
            int x = origin.X + cells[i].X;
            int y = origin.Y + cells[i].Y;
            if (!PositionCheck(x, y)) continue;
            int offset = Offset(x, y);
            if (_cells[offset] == index)
                _cells[offset] = 0;
        }
    }

    private int IndexOf(string guid)
    {
        if (_indexByGuid.TryGetValue(guid, out int index))
            return index;
        _occupants.Add(guid);
        index = _occupants.Count;
        _indexByGuid[guid] = index;
        return index;
    }

    private bool RowHasNoHole(int row, string? excludeGuid)
    {
        int start = row * Width;
        if (excludeGuid == null)
        {
            for (int x = 0; x < Width; x++)
            {
                if (_cells[start + x] == 0)
                    return false;
            }
            return true;
        }

        for (int x = 0; x < Width; x++)
        {
            if (OccupantAt(start + x, excludeGuid) == null)
                return false;
        }
        return true;
    }

    private bool RowHasNoHole(int row, ISet<string>? exclude)
    {
        int start = row * Width;
        if (exclude == null)
        {
            for (int x = 0; x < Width; x++)
            {
                if (_cells[start + x] == 0)
                    return false;
            }
            return true;
        }

        for (int x = 0; x < Width; x++)
        {
            if (OccupantAt(start + x, exclude) == null)
                return false;
        }
        return true;
    }

    private readonly struct OccupantFootprint
    {
        public OccupantFootprint(Vec2I origin, Vec2I[] cells)
        {
            Origin = origin;
            Cells = cells;
        }

        public Vec2I Origin { get; }
        public Vec2I[] Cells { get; }

        public OccupantFootprint Clone()
        {
            var copy = new Vec2I[Cells.Length];
            Array.Copy(Cells, copy, Cells.Length);
            return new OccupantFootprint(Origin, copy);
        }
    }
}
