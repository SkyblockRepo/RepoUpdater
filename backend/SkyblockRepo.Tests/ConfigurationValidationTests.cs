using Shouldly;

namespace SkyblockRepo.Tests;

public class ConfigurationValidationTests
{
	[Fact]
	public void SkyblockRepoUpdater_ThrowsHelpfulError_WhenRepoSettingsAreReplacedWithoutRequiredRemoteValues()
	{
		var config = new SkyblockRepoConfiguration
		{
			SkyblockRepo = new RepoSettings
			{
				StorageMode = RepoStorageMode.ZipArchive
			}
		};

		var exception = Should.Throw<InvalidOperationException>(() => new SkyblockRepoUpdater(config));
		exception.Message.ShouldContain("SkyblockRepo is missing required remote settings");
		exception.Message.ShouldContain("config.SkyblockRepo.StorageMode = RepoStorageMode.ZipArchive");
	}
}
