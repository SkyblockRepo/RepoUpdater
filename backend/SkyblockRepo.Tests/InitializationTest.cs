using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace SkyblockRepo.Tests;

[Collection("SkyblockRepoState")]
public class SkyblockRepoInitializeTests
{
	[Fact]
	public async Task InitializeFromLocalFolder()
	{
		var logger = Substitute.For<ILogger<SkyblockRepoUpdater>>();
		var config = new SkyblockRepoConfiguration
		{
			UseNeuRepo = false,
			SkyblockRepo = {
				LocalPath = Path.Join(SkyblockRepoUtils.GetSolutionPath(), "..", "output")
			}
		};
		var updater = new SkyblockRepoUpdater(config);
		var repo = new SkyblockRepoClient(updater, config);
		
		await repo.InitializeAsync();

		SkyblockRepoClient.Data.Items.Count.ShouldBeGreaterThan(5000);
		SkyblockRepoClient.Data.Pets.Count.ShouldBe(79);
		
		SkyblockRepoClient.Data.TaylorCollection.Items.Count.ShouldBeGreaterThan(1);
		SkyblockRepoClient.Data.SeasonalBundles.Items.Count.ShouldBeGreaterThan(1);
		
		SkyblockRepoClient.Instance.FindItem("Brown Mushroom")?.InternalId.ShouldBe("BROWN_MUSHROOM");
	}
}