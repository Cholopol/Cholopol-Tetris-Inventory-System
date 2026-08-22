namespace Ctis.Core;

public static class PlacementConfigJson
{
    /// <summary>Serializes placement rules to JSON.</summary>
    public static string Serialize(PlacementConfig config)
        => CtisJson.Serialize(config);

    /// <summary>Parses placement rules, returning defaults when JSON is empty.</summary>
    public static PlacementConfig Parse(string json)
        => CtisJson.Deserialize<PlacementConfig>(json) ?? new PlacementConfig();
}
