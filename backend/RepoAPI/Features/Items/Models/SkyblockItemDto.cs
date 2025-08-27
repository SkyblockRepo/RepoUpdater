using System.ComponentModel.DataAnnotations;
using HypixelAPI.DTOs;
using RepoAPI.Features.Recipes.Models;
using RepoAPI.Features.Wiki.Services;
using RepoAPI.Features.Wiki.Templates.ItemTemplate;
using Riok.Mapperly.Abstractions;

namespace RepoAPI.Features.Items.Models;

[Mapper]
[UseStaticMapper(typeof(SkyblockRecipeMapper))]
public static partial class SkyblockItemMapper
{
	public static partial SkyblockItemDto ToDto(this SkyblockItem item);
	public static partial IQueryable<SkyblockItemDto> SelectDto(this IQueryable<SkyblockItem> item);
}


public class SkyblockItemDto
{
	[MaxLength(512)]
	public required string InternalId { get; set; }
	public string? Category { get; set; }
	public string Source { get; set; } = "HypixelAPI";
	public double NpcValue { get; set; }
	
	public ItemFlags Flags { get; set; } = new();
    
	/// <summary>
	/// Hypixel item data from /resources/skyblock/items
	/// </summary>
	public ItemResponse? Data { get; set; }
	
	/// <summary>
	/// Parsed data from the item template on the Hypixel Wiki.
	/// </summary>
	public ItemTemplateDto? TemplateData { get; set; }
	
	/// <summary>
	/// Recipes that can produce this item as an output.
	/// </summary>
	public List<SkyblockRecipeDto> Recipes { get; set; } = [];
}

public static class SkyblockItemExtensions
{
	public static void PopulateTemplateData(this SkyblockItem item, WikiTemplateData<ItemTemplateDto>? templateData)
	{
		if (templateData == null) return;

		item.TemplateData = templateData.Data;
		item.RawTemplate = templateData.Wikitext;

		item.Flags.Tradable = templateData.Data?.Tradable == "Yes";
		item.Flags.Auctionable = templateData.Data?.Auctionable == "Yes";
		item.Flags.Bazaarable = templateData.Data?.Bazaarable == "Yes";
		item.Flags.Enchantable = templateData.Data?.Enchantable == "Yes";
		item.Flags.Museumable = templateData.Data?.Museumable != "No";
		item.Flags.Reforgeable = templateData.Data?.Reforgeable == "Yes";
		item.Flags.Soulboundable = templateData.Data?.Soulboundable == "Yes";
	}
}