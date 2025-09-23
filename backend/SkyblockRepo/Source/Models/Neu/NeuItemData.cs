using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkyblockRepo.Models.Neu;

/// <summary>
/// The top-level object representing an item from the NotEnoughUpdates repository.
/// </summary>
public record NeuItemData(
    [property: JsonPropertyName("itemid")] string ItemId,
    [property: JsonPropertyName("displayname")] string DisplayName,
    [property: JsonPropertyName("nbttag")] string NbtTag,
    [property: JsonPropertyName("damage")] int Damage,
    [property: JsonPropertyName("lore")] List<string> Lore,
    [property: JsonPropertyName("internalname")] string InternalName
)
{
    [property: JsonPropertyName("recipes")] public List<NeuRecipeBase>? Recipes { get; init; }
    [property: JsonPropertyName("recipe")] public NeuCraftingRecipeImplicit? Recipe { get; init; }
    [property: JsonPropertyName("clickcommand")] public string? ClickCommand { get; init; }
    [property: JsonPropertyName("modver")] public string? ModVer { get; init; }
    [property: JsonPropertyName("useneucraft")] public bool? UseNeuCraft { get; init; }
    [property: JsonPropertyName("infoType")] public string? InfoType { get; init; }
    [property: JsonPropertyName("info")] public List<string>? Info { get; init; }
    [property: JsonPropertyName("crafttext")] public string? CraftText { get; init; }
}

public class RecipeConverter : JsonConverter<NeuRecipeBase>
{
    public override NeuRecipeBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Create a copy of the reader to peek ahead
        var readerClone = reader;

        if (readerClone.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected the start of an object.");
        }

        // Parse the JSON object into a JsonDocument to inspect it
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        
        // Find the "type" property to determine which concrete class to use
        if (!jsonDoc.RootElement.TryGetProperty("type", out var typeProperty) || typeProperty.ValueKind != JsonValueKind.String)
        {
            throw new JsonException("Recipe object is missing a 'type' property or it's not a string.");
        }

        var recipeType = typeProperty.GetString();

        // Deserialize the document back into the specific derived type
        return recipeType switch
        {
            "crafting" => jsonDoc.RootElement.Deserialize<NeuCraftingRecipe>(options),
            "forge" => jsonDoc.RootElement.Deserialize<NeuForgeRecipe>(options),
            "trade" => jsonDoc.RootElement.Deserialize<NeuTradeRecipe>(options),
            "drops" => jsonDoc.RootElement.Deserialize<NeuDropsRecipe>(options),
            "npc_shop" => jsonDoc.RootElement.Deserialize<NeuNpcShopRecipe>(options),
            "katgrade" => jsonDoc.RootElement.Deserialize<NeuKatGradeRecipe>(options),
            _ => throw new JsonException($"Unknown recipe type '{recipeType}'.")
        };
    }

    public override void Write(Utf8JsonWriter writer, NeuRecipeBase value, JsonSerializerOptions options)
    {
        // This delegates the writing logic back to the default serializer
        JsonSerializer.Serialize(writer, (object)value, options);
    }
}

public record NeuDetailedDrop(
    [property: JsonPropertyName("id")] string Id
)
{
    [property: JsonPropertyName("chance")] public string? Chance { get; init; }
    [property: JsonPropertyName("extra")] public List<string>? Extra { get; init; }
}

// The "drops" property in DropsRecipe can be either a simple string or a complex object.
// A custom JsonConverter is needed to handle this.
public class NeuDropConverter : JsonConverter<object>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.String)
        {
            return root.GetString();
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            // Re-use the serializer to deserialize into the specific object type
            return root.Deserialize<NeuDetailedDrop>(options);
        }

        throw new JsonException("Expected a string or an object for a drop.");
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        // Not needed for deserialization-focused classes
        throw new NotImplementedException();
    }
}

[JsonConverter(typeof(RecipeConverter))]
public record NeuRecipeBase
{
    [JsonPropertyName("type")] public required string Type { get; init; }
}

public record NeuCraftingRecipeImplicit
{
    [JsonPropertyName("count")] public int? Count { get; init; }
    [JsonPropertyName("crafttext")] public string? CraftText { get; init; }
    [JsonPropertyName("overrideOutputId")] public string? OverrideOutputId { get; init; }
    [JsonPropertyName("supercraftable")] public bool? Supercraftable { get; init; }
    [JsonPropertyName("A1")] public string? A1 { get; init; }
    [JsonPropertyName("A2")] public string? A2 { get; init; }
    [JsonPropertyName("A3")] public string? A3 { get; init; }
    [JsonPropertyName("B1")] public string? B1 { get; init; }
    [JsonPropertyName("B2")] public string? B2 { get; init; }
    [JsonPropertyName("B3")] public string? B3 { get; init; }
    [JsonPropertyName("C1")] public string? C1 { get; init; }
    [JsonPropertyName("C2")] public string? C2 { get; init; }
    [JsonPropertyName("C3")] public string? C3 { get; init; }
}

public record NeuCraftingRecipe : NeuRecipeBase
{
    [JsonPropertyName("count")] public int? Count { get; init; }
    [JsonPropertyName("crafttext")] public string? CraftText { get; init; }
    [JsonPropertyName("overrideOutputId")] public string? OverrideOutputId { get; init; }
    [JsonPropertyName("supercraftable")] public bool? Supercraftable { get; init; }
    [JsonPropertyName("A1")] public string? A1 { get; init; }
    [JsonPropertyName("A2")] public string? A2 { get; init; }
    [JsonPropertyName("A3")] public string? A3 { get; init; }
    [JsonPropertyName("B1")] public string? B1 { get; init; }
    [JsonPropertyName("B2")] public string? B2 { get; init; }
    [JsonPropertyName("B3")] public string? B3 { get; init; }
    [JsonPropertyName("C1")] public string? C1 { get; init; }
    [JsonPropertyName("C2")] public string? C2 { get; init; }
    [JsonPropertyName("C3")] public string? C3 { get; init; }
}

public record NeuForgeRecipe : NeuRecipeBase
{
    [JsonPropertyName("inputs")] public required List<string> Inputs { get; init; }
    [JsonPropertyName("duration")] public required int Duration { get; init; }
    [JsonPropertyName("overrideOutputId")] public string? OverrideOutputId { get; init; }
    [JsonPropertyName("count")] public double? Count { get; init; }
    [JsonPropertyName("hotmLevel")] public int? HotmLevel { get; init; }
}

public record NeuTradeRecipe : NeuRecipeBase
{
    [JsonPropertyName("result")] public required string Result { get; init; }
    [JsonPropertyName("cost")] public required string Cost { get; init; }
    [JsonPropertyName("min")] public int? Min { get; init; }
    [JsonPropertyName("max")] public int? Max { get; init; }
}

public record NeuDropsRecipe : NeuRecipeBase
{
    [JsonPropertyName("name")] public required string Name { get; init; }
    [JsonPropertyName("drops")] public required List<object> Drops { get; init; }
    [JsonPropertyName("level")] public int? Level { get; init; }
    [JsonPropertyName("coins")] public int? Coins { get; init; }
    [JsonPropertyName("xp")] public int? Xp { get; init; }
    [JsonPropertyName("combat_xp")] public int? CombatXp { get; init; }
    [JsonPropertyName("render")] public string? Render { get; init; }
    [JsonPropertyName("extra")] public List<string>? Extra { get; init; }
    [JsonPropertyName("panorama")] public string? Panorama { get; init; }
}

public record NeuNpcShopRecipe : NeuRecipeBase
{
    [JsonPropertyName("cost")] public required List<string> Cost { get; init; }
    [JsonPropertyName("result")] public required string Result { get; init; }
}

public record NeuKatGradeRecipe : NeuRecipeBase
{
    [JsonPropertyName("input")] public required string Input { get; init; }
    [JsonPropertyName("output")] public required string Output { get; init; }
    [JsonPropertyName("coins")] public required int Coins { get; init; }
    [JsonPropertyName("time")] public required int Time { get; init; }
    [JsonPropertyName("items")] public List<string>? Items { get; init; }
}