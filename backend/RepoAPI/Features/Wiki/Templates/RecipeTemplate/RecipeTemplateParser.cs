namespace RepoAPI.Features.Wiki.Templates.RecipeTemplate;

using System.Text.RegularExpressions;

[RegisterService<ITemplateParser<RecipeTemplateDto>>(LifeTime.Singleton)]
public partial class RecipeTemplateParser : ITemplateParser<RecipeTemplateDto>
{
    [GeneratedRegex(
        @"\|(first|second|third|fourth|fifth|stranded)\s*=",
        RegexOptions.Compiled)]
    private static partial Regex RecipeNameRegex();

    [GeneratedRegex(
        @"\|(in\d+|out)\s*=\s*\{\{Item/([^|]+)\|lore\}\}(?:,(\d+))?",
        RegexOptions.Compiled)]
    private static partial Regex ItemPatternRegex();

    public RecipeTemplateDto Parse(string wikitext)
    {
        var templateDto = new RecipeTemplateDto();
        var nameMatches = RecipeNameRegex().Matches(wikitext);

        for (var i = 0; i < nameMatches.Count; i++)
        {
            var currentMatch = nameMatches[i];
            var recipeName = currentMatch.Groups[1].Value;

            var startIndex = currentMatch.Index + currentMatch.Length;
            var endIndex = (i + 1 < nameMatches.Count)
                ? nameMatches[i + 1].Index
                : wikitext.Length;

            var recipeContent = wikitext.Substring(startIndex, endIndex - startIndex);

            var currentRecipe = new CraftingRecipe { Name = recipeName };
            var itemMatches = ItemPatternRegex().Matches(recipeContent);
            
            foreach (Match itemMatch in itemMatches)
            {
                var slot = itemMatch.Groups[1].Value;
                var itemId = itemMatch.Groups[2].Value;
                var quantity = itemMatch.Groups[3].Success ? int.Parse(itemMatch.Groups[3].Value) : 1;
                
                if (slot == "out")
                {
                    // Assign the result to the current recipe being processed
                    currentRecipe.Result = new RecipeResultDto { ItemId = itemId, Quantity = quantity };
                }
                else // It's an ingredient
                {
                    currentRecipe.Ingredients.Add(new RecipeIngredientDto
                    {
                        Slot = slot,
                        ItemId = itemId,
                        Quantity = quantity
                    });
                }
            }
            
            // Add the recipe if it was successfully parsed
            if (currentRecipe.Ingredients.Count != 0 || currentRecipe.Result != null)
            {
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