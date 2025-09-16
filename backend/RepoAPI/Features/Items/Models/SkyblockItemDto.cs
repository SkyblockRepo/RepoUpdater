using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using EliteFarmers.HypixelAPI.DTOs;
using RepoAPI.Features.Items.ItemTemplate;
using RepoAPI.Features.Recipes.Models;
using RepoAPI.Features.Wiki.Services;
using RepoAPI.Features.Wiki.Templates;
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
	public string Name { get; set; } = string.Empty;
	public string? Category { get; set; }
	public string Source { get; set; } = "HypixelAPI";
	public double NpcValue { get; set; }
	public string Lore { get; set; } = string.Empty;
	
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
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public List<SkyblockRecipeDto> Recipes { get; set; } = [];
}

public static class SkyblockItemExtensions
{
	public static void PopulateTemplateData(this SkyblockItem item, WikiTemplateData<ItemTemplateDto>? templateData)
	{
		if (templateData == null) return;

		// item.TemplateData = templateData.Data;
		item.RawTemplate = templateData.Wikitext;

		item.Flags = new ItemFlags()
		{
			Tradable = templateData.Data?.Tradable?.Contains("Yes") is true,
			Auctionable = templateData.Data?.Auctionable?.Contains("Yes") is true,
			Bazaarable = templateData.Data?.Bazaarable?.Contains("Yes") is true,
			Enchantable = templateData.Data?.Enchantable?.Contains("Yes") is true,
			Museumable = templateData.Data?.Museumable is not null && !templateData.Data.Museumable.Contains("No"),
			Reforgeable = templateData.Data?.Reforgeable?.Contains("Yes") is true,
			Soulboundable = templateData.Data?.Soulboundable?.Contains("Yes") is true,
			Sackable = templateData.Data?.Sackable?.Contains("Yes") is true,
		};
		
		item.Category = templateData.Data?.Category ?? item.Category;

		var newLore = templateData.Data?.Lore ?? "";
		if (!string.IsNullOrWhiteSpace(newLore)) {
			item.Lore = ParserUtils.CleanLoreString(newLore);
		}
		item.Name = item.Data?.Name ?? templateData?.Data?.Name ?? item.InternalId;
	}
}