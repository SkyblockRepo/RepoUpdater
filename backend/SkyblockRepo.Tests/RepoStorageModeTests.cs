using System.Net;
using System.Text;
using Shouldly;

namespace SkyblockRepo.Tests;

[Collection("SkyblockRepoState")]
public class RepoStorageModeTests
{
	[Fact]
	public async Task InitializeFromLocalZipFolder()
	{
		using var sourceRepo = new TempDirectory();
		using var localPath = new TempDirectory();
		TestRepoFixture.WriteRepoContents(sourceRepo.Path);
		TestRepoFixture.CreateGithubZip(sourceRepo.Path, Path.Combine(localPath.Path, "skyblockrepo.zip"));

		ResetRepoState();

		var config = new SkyblockRepoConfiguration
		{
			UseNeuRepo = false,
			SkyblockRepo =
			{
				LocalPath = localPath.Path,
				StorageMode = RepoStorageMode.ZipArchive
			}
		};

		var client = new SkyblockRepoClient(new SkyblockRepoUpdater(config), config);
		await client.InitializeAsync();

		SkyblockRepoClient.Data.Items.Count.ShouldBe(1);
		SkyblockRepoClient.Data.Pets.Count.ShouldBe(1);
		SkyblockRepoClient.Data.TaylorCollection.Items.Count.ShouldBe(1);
		SkyblockRepoClient.Instance.FindItem("Brown Mushroom")?.InternalId.ShouldBe("BROWN_MUSHROOM");
	}

	[Fact]
	public async Task ReloadRepo_LoadsEquivalentDataFromExtractedAndZipSources()
	{
		using var extractedRepo = new TempDirectory();
		using var zipContainer = new TempDirectory();
		TestRepoFixture.WriteRepoContents(extractedRepo.Path);
		TestRepoFixture.CreateGithubZip(extractedRepo.Path, Path.Combine(zipContainer.Path, "skyblockrepo.zip"));

		ResetRepoState();
		var extractedConfig = new SkyblockRepoConfiguration
		{
			UseNeuRepo = false,
			SkyblockRepo =
			{
				LocalPath = extractedRepo.Path,
				StorageMode = RepoStorageMode.ExtractedDirectory
			}
		};

		var extractedClient = new SkyblockRepoClient(new SkyblockRepoUpdater(extractedConfig), extractedConfig);
		await extractedClient.InitializeAsync();
		var extractedSummary = CaptureSummary();

		ResetRepoState();
		var zipConfig = new SkyblockRepoConfiguration
		{
			UseNeuRepo = false,
			SkyblockRepo =
			{
				LocalPath = zipContainer.Path,
				StorageMode = RepoStorageMode.ZipArchive
			}
		};

		var zipClient = new SkyblockRepoClient(new SkyblockRepoUpdater(zipConfig), zipConfig);
		await zipClient.InitializeAsync();
		var zipSummary = CaptureSummary();

		zipSummary.ShouldBe(extractedSummary);
	}

	[Fact]
	public async Task ReloadRepo_UsesConfiguredStorageModeWhenZipAndExtractedDataBothExist()
	{
		using var localPath = new TempDirectory();
		using var zipSource = new TempDirectory();
		TestRepoFixture.WriteRepoContents(localPath.Path, "Extracted Brown Mushroom");
		TestRepoFixture.WriteRepoContents(zipSource.Path, "Zip Brown Mushroom");
		TestRepoFixture.CreateGithubZip(zipSource.Path, Path.Combine(localPath.Path, "skyblockrepo.zip"));

		ResetRepoState();
		var extractedConfig = new SkyblockRepoConfiguration
		{
			UseNeuRepo = false,
			SkyblockRepo =
			{
				LocalPath = localPath.Path,
				StorageMode = RepoStorageMode.ExtractedDirectory
			}
		};

		var extractedClient = new SkyblockRepoClient(new SkyblockRepoUpdater(extractedConfig), extractedConfig);
		await extractedClient.InitializeAsync();
		SkyblockRepoClient.Instance.FindItem("BROWN_MUSHROOM")?.Name.ShouldBe("Extracted Brown Mushroom");

		ResetRepoState();
		var zipConfig = new SkyblockRepoConfiguration
		{
			UseNeuRepo = false,
			SkyblockRepo =
			{
				LocalPath = localPath.Path,
				StorageMode = RepoStorageMode.ZipArchive
			}
		};

		var zipClient = new SkyblockRepoClient(new SkyblockRepoUpdater(zipConfig), zipConfig);
		await zipClient.InitializeAsync();
		SkyblockRepoClient.Instance.FindItem("BROWN_MUSHROOM")?.Name.ShouldBe("Zip Brown Mushroom");
	}

	[Fact]
	public async Task InitializeFromExtractedMode_ForceDownloadsWhenOnlyZipCacheExists()
	{
		using var storagePath = new TempDirectory();
		using var sourceRepo = new TempDirectory();
		TestRepoFixture.WriteRepoContents(sourceRepo.Path);

		var cachedRepoPath = Path.Combine(storagePath.Path, "data-skyblockrepo");
		TestRepoFixture.CreateGithubZip(sourceRepo.Path, Path.Combine(cachedRepoPath, "skyblockrepo.zip"));
		var zipBytes = await File.ReadAllBytesAsync(Path.Combine(cachedRepoPath, "skyblockrepo.zip"));
		var requestedUris = new List<string>();

		var client = new HttpClient(new RouteHttpMessageHandler(request =>
		{
			requestedUris.Add(request.RequestUri?.AbsoluteUri ?? string.Empty);

			if (request.RequestUri?.AbsoluteUri == "https://github.com/SkyblockRepo/Repo/archive/refs/heads/main.zip")
			{
				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new ByteArrayContent(zipBytes)
				};
			}

			return new HttpResponseMessage(HttpStatusCode.NotFound)
			{
				Content = new StringContent("unexpected request", Encoding.UTF8, "text/plain")
			};
		}));

		ResetRepoState();
		var config = new SkyblockRepoConfiguration
		{
			FileStoragePath = storagePath.Path,
			UseNeuRepo = false
		};
		config.SkyblockRepo.StorageMode = RepoStorageMode.ExtractedDirectory;

		var repo = new SkyblockRepoClient(new SkyblockRepoUpdater(config, client), config);
		await repo.InitializeAsync();

		requestedUris.ShouldBe(["https://github.com/SkyblockRepo/Repo/archive/refs/heads/main.zip"]);
		File.Exists(Path.Combine(cachedRepoPath, "manifest.json")).ShouldBeTrue();
		SkyblockRepoClient.Data.Items.Count.ShouldBe(1);
	}

	private static void ResetRepoState()
	{
		SkyblockRepoUpdater.Data = new SkyblockRepoData();
		SkyblockRepoUpdater.Manifest = null;
	}

	private static (int Items, int Pets, int Enchantments, int Npcs, int Shops, int Zones, int Taylor, int Seasonal, string MushroomName) CaptureSummary()
	{
		return (
			SkyblockRepoClient.Data.Items.Count,
			SkyblockRepoClient.Data.Pets.Count,
			SkyblockRepoClient.Data.Enchantments.Count,
			SkyblockRepoClient.Data.Npcs.Count,
			SkyblockRepoClient.Data.Shops.Count,
			SkyblockRepoClient.Data.Zones.Count,
			SkyblockRepoClient.Data.TaylorCollection.Items.Count,
			SkyblockRepoClient.Data.SeasonalBundles.Items.Count,
			SkyblockRepoClient.Data.Items["BROWN_MUSHROOM"].Name
		);
	}

	private sealed class RouteHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			return Task.FromResult(responder(request));
		}
	}
}
