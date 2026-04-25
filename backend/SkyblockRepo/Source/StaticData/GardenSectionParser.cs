using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using SkyblockRepo.Models;

namespace SkyblockRepo.StaticData;

internal sealed class GardenSectionParser : IRepoSectionParser
{
	public string Name => "Garden";
	public RepoSectionParserStage Stage => RepoSectionParserStage.ExtendedMetadata;

	public async Task ApplyAsync(RepoSectionLoadContext context, CancellationToken cancellationToken)
	{
		if (!context.HasNeuRepo)
		{
			return;
		}

		var source = await context.ReadNeuConstantAsync<GardenConstantsSource>("garden.json", cancellationToken);
		if (source is null)
		{
			return;
		}

		var visitors = source.Visitors?.ToDictionary(
			entry => entry.Key,
			entry =>
			{
				var upperId = entry.Key.ToUpperInvariant();
				var npcName = context.Data.Npcs.GetValueOrDefault(upperId)?.Name;

				return new SkyblockGardenVisitorDefinition
				{
					VisitorId = entry.Key,
					Name = npcName ?? StaticMetadataParserUtils.TitleCaseId(entry.Key),
					Rarity = entry.Value,
					Icon = new SkyblockDisplayIcon(),
				};
			},
			StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, SkyblockGardenVisitorDefinition>(StringComparer.OrdinalIgnoreCase);

		var plots = source.Plots?.ToDictionary(
			entry => entry.Key,
			entry => new SkyblockGardenPlotDefinition
			{
				PlotId = entry.Key,
				Name = StaticMetadataParserUtils.CleanText(entry.Value.Name),
				X = entry.Value.X,
				Y = entry.Value.Y,
				PlotNumber = ParsePlotNumber(entry.Value.Name),
			},
			StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, SkyblockGardenPlotDefinition>(StringComparer.OrdinalIgnoreCase);

		var plotCosts = source.PlotCosts?.ToDictionary(
			entry => entry.Key,
			entry => StaticMetadataParserUtils.ToReadOnlyCollection(
				(entry.Value ?? []).Select(cost => new SkyblockGardenCost
				{
					ItemId = cost.Item,
					Amount = cost.Amount,
				})),
			StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, ReadOnlyCollection<SkyblockGardenCost>>(StringComparer.OrdinalIgnoreCase);

		var barnSkins = source.Barn?.ToDictionary(
			entry => entry.Key,
			entry => new SkyblockGardenBarnSkin
			{
				SkinId = entry.Key,
				Name = StaticMetadataParserUtils.CleanText(entry.Value.Name),
				ItemId = entry.Value.Item,
			},
			StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, SkyblockGardenBarnSkin>(StringComparer.OrdinalIgnoreCase);

		var composterUpgrades = source.ComposterUpgrades?.ToDictionary(
			entry => entry.Key,
			entry => StaticMetadataParserUtils.ToReadOnlyCollection(
				(entry.Value ?? [])
				.Select(levelEntry => new SkyblockGardenUpgradeLevel
				{
					Level = int.Parse(levelEntry.Key),
					Reward = levelEntry.Value.Upgrade,
					CopperCost = levelEntry.Value.Copper,
					ItemCosts = StaticMetadataParserUtils.ToReadOnlyDictionary(
						(levelEntry.Value.Items ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase))
						.Select(itemCost => new KeyValuePair<string, int>(itemCost.Key, itemCost.Value))),
					TooltipTemplate = source.ComposterTooltips?.GetValueOrDefault(entry.Key),
				})
				.OrderBy(level => level.Level)),
			StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, ReadOnlyCollection<SkyblockGardenUpgradeLevel>>(StringComparer.OrdinalIgnoreCase);

		context.Data.Garden = new SkyblockGardenData
		{
			ExperienceTable = StaticMetadataParserUtils.ToReadOnlyCollection(source.GardenExp ?? []),
			CropMilestones = StaticMetadataParserUtils.ToReadOnlyDictionary(
				(source.CropMilestones ?? new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase))
				.Select(entry => new KeyValuePair<string, ReadOnlyCollection<int>>(entry.Key, StaticMetadataParserUtils.ToReadOnlyCollection(entry.Value)))),
			Visitors = StaticMetadataParserUtils.ToReadOnlyDictionary(visitors),
			Plots = StaticMetadataParserUtils.ToReadOnlyDictionary(plots),
			PlotCosts = StaticMetadataParserUtils.ToReadOnlyDictionary(plotCosts),
			BarnSkins = StaticMetadataParserUtils.ToReadOnlyDictionary(barnSkins),
			CropUpgradeThresholds = StaticMetadataParserUtils.ToReadOnlyCollection(source.CropUpgrades ?? []),
			ComposterUpgrades = StaticMetadataParserUtils.ToReadOnlyDictionary(composterUpgrades),
			ComposterTooltipTemplates = StaticMetadataParserUtils.ToReadOnlyDictionary(
				(source.ComposterTooltips ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase))
				.Select(entry => new KeyValuePair<string, string>(entry.Key, StaticMetadataParserUtils.CleanText(entry.Value)))),
			GreenhouseUpgrades = StaticMetadataDefaults.EmptyDictionary<ReadOnlyCollection<SkyblockGardenUpgradeLevel>>(),
			Mutations = StaticMetadataParserUtils.ToReadOnlyDictionary(BuildMutations(context)),
			Tools = StaticMetadataDefaults.EmptyDictionary<SkyblockGardenToolDefinition>(),
		};
	}

	private static Dictionary<string, SkyblockGardenMutationDefinition> BuildMutations(RepoSectionLoadContext context)
	{
		var mutations = new Dictionary<string, SkyblockGardenMutationDefinition>(StringComparer.OrdinalIgnoreCase);
		foreach (var (itemId, neuItem) in context.Data.NeuItems)
		{
			var lore = neuItem.Lore ?? [];
			var rarityLine = lore
				.Select(line => StaticMetadataParserUtils.CleanText(line))
				.FirstOrDefault(line => line.Contains("MUTATION", StringComparison.OrdinalIgnoreCase));

			if (string.IsNullOrWhiteSpace(rarityLine))
			{
				continue;
			}

			mutations[itemId] = new SkyblockGardenMutationDefinition
			{
				ItemId = itemId,
				Name = StaticMetadataParserUtils.GetItemName(context, itemId) ?? StaticMetadataParserUtils.TitleCaseId(itemId),
				Rarity = StaticMetadataParserUtils.ExtractRarityLabel(rarityLine) ?? string.Empty,
				Analyzable = lore.Any(line => StaticMetadataParserUtils.CleanText(line).Contains("Analyze", StringComparison.OrdinalIgnoreCase)),
				Icon = StaticMetadataParserUtils.GetItemIcon(context, itemId),
			};
		}

		return mutations;
	}

	private static int? ParsePlotNumber(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		var digits = new string(value.Where(char.IsDigit).ToArray());
		return int.TryParse(digits, out var number) ? number : null;
	}

	private sealed class GardenConstantsSource
	{
		[JsonPropertyName("garden_exp")]
		public List<int>? GardenExp { get; init; }

		[JsonPropertyName("crop_milestones")]
		public Dictionary<string, List<int>>? CropMilestones { get; init; }

		[JsonPropertyName("visitors")]
		public Dictionary<string, string>? Visitors { get; init; }

		[JsonPropertyName("plots")]
		public Dictionary<string, GardenPlotSource>? Plots { get; init; }

		[JsonPropertyName("plot_costs")]
		public Dictionary<string, List<GardenCostSource>>? PlotCosts { get; init; }

		[JsonPropertyName("barn")]
		public Dictionary<string, GardenBarnSource>? Barn { get; init; }

		[JsonPropertyName("crop_upgrades")]
		public List<int>? CropUpgrades { get; init; }

		[JsonPropertyName("composter_upgrades")]
		public Dictionary<string, Dictionary<string, GardenUpgradeSource>>? ComposterUpgrades { get; init; }

		[JsonPropertyName("composter_tooltips")]
		public Dictionary<string, string>? ComposterTooltips { get; init; }
	}

	private sealed class GardenPlotSource
	{
		[JsonPropertyName("name")]
		public string Name { get; init; } = string.Empty;

		[JsonPropertyName("x")]
		public int X { get; init; }

		[JsonPropertyName("y")]
		public int Y { get; init; }
	}

	private sealed class GardenCostSource
	{
		[JsonPropertyName("item")]
		public string Item { get; init; } = string.Empty;

		[JsonPropertyName("amount")]
		public int Amount { get; init; }
	}

	private sealed class GardenBarnSource
	{
		[JsonPropertyName("name")]
		public string Name { get; init; } = string.Empty;

		[JsonPropertyName("item")]
		public string Item { get; init; } = string.Empty;
	}

	private sealed class GardenUpgradeSource
	{
		[JsonPropertyName("upgrade")]
		public int Upgrade { get; init; }

		[JsonPropertyName("items")]
		public Dictionary<string, int>? Items { get; init; }

		[JsonPropertyName("copper")]
		public int? Copper { get; init; }
	}
}
