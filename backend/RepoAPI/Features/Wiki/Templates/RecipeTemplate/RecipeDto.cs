namespace RepoAPI.Features.Wiki.Templates.RecipeTemplate;

public class CraftingRecipe
{
	public required string Name { get; set; }
	public List<RecipeIngredientDto> Ingredients { get; set; } = [];
	public RecipeResultDto? Result { get; set; }
}

public class RecipeTemplateDto
{
	public List<CraftingRecipe> Recipes { get; set; } = []; 
}

public class RecipeIngredientDto
{
	public required string Slot { get; set; }
	public required string ItemId { get; set; }
	public int Quantity { get; set; }
}

public class RecipeResultDto
{
	public required string ItemId { get; set; }
	public int Quantity { get; set; }
}

