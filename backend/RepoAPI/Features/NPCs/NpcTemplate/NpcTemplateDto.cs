using System.Text.Json.Serialization;
using RepoAPI.Features.Wiki.Templates;

namespace RepoAPI.Features.NPCs.NpcTemplate;

/// <summary>
/// Represents the parsed data from a Hypixel SkyBlock NPC template.
/// </summary>
public class NpcTemplateDto
{
	public string? InternalId { get; set; }
	public string? Name { get; set; }
	public NpcFlags Flags { get; set; } = new();
	
	public NpcLocation Location { get; set; } = new();
	
	public NpcGardenVisitor? Visitor { get; set; }
	
	[JsonExtensionData]
	public SortedDictionary<string, object> AdditionalProperties { get; set; } = new();
}

public class NpcLocation
{
	public string Zone { get; set; } = string.Empty;
	public Coordinates Coordinates { get; set; }
}

public class NpcGardenVisitor
{
	public string Rarity { get; set; } = string.Empty;
	public int GardenLevel { get; set; } = 0;
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? Desire { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? Bonus { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public double Copper { get; set; } = 0;
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public double Iron { get; set; } = 0;
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public double FarmingXp { get; set; } = 0;
}

public record struct NpcFlags()
{
	public bool Merchant { get; set; }
	public bool Abiphone { get; set; }
	public bool Garden { get; set; }
	public bool Shop { get; set; }
}