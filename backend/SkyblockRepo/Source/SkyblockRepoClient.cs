using SkyblockRepo.Models;
using SkyblockRepo.Models.Neu;

namespace SkyblockRepo;

public interface ISkyblockRepoClient
{
	Task InitializeAsync(CancellationToken cancellationToken = default);
	Task CheckForUpdatesAsync(CancellationToken cancellationToken = default);
	Task ReloadRepoAsync(CancellationToken cancellationToken = default);
	SkyblockItemData? FindItem(string itemIdOrName);
}

public class SkyblockRepoClient : ISkyblockRepoClient
{
	private readonly ISkyblockRepoUpdater _updater;
	public static SkyblockRepoData Data => SkyblockRepoUpdater.Data;
	public static SkyblockRepoClient Instance { get; private set; } = null!;
	
	public SkyblockRepoClient(ISkyblockRepoUpdater updater)
	{
		_updater = updater;
		Instance = this;
	}
	
	/// <summary>
	/// Gets the current Skyblock Repo data in memory.
	/// </summary>
	/// <returns></returns>
	public SkyblockRepoData GetData() => Data;
	
	/// <summary>
	/// Initialize the Skyblock Repo client and load existing data. Downloads data if not already present.
	/// Uses the configuration provided during initialization.
	/// </summary>
	/// <param name="cancellationToken"></param>
	public async Task InitializeAsync(CancellationToken cancellationToken = default)
	{
		await _updater.InitializeAsync(cancellationToken);
	}
	
	/// <summary>
	/// Check for updates to the Skyblock Repo data and download them if available.
	/// Uses the configuration provided during initialization.
	/// </summary>
	/// <param name="cancellationToken"></param>
	public async Task CheckForUpdatesAsync(CancellationToken cancellationToken = default)
	{
		await _updater.CheckForUpdatesAsync(cancellationToken);
	}
	
	/// <summary>
	/// Reloads the Skyblock Repo data from the source, replacing any existing data in memory.
	/// This doesn't fetch updates, it simply reloads the data.
	/// </summary>
	/// <param name="cancellationToken"></param>
	public async Task ReloadRepoAsync(CancellationToken cancellationToken = default)
	{
		await _updater.ReloadRepoAsync(cancellationToken);
	}

	/// <summary>
	/// Searches for an item by its ID or name. First checks for an exact ID match, then searches by name if no ID match is found.
	/// For name searches, it performs a case-insensitive substring match and returns the most fitting item.
	/// </summary>
	/// <param name="itemIdOrName"></param>
	/// <returns></returns>
	public SkyblockItemData? FindItem(string itemIdOrName)
	{
		itemIdOrName = itemIdOrName.Trim();
		
		if (string.IsNullOrWhiteSpace(itemIdOrName))
			throw new ArgumentException("Item ID or name must be provided.", nameof(itemIdOrName));

		var searchInput = itemIdOrName.ToUpperInvariant();
		
		// First, try to find by exact item ID
		if (Data.Items.TryGetValue(searchInput.Replace(" ", "_"), out var itemById))
		{
			return itemById;
		}
		
		// If not found by ID, search by name (case-insensitive substring match)
		var matchingItems = Data.ItemNameSearch.Values
			.Where(item => !string.IsNullOrWhiteSpace(item.Name) &&
			               (item.NameUpper.Contains(searchInput) || item.IdToNameUpper.Contains(searchInput)))
			.OrderBy(item => item.NameUpper == searchInput ? 0 : 1)
			.ThenBy(item => item.NameUpper.StartsWith(searchInput) ? 0 : 1)
			.ThenBy(item => item.Name!.Length)                    
			.ToList();

		if (matchingItems.Count == 0)
		{
			if (searchInput.Contains("BLOCK OF ")) {
				// Try making "BLOCK OF COAL" into "COAL BLOCK"
				return FindItem(searchInput.Replace("BLOCK OF ", "") + " BLOCK");
			}
			if (searchInput.Contains("WOOD")) {
				// Try making "OAK WOOD" into "OAK LOG"
				return FindItem(searchInput.Replace("WOOD", "LOG"));
			}
		}

		// If multiple items match, select the one with the shortest name (most fitting)
		var bestMatch = matchingItems.FirstOrDefault();
		return bestMatch is not null ? Data.Items.GetValueOrDefault(bestMatch.InternalId) : null;
	}

	/// <summary>
	/// Gets Neu item data by its internal ID.
	/// </summary>
	/// <param name="internalId"></param>
	/// <returns></returns>
	public NeuItemData? FindNeuItem(string internalId)
	{
		return Data.NeuItems.GetValueOrDefault(internalId);	
	}
}