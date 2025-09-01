using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepoAPI.Core.Models;
using RepoAPI.Features.Wiki.Templates.ItemTemplate;
using Riok.Mapperly.Abstractions;

namespace RepoAPI.Features.Enchantments.Models;

[Mapper]
public static partial class SkyblockEnchantmentMapper
{
	[MapperIgnoreSource(nameof(SkyblockEnchantment.TemplateData))]
	[MapperIgnoreTarget(nameof(SkyblockEnchantmentDto.Items))]
	public static partial SkyblockEnchantmentDto ToDto(this SkyblockEnchantment entity);
	
	public static partial IQueryable<SkyblockEnchantmentDto> SelectDto(IQueryable<SkyblockEnchantment> query);
}

public class SkyblockEnchantmentDto
{
	[MaxLength(512)]
	public required string InternalId { get; set; }
	
	[MaxLength(512)]
	public string? Name { get; set; }
	
	[MaxLength(64)]
	public string Source { get; set; } = "HypixelWiki";
	
	public int MinLevel { get; set; } = 1;
	public int MaxLevel { get; set; } = 1;
	
	public List<string> Items
	{
		get
		{
			var list = new List<string>();
			for (var level = MinLevel; level <= MaxLevel; level++)
			{
				list.Add($"ENCHANTMENT_{InternalId}_{level}");
			}

			return list;
		}
	}
}