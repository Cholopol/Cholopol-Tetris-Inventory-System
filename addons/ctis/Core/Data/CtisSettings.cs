namespace Ctis.Core;

public static class CtisSettings
{
    public const int CurrentVersion = 1;
    public const float GridTileSizeWidth = 32f;
    public const float GridTileSizeHeight = 32f;
    public const int DepositoryColumns = 10;
    public const int DepositoryRows = 24;
    public const int GridMinCells = 1;
    public const int GridMaxColumns = 40;
    public const int GridMaxRows = 80;
    public const float GridMinTileSize = 16f;
    public const float GridMaxTileSize = 64f;

    public static readonly Rgba HighlightValid = new(0f, 1f, 0f, 100f / 255f);
    public static readonly Rgba HighlightInvalid = new(1f, 0f, 0f, 100f / 255f);
    public static readonly Rgba HighlightCanStack = new(1f, 1f, 0f, 100f / 255f);
    public static readonly Rgba HighlightCanExchange = new(0.4f, 0.6f, 1f, 100f / 255f);

    /// <summary>Default rarity overlay color used when placement config has no override.</summary>
    public static Rgba RarityColor(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Uncommon => new(0.55f, 0.85f, 0.55f, 0.25f),
        ItemRarity.Rare => new(0.55f, 0.7f, 0.95f, 0.25f),
        ItemRarity.Epic => new(0.85f, 0.55f, 0.9f, 0.25f),
        ItemRarity.Legendary => new(0.95f, 0.8f, 0.55f, 0.25f),
        ItemRarity.Artifact => new(0.95f, 0.6f, 0.6f, 0.25f),
        _ => new(0.9f, 0.9f, 0.9f, 0.25f)
    };
}

public readonly record struct InventoryHighlightPalette(
    Rgba ValidEmpty,
    Rgba Invalid,
    Rgba CanStack,
    Rgba CanQuickExchange)
{
    public static InventoryHighlightPalette Default { get; } = new(
        CtisSettings.HighlightValid,
        CtisSettings.HighlightInvalid,
        CtisSettings.HighlightCanStack,
        CtisSettings.HighlightCanExchange);
}
