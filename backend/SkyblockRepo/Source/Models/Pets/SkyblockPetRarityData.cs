using System.Text.Json.Serialization;

namespace SkyblockRepo.Models;

public class SkyblockPetRarityData
{
	public Dictionary<string, string> Lore { get; set; } = new();
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public double Value { get; set; }
	
	public bool KatUpgradeable { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<UpgradeCost>? KatUpgradeCosts { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public int KatUpgradeSeconds { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? KatUpgradeTime { get; set; }
}