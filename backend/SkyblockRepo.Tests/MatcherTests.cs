using System.Collections.Generic;
using System.Collections.ObjectModel;
using Shouldly;
using SkyblockRepo.Models;
using Xunit;
using SkyblockRepo;

namespace SkyblockRepo.Tests;

[Collection("SkyblockRepoState")]
public class MatcherTests
{
	[Fact]
	public void MatchItem_ReturnsVariantWhenDefinitionMatches()
	{
		var baseItem = new SkyblockItemData
		{
			InternalId = "LEGENDARY_WIDGET",
			Name = "Legendary Widget",
			Variants =
			[
				new SkyblockItemVariant
				{
					By = new ItemVariationDefinition
					{
						Type = VariationType.Attribute,
						Key = "rarity",
						Exact = "Legendary"
					},
					Item = new SkyblockItemVariantData
					{
						Name = "Legendary Widget",
						Lore = "Legendary variant"
					}
				}
			]
		};

		SetRepoData(baseItem);

		var config = new SkyblockRepoConfiguration();
		config.Matcher.Register(new SampleItemMatcher());
		var client = new SkyblockRepoClient(new NoOpUpdater(), config);

		var sourceItem = new SampleItem(
			"LEGENDARY_WIDGET",
			"Legendary Widget",
			new Dictionary<string, string> { ["rarity"] = "legendary" }
		);

		var match = client.MatchItem(sourceItem);

		match.ShouldNotBeNull();
		match!.Item.ShouldBe(baseItem);
		match.Variant.ShouldNotBeNull();
		match.Variant!.Item.Name.ShouldBe("Legendary Widget");
	}

	[Fact]
	public void MatchItem_ReturnsItemWhenVariantDoesNotMatch()
	{
		var baseItem = new SkyblockItemData
		{
			InternalId = "COMMON_WIDGET",
			Name = "Common Widget",
			Variants =
			[
				new SkyblockItemVariant
				{
					By = new ItemVariationDefinition
					{
						Type = VariationType.Name,
						Contains = "Limited"
					},
					Item = new SkyblockItemVariantData { Name = "Limited Widget" }
				}
			]
		};

		SetRepoData(baseItem);

		var config = new SkyblockRepoConfiguration();
		config.Matcher.Register(new SampleItemMatcher());
		var client = new SkyblockRepoClient(new NoOpUpdater(), config);

		var sourceItem = new SampleItem(
			"COMMON_WIDGET",
			"Common Widget",
			new Dictionary<string, string> { ["rarity"] = "common" }
		);

		var match = client.MatchItem(sourceItem);

		match.ShouldNotBeNull();
		match!.Item.InternalId.ShouldBe("COMMON_WIDGET");
		match.Variant.ShouldBeNull();
	}

	[Fact]
	public void MatchItem_ThrowsWhenNoMatcherRegistered()
	{
		SetRepoData();
		var config = new SkyblockRepoConfiguration();
		var client = new SkyblockRepoClient(new NoOpUpdater(), config);
		var sourceItem = new SampleItem("TEST", "Test", new Dictionary<string, string>());

		Should.Throw<InvalidOperationException>(() => client.MatchItem(sourceItem));
	}

	private static void SetRepoData(params SkyblockItemData[] items)
	{
		var itemsDict = new Dictionary<string, SkyblockItemData>(StringComparer.OrdinalIgnoreCase);
		var nameDict = new Dictionary<string, SkyblockItemNameSearch>(StringComparer.OrdinalIgnoreCase);

		foreach (var item in items)
		{
			itemsDict[item.InternalId] = item;
			nameDict[item.InternalId] = new SkyblockItemNameSearch
			{
				InternalId = item.InternalId,
				Name = item.Name
			};
		}

		SkyblockRepoUpdater.Data = new SkyblockRepoData
		{
			Items = new ReadOnlyDictionary<string, SkyblockItemData>(itemsDict),
			ItemNameSearch = new ReadOnlyDictionary<string, SkyblockItemNameSearch>(nameDict)
		};
	}

	private sealed record SampleItem(string Id, string Name, Dictionary<string, string> Attributes);

	private sealed class SampleItemMatcher : SkyblockRepoMatcher<SampleItem>
	{
		protected override string? GetAttributeString(SampleItem item, string attribute) =>
			item.Attributes.TryGetValue(attribute, out var value) ? value : null;

		protected override string? GetSkyblockId(SampleItem item) => item.Id;

		protected override string? GetName(SampleItem item) => item.Name;
	}

	private sealed class NoOpUpdater : ISkyblockRepoUpdater
	{
		public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
		public Task CheckForUpdatesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
		public Task ReloadRepoAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	}
}
