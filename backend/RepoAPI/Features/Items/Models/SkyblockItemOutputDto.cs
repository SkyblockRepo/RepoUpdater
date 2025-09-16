using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using EliteFarmers.HypixelAPI.DTOs;
using RepoAPI.Features.Recipes.Models;
using Riok.Mapperly.Abstractions;

namespace RepoAPI.Features.Items.Models;

public static partial class SkyblockItemMapper
{
	[UserMapping(Default = true)]
	public static SkyblockItemOutputDto ToOutputDto(this SkyblockItem item)
	{
		return new SkyblockItemOutputDto()
		{
			InternalId = item.InternalId,
			Name = item.Name ?? item.Data?.Name ?? string.Empty,
			Category = item.Category,
			Source = item.Source,
			NpcValue = item.NpcValue,
			Lore = item.Lore,
			Flags = item.Flags,
			Data = item.Data,
			Recipes = item.Recipes.Select(r => r.ToDto()).ToList() ?? []
		};
	}
}

public class SkyblockItemOutputDto
{
	[MaxLength(512)]
	public required string InternalId { get; set; }
	public string Name { get; set; } = string.Empty;
	public string? Category { get; set; }
	public string Source { get; set; } = "HypixelAPI";
	public double NpcValue { get; set; }
	public string Lore { get; set; } = string.Empty;
	public required ItemFlags Flags { get; set; }
    
	/// <summary>
	/// Hypixel item data from /resources/skyblock/items
	/// </summary>
	public ItemResponse? Data { get; set; }
	
	/// <summary>
	/// Recipes that can produce this item as an output.
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public List<SkyblockRecipeDto> Recipes { get; set; } = [];
}