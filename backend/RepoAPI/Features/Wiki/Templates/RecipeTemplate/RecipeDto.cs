using System.Security.Cryptography;

namespace RepoAPI.Features.Wiki.Templates.RecipeTemplate;

public class CraftingRecipe
{
	public required string Name { get; set; }
	public List<RecipeIngredientDto> Ingredients { get; set; } = [];
	public RecipeResultDto? Result { get; set; }
	public string Hash
	{
		get
		{
			var str =
				$"{Name}:{string.Join(",", Ingredients.Select(i => $"{i.Slot}={i.ItemId}x{i.Quantity}"))}->{Result?.ItemId}x{Result?.Quantity}";
			var bytes = System.Text.Encoding.UTF8.GetBytes(str);
			return Convert.ToHexString(MD5.HashData(bytes));
		}
	}
}

public class RecipeTemplateDto
{
	public List<CraftingRecipe> Recipes { get; set; } = [];
	public string OutputInternalId { get; set; } = string.Empty;
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

