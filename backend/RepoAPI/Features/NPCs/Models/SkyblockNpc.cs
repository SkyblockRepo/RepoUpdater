using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepoAPI.Core.Models;
using RepoAPI.Features.NPCs.NpcTemplate;
using Riok.Mapperly.Abstractions;

namespace RepoAPI.Features.NPCs.Models;

public class SkyblockNpc : IVersionedEntity
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
	
	[MaxLength(64)]
	public string Source { get; set; } = "HypixelWiki";
	
	/// <summary>
	/// Raw wikitext from the pet template on the Hypixel Wiki.
	/// </summary>
	[MapperIgnore]
	public string? RawTemplate { get; set; }
	
	[NotMapped]
	public NpcTemplateDto? TemplateData => RawTemplate == null ? null : new NpcTemplateParser().Parse(RawTemplate);
}

public class SkyblockNpcConfiguration : IEntityTypeConfiguration<SkyblockNpc>
{
	public void Configure(EntityTypeBuilder<SkyblockNpc> builder)
	{
		builder.HasKey(x => x.Id);
		
		builder.HasIndex(x => x.Name);
		builder.HasIndex(x => x.InternalId);
		builder.HasIndex(x => new { x.InternalId, x.Latest });
		
		builder.HasIndex(x => x.IngestedAt);
		
		builder.HasQueryFilter(x => x.Latest);
	}
}