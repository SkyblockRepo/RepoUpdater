using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SkyblockRepo.Models;

public class SkyblockPetData
{
	[MaxLength(512)]
	public required string InternalId { get; set; }
	public string? Name { get; set; }
	public string Source { get; set; } = "HypixelAPI";
	public string? Category { get; set; }

	public int? MinLevel { get; set; }
	public int? MaxLevel { get; set; }

	public List<string> BaseStats { get; set; } = [];

	public PetFlags Flags { get; set; } = new();

	public Dictionary<string, SkyblockPetRarityData> Rarities { get; set; } = new();
}