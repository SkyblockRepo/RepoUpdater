using RepoAPI.Core.Services;
using RepoAPI.Features.Misc.Updaters;

namespace RepoAPI.Features.Misc;

[RegisterService<MiscDataUpdater>(LifeTime.Scoped)]
public class MiscDataUpdater(TaylorsCollectionUpdater taylorsCollectionUpdater)
{
	public async Task UpdateMiscDataAsync(CancellationToken ct)
	{
		await taylorsCollectionUpdater.FetchAndLoadDataAsync(ct);
	}
}