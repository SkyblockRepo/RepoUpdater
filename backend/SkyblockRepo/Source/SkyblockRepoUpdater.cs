using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SkyblockRepo.Models;

namespace SkyblockRepo;

public interface ISkyblockRepoUpdater
{
	Task InitializeAsync(CancellationToken cancellationToken = default);
	Task CheckForUpdatesAsync(CancellationToken cancellationToken = default);
	Task ReloadRepoAsync(CancellationToken cancellationToken = default);
}

public class SkyblockRepoUpdater(SkyblockRepoConfiguration configuration, ILogger<SkyblockRepoUpdater> logger) : ISkyblockRepoUpdater
{
	public static SkyblockRepoData Data { get; set; } = new();
	public static Manifest? Manifest { get => Data.Manifest; set => Data.Manifest = value; }

	private readonly JsonSerializerOptions? _jsonSerializerOptions = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};
	
	public bool UsingLocalRepo => configuration.LocalRepoPath is not null;
	public string RepoPath => configuration.LocalRepoPath ?? Path.Combine(configuration.FileStoragePath, "data-skyblockrepo");
	
	public async Task InitializeAsync(CancellationToken cancellationToken = default)
	{
		if (configuration.LocalRepoPath is not null) {
			await LoadLocalRepo();
		} else {
			await CheckForUpdatesAsync(cancellationToken);
		}

		await ReloadRepoAsync(cancellationToken);
	}

	public async Task ReloadRepoAsync(CancellationToken cancellationToken = default)
	{
		await LoadSkyblockItems(RepoPath);
		await LoadSkyblockPets(RepoPath);
	}

	public async Task CheckForUpdatesAsync(CancellationToken cancellationToken = default)
	{
		if (UsingLocalRepo || cancellationToken.IsCancellationRequested) return;

		logger.LogInformation("Checking for updates...");

		var existingMeta = await GetLastDownloadMeta();

		var httpClient = new HttpClient();
		httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SkyblockRepo");
		if (existingMeta?.ETag is not null) {
			httpClient.DefaultRequestHeaders.TryAddWithoutValidation("etag", existingMeta.ETag);
		}

		var response = await httpClient.GetAsync(configuration.SkyblockRepoApiEndpoint, cancellationToken);
		
		if (response.StatusCode == System.Net.HttpStatusCode.NotModified ||
		    (response.Headers.ETag is not null && response.Headers.ETag.ToString() == existingMeta?.ETag))
		{
			await SaveDownloadMeta(new DownloadMeta()
			{
				LastUpdated = DateTimeOffset.Now,
				ETag = response.Headers.ETag?.ToString() ?? existingMeta?.ETag,
				Version = existingMeta?.Version ?? Data.Manifest?.Version ?? 1,
			});
			logger.LogInformation("No updates found!");
			return;
		}
		
		if (!response.IsSuccessStatusCode)
		{
			logger.LogError("Failed to fetch last update from {Endpoint}! Status: {StatusCode}",
				configuration.SkyblockRepoApiEndpoint, response.StatusCode);
			return;
		}
		
		logger.LogInformation("Updates found! Downloading new repo version...");
		await DownloadRepoAsync(response.Headers.ETag?.ToString() ?? string.Empty, cancellationToken);
	}
	
	private async Task DownloadRepoAsync(string etag, CancellationToken cancellationToken = default)
	{
		var tempPath = Path.Combine(configuration.FileStoragePath, "temp-skyblockrepo");
		if (Directory.Exists(tempPath))
		{
			Directory.Delete(tempPath, true);
		}

		var downloadPath = configuration.SkyblockRepoZipPath.StartsWith("http", StringComparison.OrdinalIgnoreCase)
			? configuration.SkyblockRepoZipPath
			: configuration.SkyblockRepoUrl + configuration.SkyblockRepoZipPath;
		
		var httpClient = new HttpClient();
		httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SkyblockRepo");
		
		var response = await httpClient.GetAsync(downloadPath, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			logger.LogError("Failed to download repo zip from {DownloadPath}! Status: {StatusCode}",
				downloadPath, response.StatusCode);
			return;
		}

		try
		{
			await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
			using var archive = new ZipArchive(stream);
			archive.ExtractToDirectory(tempPath, true);
			
			// The zip contains a root folder named Repo-main or similar, get that folder
			var rootFolder = Directory.GetDirectories(tempPath).FirstOrDefault();
			if (rootFolder is null)
			{
				logger.LogError("Invalid zip structure! No root folder found.");
				if (Directory.Exists(tempPath))
				{
					Directory.Delete(tempPath, true);
				}
				return;
			}
			
			if (Directory.Exists(RepoPath))
			{
				Directory.Delete(RepoPath, true);
			}
			
			Directory.Move(rootFolder, RepoPath);
			Directory.Delete(tempPath, true);
			
			await SaveDownloadMeta(new DownloadMeta()
			{
				LastUpdated = DateTimeOffset.Now,
				ETag = etag,
				Version = (Manifest?.Version ?? 1) + 1,
			});
			
			await LoadManifest(RepoPath);
		}
		catch (Exception e)
		{
			logger.LogError(e, "Error cloning repo!");
			if (Directory.Exists(tempPath))
			{
				Directory.Delete(tempPath, true);
			}
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

	private async Task<DownloadMeta?> GetLastDownloadMeta()
	{
		var metaPath = Path.Combine(configuration.FileStoragePath, "meta.json");
		if (!File.Exists(metaPath)) return null;

		try
		{
			await using var stream = File.OpenRead(metaPath);
			return await JsonSerializer.DeserializeAsync<DownloadMeta>(stream, _jsonSerializerOptions);
		} catch (Exception e)
		{
			logger.LogError(e, "Error loading download meta!");
			return null;
		}
	}
	
	private async Task SaveDownloadMeta(DownloadMeta meta)
	{
		var metaPath = Path.Combine(configuration.FileStoragePath, "meta.json");
		try
		{
			await using var stream = File.Create(metaPath + ".tmp");
			await JsonSerializer.SerializeAsync(stream, meta, _jsonSerializerOptions);
			await stream.FlushAsync();
			stream.Close();
			
			File.Move(metaPath + ".tmp", metaPath, true);
			logger.LogInformation("Saved download meta successfully!");
		} catch (Exception e)
		{
			logger.LogError(e, "Error saving download meta!");
		}
	}
}