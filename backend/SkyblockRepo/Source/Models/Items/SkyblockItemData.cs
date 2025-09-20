using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SkyblockRepo.Models;

public class SkyblockItemData
{
	[MaxLength(512)]
	public string InternalId { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string? Category { get; set; }
	public string Source { get; set; } = "HypixelAPI";
	public double NpcValue { get; set; }
	public string Lore { get; set; } = string.Empty;
	public ItemFlags Flags { get; set; } = new();
	
	public string NameAndLore => $"{Data?.Name ?? Name}\n{Lore}";
    
	/// <summary>
	/// Hypixel item data from /resources/skyblock/items
	/// </summary>
	public SkyblockItemResponse? Data { get; set; }
	
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