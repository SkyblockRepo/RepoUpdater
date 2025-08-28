using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepoAPI.Features.Wiki.Templates.ItemTemplate;
using Riok.Mapperly.Abstractions;

namespace RepoAPI.Features.Enchantments.Models;

public class SkyblockEnchantment
{
	[MaxLength(512)]
	public required string InternalId { get; set; }
	
	[MaxLength(64)]
	public string Source { get; set; } = "HypixelWiki";
	
	public int MinLevel { get; set; } = 1;
	public int MaxLevel { get; set; } = 1;
	
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

	public List<string> GetItemIds()
	{
		var list = new List<string>();
		for (var level = MinLevel; level <= MaxLevel; level++)
		{
			list.Add($"ENCHANTMENT_{InternalId}_{level}");
		}
		return list;
	}
}

public class SkyblockEnchantmentConfiguration : IEntityTypeConfiguration<SkyblockEnchantment>
{
	public void Configure(EntityTypeBuilder<SkyblockEnchantment> builder)
	{
		builder.HasKey(x => x.InternalId);
	}
}