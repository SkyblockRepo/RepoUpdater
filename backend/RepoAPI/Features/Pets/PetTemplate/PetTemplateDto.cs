using System.Text.Json.Serialization;
using RepoAPI.Features.Pets.Models;

namespace RepoAPI.Features.Pets.PetTemplate;

/// <summary>
/// Represents the parsed data from a Hypixel SkyBlock Pet template.
/// </summary>
public class PetTemplateDto
{
	public string? InternalId { get; set; }
	
	public Dictionary<string, Dictionary<string, string>> Lore { get; set; } = new();
	
	public string Kat { get; set; } = string.Empty;
	public string? Category { get; set; }
	public string? Name { get; set; }
	
	public int? MinLevel { get; set; }
	public int? MaxLevel { get; set; }
	
	public List<string> BaseStats { get; set; } = [];
	
	public PetFlags Flags { get; set; } = new();
	
	public string? Leveling { get; set; }
	public Dictionary<string, PetRarityDto> PetRarities { get; set; } = new();
	
	[JsonExtensionData]
	public SortedDictionary<string, object> AdditionalProperties { get; set; } = new();
}

public record struct PetFlags()
{
	public bool Auctionable { get; set; }
	public bool Mountable { get; set; }
	public bool Tradable { get; set; }
	public bool Museumable { get; set; }
}