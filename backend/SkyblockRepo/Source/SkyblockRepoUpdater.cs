using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SkyblockRepo.Models;
using SkyblockRepo.Models.Neu;

namespace SkyblockRepo;

public interface ISkyblockRepoUpdater
{
	Task InitializeAsync(CancellationToken cancellationToken = default);
	Task CheckForUpdatesAsync(CancellationToken cancellationToken = default);
	Task ReloadRepoAsync(CancellationToken cancellationToken = default);
}

public class SkyblockRepoUpdater : ISkyblockRepoUpdater
{
	public static SkyblockRepoData Data { get; set; } = new();
	public static Manifest? Manifest { get => Data.Manifest; set => Data.Manifest = value; }
	
    private readonly ILogger<SkyblockRepoUpdater> _logger;
    private readonly IGithubRepoUpdater _skyblockRepoUpdater;
    private readonly IGithubRepoUpdater? _neuRepoUpdater; // Nullable for when UseNeuRepo is false
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
	    PropertyNameCaseInsensitive = true,
	    PropertyNamingPolicy = JsonNamingPolicy.CamelCase, 
	    Converters = { new NeuDropConverter() }
    };
    
    public SkyblockRepoUpdater(
        SkyblockRepoConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<SkyblockRepoUpdater>? logger = null,
        ILogger<GithubRepoUpdater>? repoUpdaterLogger = null)
    {
        _logger = logger ?? NullLogger<SkyblockRepoUpdater>.Instance;
        _skyblockRepoUpdater = CreateUpdater(configuration.SkyblockRepo, configuration.FileStoragePath, httpClientFactory, repoUpdaterLogger);

        if (configuration.UseNeuRepo)
        {
            _neuRepoUpdater = CreateUpdater(configuration.NeuRepo, configuration.FileStoragePath, httpClientFactory, repoUpdaterLogger);
        }
    }
	
    public SkyblockRepoUpdater(
        SkyblockRepoConfiguration configuration,
        HttpClient? httpClient = null,
        ILogger<SkyblockRepoUpdater>? logger = null,
        ILogger<GithubRepoUpdater>? repoUpdaterLogger = null)
    {
        _logger = logger ?? NullLogger<SkyblockRepoUpdater>.Instance;
        var client = httpClient ?? new HttpClient();
        _skyblockRepoUpdater = CreateUpdater(configuration.SkyblockRepo, configuration.FileStoragePath, client, repoUpdaterLogger);

        if (configuration.UseNeuRepo)
        {
            _neuRepoUpdater = CreateUpdater(configuration.NeuRepo, configuration.FileStoragePath, client, repoUpdaterLogger);
        }
    }

    private static IGithubRepoUpdater CreateUpdater(RepoSettings settings, string storagePath, IHttpClientFactory factory, ILogger<GithubRepoUpdater>? logger)
    {
        var options = new GithubRepoOptions(
            RepoName: settings.Name,
            FileStoragePath: storagePath,
            ApiEndpoint: settings.ApiEndpoint,
            ZipDownloadUrl: settings.Url.TrimEnd('/') + settings.ZipPath,
            LocalRepoPath: settings.LocalPath
        );
        return new GithubRepoUpdater(options, factory, logger);
    }
    
    private static IGithubRepoUpdater CreateUpdater(RepoSettings settings, string storagePath, HttpClient client, ILogger<GithubRepoUpdater>? logger)
    {
        var options = new GithubRepoOptions(
            RepoName: settings.Name,
            FileStoragePath: storagePath,
            ApiEndpoint: settings.ApiEndpoint,
            ZipDownloadUrl: settings.Url.TrimEnd('/') + settings.ZipPath,
            LocalRepoPath: settings.LocalPath
        );
        return new GithubRepoUpdater(options, client, logger);
    }
    
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // On startup, check all repos for updates, then load all data once.
        await CheckForUpdatesAsync(cancellationToken);
        await ReloadRepoAsync(cancellationToken);
    }

    public async Task CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var updateTasks = new List<Task<bool>> { _skyblockRepoUpdater.CheckForUpdatesAsync(cancellationToken) };
        if (_neuRepoUpdater is not null)
        {
            updateTasks.Add(_neuRepoUpdater.CheckForUpdatesAsync(cancellationToken));
        }

        var results = await Task.WhenAll(updateTasks);

        // If any of the repos were updated, trigger a single reload of all data.
        if (results.Any(wasUpdated => wasUpdated))
        {
            _logger.LogInformation("One or more repositories were updated. Reloading all data...");
            await ReloadRepoAsync(cancellationToken);
        }
    }

    public async Task ReloadRepoAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading data from primary SkyblockRepo...");
        var mainRepoPath = _skyblockRepoUpdater.RepoPath;
        await LoadManifest(mainRepoPath);
        await LoadSkyblockItems(mainRepoPath);
        await LoadSkyblockPets(mainRepoPath);
        
        if (_neuRepoUpdater is not null)
        {
            _logger.LogInformation("Loading data from NEU repo...");
            var neuRepoPath = _neuRepoUpdater.RepoPath;
            
            await LoadNeuItems(neuRepoPath);
        }
    }

	private async Task LoadManifest(string repoPath)
	{
		var manifestPath = Path.Combine(repoPath, "manifest.json");
		if (!File.Exists(manifestPath))
		{
			_logger.LogCritical("Invalid {RepoPath}! No manifest.json found!", repoPath);
			return;
		}

		try
		{
			await using var stream = File.OpenRead(manifestPath);
			Manifest = await JsonSerializer.DeserializeAsync<Manifest>(stream, _jsonSerializerOptions);
			_logger.LogInformation("Manifest file loaded successfully!");
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Error loading manifest!");
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

		Data.Items = items;
		Data.ItemNameSearch = new ReadOnlyDictionary<string, SkyblockItemNameSearch>(nameSearch);

		return;

		// The key for items needs the '-' replaced with a ':'
		string ItemKeySelector(string fileName) => fileName.Replace("-", ":");
	}

	private async Task LoadSkyblockPets(string repoPath)
	{
		var folderPath = Path.Combine(repoPath, Manifest?.Paths.Pets ?? "pets");
		Data.Pets = await LoadDataAsync<SkyblockPetData>(folderPath, DefaultKeySelector);
	}

	private static string DefaultKeySelector(string file) => file;
	
	private async Task LoadNeuItems(string repoPath)
	{
		var folderPath = Path.Combine(repoPath, "items");
		var neuItems = await LoadDataAsync<NeuItemData>(folderPath, DefaultKeySelector);
		
		_logger.LogInformation("Loaded {Count} NEU items", neuItems.Count);

		foreach (var (id, item) in neuItems)
		{
			if (item.ItemId != "minecraft:skull") continue;
			
			var sbRepoItem = Data.Items.GetValueOrDefault(id);
			if (sbRepoItem is null || sbRepoItem.Data?.Skin is not null) continue;
			
			var extractedSkin = SkyblockRepoRegexUtils.ExtractSkullTexture(item.NbtTag);
			if (extractedSkin is null) continue;
			
			sbRepoItem.Data ??= new SkyblockItemResponse();
			sbRepoItem.Data.Skin = new ItemSkin()
			{
				Value = extractedSkin.Value,
				Signature = extractedSkin.Signature
			};
		}
		
		Data.NeuItems = neuItems;
	}
	
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
	        _logger.LogWarning("Directory for {ModelName} not found: {Path}", modelName, folderPath);
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
	                _logger.LogError(e, "Error loading {ModelName} from {FilePath}!", modelName, filePath);
	            }
	        });

	    _logger.LogInformation("Loaded {Count} {ModelName} models successfully!", data.Count, modelName);
	    return new ReadOnlyDictionary<string, TModel>(data);
	}
}