using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace SkyblockRepo.Tests;

public class GithubRepoUpdaterHttpClientTests
{
    [Fact]
    public void AddSkyblockRepo_AllowsCustomHttpClientConfiguration()
    {
        var services = new ServiceCollection();

        services.AddSkyblockRepo(
            new SkyblockRepoConfiguration(),
            builder => builder.ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(5);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("CustomSkyblockRepoClient/1.0");
            }));

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient(GithubRepoUpdater.HttpClientName);

        client.Timeout.ShouldBe(TimeSpan.FromSeconds(5));
        client.DefaultRequestHeaders.UserAgent.ToString().ShouldContain("CustomSkyblockRepoClient/1.0");
    }

    [Fact]
    public void GithubRepoUpdater_AddsDefaultUserAgentWhenMissing()
    {
        var client = new HttpClient(new NoOpHttpMessageHandler());
        var updater = CreateUpdater(client);

        updater.ShouldNotBeNull();
        client.DefaultRequestHeaders.UserAgent.ToString().ShouldContain(GithubRepoUpdater.DefaultUserAgent);
    }

    [Fact]
    public void GithubRepoUpdater_DoesNotOverrideExistingUserAgent()
    {
        var client = new HttpClient(new NoOpHttpMessageHandler());
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MyCustomAgent/2.0");

        var updater = CreateUpdater(client);

        updater.ShouldNotBeNull();
        client.DefaultRequestHeaders.UserAgent.ToString().ShouldContain("MyCustomAgent/2.0");
        client.DefaultRequestHeaders.UserAgent.ToString().ShouldNotContain(GithubRepoUpdater.DefaultUserAgent);
    }

    [Theory]
    [InlineData(RepoStorageMode.ExtractedDirectory)]
    [InlineData(RepoStorageMode.ZipArchive)]
    public async Task GithubRepoUpdater_PersistsDownloadedRepoUsingConfiguredStorageMode(RepoStorageMode storageMode)
    {
        using var storagePath = new TempDirectory();
        using var sourceRepo = new TempDirectory();
        TestRepoFixture.WriteRepoContents(sourceRepo.Path);

        var zipBytes = TestRepoFixture.CreateGithubZipBytes(sourceRepo.Path);
        var client = new HttpClient(new RouteHttpMessageHandler(request =>
        {
            if (request.RequestUri?.AbsoluteUri == "https://example.com/api")
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
                response.Headers.ETag = new EntityTagHeaderValue("\"etag-1\"");
                return response;
            }

            if (request.RequestUri?.AbsoluteUri == "https://example.com/download.zip")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(zipBytes)
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }));

        var options = new GithubRepoOptions(
            RepoName: "skyblockrepo",
            FileStoragePath: storagePath.Path,
            ApiEndpoint: "https://example.com/api",
            ZipDownloadUrl: "https://example.com/download.zip")
        {
            StorageMode = storageMode,
            ZipFileName = "skyblockrepo-custom.zip"
        };

        var updater = new GithubRepoUpdater(options, client);
        var updated = await updater.CheckForUpdatesAsync();

        updated.ShouldBeTrue();
        Directory.Exists(updater.RepoPath).ShouldBeTrue();

        var zipPath = Path.Combine(updater.RepoPath, "skyblockrepo-custom.zip");
        var manifestPath = Path.Combine(updater.RepoPath, "manifest.json");

        if (storageMode == RepoStorageMode.ZipArchive)
        {
            File.Exists(zipPath).ShouldBeTrue();
            File.Exists(manifestPath).ShouldBeFalse();
        }
        else
        {
            File.Exists(manifestPath).ShouldBeTrue();
            File.Exists(zipPath).ShouldBeFalse();
        }

        var metaPath = Path.Combine(storagePath.Path, "skyblockrepo-meta.json");
        File.Exists(metaPath).ShouldBeTrue();

        using var metaDocument = JsonDocument.Parse(await File.ReadAllTextAsync(metaPath));
        metaDocument.RootElement.GetProperty("eTag").GetString().ShouldBe("\"etag-1\"");
    }

    [Fact]
    public async Task GithubRepoUpdater_ReturnsFalseWhenDownloadFails()
    {
        using var storagePath = new TempDirectory();

        var client = new HttpClient(new RouteHttpMessageHandler(request =>
        {
            if (request.RequestUri?.AbsoluteUri == "https://example.com/api")
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
                response.Headers.ETag = new EntityTagHeaderValue("\"etag-1\"");
                return response;
            }

            if (request.RequestUri?.AbsoluteUri == "https://example.com/download.zip")
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }));

        var options = new GithubRepoOptions(
            RepoName: "skyblockrepo",
            FileStoragePath: storagePath.Path,
            ApiEndpoint: "https://example.com/api",
            ZipDownloadUrl: "https://example.com/download.zip");

        var updater = new GithubRepoUpdater(options, client);
        var updated = await updater.CheckForUpdatesAsync();

        updated.ShouldBeFalse();
        Directory.Exists(updater.RepoPath).ShouldBeFalse();
        File.Exists(Path.Combine(storagePath.Path, "skyblockrepo-meta.json")).ShouldBeFalse();
    }

    [Fact]
    public async Task GithubRepoUpdater_TreatsNotModifiedAsNoUpdate()
    {
        using var storagePath = new TempDirectory();
        var metaPath = Path.Combine(storagePath.Path, "skyblockrepo-meta.json");
        await File.WriteAllTextAsync(metaPath, """
        {
          "lastUpdated": "2026-01-01T00:00:00+00:00",
          "eTag": "\"etag-1\""
        }
        """);

        var logger = new TestLogger<GithubRepoUpdater>();
        var requestUris = new List<string>();
        var client = new HttpClient(new RouteHttpMessageHandler(request =>
        {
            requestUris.Add(request.RequestUri?.AbsoluteUri ?? string.Empty);

            if (request.RequestUri?.AbsoluteUri == "https://example.com/api")
            {
                return new HttpResponseMessage(HttpStatusCode.NotModified);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }));

        var options = new GithubRepoOptions(
            RepoName: "skyblockrepo",
            FileStoragePath: storagePath.Path,
            ApiEndpoint: "https://example.com/api",
            ZipDownloadUrl: "https://example.com/download.zip");

        var updater = new GithubRepoUpdater(options, client, logger);
        var updated = await updater.CheckForUpdatesAsync();

        updated.ShouldBeFalse();
        requestUris.ShouldBe(["https://example.com/api"]);
        logger.LogLevels.ShouldNotContain(LogLevel.Error);
    }

    [Fact]
    public async Task GithubRepoUpdater_KeepsExistingExtractedFilesWhenSwitchingToZipMode()
    {
        using var storagePath = new TempDirectory();
        using var sourceRepo = new TempDirectory();
        TestRepoFixture.WriteRepoContents(sourceRepo.Path);

        var zipBytes = TestRepoFixture.CreateGithubZipBytes(sourceRepo.Path);
        var client = new HttpClient(new RouteHttpMessageHandler(request =>
        {
            if (request.RequestUri?.AbsoluteUri == "https://example.com/api")
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
                response.Headers.ETag = new EntityTagHeaderValue("\"etag-zip\"");
                return response;
            }

            if (request.RequestUri?.AbsoluteUri == "https://example.com/download.zip")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(zipBytes)
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }));

        var existingRepoPath = Path.Combine(storagePath.Path, "data-skyblockrepo");
        Directory.CreateDirectory(existingRepoPath);
        await File.WriteAllTextAsync(Path.Combine(existingRepoPath, "manifest.json"), "{ }");
        await File.WriteAllTextAsync(Path.Combine(existingRepoPath, "legacy-marker.txt"), "keep me");

        var options = new GithubRepoOptions(
            RepoName: "skyblockrepo",
            FileStoragePath: storagePath.Path,
            ApiEndpoint: "https://example.com/api",
            ZipDownloadUrl: "https://example.com/download.zip")
        {
            StorageMode = RepoStorageMode.ZipArchive,
            ZipFileName = "skyblockrepo.zip"
        };

        var updater = new GithubRepoUpdater(options, client);
        var updated = await updater.CheckForUpdatesAsync();

        updated.ShouldBeTrue();
        File.Exists(Path.Combine(existingRepoPath, "legacy-marker.txt")).ShouldBeTrue();
        File.Exists(Path.Combine(existingRepoPath, "manifest.json")).ShouldBeTrue();
        File.Exists(Path.Combine(existingRepoPath, "skyblockrepo.zip")).ShouldBeTrue();
    }

    private static GithubRepoUpdater CreateUpdater(HttpClient client)
    {
        var options = new GithubRepoOptions(
            RepoName: "skyblockrepo",
            FileStoragePath: Path.GetTempPath(),
            ApiEndpoint: "https://example.com/api",
            ZipDownloadUrl: "https://example.com/download.zip");

        return new GithubRepoUpdater(options, client);
    }

    private sealed class NoOpHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class RouteHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responder(request));
        }
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogLevel> LogLevels { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            LogLevels.Add(logLevel);
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}

