using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RepoAPI.Features.Shops.Template;
using Riok.Mapperly.Abstractions;

namespace RepoAPI.Features.Shops.Models;

[Mapper]
public static partial class SkyblockShopMapper
{
	[UserMapping(Default = true)]
	public static SkyblockShopDto ToDto(this SkyblockShop shop)
	{
		var data = shop.TemplateData;
		return new SkyblockShopDto
		{
			InternalId = shop.InternalId,
			Name = shop.Name,
			Source = shop.Source,
			Slots = data?.Slots ?? new SortedDictionary<string, InventorySlot>(),
			AdditionalProperties = data?.AdditionalProperties ?? new SortedDictionary<string, object>()
		};
	}
}

public class SkyblockShopDto
{
	[MaxLength(512)]
	public required string InternalId { get; set; }
	public string? Name { get; set; }
	public string Source { get; set; } = "HypixelWiki";
	
	public SortedDictionary<string, InventorySlot> Slots { get; set; } = [];
	
	[JsonExtensionData]
	public SortedDictionary<string, object> AdditionalProperties { get; set; } = new();
}