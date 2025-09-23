using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SkyblockRepo;

public record GithubRepoOptions(
	string RepoName,
	string FileStoragePath,
	string ApiEndpoint,
	string ZipDownloadUrl,
	string? LocalRepoPath = null
);

public interface IGithubRepoUpdater
{
	string RepoPath { get; }
	Task<bool> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
}

public class GithubRepoUpdater : IGithubRepoUpdater
{
    private readonly GithubRepoOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GithubRepoUpdater> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly string _metaFilePath;
    
    /// <summary>
    /// Constructor for Dependency Injection users.
    /// </summary>
    public GithubRepoUpdater(
        GithubRepoOptions options,
        IHttpClientFactory httpClientFactory,
        ILogger<GithubRepoUpdater>? logger = null)
    {
        _options = options;
        _httpClient = httpClientFactory.CreateClient("RepoUpdater");
        _logger = logger ?? NullLogger<GithubRepoUpdater>.Instance;
        _metaFilePath = Path.Combine(_options.FileStoragePath, $"{_options.RepoName}-meta.json");
    }

    /// <summary>
    /// Constructor for non-DI users who will manage their own HttpClient instance.
    /// </summary>
    public GithubRepoUpdater(
        GithubRepoOptions options,
        HttpClient httpClient,
        ILogger<GithubRepoUpdater>? logger = null)
    {
        _options = options;
        _httpClient = httpClient;
        _logger = logger ?? NullLogger<GithubRepoUpdater>.Instance;
        _metaFilePath = Path.Combine(_options.FileStoragePath, $"{_options.RepoName}-meta.json");
    }

	public bool IsUsingLocalRepo => _options.LocalRepoPath is not null;
	public string RepoPath => _options.LocalRepoPath ?? Path.Combine(_options.FileStoragePath, $"data-{_options.RepoName}");

	/// <summary>
    /// Checks for repository updates and downloads them if available.
    /// </summary>
    /// <returns>True if an update was downloaded, otherwise false.</returns>
    public async Task<bool> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        if (IsUsingLocalRepo || cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        _logger.LogInformation("[{RepoName}] Checking for updates...", _options.RepoName);

        var existingMeta = await GetLastDownloadMeta();

        var request = new HttpRequestMessage(HttpMethod.Get, _options.ApiEndpoint);
        if (existingMeta?.ETag is not null)
        {
            request.Headers.IfNoneMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue(existingMeta.ETag));
        }

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("RepoUpdater");
        var response = await _httpClient.SendAsync(request, cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotModified || response.Headers.ETag?.Tag == existingMeta?.ETag)
        {
            _logger.LogInformation("[{RepoName}] No updates found.", _options.RepoName);
            return false;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("[{RepoName}] Failed to fetch last update from {Endpoint}! Status: {StatusCode}",
                _options.RepoName, _options.ApiEndpoint, response.StatusCode);
            return false;
        }

        var etag = response.Headers.ETag?.Tag;
        if (string.IsNullOrWhiteSpace(etag))
        {
            _logger.LogWarning("[{RepoName}] API endpoint did not provide an ETag. Cannot reliably check for updates.", _options.RepoName);
            return false;
        }

        _logger.LogInformation("[{RepoName}] New version found! Downloading...", _options.RepoName);
        await DownloadRepoAsync(etag, cancellationToken);
        return true;
    }

    private async Task DownloadRepoAsync(string etag, CancellationToken cancellationToken)
    {
        var tempPath = Path.Combine(_options.FileStoragePath, $"temp-{_options.RepoName}");
        if (Directory.Exists(tempPath))
        {
            Directory.Delete(tempPath, true);
        }

        try
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("RepoUpdater");
            await using var stream = await _httpClient.GetStreamAsync(_options.ZipDownloadUrl, cancellationToken);
            using var archive = new ZipArchive(stream);
            archive.ExtractToDirectory(tempPath, true);

            var rootFolder = Directory.GetDirectories(tempPath).FirstOrDefault();
            if (rootFolder is null)
            {
                _logger.LogError("[{RepoName}] Invalid zip structure! No root folder found.", _options.RepoName);
                return;
            }

            if (Directory.Exists(RepoPath))
            {
                Directory.Delete(RepoPath, true);
            }

            Directory.Move(rootFolder, RepoPath);

            await SaveDownloadMeta(new DownloadMeta(DateTimeOffset.Now, etag));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[{RepoName}] Error downloading or extracting repo.", _options.RepoName);
        }
        finally
        {
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }
        }
    }

    private async Task<DownloadMeta?> GetLastDownloadMeta()
    {
        if (!File.Exists(_metaFilePath)) return null;

        try
        {
            await using var stream = File.OpenRead(_metaFilePath);
            return await JsonSerializer.DeserializeAsync<DownloadMeta>(stream, _jsonSerializerOptions);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[{RepoName}] Error loading download meta.", _options.RepoName);
            return null;
        }
    }

    private async Task SaveDownloadMeta(DownloadMeta meta)
    {
        try
        {
            var tempFile = _metaFilePath + ".tmp";
            await using var stream = File.Create(tempFile);
            await JsonSerializer.SerializeAsync(stream, meta, _jsonSerializerOptions);
            await stream.FlushAsync();
            stream.Close();

            File.Move(tempFile, _metaFilePath, true);
            _logger.LogInformation("[{RepoName}] Saved download meta successfully!", _options.RepoName);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[{RepoName}] Error saving download meta.", _options.RepoName);
        }
    }

    private record DownloadMeta(DateTimeOffset LastUpdated, string ETag);
}