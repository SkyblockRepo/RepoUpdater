using SkyblockRepo.Models;

namespace SkyblockRepo.StaticData;

internal sealed class CollectionsSectionParser : IRepoSectionParser
{
	public string Name => "Collections";
	public RepoSectionParserStage Stage => RepoSectionParserStage.ExtendedMetadata;

	public Task ApplyAsync(RepoSectionLoadContext context, CancellationToken cancellationToken)
	{
		if (!context.HasNeuRepo)
		{
			return Task.CompletedTask;
		}

		var categories = new Dictionary<string, SkyblockCollectionCategory>(StringComparer.OrdinalIgnoreCase);
		var byItemId = new Dictionary<string, SkyblockCollectionEntry>(StringComparer.OrdinalIgnoreCase);

		foreach (var (categoryId, categorySource) in context.CollectionsResponse?.Collections ?? [])
		{
			var entries = (categorySource.Items ?? [])
				.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
				.Select(entry =>
				{
					var orderedTiers = (entry.Value.Tiers ?? [])
						.OrderBy(tier => tier.Tier)
						.ToArray();

					return new SkyblockCollectionEntry
					{
						ItemId = entry.Key,
						Name = string.IsNullOrWhiteSpace(entry.Value.Name)
							? StaticMetadataParserUtils.GetItemName(context, entry.Key) ?? entry.Key
							: entry.Value.Name,
						CategoryId = categoryId,
						Icon = StaticMetadataParserUtils.GetItemIcon(context, entry.Key),
						Thresholds = StaticMetadataParserUtils.ToReadOnlyCollection(orderedTiers.Select(tier => tier.AmountRequired)),
						MaxTier = entry.Value.MaxTiers > 0 ? entry.Value.MaxTiers : orderedTiers.Length,
					};
				})
				.ToArray();

			foreach (var entry in entries)
			{
				byItemId[entry.ItemId] = entry;
			}

			categories[categoryId] = new SkyblockCollectionCategory
			{
				CategoryId = categoryId,
				Name = categorySource.Name,
				Icon = entries.FirstOrDefault()?.Icon ?? new SkyblockDisplayIcon(),
				Entries = StaticMetadataParserUtils.ToReadOnlyCollection(entries),
			};
		}

		context.Data.Collections = new SkyblockCollectionsData
		{
			Version = context.CollectionsResponse?.Version ?? string.Empty,
			LastUpdated = context.CollectionsResponse?.GetLastUpdatedAt(),
			ByCategoryId = StaticMetadataParserUtils.ToReadOnlyDictionary(categories),
			ByItemId = StaticMetadataParserUtils.ToReadOnlyDictionary(byItemId),
			BossCollections = StaticMetadataDefaults.EmptyDictionary<SkyblockBossCollection>(),
		};

		return Task.CompletedTask;
	}
}
