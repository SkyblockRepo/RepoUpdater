namespace RepoAPI.Core.Services;

public interface IDataLoader
{
	public Task InitializeAsync(CancellationToken ct = default);
	public Task FetchAndLoadDataAsync(CancellationToken ct = default);
}