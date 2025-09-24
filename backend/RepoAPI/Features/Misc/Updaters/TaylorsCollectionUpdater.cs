using RepoAPI.Core.Services;
using RepoAPI.Features.Output.Services;
using RepoAPI.Features.Pets.Services;
using RepoAPI.Features.Wiki.Services;
using RepoAPI.Features.Wiki.Templates;
using SkyblockRepo.Models;
using SkyblockRepo.Models.Misc;

namespace RepoAPI.Features.Misc.Updaters;

[RegisterService<TaylorsCollectionUpdater>(LifeTime.Scoped)]
public class TaylorsCollectionUpdater(
	IWikiDataService wikiService,
	JsonWriteQueue writeQueue,
	ILogger<PetsIngestionService> logger) : IDataLoader
{
	public Task InitializeAsync(CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public async Task FetchAndLoadDataAsync(CancellationToken ct = default)
	{
		var response = await wikiService.BatchGetData([ "Template:Data/TAYLOR'S_COLLECTION", "Template:Data/SEASONAL_BUNDLES" ], ct);
		var data = response.GetPageContent("Template:Data/TAYLOR'S_COLLECTION");
		if (data is null)
		{
			logger.LogWarning("No data found for TAYLOR'S_COLLECTION");
			return;
		}
		
		await writeQueue.QueueWriteAsync(new EntityWriteRequest(
			Path: "misc/taylors_collection.json",
			Data: Parse(data)
		));
		
		var bundles = response.GetPageContent("Template:Data/SEASONAL_BUNDLES");
		if (bundles is null)
		{
			logger.LogWarning("No bundles found for SEASONAL_BUNDLES");
			return;
		}
		
		await writeQueue.QueueWriteAsync(new EntityWriteRequest(
			Path: "misc/seasonal_bundles.json",
			Data: Parse(bundles)
		));
	}

	private TaylorCollection Parse(string wikitext)
	{
		var result = new TaylorCollection();
		
		var table = WikiTableParser.Parse(wikitext);

		foreach (var row in table.Rows)
		{
			var item = new TaylorCollectionItem();

			SkyblockItemData? itemData = null;
			
			var lore = row.GetValueOrDefault("Lore");
			if (lore is not null)
			{
				var dict = ParserUtils.GetPropDictionary(lore);
				if (dict.TryGetValue("a1", out var text))
				{
					row["Lore"] = ParserUtils.FillInItemLoreTemplates(text, ref itemData);
				}
			}

			if (itemData is null)
			{
				logger.LogWarning("Could not parse lore for Taylor's Collection item: {Lore}", lore);
				continue;
			}
			
			item.Output = [UpgradeCost.ItemCost(itemData.InternalId, 1)];
			
			// String that represents the gem cost, e.g. "3,200"
			var gemString = row.GetValueOrDefault("Cost");
			if (int.TryParse(gemString?.Replace(",", "").Trim() ?? "0", out var gems))
			{
				item.Cost.Add(UpgradeCost.GemCost(gems));
			}
			
			item.Name = itemData.Name;
			
			item.Released = ParserUtils.GetStringFromColorTemplate(row.GetValueOrDefault("Release Date") ?? row.GetValueOrDefault("Bundle Release") ?? string.Empty);
			
			result.Items.Add(item);
		}
		
		return result;
	} 
}