using System.Text.Json.Serialization;
	
namespace SkyblockRepo.Models;

public class UpgradeCost
{
	[JsonPropertyName("type")]
	public UpgradeCostType Type { get; set; } = UpgradeCostType.Unknown;
	
	[JsonPropertyName("essence_type"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? EssenceType { get; set; }
	
	/// <summary>
	/// Item ID for item costs (e.g. "ENCHANTED_GOLD_INGOT")
	/// </summary>
	[JsonPropertyName("item_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? ItemId { get; set; }
	
	/// <summary>
	/// Jacob medal type (e.g. "bronze", "silver", "gold")
	/// </summary>
	[JsonPropertyName("medal_type"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? MedalType { get; set; }
	
	[JsonPropertyName("amount"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public int Amount { get; set; }
	
	public static UpgradeCost ItemCost(string itemId, int amount) => new()
	{
		Type = UpgradeCostType.Item,
		ItemId = itemId,
		Amount = amount
	};
	
	public static UpgradeCost EssenceCost(string essenceType, int amount) => new()
	{
		Type = UpgradeCostType.Essence,
		EssenceType = essenceType,
		Amount = amount
	};
	
	public static UpgradeCost CoinCost(int amount) => new()
	{
		Type = UpgradeCostType.Coins,
		Amount = amount
	};
	
	public static UpgradeCost MoteCost(int amount) => new()
	{
		Type = UpgradeCostType.Motes,
		Amount = amount
	};
	
	public static UpgradeCost CopperCost(int amount) => new()
	{
		Type = UpgradeCostType.Copper,
		Amount = amount
	};
	
	public static UpgradeCost GemCost(int amount) => new()
	{
		Type = UpgradeCostType.Gems,
		Amount = amount
	};
	
	public static UpgradeCost BitCost(int amount) => new()
	{
		Type = UpgradeCostType.Bits,
		Amount = amount
	};
	
	public static UpgradeCost JacobMedalCost(string medalType, int amount) => new()
	{
		Type = UpgradeCostType.JacobMedal,
		MedalType = medalType,
		Amount = amount
	};
	
	public static UpgradeCost PeltCost(int amount) => new()
	{
		Type = UpgradeCostType.Pelts,
		Amount = amount
	};
}

[JsonStringEnumCapitalize]
public enum UpgradeCostType
{
	Unknown = 0,
	Item = 1,
	Essence = 2,
	Coins = 3,
	Motes = 4,
	Copper = 5,
	Gems = 6,
	Bits = 7,
	JacobMedal = 8,
	Pelts = 9
}