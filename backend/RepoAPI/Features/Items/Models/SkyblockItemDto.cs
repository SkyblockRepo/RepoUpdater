using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RepoAPI.Features.Items.ItemTemplate;
using RepoAPI.Features.Recipes.Models;
using RepoAPI.Features.Wiki.Services;
using RepoAPI.Features.Wiki.Templates;
using Riok.Mapperly.Abstractions;
using SkyblockRepo.Models;

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
	public List<SkyblockRecipeData> Recipes { get; set; } = [];
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public List<SkyblockSoldBy>? SoldBy { get; set; }
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
		
		var name = templateData?.Data?.Name;
		
		if (string.IsNullOrWhiteSpace(name) && item.Data != null && !string.IsNullOrWhiteSpace(item.Data.Name))
		{
			name = item.Data.Name;
		}
		
		if (string.IsNullOrWhiteSpace(name))
		{
			name = item.InternalId;
		}
		
		item.Name = name!;

		if (item.Data == null)
		{
			item.Data = new EliteFarmers.HypixelAPI.DTOs.ItemResponse
			{
				Id = item.InternalId,
				Name = item.Name
			};
		}
	}
}