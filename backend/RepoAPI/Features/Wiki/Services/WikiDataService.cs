using RepoAPI.Features.Wiki.Templates;
using RepoAPI.Features.Wiki.Templates.AttributeList;
using RepoAPI.Features.Wiki.Templates.ItemTemplate;
using RepoAPI.Features.Wiki.Templates.PetTemplate;
using RepoAPI.Features.Wiki.Templates.RecipeTemplate;
namespace RepoAPI.Features.Wiki.Services;

public record WikiTemplateData<T>(T? Data, string Wikitext);

[RegisterService<WikiDataService>(LifeTime.Scoped)]
public partial class WikiDataService(IWikiApi wikiApi)
{
	public ITemplateParser<RecipeTemplateDto> RecipeTemplateParser { get; } = new RecipeTemplateParser();
	public ITemplateParser<ItemTemplateDto> ItemTemplateParser { get; } = new ItemTemplateParser();
	public ITemplateParser<PetTemplateDto> PetTemplateParser { get; } = new PetTemplateParser();
	public ITemplateParser<AttributeListTemplateDto> AttributeListParser { get; } = new AttributeListParser();
	
	public async Task<RecipeTemplateDto?> GetRecipeData(string itemId)
	{
		var apiResponse = await wikiApi.GetTemplateContentAsync(RecipeTemplateParser.GetTemplate(itemId));

		var page = apiResponse.Query.Pages.Values.FirstOrDefault();
		var wikitext = page?.Revisions.FirstOrDefault()?.Slots.Main.Content;

		if (string.IsNullOrEmpty(wikitext))
		{
			return null;
		}

		return RecipeTemplateParser.Parse(wikitext);
	}
	
	public async Task<WikiTemplateData<ItemTemplateDto>?> GetItemData(string itemId) 
	{
		var apiResponse = await wikiApi.GetTemplateContentAsync(ItemTemplateParser.GetTemplate(itemId));

		var page = apiResponse.Query.Pages.Values.FirstOrDefault();
		var wikitext = page?.Revisions.FirstOrDefault()?.Slots.Main.Content;

		if (string.IsNullOrEmpty(wikitext)) {
			return null;
		}

		return new WikiTemplateData<ItemTemplateDto>(ItemTemplateParser.Parse(wikitext), wikitext);
	}
	
	public async Task<Dictionary<string, WikiTemplateData<ItemTemplateDto>?>> BatchGetItemData(List<string> itemIds, bool templates = false)
	{
		var titleMapping = templates 
			? itemIds.ToDictionary(x => x, x => x)
			: itemIds.ToDictionary(ItemTemplateParser.GetTemplate, id => id);
		
		var titles = string.Join("|", titleMapping.Keys);
		var apiResponse = await wikiApi.GetTemplateContentAsync(titles);
		
		var result = new Dictionary<string, WikiTemplateData<ItemTemplateDto>?>();

		var normalizations = new Dictionary<string, string>();
		foreach (var item in apiResponse.Query.Normalized)
		{
			normalizations[item.To] = item.From;
		}
		
		foreach (var page in apiResponse.Query.Pages.Values) 
		{
			var matchingId = page.Title;
			if (normalizations.TryGetValue(matchingId, out var normalizedTitle)) 
			{
				matchingId = normalizedTitle;
			}
			
			var itemId = titleMapping.GetValueOrDefault(matchingId);
			if (itemId == null) continue; // No matching item id found
			
			var wikitext = page.Revisions.FirstOrDefault()?.Slots.Main.Content;
			if (string.IsNullOrEmpty(wikitext)) {
				result[itemId] = null;
			} else {
				result[itemId] = new WikiTemplateData<ItemTemplateDto>(ItemTemplateParser.Parse(wikitext), wikitext);
			}
		}
		
		return result;
	}
	
	public async Task<Dictionary<string, WikiTemplateData<RecipeTemplateDto>?>> BatchGetRecipeData(List<string> recipeTemplates)
	{
		var titles = string.Join("|", recipeTemplates);
		var apiResponse = await wikiApi.GetTemplateContentAsync(titles);
		
		var result = new Dictionary<string, WikiTemplateData<RecipeTemplateDto>?>();
		
		foreach (var page in apiResponse.Query.Pages.Values) 
		{
			var wikitext = page.Revisions.FirstOrDefault()?.Slots.Main.Content;
			if (wikitext is null) continue;
			
			var data = new WikiTemplateData<RecipeTemplateDto>(RecipeTemplateParser.Parse(wikitext), wikitext);
			var internalId = data.Data?.OutputInternalId;
			if (internalId is null) continue;
			
			if (string.IsNullOrEmpty(wikitext)) {
				result[internalId] = null;
			} else {
				result[internalId] = new WikiTemplateData<RecipeTemplateDto>(RecipeTemplateParser.Parse(wikitext), wikitext);
			}
		}
		
		return result;
	}
	
	public async Task<Dictionary<string, WikiTemplateData<PetTemplateDto>?>> BatchGetPetData(List<string> petTemplates)
	{
		var titles = string.Join("|", petTemplates);
		var apiResponse = await wikiApi.GetTemplateContentAsync(titles);
		
		var result = new Dictionary<string, WikiTemplateData<PetTemplateDto>?>();
		
		foreach (var page in apiResponse.Query.Pages.Values) 
		{
			var wikitext = page.Revisions.FirstOrDefault()?.Slots.Main.Content;
			if (wikitext is null) continue;
			
			var data = new WikiTemplateData<PetTemplateDto>(PetTemplateParser.Parse(wikitext), wikitext);
			var internalId = data.Data?.InternalId;
			if (internalId is null) continue;
			
			if (string.IsNullOrEmpty(wikitext)) {
				result[internalId] = null;
			} else {
				result[internalId] = new WikiTemplateData<PetTemplateDto>(PetTemplateParser.Parse(wikitext), wikitext);
			}
		}
		
		return result;
	}
	
	public async Task<List<string>> GetAllWikiItemsAsync()
	{
		return await GetWikiCategoryAsync("DataItem");
	}
	
	public async Task<List<string>> GetAllWikiEnchantmentsAsync()
	{
		return await GetWikiCategoryAsync("DataEnchantment");
	}
	
	public async Task<List<string>> GetAllWikiPetsAsync()
	{
		return await GetWikiCategoryAsync("DataPet");
	}
	
	public async Task<List<string>> GetAllWikiRecipesAsync()
	{
		return await GetWikiCategoryAsync("DataRecipe");
	}
	
	public async Task<List<string>> GetAllLootTablesAsync()
	{
		var category = await GetWikiCategoryAsync("DataLootTable");
		return category.Where(c => c.Contains("Loot_Table")).ToList();
	}
	
	public async Task<string> GetPageContentAsync(string title)
	{
		var response = await wikiApi.GetTemplateContentAsync(title);
		var page = response.Query.Pages.Values.FirstOrDefault();
		return page?.Revisions.FirstOrDefault()?.Slots.Main.Content ?? "";
	}
	
	public async Task<AttributeListTemplateDto> GetAttributeListAsync()
	{
		var wikitext = await GetPageContentAsync("Template:Attribute_Shard_List");
		return AttributeListParser.Parse(wikitext);
	}
	
	public async Task<List<string>> GetWikiCategoryAsync(string category)
	{
		var allItems = new List<string>();
		string? cmcontinue = null;
		do
		{
			var response = await wikiApi.GetCategoryMembersAsync(category, cmcontinue ?? "");
			var jsonDoc = System.Text.Json.JsonDocument.Parse(response);
			var query = jsonDoc.RootElement.GetProperty("query");
			var categoryMembers = query.GetProperty("categorymembers").EnumerateArray();

			foreach (var member in categoryMembers)
			{
				if (member.TryGetProperty("title", out var titleElement))
				{
					allItems.Add(titleElement.GetString() ?? "");
				}
			}

			cmcontinue = jsonDoc.RootElement.TryGetProperty("continue", out var continueElement) &&
			             continueElement.TryGetProperty("cmcontinue", out var cmcontinueElement)
				? cmcontinueElement.GetString()
				: null;

		} while (cmcontinue != null);
		
		return allItems;
	}
}