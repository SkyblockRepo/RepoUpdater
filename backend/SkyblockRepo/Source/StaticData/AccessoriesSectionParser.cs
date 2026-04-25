using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using SkyblockRepo.Models;
using SkyblockRepo.Models.Neu;

namespace SkyblockRepo.StaticData;

internal sealed partial class AccessoriesSectionParser : IRepoSectionParser
{
	private const string EnrichmentPrefix = "TALISMAN_ENRICHMENT_";

	public string Name => "Accessories";
	public RepoSectionParserStage Stage => RepoSectionParserStage.ExtendedMetadata;

	public async Task ApplyAsync(RepoSectionLoadContext context, CancellationToken cancellationToken)
	{
		if (!context.HasNeuRepo)
		{
			return;
		}

		var misc = await context.ReadNeuConstantAsync<AccessoryMiscSource>("misc.json", cancellationToken);
		if (misc is null && context.Data.NeuItems.Count == 0 && context.Data.Items.Count == 0)
		{
			return;
		}

		var fullChains = BuildUpgradeChains(misc?.TalismanUpgrades ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase));
		var ignored = new HashSet<string>(misc?.IgnoredTalisman ?? [], StringComparer.OrdinalIgnoreCase);

		var candidateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var item in context.Data.Items.Values.Where(item => string.Equals(item.Category, "Accessory", StringComparison.OrdinalIgnoreCase)))
		{
			candidateIds.Add(item.InternalId);
		}

		foreach (var (itemId, neuItem) in context.Data.NeuItems)
		{
			if (IsAccessory(neuItem))
			{
				candidateIds.Add(itemId);
			}
		}

		foreach (var itemId in fullChains.Keys)
		{
			candidateIds.Add(itemId);
		}

		var accessories = new Dictionary<string, SkyblockAccessoryDefinition>(StringComparer.OrdinalIgnoreCase);
		foreach (var itemId in candidateIds.Where(id => !ignored.Contains(id)))
		{
			var (magicalPowerOverride, magicalPowerMultiplier) = ParseMagicalPowerMetadata(StaticMetadataParserUtils.GetNeuItem(context, itemId));

			accessories[itemId] = new SkyblockAccessoryDefinition
			{
				ItemId = itemId,
				Name = StaticMetadataParserUtils.GetItemName(context, itemId) ?? StaticMetadataParserUtils.TitleCaseId(itemId),
				BaseRarity = StaticMetadataParserUtils.GetItemRarity(context, itemId),
				Icon = StaticMetadataParserUtils.GetItemIcon(context, itemId),
				Aliases = StaticMetadataDefaults.EmptyList<string>(),
				UpgradeChain = StaticMetadataParserUtils.ToReadOnlyCollection(fullChains.GetValueOrDefault(itemId) ?? [itemId]),
				MagicalPowerOverride = magicalPowerOverride,
				MagicalPowerMultiplier = magicalPowerMultiplier,
			};
		}

		context.Data.Accessories = new SkyblockAccessoriesData
		{
			ByItemId = StaticMetadataParserUtils.ToReadOnlyDictionary(accessories),
			AliasToBaseId = StaticMetadataDefaults.EmptyDictionary<string>(),
			MagicalPowerByRarity = StaticMetadataDefaults.EmptyDictionary<int>(),
			EnrichmentKeyToStatName = StaticMetadataParserUtils.ToReadOnlyDictionary(BuildEnrichmentMap(context.Data.NeuItems)),
			IgnoredIds = StaticMetadataParserUtils.ToReadOnlyCollection(ignored.Order(StringComparer.OrdinalIgnoreCase)),
		};
	}

	private static bool IsAccessory(NeuItemData neuItem)
	{
		var terminalLine = neuItem.Lore?
			.Select(line => StaticMetadataParserUtils.CleanText(line))
			.LastOrDefault(line => !string.IsNullOrWhiteSpace(line));

		return !string.IsNullOrWhiteSpace(terminalLine) &&
		       terminalLine.Contains("ACCESSORY", StringComparison.OrdinalIgnoreCase);
	}

	private static Dictionary<string, string> BuildEnrichmentMap(IReadOnlyDictionary<string, NeuItemData> neuItems)
	{
		var enrichments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var (itemId, item) in neuItems)
		{
			if (!itemId.StartsWith(EnrichmentPrefix, StringComparison.OrdinalIgnoreCase) ||
			    itemId.EndsWith("SWAPPER", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var enrichmentKey = itemId[EnrichmentPrefix.Length..].ToLowerInvariant();
			var statName = StaticMetadataParserUtils.CleanText(item.DisplayName).Replace(" Enrichment", string.Empty, StringComparison.OrdinalIgnoreCase);
			if (!string.IsNullOrWhiteSpace(statName))
			{
				enrichments[enrichmentKey] = statName;
			}
		}

		return enrichments;
	}

	private static (int? MagicalPowerOverride, double? MagicalPowerMultiplier) ParseMagicalPowerMetadata(NeuItemData? neuItem)
	{
		if (neuItem?.Lore is null)
		{
			return (null, null);
		}

		var cleanedLore = neuItem.Lore
			.Select(line => StaticMetadataParserUtils.CleanText(line))
			.Where(line => !string.IsNullOrWhiteSpace(line))
			.ToArray();

		if (cleanedLore.Any(line => line.Contains("Counts for twice the Magical Power", StringComparison.OrdinalIgnoreCase)))
		{
			return (null, 2d);
		}

		foreach (var line in cleanedLore.Where(line => line.Contains("Magical Power", StringComparison.OrdinalIgnoreCase)))
		{
			if (line.Contains("per", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var overrideMatch = FixedMagicalPowerRegex().Match(line);
			if (overrideMatch.Success && int.TryParse(overrideMatch.Groups["value"].Value, out var overrideValue))
			{
				return (overrideValue, null);
			}

			var multiplierMatch = MagicalPowerMultiplierRegex().Match(line);
			if (multiplierMatch.Success && double.TryParse(multiplierMatch.Groups["multiplier"].Value, out var multiplier))
			{
				return (null, multiplier);
			}
		}

		return (null, null);
	}

	private static Dictionary<string, List<string>> BuildUpgradeChains(Dictionary<string, List<string>> talismanUpgrades)
	{
		var allUpgradeValues = talismanUpgrades.Values.SelectMany(values => values).ToHashSet(StringComparer.OrdinalIgnoreCase);
		var chains = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

		foreach (var (rootId, upgrades) in talismanUpgrades.Where(entry => !allUpgradeValues.Contains(entry.Key)))
		{
			var chain = new[] { rootId }.Concat(upgrades).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			foreach (var itemId in chain)
			{
				chains[itemId] = chain;
			}
		}

		return chains;
	}

	private sealed class AccessoryMiscSource
	{
		[JsonPropertyName("talisman_upgrades")]
		public Dictionary<string, List<string>> TalismanUpgrades { get; init; } = new(StringComparer.OrdinalIgnoreCase);

		[JsonPropertyName("ignored_talisman")]
		public List<string>? IgnoredTalisman { get; init; }
	}

	[GeneratedRegex(@"(?<value>[+-]?\d+)\s+Magical Power", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
	private static partial Regex FixedMagicalPowerRegex();

	[GeneratedRegex(@"Counts for\s+(?<multiplier>\d+(?:\.\d+)?)x?\s+the Magical Power", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
	private static partial Regex MagicalPowerMultiplierRegex();
}
