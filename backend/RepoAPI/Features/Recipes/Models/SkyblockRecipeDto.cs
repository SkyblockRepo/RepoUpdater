using System.ComponentModel.DataAnnotations;
using Riok.Mapperly.Abstractions;
using SkyblockRepo.Models;

namespace RepoAPI.Features.Recipes.Models;

[Mapper]
public static partial class SkyblockRecipeMapper
{
	[MapProperty(nameof(SkyblockRecipe.ResultInternalId), nameof(SkyblockRecipeData.ResultId))]
	[MapProperty(nameof(SkyblockRecipe.Ingredients), nameof(SkyblockRecipeData.Crafting))]
	public static partial SkyblockRecipeData ToDto(this SkyblockRecipe source);
	
	public static partial IQueryable<SkyblockRecipeData> SelectDto(this IQueryable<SkyblockRecipe> source);
	
	[UserMapping(Default = true)]
	public static Dictionary<string, RecipeIngredientDto> MapIngredients(List<RecipeIngredient> ingredients) => 
		ingredients.Where(i => i.Slot != null)
			.OrderBy(i => i.Slot!)
			.ToDictionary(i => i.Slot!, i => new RecipeIngredientDto() {
				Quantity = i.Quantity,
				ItemId = i.InternalId
			});
}
