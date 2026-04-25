using System.Text.Json.Serialization;
using SkyblockRepo.Models;

namespace SkyblockRepo.StaticData;

internal sealed class MinionsSectionParser : IRepoSectionParser
{
	public string Name => "Minions";
	public RepoSectionParserStage Stage => RepoSectionParserStage.ExtendedMetadata;

	public async Task ApplyAsync(RepoSectionLoadContext context, CancellationToken cancellationToken)
	{
		if (!context.HasNeuRepo)
		{
			return;
		}

		var misc = await context.ReadNeuConstantAsync<MinionMiscSource>("misc.json", cancellationToken);
		if (misc?.Minions is null || misc.Minions.Count == 0)
		{
			return;
		}

		var parents = await context.ReadNeuConstantAsync<Dictionary<string, List<string>>>("parents.json", cancellationToken)
		              ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
		var byGeneratorId = new Dictionary<string, SkyblockMinionDefinition>(StringComparer.OrdinalIgnoreCase);
		var byBaseId = new Dictionary<string, SkyblockMinionDefinition>(StringComparer.OrdinalIgnoreCase);
		var categories = new Dictionary<string, SkyblockMinionCategory>(StringComparer.OrdinalIgnoreCase);

		foreach (var (generatorId, maxTier) in misc.Minions)
		{
			var baseId = generatorId.Replace("_GENERATOR", string.Empty, StringComparison.OrdinalIgnoreCase);
			var firstTierId = $"{generatorId}_1";
			var representativeItemId = ResolveRepresentativeTierItemId(generatorId, firstTierId, parents, context);
			var categoryId = ResolveCategoryId(baseId, context.CollectionsResponse);
			var icon = representativeItemId is null
				? new SkyblockDisplayIcon()
				: StaticMetadataParserUtils.GetItemIcon(context, representativeItemId);

			var definition = new SkyblockMinionDefinition
			{
				GeneratorId = generatorId,
				BaseId = baseId,
				Name = representativeItemId is null
					? StaticMetadataParserUtils.TitleCaseId(baseId)
					: TrimMinionTierSuffix(StaticMetadataParserUtils.GetItemName(context, representativeItemId) ?? StaticMetadataParserUtils.TitleCaseId(baseId)),
				CategoryId = categoryId,
				MaxTier = maxTier,
				Icon = icon,
			};

			byGeneratorId[definition.GeneratorId] = definition;
			byBaseId[definition.BaseId] = definition;

			if (string.IsNullOrWhiteSpace(categoryId))
			{
				continue;
			}

			if (!categories.ContainsKey(categoryId))
			{
				categories[categoryId] = new SkyblockMinionCategory
				{
					CategoryId = categoryId,
					Name = ResolveCategoryName(categoryId, context.CollectionsResponse),
					Icon = icon,
				};
			}
			else if (!string.IsNullOrWhiteSpace(icon.ItemId) || !string.IsNullOrWhiteSpace(icon.Texture))
			{
				categories[categoryId] = new SkyblockMinionCategory
				{
					CategoryId = categoryId,
					Name = categories[categoryId].Name,
					Icon = icon,
				};
			}
		}

		context.Data.Minions = new SkyblockMinionsData
		{
			ByGeneratorId = StaticMetadataParserUtils.ToReadOnlyDictionary(byGeneratorId),
			ByBaseId = StaticMetadataParserUtils.ToReadOnlyDictionary(byBaseId),
			Categories = StaticMetadataParserUtils.ToReadOnlyDictionary(categories),
			SlotThresholds = StaticMetadataDefaults.EmptyIntDictionary<int>(),
			MaxSlots = 0,
		};
	}

	private static string? ResolveRepresentativeTierItemId(
		string generatorId,
		string firstTierId,
		Dictionary<string, List<string>> parents,
		RepoSectionLoadContext context)
	{
		if (StaticMetadataParserUtils.GetNeuItem(context, firstTierId) is not null)
		{
			return firstTierId;
		}

		if (!parents.TryGetValue(firstTierId, out var descendants))
		{
			return null;
		}

		return descendants
			.Prepend(firstTierId)
			.FirstOrDefault(itemId =>
				itemId.StartsWith($"{generatorId}_", StringComparison.OrdinalIgnoreCase) &&
				StaticMetadataParserUtils.GetNeuItem(context, itemId) is not null);
	}

	private static string ResolveCategoryId(string baseId, HypixelCollectionsApiResponse? response)
	{
		foreach (var (categoryId, category) in response?.Collections ?? [])
		{
			if (category.Items?.ContainsKey(baseId) == true)
			{
				return categoryId;
			}
		}

		return string.Empty;
	}

	private static string ResolveCategoryName(string categoryId, HypixelCollectionsApiResponse? response)
	{
		return response?.Collections?.GetValueOrDefault(categoryId)?.Name
		       ?? StaticMetadataParserUtils.TitleCaseId(categoryId);
	}

	private static string TrimMinionTierSuffix(string value)
	{
		var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0)
		{
			return value;
		}

		return IsRomanNumeral(parts[^1])
			? string.Join(' ', parts[..^1])
			: value;
	}

	private static bool IsRomanNumeral(string value)
	{
		return !string.IsNullOrWhiteSpace(value) &&
		       value.All(character => "IVXLCDM".Contains(char.ToUpperInvariant(character)));
	}

	private sealed class MinionMiscSource
	{
		[JsonPropertyName("minions")]
		public Dictionary<string, int>? Minions { get; init; }
	}
}
