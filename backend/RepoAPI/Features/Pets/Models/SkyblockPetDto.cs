using System.ComponentModel.DataAnnotations;
using RepoAPI.Features.Wiki.Templates.PetTemplate;
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
	public string Source { get; set; } = "HypixelAPI";
	
	/// <summary>
	/// Parsed data from the item template on the Hypixel Wiki.
	/// </summary>
	public PetTemplateDto? TemplateData { get; set; }
}