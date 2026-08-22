using System.Text.Json;
using System.Text.Json.Serialization;
using TetrisCoordLib.Core.Math;

namespace Ctis.Core;

/// <summary>
/// Compile-time System.Text.Json source generator context to eliminate runtime reflection and optimize serialization performance.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    Converters = [typeof(CtisJson.JsonInt32Converter), typeof(CtisJson.Vec2IJsonConverter)])]
[JsonSerializable(typeof(SaveFileWrapper<GameSaveData>))]
[JsonSerializable(typeof(GameSaveData))]
[JsonSerializable(typeof(TetrisItemPersistentData))]
[JsonSerializable(typeof(GridContainerConfig))]
[JsonSerializable(typeof(SaveSlotInfo))]
[JsonSerializable(typeof(List<ItemDetails>))]
[JsonSerializable(typeof(IReadOnlyList<ItemDetails>))]
[JsonSerializable(typeof(IEnumerable<ItemDetails>))]
[JsonSerializable(typeof(ItemDetails[]))]
[JsonSerializable(typeof(ItemDetails))]
[JsonSerializable(typeof(ItemOccupancy))]
[JsonSerializable(typeof(OccupancyPatch))]
[JsonSerializable(typeof(PlacementConfig))]
[JsonSerializable(typeof(EquipmentLayout))]
[JsonSerializable(typeof(EquipmentSlotSpec))]
[JsonSerializable(typeof(List<EquipmentSlotSpec>))]
[JsonSerializable(typeof(IReadOnlyList<EquipmentSlotSpec>))]
[JsonSerializable(typeof(IEnumerable<EquipmentSlotSpec>))]
[JsonSerializable(typeof(EquipmentSlotSpec[]))]
[JsonSerializable(typeof(Dictionary<string, GridContainerConfig>))]
[JsonSerializable(typeof(List<TetrisItemPersistentData>))]
[JsonSerializable(typeof(IReadOnlyList<TetrisItemPersistentData>))]
[JsonSerializable(typeof(IEnumerable<TetrisItemPersistentData>))]
[JsonSerializable(typeof(TetrisItemPersistentData[]))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(List<Vec2I>))]
[JsonSerializable(typeof(IReadOnlyList<Vec2I>))]
[JsonSerializable(typeof(IEnumerable<Vec2I>))]
[JsonSerializable(typeof(Vec2I[]))]
[JsonSerializable(typeof(Vec2I))]
[JsonSerializable(typeof(InventoryPlacementBlockColorOverride))]
[JsonSerializable(typeof(RarityColorOverride))]
[JsonSerializable(typeof(InventoryHighlightPalette))]
[JsonSerializable(typeof(Rgba))]
[JsonSerializable(typeof(ItemRarity))]
[JsonSerializable(typeof(InventorySlotType))]
[JsonSerializable(typeof(Dir))]
[JsonSerializable(typeof(InventoryPlacementBlockReason))]
public partial class CtisJsonContext : JsonSerializerContext
{
}

public static class CtisJson
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    /// <summary>Serializes a value with CTIS JSON conventions (camelCase, string enums).</summary>
    public static string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, Options);

    /// <summary>Deserializes JSON, returning default when the payload is empty.</summary>
    public static T? Deserialize<T>(string json)
        => string.IsNullOrWhiteSpace(json)
            ? default
            : JsonSerializer.Deserialize<T>(json, Options);

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = CtisJsonContext.Default
        };
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new JsonInt32Converter());
        options.Converters.Add(new Vec2IJsonConverter());
        return options;
    }

    /// <summary>
    /// Godot JSON.stringify writes Variant numbers as floats (2.0). STJ rejects those for int properties.
    /// </summary>
    public sealed class JsonInt32Converter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt32(out var i)) return i;
                if (reader.TryGetInt64(out var l)) return (int)l;
                if (reader.TryGetDouble(out var d)) return (int)d;
            }
            if (reader.TokenType == JsonTokenType.String
                && int.TryParse(reader.GetString(), out var parsed))
                return parsed;
            throw new JsonException($"Cannot convert {reader.TokenType} to Int32.");
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value);
    }

    public sealed class Vec2IJsonConverter : JsonConverter<Vec2I>
    {
        public override Vec2I Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            return new Vec2I(ReadInt(doc.RootElement, "x"), ReadInt(doc.RootElement, "y"));
        }

        public override void Write(Utf8JsonWriter writer, Vec2I value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.X);
            writer.WriteNumber("y", value.Y);
            writer.WriteEndObject();
        }

        private static int ReadInt(JsonElement root, string name)
        {
            if (!root.TryGetProperty(name, out var el))
                throw new JsonException($"Missing '{name}' on Vec2I.");
            if (el.ValueKind == JsonValueKind.Number)
            {
                if (el.TryGetInt32(out var i)) return i;
                return (int)el.GetDouble();
            }
            if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var parsed))
                return parsed;
            throw new JsonException($"Cannot convert {el.ValueKind} to Int32.");
        }
    }
}
