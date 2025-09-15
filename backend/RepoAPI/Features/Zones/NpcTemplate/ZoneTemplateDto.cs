using System.Text.Json.Serialization;
using RepoAPI.Features.NPCs.NpcTemplate;
using RepoAPI.Features.Wiki.Templates;

namespace RepoAPI.Features.Zones.NpcTemplate;

/// <summary>
/// Represents the parsed data from a Hypixel SkyBlock NPC template.
/// </summary>
public class ZoneTemplateDto
{
	public string? InternalId { get; set; }
	public string? Name { get; set; }
	public string? DiscoveryText { get; set; }
	
	public List<ListItem> Npcs { get; set; } = [];
	public List<ListItem> Mobs { get; set; } = [];
	public List<ListItem> MobDrops { get; set; } = [];
	
	public List<FairySoul> FairySouls { get; set; } = [];
	
	[JsonExtensionData]
	public SortedDictionary<string, object> AdditionalProperties { get; set; } = new();
}

public class FairySoul
{
	public string? Location { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public int Number { get; set; }
	public Coordinates Coordinates { get; set; }
}