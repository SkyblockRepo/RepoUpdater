using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RepoAPI.Core.Models;
using RepoAPI.Features.Pets.PetTemplate;
using Riok.Mapperly.Abstractions;

namespace RepoAPI.Features.Pets.Models;

[Mapper]
public static partial class SkyblockPetMapper
{
	[UserMapping(Default = true)]
	public static SkyblockPetDto ToDto(this SkyblockPet pet)
	{
		var data = pet.TemplateData;
		return new SkyblockPetDto
		{
			InternalId = pet.InternalId,
			Name = pet.Name,
			Category = pet.Category,
			Source = pet.Source,
			Flags = data?.Flags ?? new PetFlags(),
			BaseStats = data?.BaseStats ?? [],
			MinLevel = data?.MinLevel,
			MaxLevel = data?.MaxLevel,
			Rarities = data?.PetRarities ?? new Dictionary<string, PetRarityDto>()
		};
	}
}

public class SkyblockPetDto
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
	
	public Dictionary<string, PetRarityDto> Rarities { get; set; } = new();
}

public class PetRarityDto
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