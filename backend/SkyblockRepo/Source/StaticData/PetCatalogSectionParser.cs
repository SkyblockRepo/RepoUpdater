using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using SkyblockRepo.Models;

namespace SkyblockRepo.StaticData;

internal sealed class PetCatalogSectionParser : IRepoSectionParser
{
	public string Name => "PetCatalog";
	public RepoSectionParserStage Stage => RepoSectionParserStage.ExtendedMetadata;

	public async Task ApplyAsync(RepoSectionLoadContext context, CancellationToken cancellationToken)
	{
		if (!context.HasNeuRepo)
		{
			return;
		}

		var pets = await context.ReadNeuConstantAsync<PetConstantsSource>("pets.json", cancellationToken);
		var petNums = await context.ReadNeuConstantAsync<Dictionary<string, Dictionary<string, JsonElement>>>("petnums.json", cancellationToken);
		if (pets is null)
		{
			return;
		}

		var parents = await context.ReadNeuConstantAsync<Dictionary<string, List<string>>>("parents.json", cancellationToken)
		              ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
		var knownPetIds = (petNums?.Keys ?? Enumerable.Empty<string>())
			.Union(pets.PetTypes?.Keys ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
		var parentAliases = BuildParentAliases(knownPetIds, parents);

		var entries = new Dictionary<string, SkyblockPetCatalogEntry>(StringComparer.OrdinalIgnoreCase);
		foreach (var petId in knownPetIds)
		{
			var raritySnapshots = ParseRaritySnapshots(petNums?.GetValueOrDefault(petId), context.SerializerOptions);
			var orderedRarities = raritySnapshots.Keys.OrderBy(GetRaritySortIndex).ToArray();
			var rarityDefinitions = raritySnapshots.ToDictionary(
				entry => entry.Key,
				entry => new SkyblockPetCatalogRarityDefinition
				{
					Rarity = entry.Key,
					LevelOneStats = new ReadOnlyDictionary<string, double>(
						new Dictionary<string, double>(entry.Value.GetValueOrDefault("1")?.StatNums ?? new Dictionary<string, double>(), StringComparer.OrdinalIgnoreCase)),
					LevelHundredStats = new ReadOnlyDictionary<string, double>(
						new Dictionary<string, double>(entry.Value.GetValueOrDefault("100")?.StatNums ?? new Dictionary<string, double>(), StringComparer.OrdinalIgnoreCase)),
					LevelOneOtherNumbers = StaticMetadataParserUtils.ToReadOnlyCollection(entry.Value.GetValueOrDefault("1")?.OtherNums ?? []),
					LevelHundredOtherNumbers = StaticMetadataParserUtils.ToReadOnlyCollection(entry.Value.GetValueOrDefault("100")?.OtherNums ?? []),
				},
				StringComparer.OrdinalIgnoreCase);

			entries[petId] = new SkyblockPetCatalogEntry
			{
				PetId = petId,
				DisplayName = pets.IdToDisplayName?.GetValueOrDefault(petId) ?? StaticMetadataParserUtils.TitleCaseId(petId),
				SkillType = pets.PetTypes?.GetValueOrDefault(petId),
				ParentId = parentAliases.GetValueOrDefault(petId),
				AvailableRarities = StaticMetadataParserUtils.ToReadOnlyCollection(orderedRarities),
				MaxRarity = orderedRarities.LastOrDefault(),
				Rarities = StaticMetadataParserUtils.ToReadOnlyDictionary(rarityDefinitions),
				Icon = ResolvePetIcon(context, petId),
			};
		}

		var customLeveling = (pets.CustomPetLeveling ?? new Dictionary<string, CustomPetLevelingSource>(StringComparer.OrdinalIgnoreCase))
			.ToDictionary(
				entry => entry.Key,
				entry => new SkyblockPetLevelingDefinition
				{
					PetId = entry.Key,
					Type = entry.Value.Type,
					MaxLevel = entry.Value.MaxLevel,
					LevelExperience = StaticMetadataParserUtils.ToReadOnlyCollection(entry.Value.LevelExperience ?? []),
					ExperienceMultiplier = entry.Value.ExperienceMultiplier,
				},
				StringComparer.OrdinalIgnoreCase);

		context.Data.PetCatalog = new SkyblockPetCatalogData
		{
			ByPetId = StaticMetadataParserUtils.ToReadOnlyDictionary(entries),
			LevelExperience = StaticMetadataParserUtils.ToReadOnlyCollection(pets.PetLevels ?? []),
			RarityOffsets = StaticMetadataParserUtils.ToReadOnlyDictionary(
				(pets.PetRarityOffset ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase))
				.Select(entry => new KeyValuePair<string, int>(entry.Key, entry.Value))),
			CustomLeveling = StaticMetadataParserUtils.ToReadOnlyDictionary(customLeveling),
			ScoreRewards = StaticMetadataDefaults.EmptyIntDictionary<SkyblockPetScoreReward>(),
			ParentAliases = StaticMetadataParserUtils.ToReadOnlyDictionary(parentAliases),
			HeldItemDisplayNameToId = StaticMetadataParserUtils.ToReadOnlyDictionary(
				(pets.PetItemDisplayNameToId ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase))
				.Select(entry => new KeyValuePair<string, string>(StaticMetadataParserUtils.CleanText(entry.Key), entry.Value))),
		};
	}

	private static Dictionary<string, Dictionary<string, PetLevelSnapshotSource>> ParseRaritySnapshots(
		Dictionary<string, JsonElement>? rarityElements,
		JsonSerializerOptions serializerOptions)
	{
		var rarities = new Dictionary<string, Dictionary<string, PetLevelSnapshotSource>>(StringComparer.OrdinalIgnoreCase);
		if (rarityElements is null)
		{
			return rarities;
		}

		foreach (var (rarity, rarityElement) in rarityElements)
		{
			if (rarityElement.ValueKind != JsonValueKind.Object)
			{
				continue;
			}

			var levelSnapshots = new Dictionary<string, PetLevelSnapshotSource>(StringComparer.OrdinalIgnoreCase);
			foreach (var property in rarityElement.EnumerateObject())
			{
				if (!int.TryParse(property.Name, out _) || property.Value.ValueKind != JsonValueKind.Object)
				{
					continue;
				}

				var snapshot = property.Value.Deserialize<PetLevelSnapshotSource>(serializerOptions);
				if (snapshot is not null)
				{
					levelSnapshots[property.Name] = snapshot;
				}
			}

			rarities[rarity] = levelSnapshots;
		}

		return rarities;
	}

	private static Dictionary<string, string> BuildParentAliases(IReadOnlySet<string> knownPetIds, Dictionary<string, List<string>> parents)
	{
		var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var (rootId, descendants) in parents)
		{
			var rootBaseId = StripPetTier(rootId);
			if (!knownPetIds.Contains(rootBaseId))
			{
				continue;
			}

			foreach (var descendantId in descendants)
			{
				var descendantBaseId = StripPetTier(descendantId);
				if (!knownPetIds.Contains(descendantBaseId) ||
				    string.Equals(descendantBaseId, rootBaseId, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				aliases[descendantBaseId] = rootBaseId;
			}
		}

		return aliases;
	}

	private static SkyblockDisplayIcon ResolvePetIcon(RepoSectionLoadContext context, string petId)
	{
		var itemId = context.Data.NeuItems.Keys
			.Where(id => id.StartsWith($"{petId};", StringComparison.OrdinalIgnoreCase))
			.OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
			.FirstOrDefault();

		return itemId is null
			? new SkyblockDisplayIcon()
			: StaticMetadataParserUtils.GetItemIcon(context, itemId);
	}

	private static string StripPetTier(string petItemId)
	{
		var separatorIndex = petItemId.IndexOf(';');
		return separatorIndex >= 0 ? petItemId[..separatorIndex] : petItemId;
	}

	private static int GetRaritySortIndex(string rarity)
	{
		return rarity.ToUpperInvariant() switch
		{
			"COMMON" => 0,
			"UNCOMMON" => 1,
			"RARE" => 2,
			"EPIC" => 3,
			"LEGENDARY" => 4,
			"MYTHIC" => 5,
			"DIVINE" => 6,
			_ => int.MaxValue,
		};
	}

	private sealed class PetConstantsSource
	{
		[JsonPropertyName("pet_rarity_offset")]
		public Dictionary<string, int>? PetRarityOffset { get; init; }

		[JsonPropertyName("pet_levels")]
		public List<int>? PetLevels { get; init; }

		[JsonPropertyName("custom_pet_leveling")]
		public Dictionary<string, CustomPetLevelingSource>? CustomPetLeveling { get; init; }

		[JsonPropertyName("pet_types")]
		public Dictionary<string, string>? PetTypes { get; init; }

		[JsonPropertyName("id_to_display_name")]
		public Dictionary<string, string>? IdToDisplayName { get; init; }

		[JsonPropertyName("pet_item_display_name_to_id")]
		public Dictionary<string, string>? PetItemDisplayNameToId { get; init; }
	}

	private sealed class CustomPetLevelingSource
	{
		[JsonPropertyName("type")]
		public int Type { get; init; }

		[JsonPropertyName("pet_levels")]
		public List<int>? LevelExperience { get; init; }

		[JsonPropertyName("max_level")]
		public int MaxLevel { get; init; }

		[JsonPropertyName("xp_multiplier")]
		public double? ExperienceMultiplier { get; init; }
	}

	private sealed class PetLevelSnapshotSource
	{
		[JsonPropertyName("otherNums")]
		public List<double>? OtherNums { get; init; }

		[JsonPropertyName("statNums")]
		public Dictionary<string, double>? StatNums { get; init; }
	}
}
