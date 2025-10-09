using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SkyblockRepo.Models;

public class SkyblockItemData
{
	/// <summary>
	/// SkyBlock internal ID of the item
	/// </summary>
	[MaxLength(512)]
	public string InternalId { get; set; } = string.Empty;
	
	/// <summary>
	/// Name of the item, without any formatting codes
	/// </summary>
	public string Name { get; set; } = string.Empty;
	
	/// <summary>
	/// Category of the item, e.g. "Weapon", "Armor", "Accessory", "Consumable", etc.
	/// </summary>
	public string? Category { get; set; }
	
	/// <summary>
	/// The source of the item, e.g. "HypixelAPI", "HypixelWiki", etc.
	/// </summary>
	public string Source { get; set; } = "HypixelAPI";
	
	/// <summary>
	/// Npc sell value of the item, 0 if the item cannot be sold to an npc or the value is unknown
	/// </summary>
	public double NpcValue { get; set; }
	
	/// <summary>
	/// Lore of the item, new lines are separated by \n
	/// </summary>
	public string Lore { get; set; } = string.Empty;

	/// <summary>
	/// Flags on the item like tradeable, bazaarable, etc.
	/// </summary>
	public ItemFlags Flags { get; set; } = new();
	
	/// <summary>
	/// Gets the name and lore combined, with a newline in between
	/// </summary>
	public string NameAndLore => $"{Data?.Name ?? Name}\n{Lore}";
    
	/// <summary>
	/// Hypixel item data from /resources/skyblock/items
	/// </summary>
	public SkyblockItemResponse? Data { get; set; }
	
	/// <summary>
	/// Variants of the item, e.g. different colors of the same item
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public List<SkyblockItemVariant>? Variants { get; set; }
	
	/// <summary>
	/// Recipes that can produce this item as an output.
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public List<SkyblockRecipeData> Recipes { get; set; } = [];

}

public class SkyblockItemNameSearch
{
	public string InternalId
	{
		get;
		set
		{
			field = value;
			IdToNameUpper = value.Replace("_", " ").ToUpperInvariant();
		}
	}

	public string Name
	{
		get;
		set
		{
			field = value;
			NameUpper = value.ToUpperInvariant();
		}
	}
	
	public string NameUpper = string.Empty;
	public string IdToNameUpper { get; set; } = string.Empty;
}