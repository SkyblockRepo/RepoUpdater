using RepoAPI.Features.Items.ItemTemplate;
using RepoAPI.Features.NPCs.NpcTemplate;
using RepoAPI.Features.Pets.PetTemplate;
using RepoAPI.Features.Shops.Template;
using RepoAPI.Features.Wiki.Responses;
using RepoAPI.Features.Wiki.Templates;
using RepoAPI.Features.Wiki.Templates.AttributeList;
using RepoAPI.Features.Wiki.Templates.RecipeTemplate;
using RepoAPI.Features.Zones.NpcTemplate;

namespace RepoAPI.Features.Wiki.Services;

public record WikiTemplateData<T>(T? Data, string Wikitext);

public interface IWikiDataService
{
	Task<RecipeTemplateDto?> GetRecipeData(string itemId);
	Task<WikiTemplateData<ItemTemplateDto>?> GetItemData(string itemId);
	Task<FormattedWikiResponse> BatchGetData(List<string> titleList, CancellationToken cancellationToken = default);
	Task<Dictionary<string, WikiTemplateData<ItemTemplateDto>?>> BatchGetItemData(List<string> itemIds, bool templates = false);
	Task<Dictionary<string, WikiTemplateData<RecipeTemplateDto>?>> BatchGetRecipeData(List<string> recipeTemplates);
	Task<Dictionary<string, WikiTemplateData<PetTemplateDto>?>> BatchGetPetData(List<string> petTemplates);
	Task<Dictionary<string, WikiTemplateData<NpcTemplateDto>?>> BatchGetNpcData(List<string> petIds);
	Task<Dictionary<string, WikiTemplateData<ZoneTemplateDto>?>> BatchGetZoneData(List<string> zoneTemplates);
	Task<Dictionary<string, WikiTemplateData<ShopTemplateDto>?>> BatchGetShopData(List<string> shopTemplates);
	Task<List<string>> GetAllWikiItemsAsync();
	Task<List<string>> GetAllWikiEnchantmentsAsync();
	Task<List<string>> GetAllWikiPetsAsync();
	Task<List<string>> GetAllWikiNpcsAsync();
	Task<List<string>> GetAllWikiRecipesAsync();
	Task<List<string>> GetAllWikiZonesAsync();
	Task<List<string>> GetAllWikiShops();
	Task<List<string>> GetAllLootTablesAsync();
	Task<string> GetPageContentAsync(string title);
	Task<AttributeListTemplateDto> GetAttributeListAsync();
	Task<List<string>> GetWikiCategoryAsync(string category);
}


[RegisterService<IWikiDataService>(LifeTime.Scoped)]
public class WikiDataService(IWikiApi wikiApi) : IWikiDataService
{
	public ITemplateParser<RecipeTemplateDto> RecipeTemplateParser { get; } = new RecipeTemplateParser();
	public ITemplateParser<ItemTemplateDto> ItemTemplateParser { get; } = new ItemTemplateParser();
	public ITemplateParser<PetTemplateDto> PetTemplateParser { get; } = new PetTemplateParser();
	public ITemplateParser<AttributeListTemplateDto> AttributeListParser { get; } = new AttributeListParser();
	public ITemplateParser<NpcTemplateDto> NpcTemplateParser { get; } = new NpcTemplateParser();
	public ITemplateParser<ZoneTemplateDto> ZoneTemplateParser { get; } = new ZoneTemplateParser();
	public ITemplateParser<ShopTemplateDto> ShopTemplateParser { get; } = new ShopTemplateParser();
	
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
	
	public async Task<FormattedWikiResponse> BatchGetData(List<string> titleList, CancellationToken cancellationToken = default)
	{
		const int maxBatchSize = 50;
		
		var result = new FormattedWikiResponse
		{
			Normalized = [],
			Pages = new Dictionary<string, FormattedPage>()
		};
		
		for (var i = 0; i < titleList.Count; i += maxBatchSize)
		{
			if (cancellationToken.IsCancellationRequested) break;
			
			var batch = titleList.Skip(i).Take(maxBatchSize).ToList();
			var titles = string.Join("|", batch);
			var apiResponse = await wikiApi.GetTemplateContentAsync(titles);

			result.Normalized.AddRange(apiResponse.Query.Normalized);
			foreach (var page in apiResponse.Query.Pages.Values)
			{
				result.Pages.TryAdd(page.Title, new FormattedPage
				{
					Pageid = page.Pageid,
					Title = page.Title,
					Ns = page.Ns,
					Content = page.Revisions.FirstOrDefault()?.Slots.Main.Content ?? ""
				});
			}
		}
		
		return result;
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
	
	public async Task<Dictionary<string, WikiTemplateData<NpcTemplateDto>?>> BatchGetNpcData(List<string> petTemplates)
	{
		var titles = string.Join("|", petTemplates);
		var apiResponse = await wikiApi.GetTemplateContentAsync(titles);
		
		var result = new Dictionary<string, WikiTemplateData<NpcTemplateDto>?>();
		
		foreach (var page in apiResponse.Query.Pages.Values) 
		{
			var wikitext = page.Revisions.FirstOrDefault()?.Slots.Main.Content;
			if (wikitext is null) continue;
			
			var data = new WikiTemplateData<NpcTemplateDto>(NpcTemplateParser.Parse(wikitext), wikitext);
			var internalId = data.Data?.InternalId;
			if (internalId is null) continue;
			
			if (string.IsNullOrEmpty(wikitext)) {
				result[internalId] = null;
			} else {
				result[internalId] = new WikiTemplateData<NpcTemplateDto>(NpcTemplateParser.Parse(wikitext), wikitext);
			}
		}
		
		return result;
	}
	
	public async Task<Dictionary<string, WikiTemplateData<ZoneTemplateDto>?>> BatchGetZoneData(List<string> petTemplates)
	{
		var titles = string.Join("|", petTemplates);
		var apiResponse = await wikiApi.GetTemplateContentAsync(titles);
		
		var result = new Dictionary<string, WikiTemplateData<ZoneTemplateDto>?>();
		
		foreach (var page in apiResponse.Query.Pages.Values) 
		{
			var wikitext = page.Revisions.FirstOrDefault()?.Slots.Main.Content;
			if (wikitext is null) continue;
			
			var data = new WikiTemplateData<ZoneTemplateDto>(ZoneTemplateParser.Parse(wikitext), wikitext);
			var internalId = data.Data?.InternalId;
			if (internalId is null) continue;
			
			if (string.IsNullOrEmpty(wikitext)) {
				result[internalId] = null;
			} else {
				result[internalId] = new WikiTemplateData<ZoneTemplateDto>(ZoneTemplateParser.Parse(wikitext), wikitext);
			}
		}
		
		return result;
	}
	
	public async Task<Dictionary<string, WikiTemplateData<ShopTemplateDto>?>> BatchGetShopData(List<string> shopTemplates)
	{
		var titles = string.Join("|", shopTemplates);
		var apiResponse = await wikiApi.GetTemplateContentAsync(titles);
		
		var result = new Dictionary<string, WikiTemplateData<ShopTemplateDto>?>();
		
		foreach (var page in apiResponse.Query.Pages.Values) 
		{
			var wikitext = page.Revisions.FirstOrDefault()?.Slots.Main.Content;
			if (wikitext is null) continue;
			
			var backupId = page.Title.Replace("Template:","").Trim().Replace(" ","_").Replace("/","-").ToUpperInvariant();
			
			var data = new WikiTemplateData<ShopTemplateDto>(ShopTemplateParser.Parse(wikitext, backupId), wikitext);
			var internalId = data.Data?.InternalId;
			if (internalId is null) continue;
			
			if (string.IsNullOrEmpty(wikitext)) {
				result[internalId] = null;
			} else
			{
				result[internalId] = data;
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

	public async Task<List<string>> GetAllWikiNpcsAsync()
	{
		return await GetWikiCategoryAsync("DataNPC");
	}
	
	public async Task<List<string>> GetAllWikiZonesAsync()
	{
		return await GetWikiCategoryAsync("DataZone");
	}

	public async Task<List<string>> GetAllWikiRecipesAsync()
	{
		return await GetWikiCategoryAsync("DataRecipe");
	}
	
	public async Task<List<string>> GetAllWikiShops()
	{
		return await GetWikiCategoryAsync("NPC_UI_Templates");
	}
	
	public async Task<List<string>> GetAllLootTablesAsync()
	{
		var category = await GetWikiCategoryAsync("DataLootTable");
		return category.Where(c => c.Contains("Loot_Table")).ToList();
	}
	
	public async Task<string> GetPageContentAsync(string title)
	{
		var apiResponse = await wikiApi.GetTemplateContentAsync(title);
		var page = apiResponse.Query.Pages.Values.FirstOrDefault();
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