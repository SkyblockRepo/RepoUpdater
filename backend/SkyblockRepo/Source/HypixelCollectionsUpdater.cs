using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SkyblockRepo.StaticData;

namespace SkyblockRepo;

internal sealed class HypixelCollectionsUpdater
{
	private readonly HypixelCollectionsSettings _settings;
	private readonly HttpClient _httpClient;
	private readonly ILogger<HypixelCollectionsUpdater> _logger;
	private readonly JsonSerializerOptions _serializerOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};
	private readonly string _payloadPath;
	private readonly string _metaFilePath;

	public HypixelCollectionsUpdater(
		HypixelCollectionsSettings settings,
		string fileStoragePath,
		IHttpClientFactory httpClientFactory,
		ILogger<HypixelCollectionsUpdater>? logger = null)
		: this(settings, fileStoragePath, httpClientFactory.CreateClient(GithubRepoUpdater.HttpClientName), logger)
	{
	}

	public HypixelCollectionsUpdater(
		HypixelCollectionsSettings settings,
		string fileStoragePath,
		HttpClient httpClient,
		ILogger<HypixelCollectionsUpdater>? logger = null)
	{
		_settings = settings;
		_httpClient = httpClient;
		_logger = logger ?? NullLogger<HypixelCollectionsUpdater>.Instance;
		EnsureDefaultUserAgent(_httpClient);

		var storageRoot = settings.LocalPath is null
			? Path.Combine(fileStoragePath, $"data-{settings.Name}")
			: ResolveLocalPayloadPath(settings.LocalPath, settings.CacheFileName);

		_payloadPath = settings.LocalPath is null
			? Path.Combine(storageRoot, settings.CacheFileName)
			: storageRoot;
		_metaFilePath = Path.Combine(fileStoragePath, $"{settings.Name}-meta.json");
	}

	public bool IsLocal => _settings.LocalPath is not null;
	public string PayloadPath => _payloadPath;

	public bool DataExists()
	{
		return File.Exists(_payloadPath);
	}

	public async Task<HypixelCollectionsApiResponse?> LoadAsync(CancellationToken cancellationToken = default)
	{
		if (!File.Exists(_payloadPath))
		{
			return null;
		}

		try
		{
			await using var stream = File.OpenRead(_payloadPath);
			return await JsonSerializer.DeserializeAsync<HypixelCollectionsApiResponse>(stream, _serializerOptions, cancellationToken);
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "Failed to load cached Hypixel collections payload from {PayloadPath}", _payloadPath);
			return null;
		}
	}

	public async Task<bool> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
	{
		if (IsLocal || cancellationToken.IsCancellationRequested)
		{
			return false;
		}

		var existingMeta = await LoadMetaAsync(cancellationToken);
		var download = await DownloadAsync(cancellationToken);
		if (download is null)
		{
			return false;
		}

		var metadata = download.Value.Metadata;
		if (existingMeta is not null &&
		    existingMeta.SourceLastUpdated == metadata.SourceLastUpdated &&
		    string.Equals(existingMeta.Version, metadata.Version, StringComparison.Ordinal) &&
		    string.Equals(existingMeta.ETag, metadata.ETag, StringComparison.Ordinal))
		{
			_logger.LogInformation("[{ResourceName}] No updates found.", _settings.Name);
			return false;
		}

		await PersistAsync(download.Value.Payload, metadata, cancellationToken);
		return true;
	}

	public async Task<bool> ForceDownloadAsync(CancellationToken cancellationToken = default)
	{
		if (IsLocal || cancellationToken.IsCancellationRequested)
		{
			return false;
		}

		var download = await DownloadAsync(cancellationToken);
		if (download is null)
		{
			return false;
		}

		await PersistAsync(download.Value.Payload, download.Value.Metadata, cancellationToken);
		return true;
	}

	private async Task<(string Payload, HypixelCollectionsDownloadMeta Metadata)?> DownloadAsync(CancellationToken cancellationToken)
	{
		try
		{
			using var response = await _httpClient.GetAsync(_settings.ApiEndpoint, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			if (!response.IsSuccessStatusCode)
			{
				_logger.LogError("[{ResourceName}] Failed to download collections from {Endpoint}. Status: {StatusCode}",
					_settings.Name,
					_settings.ApiEndpoint,
					response.StatusCode);
				return null;
			}

			var payload = await response.Content.ReadAsStringAsync(cancellationToken);
			if (string.IsNullOrWhiteSpace(payload))
			{
				_logger.LogWarning("[{ResourceName}] Hypixel collections endpoint returned an empty payload.", _settings.Name);
				return null;
			}

			var parsed = JsonSerializer.Deserialize<HypixelCollectionsApiResponse>(payload, _serializerOptions);
			if (parsed is null || !parsed.Success)
			{
				_logger.LogWarning("[{ResourceName}] Failed to parse a valid Hypixel collections payload from {Endpoint}.", _settings.Name, _settings.ApiEndpoint);
				return null;
			}

			return (
				payload,
				new HypixelCollectionsDownloadMeta(
					DownloadedAt: DateTimeOffset.UtcNow,
					SourceLastUpdated: parsed.GetLastUpdatedAt(),
					Version: parsed.Version,
					ETag: response.Headers.ETag?.Tag));
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "[{ResourceName}] Failed to refresh collections from {Endpoint}", _settings.Name, _settings.ApiEndpoint);
			return null;
		}
	}

	private async Task PersistAsync(string payload, HypixelCollectionsDownloadMeta metadata, CancellationToken cancellationToken)
	{
		var payloadDirectory = Path.GetDirectoryName(_payloadPath);
		if (!string.IsNullOrWhiteSpace(payloadDirectory))
		{
			Directory.CreateDirectory(payloadDirectory);
		}

		var tempPayloadPath = $"{_payloadPath}.{Guid.NewGuid():N}.tmp";
		await File.WriteAllTextAsync(tempPayloadPath, payload, cancellationToken);
		File.Move(tempPayloadPath, _payloadPath, true);

		await SaveMetaAsync(metadata, cancellationToken);
		_logger.LogInformation("[{ResourceName}] Cached Hypixel collections at {PayloadPath}", _settings.Name, _payloadPath);
	}

	private async Task<HypixelCollectionsDownloadMeta?> LoadMetaAsync(CancellationToken cancellationToken)
	{
		if (!File.Exists(_metaFilePath))
		{
			return null;
		}

		try
		{
			await using var stream = File.OpenRead(_metaFilePath);
			return await JsonSerializer.DeserializeAsync<HypixelCollectionsDownloadMeta>(stream, _serializerOptions, cancellationToken);
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "[{ResourceName}] Failed to read collections metadata from {MetaPath}", _settings.Name, _metaFilePath);
			return null;
		}
	}

	private async Task SaveMetaAsync(HypixelCollectionsDownloadMeta meta, CancellationToken cancellationToken)
	{
		try
		{
			var tempFile = $"{_metaFilePath}.{Guid.NewGuid():N}.tmp";
			var metaDirectory = Path.GetDirectoryName(_metaFilePath);
			if (!string.IsNullOrWhiteSpace(metaDirectory))
			{
				Directory.CreateDirectory(metaDirectory);
			}

			await using (var stream = File.Create(tempFile))
			{
				await JsonSerializer.SerializeAsync(stream, meta, _serializerOptions, cancellationToken);
				await stream.FlushAsync(cancellationToken);
			}

			File.Move(tempFile, _metaFilePath, true);
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "[{ResourceName}] Failed to save collections metadata to {MetaPath}", _settings.Name, _metaFilePath);
		}
	}

	private static string ResolveLocalPayloadPath(string localPath, string cacheFileName)
	{
		if (File.Exists(localPath))
		{
			return localPath;
		}

		return Path.Combine(localPath, cacheFileName);
	}

	private static void EnsureDefaultUserAgent(HttpClient httpClient)
	{
		if (httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
		{
			httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(GithubRepoUpdater.DefaultUserAgent);
		}
	}

	private sealed record HypixelCollectionsDownloadMeta(
		DateTimeOffset DownloadedAt,
		DateTimeOffset? SourceLastUpdated,
		string? Version,
		string? ETag);
}
