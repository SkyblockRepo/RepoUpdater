using Shouldly;
using Xunit;

namespace SkyblockRepo.Tests;

[Collection("SkyblockRepoState")]
public class StaticMetadataTests
{
	[Fact]
	public async Task InitializeAsync_LoadsExtendedMetadataFromLocalNeuAndCollectionsSources()
	{
		using var repoRoot = new TempDirectory();
		using var neuRoot = new TempDirectory();
		using var collectionsRoot = new TempDirectory();

		TestRepoFixture.WriteExtendedRepoContents(repoRoot.Path);
		TestRepoFixture.WriteNeuRepoContents(neuRoot.Path);
		TestRepoFixture.WriteCollectionsPayload(Path.Combine(collectionsRoot.Path, "collections.json"));

		ResetRepoState();
		var config = CreateExtendedConfig(repoRoot.Path, neuRoot.Path, collectionsRoot.Path);
		var client = new SkyblockRepoClient(new SkyblockRepoUpdater(config), config);

		await client.InitializeAsync();

		SkyblockRepoClient.Data.Bestiary.GetMob("brood_mother")?.Name.ShouldBe("Brood Mother");
		SkyblockRepoClient.Data.Accessories.GetAccessory("RIFT_PRISM").ShouldNotBeNull();
		SkyblockRepoClient.Data.Accessories.ByItemId["HEGEMONY_ARTIFACT"].MagicalPowerMultiplier.ShouldBe(2d);
		SkyblockRepoClient.Data.Accessories.EnrichmentKeyToStatName["defense"].ShouldBe("Defense");
		SkyblockRepoClient.Data.AttributeShards.GetByShardId("speed")?.AbilityName.ShouldBe("Fleet");
		SkyblockRepoClient.Data.Garden.Visitors["guide_visitor"].Name.ShouldBe("Guide Visitor");
		SkyblockRepoClient.Data.Garden.Mutations["CHOCOBERRY"].Analyzable.ShouldBeTrue();
		SkyblockRepoClient.Data.Fishing.GetTrophyFish("BLOBFISH")?.ThresholdsByTier["bronze"].ShouldBe(1);
		SkyblockRepoClient.Data.Fishing.DolphinBrackets.Count.ShouldBe(3);
		SkyblockRepoClient.Data.Collections.GetEntry("INK_SACK:3")?.Thresholds[0].ShouldBe(75);
		SkyblockRepoClient.Data.Collections.BossCollections.ShouldBeEmpty();
		SkyblockRepoClient.Data.Minions.ByGeneratorId["WHEAT_GENERATOR"].Name.ShouldBe("Wheat Minion");
		SkyblockRepoClient.Data.PetCatalog.ByPetId["DROPLET_WISP"].ParentId.ShouldBe("SUBZERO_WISP");
		SkyblockRepoClient.Data.Rift.Areas["castle"].Tasks.Count.ShouldBe(3);
		SkyblockRepoClient.Data.Rift.Timecharms["wyldly_supreme"].Name.ShouldBe("Supreme Timecharm");
		SkyblockRepoClient.Data.Rift.Eyes["rift_eye_1"].Name.ShouldBe("First Eye");
		SkyblockRepoClient.Data.Rift.EnigmaSoulCount.ShouldBe(1);
		SkyblockRepoClient.Data.Rift.MaxGrubberStacks.ShouldBe(5);
		SkyblockRepoClient.Data.EssencePerks.Categories["ESSENCE_UNDEAD"].Perks[0].MaxLevel.ShouldBe(2);
		SkyblockRepoClient.Data.Gear.Farming.AllIds.ShouldBeEmpty();
	}

	[Fact]
	public async Task RefreshCollectionsAsync_ReloadsCollectionsFromLocalOverride()
	{
		using var repoRoot = new TempDirectory();
		using var neuRoot = new TempDirectory();
		using var collectionsRoot = new TempDirectory();
		var collectionsFilePath = Path.Combine(collectionsRoot.Path, "collections.json");

		TestRepoFixture.WriteExtendedRepoContents(repoRoot.Path);
		TestRepoFixture.WriteNeuRepoContents(neuRoot.Path);
		TestRepoFixture.WriteCollectionsPayload(collectionsFilePath, lastUpdated: 1710000000000, firstThreshold: 75, secondThreshold: 200);

		ResetRepoState();
		var config = CreateExtendedConfig(repoRoot.Path, neuRoot.Path, collectionsRoot.Path);
		var client = new SkyblockRepoClient(new SkyblockRepoUpdater(config), config);

		await client.InitializeAsync();
		SkyblockRepoClient.Data.Collections.GetEntry("INK_SACK:3")?.Thresholds[0].ShouldBe(75);

		TestRepoFixture.WriteCollectionsPayload(collectionsFilePath, lastUpdated: 1710000000500, firstThreshold: 500, secondThreshold: 900);
		await client.RefreshCollectionsAsync();

		SkyblockRepoClient.Data.Collections.GetEntry("INK_SACK:3")?.Thresholds[0].ShouldBe(500);
		SkyblockRepoClient.Data.Collections.LastUpdated.ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(1710000000500));
	}

	[Fact]
	public async Task InitializeAsync_WhenUseNeuRepoDisabled_KeepsExtendedMetadataEmpty()
	{
		using var repoRoot = new TempDirectory();
		TestRepoFixture.WriteRepoContents(repoRoot.Path);

		ResetRepoState();
		var config = new SkyblockRepoConfiguration
		{
			UseNeuRepo = false,
			SkyblockRepo =
			{
				LocalPath = repoRoot.Path,
				StorageMode = RepoStorageMode.ExtractedDirectory
			}
		};

		var client = new SkyblockRepoClient(new SkyblockRepoUpdater(config), config);
		await client.InitializeAsync();

		SkyblockRepoClient.Data.Bestiary.ByBestiaryId.ShouldBeEmpty();
		SkyblockRepoClient.Data.Collections.ByItemId.ShouldBeEmpty();
		SkyblockRepoClient.Data.PetCatalog.ByPetId.ShouldBeEmpty();
		SkyblockRepoClient.Instance.FindItem("Brown Mushroom")?.InternalId.ShouldBe("BROWN_MUSHROOM");
	}

	private static SkyblockRepoConfiguration CreateExtendedConfig(string repoRoot, string neuRoot, string collectionsRoot)
	{
		return new SkyblockRepoConfiguration
		{
			UseNeuRepo = true,
			SkyblockRepo =
			{
				LocalPath = repoRoot,
				StorageMode = RepoStorageMode.ExtractedDirectory
			},
			NeuRepo =
			{
				LocalPath = neuRoot,
				StorageMode = RepoStorageMode.ExtractedDirectory
			},
			Collections =
			{
				LocalPath = collectionsRoot
			}
		};
	}

	private static void ResetRepoState()
	{
		SkyblockRepoUpdater.Data = new SkyblockRepoData();
		SkyblockRepoUpdater.Manifest = null;
	}
}
