namespace Ctis.Core;

public static class ItemCatalogJson
{
    /// <summary>Serializes catalog items to JSON.</summary>
    public static string Serialize(IEnumerable<ItemDetails> items)
    {
        if (items is List<ItemDetails> list)
            return CtisJson.Serialize(list);
        if (items is IReadOnlyList<ItemDetails> readOnlyList)
            return CtisJson.Serialize(readOnlyList);
        return CtisJson.Serialize(new List<ItemDetails>(items));
    }

    /// <summary>Parses a catalog JSON array.</summary>
    public static List<ItemDetails> Parse(string json)
        => CtisJson.Deserialize<List<ItemDetails>>(json) ?? new List<ItemDetails>();
}
