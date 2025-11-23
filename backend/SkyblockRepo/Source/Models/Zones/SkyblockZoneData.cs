using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SkyblockRepo.Models;

public class SkyblockZoneData
{
	[MaxLength(512)]
	public string InternalId { get; set; } = string.Empty;
	public string? Name { get; set; }
	public string Source { get; set; } = "HypixelWiki";
	public string? DiscoveryText { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<ListItem>? Npcs { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<ListItem>? Mobs { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<ListItem>? MobDrops { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<FairySoul>? FairySouls { get; set; }
}

public class FairySoul
{
	public string? Location { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public int Number { get; set; }
	public Coordinates Coordinates { get; set; }
}
