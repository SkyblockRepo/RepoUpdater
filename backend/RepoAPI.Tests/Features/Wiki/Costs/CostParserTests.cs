using RepoAPI.Features.Wiki.Templates;
using SkyblockRepo;
using SkyblockRepo.Models;

namespace RepoAPI.Tests.Features.Wiki.Costs;

public class CostParserTests
{
	[Fact]
	public void CostParser_CoinsParsesCorrectly()
	{
		SkyblockRepoUpdater.Data = new SkyblockRepoData();
		_ = new SkyblockRepoClient(new NoOpUpdater(), new SkyblockRepoConfiguration());

		var input = @"{{Item/SAND:1|lore}}\n\n&7Cost\n&620 Coins";
		var cost = ParserUtils.ParseUpgradeCost(input).Cost;

		cost.ShouldNotBeNull();
		cost.Count.ShouldBe(1);

		cost[0].Type.ShouldBe(UpgradeCostType.Coins);
		cost[0].Amount.ShouldBe(20);
	}
	
	[Fact]
	public async Task CostParser_ItemsParseCorrectly()
	{
		var input = @"{{Item/PUMPKIN_DICER|lore}}\n\n&7Cost\n&6Gold medal\n&aJacob's Ticket &8x32";
		
		var config = new SkyblockRepoConfiguration
		{
			UseNeuRepo = false,
			SkyblockRepo = new RepoSettings
			{
				LocalPath = Path.Join(SkyblockRepoUtils.GetSolutionPath(), "..", "output")
			}
		};
		var updater = new SkyblockRepoUpdater(config);
		var repo = new SkyblockRepoClient(updater, config);
		
		await repo.InitializeAsync(TestContext.Current.CancellationToken);
		
		var cost = ParserUtils.ParseUpgradeCost(input).Cost;

		cost.ShouldNotBeNull();
		cost.Count.ShouldBe(2);

		cost[0].Type.ShouldBe(UpgradeCostType.JacobMedal);
		cost[0].MedalType.ShouldBe("gold");
		cost[0].Amount.ShouldBe(1);
		
		cost[1].Type.ShouldBe(UpgradeCostType.Item);
		cost[1].ItemId.ShouldBe("JACOBS_TICKET");
		cost[1].Amount.ShouldBe(32);
		
		
		var input2 = @"{{Item/FRESHLY_MINTED_COINS|lore}}\n\n&7Cost\n&5Stock of Stonks\n\n&cThis item has a SkyBlock-wide limit of\n&c5,000 units every 4h.\n\n&7Stock: &a5,000\n&7Resets in: &a4h 00m\n\n&eClick to trade!";
		var cost2 = ParserUtils.ParseUpgradeCost(input2).Cost;
		cost2.ShouldNotBeNull();
		cost2.Count.ShouldBe(1);
		
		cost2[0].Type.ShouldBe(UpgradeCostType.Item);
		cost2[0].ItemId.ShouldBe("STOCK_OF_STONKS");
		cost2[0].Amount.ShouldBe(1);
		
		var input3 = @"{{Item/SNOW_BLASTER|lore}}\n\n&7Cost\n&675,000 Coins\n\n&eClick to trade!";
		var cost3 = ParserUtils.ParseUpgradeCost(input3).Cost;
		cost3.ShouldNotBeNull();
		cost3.Count.ShouldBe(1);
		
		cost3[0].Type.ShouldBe(UpgradeCostType.Coins);
		cost3[0].Amount.ShouldBe(75000);
		
		var input4 = @"{{Item_grass}}\n\n&7Cost\n&fDirt &8x4";
		var cost4 = ParserUtils.ParseUpgradeCost(input4).Cost;
		cost4.ShouldNotBeNull();
		cost4.Count.ShouldBe(1);
		cost4[0].Type.ShouldBe(UpgradeCostType.Item);
		cost4[0].ItemId.ShouldBe("DIRT");
		cost4[0].Amount.ShouldBe(4);
		
		var input5 = @"&8Farming Pet\n\n&7Speed: &a+10\n&7Strength: &a+30\n&7Intelligence: &a+50\n\n&6Hive\n&7Gain &b+20✎ Intelligence &7and\n&c+15❁ Strength &7for each nearby\n&7bee.\n&8Max 15 bees\n\n&6Busy Buzz Buzz\n&7Has &a100% &7chance for flowers\n&7to drop an extra one\n\n&6Weaponized Honey\n&7Gain &a25% &7of received\n&7damage as &6❤ Absorption\n\n&cThis is a preview of Lvl 100.\n&cNew pets are lowest level!\n\n&7Cost\n&6650.000 Coins\n&9Enchanted Block of Coal &8x8\n&9Enchanted Gold Block &8x8";
		var cost5 = ParserUtils.ParseUpgradeCost(input5).Cost;
		cost5.ShouldNotBeNull();
		cost5.Count.ShouldBe(3);
		
		cost5[0].Type.ShouldBe(UpgradeCostType.Coins);
		cost5[0].Amount.ShouldBe(650000);
		
		cost5[1].Type.ShouldBe(UpgradeCostType.Item);
		cost5[1].ItemId.ShouldBe("ENCHANTED_COAL_BLOCK");
		cost5[1].Amount.ShouldBe(8);
		
		cost5[2].Type.ShouldBe(UpgradeCostType.Item);
		cost5[2].ItemId.ShouldBe("ENCHANTED_GOLD_BLOCK");
		cost5[2].Amount.ShouldBe(8);
	}

	private sealed class NoOpUpdater : ISkyblockRepoUpdater
	{
		public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
		public Task CheckForUpdatesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
		public Task ReloadRepoAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	}
}