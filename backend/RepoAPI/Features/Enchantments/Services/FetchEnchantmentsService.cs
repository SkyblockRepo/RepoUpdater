using RepoAPI.Core.Services;
using RepoAPI.Data;
using RepoAPI.Features.Wiki.Services;

namespace RepoAPI.Features.Enchantments.Services;

[RegisterService<FetchEnchantmentsService>(LifeTime.Scoped)]
public class FetchEnchantmentsService(
	DataContext context,
	IWikiDataService wikiDataService
	) : IDataLoader
{
	public Task InitializeAsync(CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task FetchAndLoadDataAsync(CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}
}