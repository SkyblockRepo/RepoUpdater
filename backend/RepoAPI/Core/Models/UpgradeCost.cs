using System.Text.Json.Serialization;

namespace RepoAPI.Core.Models;

public class UpgradeCost
{
	[JsonPropertyName("type")]
	public string? Type { get; set; }
	
	[JsonPropertyName("essence_type"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? EssenceType { get; set; }
	
	[JsonPropertyName("item_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? ItemId { get; set; }
	
	[JsonPropertyName("amount"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public int Amount { get; set; }
	
	public static UpgradeCost ItemCost(string itemId, int amount) => new()
	{
		Type = "ITEM",
		ItemId = itemId,
		Amount = amount
	};
	
	public static UpgradeCost EssenceCost(string essenceType, int amount) => new()
	{
		Type = "ESSENCE",
		EssenceType = essenceType,
		Amount = amount
	};
	
	public static UpgradeCost CoinCost(int amount) => new()
	{
		Type = "COINS",
		Amount = amount
	};
}