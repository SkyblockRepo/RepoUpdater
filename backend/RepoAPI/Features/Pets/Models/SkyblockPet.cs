using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepoAPI.Core.Models;
using RepoAPI.Features.Wiki.Templates.PetTemplate;
using Riok.Mapperly.Abstractions;

namespace RepoAPI.Features.Pets.Models;

public class SkyblockPet : IVersionedEntity
{
	#region IVersionedEntity Implementation
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	[MapperIgnore]
	public int Id { get; set; }
	
	[MapperIgnore]
	public DateTimeOffset IngestedAt { get; set; } = DateTimeOffset.UtcNow;
	
	[MapperIgnore]
	public bool Latest { get; set; } = true;
	#endregion

	[MaxLength(512)]
	public required string InternalId { get; set; }
	
	[MaxLength(512)]
	public string? Name { get; set; }
	public string? Category { get; set; }
	
	[MaxLength(64)]
	public string Source { get; set; } = "HypixelAPI";
	
	public string Lore { get; set; } = string.Empty;
	
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
		builder.HasKey(x => x.Id);
		
		builder.HasIndex(x => x.Name);
		builder.HasIndex(x => x.InternalId);
		builder.HasIndex(x => new { x.InternalId, x.Latest });
		
		builder.HasIndex(x => x.IngestedAt);
		
		builder.HasQueryFilter(x => x.Latest);
	}
}