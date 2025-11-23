using System.Text.Json.Serialization;

namespace SkyblockRepo.Models;

public class SkyblockSoldBy
{
	public string Id { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public List<UpgradeCost> Cost { get; set; } = [];
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public int Amount { get; set; }
}

public class SkyblockCost
{
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public SkyblockCostType Type { get; set; }
	
	public double Amount { get; set; }
	
	[JsonPropertyName("item_id")]
	public string? ItemId { get; set; }
}

public enum SkyblockCostType
{
	COINS,
	ITEM
}
