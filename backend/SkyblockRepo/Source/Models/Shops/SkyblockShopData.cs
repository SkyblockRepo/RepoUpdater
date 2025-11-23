using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SkyblockRepo.Models;

public class SkyblockShopData
{
	[MaxLength(512)]
	public string InternalId { get; set; } = string.Empty;
	public string? Name { get; set; }
	public string Source { get; set; } = "HypixelWiki";
	
	public SortedDictionary<string, InventorySlot> Slots { get; set; } = [];
	
	[JsonExtensionData]
	public SortedDictionary<string, object> AdditionalProperties { get; set; } = new();
}

public class InventorySlot
{
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Material { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Name { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Lore { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public List<UpgradeCost>? Cost { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public List<UpgradeCost>? Output { get; set; }
}
