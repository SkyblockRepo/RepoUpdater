namespace RepoAPI.Features.Wiki.Templates.RecipeTemplate;

using System.Text.RegularExpressions;

[RegisterService<ITemplateParser<RecipeTemplateDto>>(LifeTime.Singleton)]
public partial class RecipeTemplateParser : ITemplateParser<RecipeTemplateDto>
{
    [GeneratedRegex(
        @"\{\{Item/([^|]+)\|lore\}\}(?:,(\d+))?",
        RegexOptions.Compiled)]
    private static partial Regex RecipeIngredientPatternRegex();

    [GeneratedRegex(@"\{\{Recipe/doc\|(?<id>[A-Z0-9_]+)\}\}", RegexOptions.Compiled)]
    private static partial Regex RecipeOutputInternalIdRegex();
    
    public RecipeTemplateDto Parse(string wikitext)
    {
        var templateDto = new RecipeTemplateDto();
        
        var internalId = RecipeOutputInternalIdRegex().Match(wikitext).Groups["id"].Value;
        if (!string.IsNullOrEmpty(internalId))
        {
            templateDto.OutputInternalId = internalId;
        }
        
        var properties = ParserUtils.GetPropDictionary(wikitext);
        foreach (var (recipeName, recipeText) in properties)
        {
            var recipeProperties = ParserUtils.GetPropDictionary(recipeText);
            
            var currentRecipe = new CraftingRecipe { Name = recipeName };

            foreach (var (key, text) in recipeProperties)
            {
                var matches = RecipeIngredientPatternRegex().Matches(text);
                foreach (Match match in matches)
                {
                    var itemId = match.Groups[1].Value;
                    var quantity = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 1;
                    if (key == "out")
                    {
                        currentRecipe.Result = new RecipeResultDto { ItemId = itemId, Quantity = quantity };
                    }
                    else
                    {
                        currentRecipe.Ingredients.Add(new RecipeIngredientDto
                        {
                            Slot = key.Replace("in", "").Trim(),
                            ItemId = itemId,
                            Quantity = quantity
                        });
                    }
                }
            }

            if (currentRecipe.Ingredients.Count != 0 || currentRecipe.Result != null) {
                templateDto.Recipes.Add(currentRecipe);
            }
        }
        
        return templateDto;
    }

	public string GetTemplate(string input)
	{
		return $"Template:Recipe/{input}";
	}
}