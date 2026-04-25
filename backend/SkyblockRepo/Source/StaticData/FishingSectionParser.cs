using System.Text.Json;
using SkyblockRepo.Models;

namespace SkyblockRepo.StaticData;

internal sealed class FishingSectionParser : IRepoSectionParser
{
	private static readonly Dictionary<string, int> TrophyTierOrder = new(StringComparer.OrdinalIgnoreCase)
	{
		["bronze"] = 0,
		["silver"] = 1,
		["gold"] = 2,
		["diamond"] = 3,
	};

	public string Name => "Fishing";
	public RepoSectionParserStage Stage => RepoSectionParserStage.ExtendedMetadata;

	public async Task ApplyAsync(RepoSectionLoadContext context, CancellationToken cancellationToken)
	{
		if (!context.HasNeuRepo)
		{
			return;
		}

		var trophyThresholds = await context.ReadNeuConstantAsync<Dictionary<string, List<int>>>("trophyfish.json", cancellationToken);
		if (trophyThresholds is null)
		{
			return;
		}

		var parents = await context.ReadNeuConstantAsync<Dictionary<string, List<string>>>("parents.json", cancellationToken)
		              ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
		var skyblockLevels = await context.ReadNeuConstantAsync<Dictionary<string, JsonElement>>("sblevels.json", cancellationToken);

		var discoveredTiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var trophyFish = new Dictionary<string, SkyblockTrophyFishDefinition>(StringComparer.OrdinalIgnoreCase);
		foreach (var (trophyFishId, thresholds) in trophyThresholds)
		{
			var tierEntries = ResolveTierEntries(trophyFishId, parents, context)
				.OrderBy(entry => TrophyTierOrder.GetValueOrDefault(entry.Tier, int.MaxValue))
				.ToArray();

			foreach (var tier in tierEntries.Select(entry => entry.Tier))
			{
				discoveredTiers.Add(tier);
			}

			var thresholdPairs = tierEntries
				.Select((entry, index) => new KeyValuePair<string, int>(entry.Tier, index < thresholds.Count ? thresholds[index] : 0))
				.Where(entry => entry.Value > 0);

			var iconPairs = tierEntries.Select(entry => new KeyValuePair<string, SkyblockDisplayIcon>(entry.Tier, entry.Icon));
			var fallbackName = tierEntries.FirstOrDefault()?.DisplayName ?? StaticMetadataParserUtils.TitleCaseId(trophyFishId);
			var fallbackDescription = tierEntries.FirstOrDefault()?.Description ?? string.Empty;

			trophyFish[trophyFishId] = new SkyblockTrophyFishDefinition
			{
				TrophyFishId = trophyFishId,
				DisplayName = fallbackName,
				Description = fallbackDescription,
				ThresholdsByTier = StaticMetadataParserUtils.ToReadOnlyDictionary(thresholdPairs),
				IconsByTier = StaticMetadataParserUtils.ToReadOnlyDictionary(iconPairs),
			};
		}

		var orderedTiers = discoveredTiers.OrderBy(tier => TrophyTierOrder.GetValueOrDefault(tier, int.MaxValue)).ToArray();
		context.Data.Fishing = new SkyblockFishingData
		{
			WaterCreatureIds = StaticMetadataDefaults.EmptyList<string>(),
			LavaCreatureIds = StaticMetadataDefaults.EmptyList<string>(),
			TrophyFishById = StaticMetadataParserUtils.ToReadOnlyDictionary(trophyFish),
			TrophyFishTiers = StaticMetadataParserUtils.ToReadOnlyCollection(orderedTiers),
			TrophyFishStages = StaticMetadataParserUtils.ToReadOnlyCollection(orderedTiers),
			DolphinBrackets = StaticMetadataParserUtils.ToReadOnlyCollection(BuildDolphinBrackets(skyblockLevels)),
		};
	}

	private static IEnumerable<TrophyFishTierEntry> ResolveTierEntries(
		string trophyFishId,
		Dictionary<string, List<string>> parents,
		RepoSectionLoadContext context)
	{
		var rootId = $"{trophyFishId}_DIAMOND";
		var itemIds = new List<string>();
		if (parents.TryGetValue(rootId, out var descendants))
		{
			itemIds.Add(rootId);
			itemIds.AddRange(descendants);
		}
		else
		{
			itemIds.AddRange(context.Data.NeuItems.Keys.Where(id => id.StartsWith($"{trophyFishId}_", StringComparison.OrdinalIgnoreCase)));
		}

		foreach (var itemId in itemIds.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			var tier = ExtractTier(itemId);
			if (tier is null)
			{
				continue;
			}

			var displayName = StaticMetadataParserUtils.GetItemName(context, itemId) ?? StaticMetadataParserUtils.TitleCaseId(trophyFishId);
			yield return new TrophyFishTierEntry(
				Tier: tier,
				DisplayName: RemoveTrailingTier(displayName, tier),
				Description: ExtractDescription(StaticMetadataParserUtils.GetNeuItem(context, itemId)?.Lore),
				Icon: StaticMetadataParserUtils.GetItemIcon(context, itemId));
		}
	}

	private static string? ExtractTier(string itemId)
	{
		var suffix = itemId[(itemId.LastIndexOf('_') + 1)..];
		return TrophyTierOrder.ContainsKey(suffix) ? suffix.ToLowerInvariant() : null;
	}

	private static string RemoveTrailingTier(string value, string tier)
	{
		var tierSuffix = $" {tier}";
		return value.EndsWith(tierSuffix, StringComparison.OrdinalIgnoreCase)
			? value[..^tierSuffix.Length]
			: value;
	}

	private static string ExtractDescription(IEnumerable<string>? lore)
	{
		return (lore ?? [])
			.Select(line => StaticMetadataParserUtils.CleanText(line))
			.FirstOrDefault(line =>
				!string.IsNullOrWhiteSpace(line) &&
				!line.Contains("Soulbound", StringComparison.OrdinalIgnoreCase) &&
				!line.Contains("Odger", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
	}

	private static IEnumerable<SkyblockMilestoneBracket> BuildDolphinBrackets(Dictionary<string, JsonElement>? source)
	{
		if (source is null)
		{
			yield break;
		}

		if (!TryFindIntArray(source, "dolphin_milestone_required", out var thresholds))
		{
			yield break;
		}

		for (var index = 0; index < thresholds.Count; index++)
		{
			yield return new SkyblockMilestoneBracket
			{
				Name = $"Milestone {index + 1}",
				Rarity = string.Empty,
				Requirement = thresholds[index],
			};
		}
	}

	private static bool TryFindIntArray(Dictionary<string, JsonElement> source, string propertyName, out List<int> values)
	{
		foreach (var element in source.Values)
		{
			if (TryFindIntArray(element, propertyName, out values))
			{
				return true;
			}
		}

		values = [];
		return false;
	}

	private static bool TryFindIntArray(JsonElement element, string propertyName, out List<int> values)
	{
		if (element.ValueKind == JsonValueKind.Object)
		{
			foreach (var property in element.EnumerateObject())
			{
				if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase) &&
				    property.Value.ValueKind == JsonValueKind.Array)
				{
					values = property.Value.EnumerateArray()
						.Where(item => item.ValueKind == JsonValueKind.Number)
						.Select(item => item.GetInt32())
						.ToList();
					return true;
				}

				if (TryFindIntArray(property.Value, propertyName, out values))
				{
					return true;
				}
			}
		}
		else if (element.ValueKind == JsonValueKind.Array)
		{
			foreach (var child in element.EnumerateArray())
			{
				if (TryFindIntArray(child, propertyName, out values))
				{
					return true;
				}
			}
		}

		values = [];
		return false;
	}

	private sealed record TrophyFishTierEntry(
		string Tier,
		string DisplayName,
		string Description,
		SkyblockDisplayIcon Icon);
}
