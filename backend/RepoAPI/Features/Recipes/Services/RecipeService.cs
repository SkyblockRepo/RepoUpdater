using RepoAPI.Data;

namespace RepoAPI.Features.Recipes.Services;

public interface IRecipeService
{
	
}

[RegisterService<RecipeService>(LifeTime.Scoped)]
public class RecipeService(DataContext context): IRecipeService
{
	
}