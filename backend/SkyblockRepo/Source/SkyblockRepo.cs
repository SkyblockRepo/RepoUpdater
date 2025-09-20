using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SkyblockRepo.Models;

namespace SkyblockRepo;

public interface ISkyblockRepo
{
	Task InitializeAsync(CancellationToken cancellationToken = default);
}

public class SkyblockRepo(SkyblockRepoConfiguration configuration, ILogger<SkyblockRepo> logger) : ISkyblockRepo
{
	public static SkyblockRepoCache Cache { get; set; } = new();
	public static Manifest? Manifest { get => Cache.Manifest; set => Cache.Manifest = value; }

	private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};
	
	public async Task InitializeAsync(CancellationToken cancellationToken = default)
	{
		if (configuration.LocalRepoPath is not null)
		{
			await LoadLocalRepo();
			await LoadSkyblockItems(configuration.LocalRepoPath);
			await LoadSkyblockPets(configuration.LocalRepoPath);
		}

	}

	private async Task LoadLocalRepo()
	{
		if (configuration.LocalRepoPath is null) return;
		await LoadManifest(configuration.LocalRepoPath);
	}

	private async Task LoadManifest(string repoPath)
	{
		var manifestPath = Path.Combine(repoPath, "manifest.json");
		if (!File.Exists(manifestPath))
		{
			logger.LogCritical("Invalid {RepoPath}! No manifest.json found!", repoPath);
			return;
		}

		try
		{
			await using var stream = File.OpenRead(manifestPath);
			Manifest = await JsonSerializer.DeserializeAsync<Manifest>(stream, _jsonSerializerOptions);
			logger.LogInformation("Manifest file loaded successfully!");
		}
		catch (Exception e)
		{
			logger.LogError(e, "Error loading manifest!");
		}
	}

	private async Task LoadSkyblockItems(string repoPath)
	{
		var itemsFolderPath = Path.Combine(repoPath, Manifest?.Paths.Items ?? "items");

		var items = await LoadDataAsync<SkyblockItemData>(itemsFolderPath, ItemKeySelector);
		var nameSearch = items.ToDictionary(kvp => kvp.Key, kvp => new SkyblockItemNameSearch
		{
			InternalId = kvp.Value.InternalId,
			Name = kvp.Value.Name,
		});
		
		Cache.Items = items;
		Cache.ItemNameSearch = new ReadOnlyDictionary<string, SkyblockItemNameSearch>(nameSearch);
		
		return;

		// The key for items needs the '-' replaced with a ':'
		string ItemKeySelector(string fileName) => fileName.Replace("-", ":");
	}
	
	private async Task LoadSkyblockPets(string repoPath)
	{
		var folderPath = Path.Combine(repoPath, Manifest?.Paths.Pets ?? "pets");
		Cache.Pets = await LoadDataAsync<SkyblockPetData>(folderPath, DefaultKeySelector);
	}
	
	private static string DefaultKeySelector(string file) => file;
	
	/// <summary>
	/// Loads and deserializes JSON files from a specified folder into a read-only dictionary.
	/// </summary>
	/// <typeparam name="TModel">The type of the model to deserialize the JSON into.</typeparam>
	/// <param name="folderPath">The path to the folder containing the .json files.</param>
	/// <param name="keySelector">A function that takes a file name (without extension) and returns the desired dictionary key.</param>
	/// <returns>A read-only dictionary of the loaded data.</returns>
	private async Task<ReadOnlyDictionary<string, TModel>> LoadDataAsync<TModel>(
	    string folderPath,
	    Func<string, string> keySelector) where TModel : class
	{
	    var modelName = typeof(TModel).Name;
	    if (!Directory.Exists(folderPath))
	    {
	        logger.LogWarning("Directory for {ModelName} not found: {Path}", modelName, folderPath);
	        return new ReadOnlyDictionary<string, TModel>(new Dictionary<string, TModel>());
	    }

	    var files = Directory.GetFiles(folderPath, "*.json");
	    var data = new System.Collections.Concurrent.ConcurrentDictionary<string, TModel>();

	    await Parallel.ForEachAsync(files,
	        new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 },
	        async (filePath, cancellationToken) =>
	        {
	            try
	            {
	                await using var stream = File.OpenRead(filePath);
	                var model = await JsonSerializer.DeserializeAsync<TModel>(stream, _jsonSerializerOptions, cancellationToken);
	                
	                if (model is not null)
	                {
	                    var key = keySelector(Path.GetFileNameWithoutExtension(filePath));
	                    data.TryAdd(key, model);
	                }
	            }
	            catch (Exception e)
	            {
	                logger.LogError(e, "Error loading {ModelName} from {FilePath}!", modelName, filePath);
	            }
	        });

	    logger.LogInformation("Loaded {Count} {ModelName} models successfully!", data.Count, modelName);
	    return new ReadOnlyDictionary<string, TModel>(data);
	}
}