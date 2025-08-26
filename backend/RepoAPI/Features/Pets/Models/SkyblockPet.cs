using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepoAPI.Features.Wiki.Templates.ItemTemplate;
using RepoAPI.Features.Wiki.Templates.PetTemplate;
using Riok.Mapperly.Abstractions;

namespace RepoAPI.Features.Pets.Models;

public class SkyblockPet
{
	[MaxLength(512)]
	public required string InternalId { get; set; }
	public string? Category { get; set; }
	
	[MaxLength(64)]
	public string Source { get; set; } = "HypixelAPI";
	
	/// <summary>
	/// Raw wikitext from the pet template on the Hypixel Wiki.
	/// </summary>
	[MapperIgnore]
	public string? RawTemplate { get; set; }
	
	/// <summary>
	/// Parsed data from the pet template on the Hypixel Wiki.
	/// </summary>
	[Column(TypeName = "jsonb")]
	public PetTemplateDto? TemplateData { get; set; }
}

public class SkyblockPetConfiguration : IEntityTypeConfiguration<SkyblockPet>
{
	public void Configure(EntityTypeBuilder<SkyblockPet> builder)
	{
		builder.HasKey(x => x.InternalId);
	}
}