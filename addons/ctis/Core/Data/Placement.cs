using TetrisCoordLib.Core.Math;

namespace Ctis.Core;

public readonly struct InventoryPlacementContext
{
    public TetrisItemVM? Item { get; }
    public TetrisItemContainerVM? TargetContainer { get; }
    public TetrisGridVM? TargetGrid { get; }
    public TetrisSlotVM? TargetSlot { get; }
    public TetrisItemVM? HoveredItem { get; }
    public Vec2I Origin { get; }
    public int ShapeWidth { get; }
    public int ShapeHeight { get; }
    public IReadOnlyList<Vec2I> ShapeCoordinates { get; }

    private InventoryPlacementContext(
        TetrisItemVM? item,
        TetrisItemContainerVM? targetContainer,
        Vec2I origin,
        int shapeWidth,
        int shapeHeight,
        IReadOnlyList<Vec2I>? shapeCoordinates,
        TetrisItemVM? hoveredItem)
    {
        Item = item;
        TargetContainer = targetContainer;
        TargetGrid = targetContainer as TetrisGridVM;
        TargetSlot = targetContainer as TetrisSlotVM;
        HoveredItem = hoveredItem;
        Origin = origin;
        ShapeWidth = shapeWidth;
        ShapeHeight = shapeHeight;
        ShapeCoordinates = shapeCoordinates ?? Array.Empty<Vec2I>();
    }

    /// <summary>Builds a placement context from a live item's occupancy.</summary>
    public static InventoryPlacementContext ForItem(TetrisItemVM item, TetrisItemContainerVM target, Vec2I origin)
        => new(item, target, origin, item.Width, item.Height, item.TetrisCoordinateSet, null);

    /// <summary>Builds a placement context from the dragged ghost's occupancy.</summary>
    public static InventoryPlacementContext ForGhost(
        TetrisItemVM? item,
        TetrisItemGhostVM ghost,
        TetrisItemContainerVM? target,
        Vec2I origin,
        TetrisItemVM? hoveredItem = null)
        => new(item, target, origin, ghost.Width, ghost.Height, ghost.TetrisCoordinateSet, hoveredItem);
}

public readonly struct InventoryDropResult
{
    public InventoryDropKind Kind { get; }
    public InventoryPlacementBlockReason Reason { get; }
    public TetrisItemVM? Overlap { get; }
    public TetrisGridVM? InnerGrid { get; }
    public Vec2I InnerOrigin { get; }
    public Dir InnerDirection { get; }

    private InventoryDropResult(
        InventoryDropKind kind,
        InventoryPlacementBlockReason reason,
        TetrisItemVM? overlap,
        TetrisGridVM? innerGrid = null,
        Vec2I innerOrigin = default,
        Dir innerDirection = default)
    {
        Kind = kind;
        Reason = reason;
        Overlap = overlap;
        InnerGrid = innerGrid;
        InnerOrigin = innerOrigin;
        InnerDirection = innerDirection;
    }

    /// <summary>A drop that violates a placement rule.</summary>
    public static InventoryDropResult Invalid(InventoryPlacementBlockReason reason)
        => new(InventoryDropKind.Invalid, reason, null);

    /// <summary>A drop onto empty cells or an empty slot.</summary>
    public static InventoryDropResult Vacant()
        => new(InventoryDropKind.Vacant, InventoryPlacementBlockReason.None, null);

    /// <summary>A drop that should merge stacks onto <paramref name="overlap"/>.</summary>
    public static InventoryDropResult Stack(TetrisItemVM overlap)
        => new(InventoryDropKind.Stack, InventoryPlacementBlockReason.None, overlap);

    /// <summary>A drop that should swap with fully covered occupants.</summary>
    public static InventoryDropResult Exchange(TetrisItemVM overlap)
        => new(InventoryDropKind.Exchange, InventoryPlacementBlockReason.None, overlap);

    /// <summary>A drop into <paramref name="host"/>'s inner grid at <paramref name="origin"/>.</summary>
    public static InventoryDropResult InsertIntoInner(
        TetrisGridVM grid,
        Vec2I origin,
        Dir direction,
        TetrisItemVM host)
        => new(InventoryDropKind.InsertIntoInner, InventoryPlacementBlockReason.None, host, grid, origin, direction);
}

public readonly struct InventoryDropPreviewCell
{
    public Vec2I Cell { get; }
    public InventoryDropCellKind Kind { get; }
    public Rgba Color { get; }

    public InventoryDropPreviewCell(Vec2I cell, InventoryDropCellKind kind, Rgba color)
    {
        Cell = cell;
        Kind = kind;
        Color = color;
    }
}

public sealed class InventoryDropPreview
{
    public TetrisGridVM? Grid { get; set; }
    public Vec2I Origin { get; set; }
    public InventoryDropResult Result { get; set; }
    public IReadOnlyList<InventoryDropPreviewCell> Cells { get; set; } = Array.Empty<InventoryDropPreviewCell>();
}

public sealed class InventoryPlacementBlockColorOverride
{
    public InventoryPlacementBlockReason Reason { get; set; }
    public Rgba Color { get; set; } = CtisSettings.HighlightInvalid;
}

public sealed class RarityColorOverride
{
    public ItemRarity Rarity { get; set; }
    public Rgba Color { get; set; }
}

public sealed class PlacementConfig
{
    public bool BlockSelfOwnedContainer { get; set; } = true;
    public bool BlockOutOfBounds { get; set; } = true;
    public bool BlockSlotOccupied { get; set; } = true;
    public bool BlockSlotTypeMismatch { get; set; } = true;
    public bool OverrideHighlightPalette { get; set; }
    public InventoryHighlightPalette HighlightPalette { get; set; } = InventoryHighlightPalette.Default;
    public List<InventoryPlacementBlockColorOverride> InvalidReasonColors { get; set; } = new();
    public List<RarityColorOverride> RarityColors { get; set; } = new();

    /// <summary>Returns the active highlight palette, falling back to defaults.</summary>
    public InventoryHighlightPalette ResolveHighlightPalette()
        => OverrideHighlightPalette ? HighlightPalette : InventoryHighlightPalette.Default;

    /// <summary>Highlight color for a blocked drop, using per-reason overrides when set.</summary>
    public Rgba GetInvalidColor(InventoryPlacementBlockReason reason)
    {
        if (reason != InventoryPlacementBlockReason.None)
        {
            for (int i = 0; i < InvalidReasonColors.Count; i++)
            {
                var entry = InvalidReasonColors[i];
                if (entry.Reason == reason) return entry.Color;
            }
        }
        return ResolveHighlightPalette().Invalid;
    }

    /// <summary>Rarity tint from config overrides, else <see cref="CtisSettings.RarityColor"/>.</summary>
    public Rgba GetRarityColor(ItemRarity rarity)
    {
        for (int i = 0; i < RarityColors.Count; i++)
        {
            var entry = RarityColors[i];
            if (entry.Rarity == rarity) return entry.Color;
        }
        return CtisSettings.RarityColor(rarity);
    }

    /// <summary>Sets or replaces the rarity tint override.</summary>
    public void SetRarityColor(ItemRarity rarity, Rgba color)
    {
        for (int i = 0; i < RarityColors.Count; i++)
        {
            if (RarityColors[i].Rarity != rarity) continue;
            RarityColors[i].Color = color;
            return;
        }
        RarityColors.Add(new RarityColorOverride { Rarity = rarity, Color = color });
    }

    /// <summary>Fills missing rarity tint overrides from <see cref="CtisSettings"/>.</summary>
    public void EnsureRarityColors()
    {
        foreach (var rarity in Enum.GetValues<ItemRarity>())
        {
            if (RarityColors.Exists(entry => entry.Rarity == rarity)) continue;
            RarityColors.Add(new RarityColorOverride
            {
                Rarity = rarity,
                Color = CtisSettings.RarityColor(rarity)
            });
        }
    }

    /// <summary>Copies every placement rule and color override from another config.</summary>
    public void CopyFrom(PlacementConfig other)
    {
        BlockSelfOwnedContainer = other.BlockSelfOwnedContainer;
        BlockOutOfBounds = other.BlockOutOfBounds;
        BlockSlotOccupied = other.BlockSlotOccupied;
        BlockSlotTypeMismatch = other.BlockSlotTypeMismatch;
        OverrideHighlightPalette = other.OverrideHighlightPalette;
        HighlightPalette = other.HighlightPalette;
        var invalidReasons = new List<InventoryPlacementBlockColorOverride>(other.InvalidReasonColors.Count);
        for (int i = 0; i < other.InvalidReasonColors.Count; i++)
        {
            var entry = other.InvalidReasonColors[i];
            invalidReasons.Add(new InventoryPlacementBlockColorOverride
            {
                Reason = entry.Reason,
                Color = entry.Color
            });
        }
        InvalidReasonColors = invalidReasons;

        var rarityColors = new List<RarityColorOverride>(other.RarityColors.Count);
        for (int i = 0; i < other.RarityColors.Count; i++)
        {
            var entry = other.RarityColors[i];
            rarityColors.Add(new RarityColorOverride
            {
                Rarity = entry.Rarity,
                Color = entry.Color
            });
        }
        RarityColors = rarityColors;
    }

    /// <summary>Runs bounds, slot, and self-container rules; occupancy is checked separately.</summary>
    public bool Evaluate(in InventoryPlacementContext context, IInventoryTreeCache tree, out InventoryPlacementBlockReason reason)
    {
        reason = InventoryPlacementBlockReason.None;

        if (BlockOutOfBounds && context.TargetGrid != null)
        {
            var grid = context.TargetGrid;
            var board = !string.IsNullOrEmpty(grid.GridGuid) && tree.TryGetContainer(grid.GridGuid, out var node)
                ? new OccupancyBoard(node.GridSizeWidth, node.GridSizeHeight)
                : new OccupancyBoard(grid.GridSizeWidth, grid.GridSizeHeight);
            var inBounds = context.ShapeCoordinates.Count > 0
                ? board.ContainsShape(context.ShapeCoordinates, context.Origin)
                : board.BoundryCheck(context.Origin.X, context.Origin.Y, context.ShapeWidth, context.ShapeHeight);
            if (!inBounds)
            {
                reason = InventoryPlacementBlockReason.OutOfBounds;
                return false;
            }
        }

        if (context.TargetSlot != null)
        {
            if (BlockSlotOccupied && context.TargetSlot.RelatedTetrisItem != null)
            {
                reason = InventoryPlacementBlockReason.SlotOccupied;
                return false;
            }
            if (BlockSlotTypeMismatch && context.Item != null && context.Item.SlotType != context.TargetSlot.SlotType)
            {
                reason = InventoryPlacementBlockReason.SlotTypeMismatch;
                return false;
            }
        }

        if (BlockSelfOwnedContainer && context.Item != null)
        {
            var targetId = context.TargetGrid?.GridGuid
                ?? (context.TargetSlot != null ? InventoryTreeIds.Slot(context.TargetSlot.SlotIndex) : null);
            if (InventoryLogic.IsPlacingIntoSelfOwnedContainer(context.Item.Guid, targetId, tree))
            {
                reason = InventoryPlacementBlockReason.SelfOwnedContainer;
                return false;
            }
        }

        return true;
    }
}
