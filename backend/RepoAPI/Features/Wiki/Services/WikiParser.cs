using Microsoft.Extensions.Caching.Hybrid;
using RepoAPI.Core.Models;
using RepoAPI.Features.Items.Services;
using RepoAPI.Features.Wiki.Templates;

namespace RepoAPI.Features.Wiki.Services;

public interface IWikiParser
{
	public Task<List<UpgradeCost>> ParseCostAsync(string wikitext);
}

[RegisterService<IWikiParser>(LifeTime.Scoped)]
public class WikiParser(IItemService itemService) : IWikiParser
{
	public async Task<List<UpgradeCost>> ParseCostAsync(string wikitext)
	{
		var parsedCost = ParserUtils.ParseUpgradeCost(wikitext);

		foreach (var item in parsedCost)
		{
			if (item.ItemId is null) continue;
			item.ItemId = await itemService.GetItemIdByNameAsync(item.ItemId, CancellationToken.None);
		}
		
		return parsedCost;
	}
}