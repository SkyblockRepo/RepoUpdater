using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using EliteFarmers.HypixelAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepoAPI.Core.Models;
using RepoAPI.Features.Items.ItemTemplate;
using RepoAPI.Features.Recipes.Models;
using Riok.Mapperly.Abstractions;
using ItemTemplateParser = RepoAPI.Features.Items.ItemTemplate.ItemTemplateParser;
using SkyblockRepo.Models;

namespace RepoAPI.Features.Items.Models;

public class SkyblockItem : IVersionedEntity
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
	
	[MaxLength(512)]
	public string? Category { get; set; }
	
	public double NpcValue { get; set; }
	
	[Column(TypeName = "jsonb")]
	public ItemFlags Flags { get; set; } = new();
	
	[MaxLength(64)]
	public string Source { get; set; } = "HypixelAPI";
	
	public string Lore { get; set; } = string.Empty;
    
	/// <summary>
	/// Hypixel item data from /resources/skyblock/items
	/// </summary>
	[Column(TypeName = "jsonb")]
	public ItemResponse? Data { get; set; }
	
	/// <summary>
	/// Raw wikitext from the item template on the Hypixel Wiki.
	/// </summary>
	[MapperIgnore]
	public string? RawTemplate { get; set; }
	
	/// <summary>
	/// Parsed data from the item template on the Hypixel Wiki.
	/// </summary>
	// [Column(TypeName = "jsonb")]
	// public ItemTemplateDto? TemplateData { get; set; }
	public ItemTemplateDto? TemplateData => RawTemplate == null ? null : new ItemTemplateParser().Parse(RawTemplate);
	
	public List<SkyblockRecipe> Recipes { get; set; } = [];
	
	[NotMapped]
	public List<SkyblockSoldBy>? SoldBy { get; set; }
}

public class SkyblockItemConfiguration : IEntityTypeConfiguration<SkyblockItem>
{
	public void Configure(EntityTypeBuilder<SkyblockItem> builder)
	{
		builder.HasKey(x => x.Id);
		
		builder.HasIndex(x => x.Name);
		builder.HasIndex(x => x.InternalId);
		
		// Enforce uniqueness of InternalId where Latest is true
		builder.HasIndex(i => i.InternalId)
			.IsUnique()
			.HasFilter(@"""Latest"" = true");
		
		builder.HasIndex(x => x.IngestedAt);
		
		builder.HasQueryFilter(x => x.Latest);
	}
}