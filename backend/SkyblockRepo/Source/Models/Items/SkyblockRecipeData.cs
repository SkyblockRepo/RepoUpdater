using System.ComponentModel.DataAnnotations;

namespace SkyblockRepo.Models;

public class SkyblockRecipeData
{
	/// <summary>
	/// Slot name of the recipe, for example "first", "second", etc.
	/// </summary>
	public string? Name { get; set; }
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

[JsonStringEnum]
public enum RecipeType
{
	Crafting,
}