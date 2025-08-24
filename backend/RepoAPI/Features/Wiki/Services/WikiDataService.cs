using RepoAPI.Features.Wiki.Templates;
using RepoAPI.Features.Wiki.Templates.ItemTemplate;
using RepoAPI.Features.Wiki.Templates.RecipeTemplate;

namespace RepoAPI.Features.Wiki.Services;

[RegisterService<WikiDataService>(LifeTime.Scoped)]
public class WikiDataService(
	IWikiApi wikiApi,
	ITemplateParser<RecipeTemplateDto> recipeParser,
	ITemplateParser<ItemTemplateDto> itemTemplateParser
	)
{
	public async Task<RecipeTemplateDto?> GetRecipeData(string itemId)
	{
		var apiResponse = await wikiApi.GetTemplateContentAsync(recipeParser.GetTemplate(itemId));

		var page = apiResponse.Query.Pages.Values.FirstOrDefault();
		var wikitext = page?.Revisions.FirstOrDefault()?.Slots.Main.Content;

		if (string.IsNullOrEmpty(wikitext))
		{
			return null;
		}

		return recipeParser.Parse(ExtractIncludeOnlyContent(wikitext));
	}
	
	public async Task<ItemTemplateDto?> GetItemData(string itemId) 
	{
		var apiResponse = await wikiApi.GetTemplateContentAsync(itemTemplateParser.GetTemplate(itemId));

		var page = apiResponse.Query.Pages.Values.FirstOrDefault();
		var wikitext = page?.Revisions.FirstOrDefault()?.Slots.Main.Content;

		if (string.IsNullOrEmpty(wikitext))
		{
			return null;
		}

		return itemTemplateParser.Parse(ExtractIncludeOnlyContent(wikitext));
	}
	
	private static string ExtractIncludeOnlyContent(string wikitext)
	{
		const string startTag = "<includeonly>";
		const string endTag = "</includeonly>";

		var startIndex = wikitext.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
		if (startIndex == -1) return wikitext; // Fallback to full text if tag not found

		startIndex += startTag.Length;

		var endIndex = wikitext.IndexOf(endTag, startIndex, StringComparison.OrdinalIgnoreCase);
		return endIndex == -1 
			? wikitext 
			: wikitext.Substring(startIndex, endIndex - startIndex);
	}
}