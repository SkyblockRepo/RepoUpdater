using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using EliteFarmers.HypixelAPI.DTOs;
using RepoAPI.Features.Recipes.Models;
using Riok.Mapperly.Abstractions;
using SkyblockRepo.Models;

namespace RepoAPI.Features.Items.Models;

public static partial class SkyblockItemMapper
{
	[UserMapping(Default = true)]
	public static SkyblockItemData ToOutputDto(this SkyblockItem item)
	{
		return new SkyblockItemData()
		{
			InternalId = item.InternalId,
			Name = item.Name ?? item.Data?.Name ?? string.Empty,
			Category = item.Category,
			Source = item.Source,
			NpcValue = item.NpcValue,
			Lore = item.Lore,
			Flags = new SkyblockRepo.Models.ItemFlags
			{
				Tradable = item.Flags.Tradable,
				Bazaarable = item.Flags.Bazaarable,
				Auctionable = item.Flags.Auctionable,
				Reforgeable = item.Flags.Reforgeable,
				Enchantable = item.Flags.Enchantable,
				Museumable = item.Flags.Museumable,
				Soulboundable = item.Flags.Soulboundable,
				Sackable = item.Flags.Sackable,
				Other = item.Flags.Other
			},
			Data = item.Data?.ToSkyblockItemResponse(),
			Recipes = item.Recipes.Select(r => r.ToDto()).ToList(),
			SoldBy = item.SoldBy
		};
	}

	private static SkyblockItemResponse ToSkyblockItemResponse(this ItemResponse response)
	{
		// Using JSON serialization for simplicity as the structures are likely identical/compatible
		var json = System.Text.Json.JsonSerializer.Serialize(response);
		return System.Text.Json.JsonSerializer.Deserialize<SkyblockItemResponse>(json)!;
	}
}
