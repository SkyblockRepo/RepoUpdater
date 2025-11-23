using System.Text.Json.Serialization;

namespace SkyblockRepo.Models;

public class SkyblockItemVariant
{
	public required ItemVariationDefinition By { get; set; }
	public required SkyblockItemVariantData Item { get; set; }
}

public class ItemVariationDefinition
{
	[JsonPropertyName("type")]
	public VariationType Type { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Key { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Exact { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? StartsWith { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? EndsWith { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Contains { get; set; }
}

[JsonStringEnumCapitalize]
public enum VariationType
{
	Name,
	Attribute,
}

public class SkyblockItemVariantData
{
	/// <summary>
	/// Name of the item, without any formatting codes
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Name { get; set; } = string.Empty;
	
	/// <summary>
	/// Npc sell value of the item, 0 if the item cannot be sold to an npc or the value is unknown
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public double NpcValue { get; set; }
	
	/// <summary>
	/// Lore of the item, new lines are separated by \n
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Lore { get; set; }
	
	/// <summary>
	/// Gets the name and lore combined, with a newline in between
	/// </summary>
	[JsonIgnore]
	public string NameAndLore => $"{Data?.Name ?? Name}\n{Lore}";
    
	/// <summary>
	/// Hypixel item data from /resources/skyblock/items
	/// </summary>
	public SkyblockItemResponse? Data { get; set; }
}