namespace Ctis.Core;

public static class EquipmentLayoutJson
{
    /// <summary>Serializes equipment slots to JSON.</summary>
    public static string Serialize(IEnumerable<EquipmentSlotSpec> slots)
    {
        if (slots is List<EquipmentSlotSpec> list)
            return CtisJson.Serialize(list);
        if (slots is IReadOnlyList<EquipmentSlotSpec> readOnlyList)
            return CtisJson.Serialize(readOnlyList);
        return CtisJson.Serialize(new List<EquipmentSlotSpec>(slots));
    }

    /// <summary>Parses an equipment-slot JSON array.</summary>
    public static List<EquipmentSlotSpec> Parse(string json)
        => CtisJson.Deserialize<List<EquipmentSlotSpec>>(json) ?? new List<EquipmentSlotSpec>();
}
