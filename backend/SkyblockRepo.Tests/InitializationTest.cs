using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace SkyblockRepo.Tests;

public class SkyblockRepoInitializeTests
{
	[Fact]
	public async Task InitializeFromLocalFolder()
	{
		var logger = Substitute.For<ILogger<SkyblockRepoUpdater>>();
		var config = new SkyblockRepoConfiguration
		{
			LocalRepoPath = Path.Join(SkyblockRepoUtils.GetSolutionPath(), "..", "output")
		};
		var updater = new SkyblockRepoUpdater(config, logger);
		var repo = new SkyblockRepoClient(updater);
		
		await repo.InitializeAsync();

		SkyblockRepoClient.Data.Items.Count.ShouldBeGreaterThan(5000);
		SkyblockRepoClient.Data.Pets.Count.ShouldBe(79);
	}
}