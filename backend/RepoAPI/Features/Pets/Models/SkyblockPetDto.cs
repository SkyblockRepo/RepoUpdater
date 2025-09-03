using System.ComponentModel.DataAnnotations;
using RepoAPI.Features.Pets.PetTemplate;
using Riok.Mapperly.Abstractions;

namespace RepoAPI.Features.Pets.Models;

[Mapper]
public static partial class SkyblockPetMapper
{
	public static partial SkyblockPetDto ToDto(this SkyblockPet pet);
	public static partial IQueryable<SkyblockPetDto> SelectDto(this IQueryable<SkyblockPet> pet);
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
	
	public Dictionary<string, PetRarityDto> PetRarities { get; set; } = new();
	/// <summary>
	/// Parsed data from the item template on the Hypixel Wiki.
	/// </summary>
	public PetTemplateDto? TemplateData { get; set; }
}

public class PetRarityDto
{
	public string Lore { get; set; } = string.Empty;
	
	public double? BaseHealth { get; set; }
	public double? BaseDefense { get; set; }
	public double? BaseStrength { get; set; }
	public double? BaseSpeed { get; set; }
	public double? BaseCritChance { get; set; }
	public double? BaseCritDamage { get; set; }
	public double? BaseIntelligence { get; set; }
}