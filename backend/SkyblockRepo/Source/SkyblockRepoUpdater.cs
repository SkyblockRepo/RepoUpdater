using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SkyblockRepo.Models;
using SkyblockRepo.Models.Misc;
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
    private readonly RepoRuntime _skyblockRepo;
    private readonly RepoRuntime? _neuRepo; // Nullable for when UseNeuRepo is false
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
        _skyblockRepo = CreateUpdater(configuration.SkyblockRepo, configuration.FileStoragePath, httpClientFactory, repoUpdaterLogger, nameof(configuration.SkyblockRepo));

        if (configuration.UseNeuRepo)
        {
            _neuRepo = CreateUpdater(configuration.NeuRepo, configuration.FileStoragePath, httpClientFactory, repoUpdaterLogger, nameof(configuration.NeuRepo));
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
        _skyblockRepo = CreateUpdater(configuration.SkyblockRepo, configuration.FileStoragePath, client, repoUpdaterLogger, nameof(configuration.SkyblockRepo));

        if (configuration.UseNeuRepo)
        {
            _neuRepo = CreateUpdater(configuration.NeuRepo, configuration.FileStoragePath, client, repoUpdaterLogger, nameof(configuration.NeuRepo));
        }
    }

    private static RepoRuntime CreateUpdater(
	    RepoSettings settings,
	    string storagePath,
	    IHttpClientFactory factory,
	    ILogger<GithubRepoUpdater>? logger,
	    string settingsName)
    {
	    ValidateRepoSettings(settings, settingsName);

        var options = new GithubRepoOptions(
            RepoName: settings.Name,
            FileStoragePath: storagePath,
            ApiEndpoint: settings.ApiEndpoint,
            ZipDownloadUrl: settings.Url.TrimEnd('/') + settings.ZipPath,
            LocalRepoPath: settings.LocalPath
        )
        {
	        StorageMode = settings.StorageMode,
	        ZipFileName = settings.ZipFileName
        };
        return CreateRepoRuntime(new GithubRepoUpdater(options, factory, logger), settings, settingsName);
    }
    
    private static RepoRuntime CreateUpdater(
	    RepoSettings settings,
	    string storagePath,
	    HttpClient client,
	    ILogger<GithubRepoUpdater>? logger,
	    string settingsName)
    {
	    ValidateRepoSettings(settings, settingsName);

        var options = new GithubRepoOptions(
            RepoName: settings.Name,
            FileStoragePath: storagePath,
            ApiEndpoint: settings.ApiEndpoint,
            ZipDownloadUrl: settings.Url.TrimEnd('/') + settings.ZipPath,
            LocalRepoPath: settings.LocalPath
        )
        {
	        StorageMode = settings.StorageMode,
	        ZipFileName = settings.ZipFileName
        };
        return CreateRepoRuntime(new GithubRepoUpdater(options, client, logger), settings, settingsName);
    }

    private static RepoRuntime CreateRepoRuntime(IGithubRepoUpdater updater, RepoSettings settings, string settingsName)
    {
        var extractedProbe = settingsName switch
        {
            nameof(SkyblockRepoConfiguration.SkyblockRepo) => new RepoExtractedProbe("manifest.json", false),
            nameof(SkyblockRepoConfiguration.NeuRepo) => new RepoExtractedProbe("items", true),
            _ => null
        };

        return new RepoRuntime(
            updater,
            settings.Name,
            settings.StorageMode,
            settings.ZipFileName,
            settings.LocalPath is not null,
            extractedProbe);
    }

    private static void ValidateRepoSettings(RepoSettings settings, string settingsName)
    {
	    ArgumentNullException.ThrowIfNull(settings);

	    if (settings.LocalPath is not null)
	    {
		    return;
	    }

	    var missingFields = new List<string>();
	    if (string.IsNullOrWhiteSpace(settings.Name))
	    {
		    missingFields.Add(nameof(settings.Name));
	    }

	    if (string.IsNullOrWhiteSpace(settings.Url))
	    {
		    missingFields.Add(nameof(settings.Url));
	    }

	    if (string.IsNullOrWhiteSpace(settings.ZipPath))
	    {
		    missingFields.Add(nameof(settings.ZipPath));
	    }

	    if (string.IsNullOrWhiteSpace(settings.ApiEndpoint))
	    {
		    missingFields.Add(nameof(settings.ApiEndpoint));
	    }

	    if (missingFields.Count == 0)
	    {
		    return;
	    }

	    throw new InvalidOperationException(
		    $"{settingsName} is missing required remote settings: {string.Join(", ", missingFields)}. " +
		    $"If you only want to change one option such as StorageMode, mutate the existing settings object instead of replacing it. " +
		    $"Example: config.{settingsName}.StorageMode = RepoStorageMode.ZipArchive;");
    }
    
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var missingRepos = GetRepos()
            .Where(repo => !RepoDataExists(repo))
            .ToArray();
        
        if (missingRepos.Length > 0)
        {
            _logger.LogInformation("Repository data not found locally. Downloading...");
            await Task.WhenAll(missingRepos.Select(repo => DownloadMissingRepoAsync(repo, cancellationToken)));
        }
        else
        {
            _logger.LogInformation("Repository data found locally. Skipping update check on startup.");
        }
        
        await ReloadRepoAsync(cancellationToken);
    }

    public async Task CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var results = await Task.WhenAll(GetRepos().Select(repo => CheckForUpdatesOrRecoverAsync(repo, cancellationToken)));

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
        if (!RepoDataExists(_skyblockRepo))
        {
            _logger.LogWarning("Primary SkyblockRepo data source not found: {SourcePath}", GetSourcePath(_skyblockRepo));
            return;
        }

        var primaryStopwatch = Stopwatch.StartNew();
        await using (var mainSnapshot = await RepoSnapshot.OpenAsync(
	                     _skyblockRepo.Updater.RepoPath,
	                     _skyblockRepo.StorageMode,
	                     _skyblockRepo.ZipFileName,
	                     cancellationToken))
        {
	        if (!await LoadManifest(mainSnapshot, cancellationToken))
	        {
		        return;
	        }

	        await Task.WhenAll(
		        LoadSkyblockItems(mainSnapshot, cancellationToken),
		        LoadSkyblockPets(mainSnapshot, cancellationToken),
		        LoadMiscData(mainSnapshot, cancellationToken),
		        LoadSkyblockEnchantments(mainSnapshot, cancellationToken),
		        LoadSkyblockNpcs(mainSnapshot, cancellationToken),
		        LoadSkyblockShops(mainSnapshot, cancellationToken),
		        LoadSkyblockZones(mainSnapshot, cancellationToken)
	        );
        }
        primaryStopwatch.Stop();
        _logger.LogInformation("Loaded primary SkyblockRepo in {ElapsedMs} ms.", primaryStopwatch.ElapsedMilliseconds);
        
        if (_neuRepo is not null)
        {
            _logger.LogInformation("Loading data from NEU repo...");
            if (!RepoDataExists(_neuRepo))
            {
	            _logger.LogWarning("NEU data source not found: {SourcePath}", GetSourcePath(_neuRepo));
	            return;
            }

            var neuStopwatch = Stopwatch.StartNew();
            await using var neuSnapshot = await RepoSnapshot.OpenAsync(
	            _neuRepo.Updater.RepoPath,
	            _neuRepo.StorageMode,
	            _neuRepo.ZipFileName,
	            cancellationToken);

            await LoadNeuItems(neuSnapshot, cancellationToken);
            neuStopwatch.Stop();
            _logger.LogInformation("Loaded NEU repo in {ElapsedMs} ms.", neuStopwatch.ElapsedMilliseconds);
        }
    }

	private async Task<bool> LoadManifest(IRepoSnapshot snapshot, CancellationToken cancellationToken)
	{
		const string manifestPath = "manifest.json";
		if (!snapshot.FileExists(manifestPath))
		{
			_logger.LogCritical("Invalid repo source {RepoPath}! No manifest.json found!", snapshot.SourcePath);
			return false;
		}

		try
		{
			await using var stream = await snapshot.OpenReadAsync(manifestPath, cancellationToken);
			if (stream is null)
			{
				_logger.LogCritical("Unable to open manifest.json from {RepoPath}", snapshot.SourcePath);
				return false;
			}

			Manifest = await JsonSerializer.DeserializeAsync<Manifest>(stream, _jsonSerializerOptions, cancellationToken);
			_logger.LogInformation("Manifest file loaded successfully!");
			return Manifest is not null;
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Error loading manifest!");
			return false;
		}
	}

	private async Task LoadSkyblockItems(IRepoSnapshot snapshot, CancellationToken cancellationToken)
	{
		var itemsFolderPath = Manifest?.Paths.Items ?? "items";

		var items = await LoadDataAsync<SkyblockItemData>(snapshot, itemsFolderPath, ItemKeySelector, cancellationToken);
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

	private async Task LoadSkyblockPets(IRepoSnapshot snapshot, CancellationToken cancellationToken)
	{
		var folderPath = Manifest?.Paths.Pets ?? "pets";
		Data.Pets = await LoadDataAsync<SkyblockPetData>(snapshot, folderPath, DefaultKeySelector, cancellationToken);
	}
	
	private async Task LoadMiscData(IRepoSnapshot snapshot, CancellationToken cancellationToken)
	{
		// Load Taylor Collections
		var folderPath = Manifest?.Paths.Misc ?? "misc";
		
		var taylorCollections = await LoadFileAsync<TaylorCollection>(snapshot, Path.Combine(folderPath, "taylors_collection.json"), cancellationToken);
		Data.TaylorCollection = taylorCollections ?? new TaylorCollection();

		var seasonalBundles = await LoadFileAsync<TaylorCollection>(snapshot, Path.Combine(folderPath, "seasonal_bundles.json"), cancellationToken);
		Data.SeasonalBundles = seasonalBundles ?? new TaylorCollection();
	}

	private static string DefaultKeySelector(string file) => file;
	
	private async Task LoadNeuItems(IRepoSnapshot snapshot, CancellationToken cancellationToken)
	{
		const string folderPath = "items";
		var neuItems = await LoadDataAsync<NeuItemData>(snapshot, folderPath, DefaultKeySelector, cancellationToken);
		
		_logger.LogInformation("Loaded {Count} NEU items", neuItems.Count);

		foreach (var (id, item) in neuItems)
		{
			if (item.ItemId != "minecraft:skull") continue;
			
			var sbRepoItem = Data.Items.GetValueOrDefault(id);
			if (sbRepoItem is null || sbRepoItem.Data?.Skin is not null) continue;
			
			var extractedSkin = SkyblockRepoRegexUtils.ExtractSkullTexture(item.NbtTag);
			if (extractedSkin is null) continue;
			
			sbRepoItem.Data ??= new SkyblockItemResponse();
			sbRepoItem.Data.Id ??= sbRepoItem.InternalId;
			sbRepoItem.Data.Skin = new ItemSkin()
			{
				Value = extractedSkin.Value,
				Signature = extractedSkin.Signature
			};
		}
		
		Data.NeuItems = neuItems;
	}
	
	private async Task LoadSkyblockEnchantments(IRepoSnapshot snapshot, CancellationToken cancellationToken)
	{
		const string folderPath = "enchantments";
		Data.Enchantments = await LoadDataAsync<SkyblockEnchantmentData>(snapshot, folderPath, DefaultKeySelector, cancellationToken);
	}

	private async Task LoadSkyblockNpcs(IRepoSnapshot snapshot, CancellationToken cancellationToken)
	{
		const string folderPath = "npcs";
		Data.Npcs = await LoadDataAsync<SkyblockNpcData>(snapshot, folderPath, DefaultKeySelector, cancellationToken);
	}

	private async Task LoadSkyblockShops(IRepoSnapshot snapshot, CancellationToken cancellationToken)
	{
		const string folderPath = "shops";
		Data.Shops = await LoadDataAsync<SkyblockShopData>(snapshot, folderPath, DefaultKeySelector, cancellationToken);
	}

	private async Task LoadSkyblockZones(IRepoSnapshot snapshot, CancellationToken cancellationToken)
	{
		const string folderPath = "zones";
		Data.Zones = await LoadDataAsync<SkyblockZoneData>(snapshot, folderPath, DefaultKeySelector, cancellationToken);
	}
	
	/// <summary>
	/// Loads and deserializes JSON files from a specified folder into a read-only dictionary.
	/// </summary>
	/// <typeparam name="TModel">The type of the model to deserialize the JSON into.</typeparam>
	/// <param name="folderPath">The path to the folder containing the .json files.</param>
	/// <param name="keySelector">A function that takes a file name (without extension) and returns the desired dictionary key.</param>
	/// <returns>A read-only dictionary of the loaded data.</returns>
	private async Task<ReadOnlyDictionary<string, TModel>> LoadDataAsync<TModel>(
		IRepoSnapshot snapshot,
		string folderPath,
		Func<string, string> keySelector,
		CancellationToken cancellationToken = default) where TModel : class
	{
	    var modelName = typeof(TModel).Name;
	    var files = snapshot.GetFiles(folderPath, "*.json");
	    if (files.Count == 0)
	    {
	        _logger.LogWarning("Directory for {ModelName} not found: {Path}", modelName, $"{snapshot.SourcePath}::{folderPath}");
	        return new ReadOnlyDictionary<string, TModel>(new Dictionary<string, TModel>());
	    }

	    var data = new System.Collections.Concurrent.ConcurrentDictionary<string, TModel>();

	    await Parallel.ForEachAsync(files,
	        new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2, CancellationToken = cancellationToken },
	        async (file, itemCancellationToken) =>
	        {
	            try
	            {
	                await using var stream = await snapshot.OpenReadAsync(file.RelativePath, itemCancellationToken);
	                if (stream is null)
	                {
		                return;
	                }

	                var model = await JsonSerializer.DeserializeAsync<TModel>(stream, _jsonSerializerOptions, itemCancellationToken);
	                
	                if (model is not null)
	                {
	                    var key = keySelector(file.NameWithoutExtension);
	                    data.TryAdd(key, model);
	                }
	            }
	            catch (Exception e)
	            {
	                _logger.LogError(e, "Error loading {ModelName} from {FilePath}!", modelName, file.RelativePath);
	            }
	        });

	    _logger.LogInformation("Loaded {Count} {ModelName} models successfully!", data.Count, modelName);
	    return new ReadOnlyDictionary<string, TModel>(data);
	}
	
	private async Task<TModel?> LoadFileAsync<TModel>(
		IRepoSnapshot snapshot,
		string filePath,
		CancellationToken cancellationToken = default) where TModel : class
	{
		if (!snapshot.FileExists(filePath))
		{
			_logger.LogWarning("File not found: {FilePath}", filePath);
			return null;
		}

		try
		{
			await using var stream = await snapshot.OpenReadAsync(filePath, cancellationToken);
			if (stream is null)
			{
				return null;
			}

			var model = await JsonSerializer.DeserializeAsync<TModel>(stream, _jsonSerializerOptions, cancellationToken);
			return model;
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Error loading file: {FilePath}", filePath);
			return null;
		}
	}

	private static bool RepoDataExists(RepoRuntime repo)
	{
		if (!RepoSnapshot.Exists(repo.Updater.RepoPath, repo.StorageMode, repo.ZipFileName))
		{
			return false;
		}

		if (repo.StorageMode == RepoStorageMode.ZipArchive || repo.ExtractedProbe is null)
		{
			return true;
		}

		var extractedPath = RepoSnapshot.GetFullPath(repo.Updater.RepoPath, repo.ExtractedProbe.RelativePath);
		return repo.ExtractedProbe.IsDirectory
			? Directory.Exists(extractedPath)
			: File.Exists(extractedPath);
	}

	private static string GetSourcePath(RepoRuntime repo)
	{
		return repo.StorageMode == RepoStorageMode.ZipArchive
			? Path.Combine(repo.Updater.RepoPath, repo.ZipFileName)
			: repo.Updater.RepoPath;
	}

	private sealed record RepoRuntime(
		IGithubRepoUpdater Updater,
		string RepoName,
		RepoStorageMode StorageMode,
		string ZipFileName,
		bool IsLocal,
		RepoExtractedProbe? ExtractedProbe);

	private sealed record RepoExtractedProbe(string RelativePath, bool IsDirectory);

	private IEnumerable<RepoRuntime> GetRepos()
	{
		yield return _skyblockRepo;

		if (_neuRepo is not null)
		{
			yield return _neuRepo;
		}
	}

	private Task<bool> CheckForUpdatesOrRecoverAsync(RepoRuntime repo, CancellationToken cancellationToken)
	{
		return RepoDataExists(repo)
			? repo.Updater.CheckForUpdatesAsync(cancellationToken)
			: DownloadMissingRepoAsync(repo, cancellationToken);
	}

	private Task<bool> DownloadMissingRepoAsync(RepoRuntime repo, CancellationToken cancellationToken)
	{
		if (repo.IsLocal)
		{
			_logger.LogWarning("[{RepoName}] Local repo data source not found: {SourcePath}", repo.RepoName, GetSourcePath(repo));
			return Task.FromResult(false);
		}

		_logger.LogInformation("[{RepoName}] Repo data not found locally. Forcing download...", repo.RepoName);
		return repo.Updater.ForceDownloadAsync(cancellationToken);
	}
}
