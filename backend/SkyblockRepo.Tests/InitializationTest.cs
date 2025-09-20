using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace SkyblockRepo.Tests;

public class SkyblockRepoInitializeTests
{
	[Fact]
	public async Task InitializeFromLocalFolder()
	{
		var logger = Substitute.For<ILogger<SkyblockRepo>>();
		var repo = new SkyblockRepo(new SkyblockRepoConfiguration
		{
			LocalRepoPath = Path.Join(SkyblockRepoUtils.GetSolutionPath(), "..", "output")
		}, logger);
		
		await repo.InitializeAsync();
		
		SkyblockRepo.Cache.Items.Count.ShouldBeGreaterThan(5000);
		SkyblockRepo.Cache.Pets.Count.ShouldBe(79);
	}
}