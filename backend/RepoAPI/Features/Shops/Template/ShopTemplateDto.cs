using System.Text.Json.Serialization;
using RepoAPI.Core.Models;
using SkyblockRepo.Models;

namespace RepoAPI.Features.Shops.Template;

/// <summary>
/// Represents the parsed data from a Hypixel SkyBlock NPC template.
/// </summary>
public class ShopTemplateDto
{
	public string? InternalId { get; set; }
	public string? Name { get; set; }
	
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