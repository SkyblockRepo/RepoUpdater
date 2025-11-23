using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SkyblockRepo.Models;

public class SkyblockNpcData
{
	[MaxLength(512)]
	public string InternalId { get; set; } = string.Empty;
	public string? Name { get; set; }
	public NpcFlags Flags { get; set; } = new();
	
	public NpcLocation Location { get; set; } = new();
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public NpcGardenVisitor? Visitor { get; set; }
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
