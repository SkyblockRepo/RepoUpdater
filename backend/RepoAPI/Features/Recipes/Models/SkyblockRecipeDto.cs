using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepoAPI.Features.Items.Models;
using RepoAPI.Util;
using Riok.Mapperly.Abstractions;

namespace RepoAPI.Features.Recipes.Models;

[Mapper]
public static partial class SkyblockRecipeMapper
{
	[MapProperty(nameof(SkyblockRecipe.ResultInternalId), nameof(SkyblockRecipeDto.ResultId))]
	[MapProperty(nameof(SkyblockRecipe.Ingredients), nameof(SkyblockRecipeDto.Crafting))]
	public static partial SkyblockRecipeDto ToDto(this SkyblockRecipe source);
	
	public static partial IQueryable<SkyblockRecipeDto> SelectDto(this IQueryable<SkyblockRecipe> source);
	
	[UserMapping(Default = true)]
	public static Dictionary<string, RecipeIngredientDto> MapIngredients(List<RecipeIngredient> ingredients) => 
		ingredients.Where(i => i.Slot != null).ToDictionary(i => i.Slot!, i => new RecipeIngredientDto()
		{
			Quantity = i.Quantity,
			ItemId = i.InternalId
		});
}

public class SkyblockRecipeDto
{
	public RecipeType Type { get; set; } = RecipeType.Crafting;
	
	[MaxLength(512)]
	public string? ResultId { get; set; }
	
	public int ResultQuantity { get; set; } = 1;
	
	public Dictionary<string, RecipeIngredientDto> Crafting { get; set; } = [];
}

public class RecipeIngredientDto
{
	[MaxLength(512)]
	public string ItemId { get; set; } = string.Empty;
	public int Quantity { get; set; } = 1;
}