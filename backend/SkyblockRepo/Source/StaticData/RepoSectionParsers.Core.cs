using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using SkyblockRepo.Models;

namespace SkyblockRepo.StaticData;

internal sealed class BestiarySectionParser : IRepoSectionParser
{
	public string Name => "Bestiary";
	public RepoSectionParserStage Stage => RepoSectionParserStage.ExtendedMetadata;

	public async Task ApplyAsync(RepoSectionLoadContext context, CancellationToken cancellationToken)
	{
		if (!context.HasNeuRepo)
		{
			return;
		}

		var source = await context.ReadNeuConstantAsync<BestiaryConstantsSource>("bestiary.json", cancellationToken);
		if (source is null)
		{
			return;
		}

		var brackets = source.Brackets
			.Select(entry => new KeyValuePair<int, ReadOnlyCollection<int>>(int.Parse(entry.Key), StaticMetadataParserUtils.ExtractThresholds(entry.Value)))
			.ToArray();

		var categories = new Dictionary<string, SkyblockBestiaryCategory>(StringComparer.OrdinalIgnoreCase);
		var mobs = new Dictionary<string, SkyblockBestiaryMob>(StringComparer.OrdinalIgnoreCase);

		foreach (var (categoryId, element) in source.ExtraData)
		{
			var categorySource = element.Deserialize<BestiaryCategorySource>(context.SerializerOptions);
			if (categorySource is null)
			{
				continue;
			}

			categories[categoryId] = ParseCategory(categoryId, categoryId, null, categorySource, mobs, context);
		}

		context.Data.Bestiary = new SkyblockBestiaryData
		{
			Brackets = StaticMetadataParserUtils.ToReadOnlyDictionary(brackets),
			ByBestiaryId = StaticMetadataParserUtils.ToReadOnlyDictionary(categories),
			ByMobId = StaticMetadataParserUtils.ToReadOnlyDictionary(mobs),
		};
	}

	private static SkyblockBestiaryCategory ParseCategory(
		string categoryId,
		string rootCategoryId,
		string? subcategoryId,
		BestiaryCategorySource source,
		Dictionary<string, SkyblockBestiaryMob> mobIndex,
		RepoSectionLoadContext context)
	{
		var directMobs = source.Mobs
			?.Select(mob => ParseMob(rootCategoryId, subcategoryId, mob, mobIndex, context))
			.ToArray()
			?? [];

		var subcategories = new Dictionary<string, SkyblockBestiaryCategory>(StringComparer.OrdinalIgnoreCase);
		foreach (var (childId, element) in source.ExtraData)
		{
			if (string.Equals(childId, "name", StringComparison.OrdinalIgnoreCase) ||
			    string.Equals(childId, "icon", StringComparison.OrdinalIgnoreCase) ||
			    string.Equals(childId, "mobs", StringComparison.OrdinalIgnoreCase) ||
			    string.Equals(childId, "hasSubcategories", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var childSource = element.Deserialize<BestiaryCategorySource>(context.SerializerOptions);
			if (childSource is null)
			{
				continue;
			}

			subcategories[childId] = ParseCategory(childId, rootCategoryId, childId, childSource, mobIndex, context);
		}

		return new SkyblockBestiaryCategory
		{
			BestiaryId = categoryId,
			Name = StaticMetadataParserUtils.CleanText(source.Name),
			Icon = new SkyblockDisplayIcon
			{
				SkullOwner = source.Icon?.SkullOwner,
				Texture = source.Icon?.Texture,
			},
			Mobs = StaticMetadataParserUtils.ToReadOnlyCollection(directMobs),
			Subcategories = StaticMetadataParserUtils.ToReadOnlyDictionary(subcategories),
		};
	}

	private static SkyblockBestiaryMob ParseMob(
		string categoryId,
		string? subcategoryId,
		BestiaryMobSource source,
		Dictionary<string, SkyblockBestiaryMob> mobIndex,
		RepoSectionLoadContext context)
	{
		var mob = new SkyblockBestiaryMob
		{
			CategoryId = categoryId,
			SubcategoryId = subcategoryId,
			Name = StaticMetadataParserUtils.CleanText(source.Name),
			Icon = new SkyblockDisplayIcon
			{
				SkullOwner = source.SkullOwner,
				Texture = source.Texture,
			},
			Cap = source.Cap,
			BracketId = source.Bracket,
			MobIds = StaticMetadataParserUtils.ToReadOnlyCollection(source.MobIds ?? []),
		};

		foreach (var mobId in source.MobIds ?? [])
		{
			mobIndex[mobId] = mob;
		}

		return mob;
	}

	private sealed class BestiaryConstantsSource
	{
		[JsonPropertyName("brackets")]
		public Dictionary<string, List<int>> Brackets { get; init; } = new();

		[JsonExtensionData]
		public Dictionary<string, JsonElement> ExtraData { get; init; } = new(StringComparer.OrdinalIgnoreCase);
	}

	private sealed class BestiaryCategorySource
	{
		[JsonPropertyName("name")]
		public string Name { get; init; } = string.Empty;

		[JsonPropertyName("icon")]
		public BestiaryIconSource? Icon { get; init; }

		[JsonPropertyName("mobs")]
		public List<BestiaryMobSource>? Mobs { get; init; }

		[JsonPropertyName("hasSubcategories")]
		public bool HasSubcategories { get; init; }

		[JsonExtensionData]
		public Dictionary<string, JsonElement> ExtraData { get; init; } = new(StringComparer.OrdinalIgnoreCase);
	}

	private sealed class BestiaryIconSource
	{
		[JsonPropertyName("skullOwner")]
		public string? SkullOwner { get; init; }

		[JsonPropertyName("texture")]
		public string? Texture { get; init; }
	}

	private sealed class BestiaryMobSource
	{
		[JsonPropertyName("name")]
		public string Name { get; init; } = string.Empty;

		[JsonPropertyName("skullOwner")]
		public string? SkullOwner { get; init; }

		[JsonPropertyName("texture")]
		public string? Texture { get; init; }

		[JsonPropertyName("cap")]
		public int Cap { get; init; }

		[JsonPropertyName("mobs")]
		public List<string>? MobIds { get; init; }

		[JsonPropertyName("bracket")]
		public int Bracket { get; init; }
	}
}

internal sealed class AttributeShardsSectionParser : IRepoSectionParser
{
	public string Name => "AttributeShards";
	public RepoSectionParserStage Stage => RepoSectionParserStage.ExtendedMetadata;

	public async Task ApplyAsync(RepoSectionLoadContext context, CancellationToken cancellationToken)
	{
		if (!context.HasNeuRepo)
		{
			return;
		}

		var source = await context.ReadNeuConstantAsync<AttributeShardConstantsSource>("attribute_shards.json", cancellationToken);
		if (source is null)
		{
			return;
		}

		var byInternalId = new Dictionary<string, SkyblockAttributeShardDefinition>(StringComparer.OrdinalIgnoreCase);
		var byShardId = new Dictionary<string, SkyblockAttributeShardDefinition>(StringComparer.OrdinalIgnoreCase);
		var byBazaarId = new Dictionary<string, SkyblockAttributeShardDefinition>(StringComparer.OrdinalIgnoreCase);

		foreach (var shard in source.Attributes)
		{
			var stackId = shard.InternalName
				.Replace("ATTRIBUTE_SHARD_", string.Empty, StringComparison.OrdinalIgnoreCase)
				.Replace(";1", string.Empty, StringComparison.OrdinalIgnoreCase);

			var ownedId = shard.BazaarName.Replace("SHARD_", string.Empty, StringComparison.OrdinalIgnoreCase);

			context.Data.NeuItems.TryGetValue(shard.InternalName, out var neuItem);
			var extractedTexture = neuItem is null ? null : SkyblockRepoRegexUtils.ExtractSkullTexture(neuItem.NbtTag);

			var definition = new SkyblockAttributeShardDefinition
			{
				InternalId = shard.InternalName,
				StackId = stackId,
				OwnedId = ownedId,
				ShardId = shard.ShardId,
				BazaarId = shard.BazaarName,
				DisplayName = shard.DisplayName,
				AbilityName = shard.AbilityName,
				Rarity = shard.Rarity,
				Alignment = shard.Alignment,
				Family = StaticMetadataParserUtils.ToReadOnlyCollection(shard.Family ?? []),
				Icon = extractedTexture is not null
					? StaticMetadataParserUtils.BuildTextureIcon(extractedTexture.Value)
					: StaticMetadataParserUtils.BuildItemIcon(shard.InternalName),
				Lore = StaticMetadataParserUtils.ToReadOnlyCollection((neuItem?.Lore ?? []).Select(s => StaticMetadataParserUtils.CleanText(s))),
			};

			byInternalId[definition.InternalId] = definition;
			byShardId[definition.ShardId] = definition;
			byBazaarId[definition.BazaarId] = definition;
		}

		context.Data.AttributeShards = new SkyblockAttributeShardsData
		{
			ByInternalId = StaticMetadataParserUtils.ToReadOnlyDictionary(byInternalId),
			ByShardId = StaticMetadataParserUtils.ToReadOnlyDictionary(byShardId),
			ByBazaarId = StaticMetadataParserUtils.ToReadOnlyDictionary(byBazaarId),
			LevellingByRarity = StaticMetadataParserUtils.ToReadOnlyDictionary(
				source.AttributeLevelling.Select(entry =>
					new KeyValuePair<string, ReadOnlyCollection<int>>(entry.Key, StaticMetadataParserUtils.ToReadOnlyCollection(entry.Value)))),
			UnconsumableIds = StaticMetadataParserUtils.ToReadOnlyCollection(source.UnconsumableAttributes ?? []),
		};
	}

	private sealed class AttributeShardConstantsSource
	{
		[JsonPropertyName("attribute_levelling")]
		public Dictionary<string, List<int>> AttributeLevelling { get; init; } = new(StringComparer.OrdinalIgnoreCase);

		[JsonPropertyName("unconsumable_attributes")]
		public List<string>? UnconsumableAttributes { get; init; }

		[JsonPropertyName("attributes")]
		public List<AttributeShardSource> Attributes { get; init; } = [];
	}

	private sealed class AttributeShardSource
	{
		[JsonPropertyName("bazaarName")]
		public string BazaarName { get; init; } = string.Empty;

		[JsonPropertyName("displayName")]
		public string DisplayName { get; init; } = string.Empty;

		[JsonPropertyName("rarity")]
		public string Rarity { get; init; } = string.Empty;

		[JsonPropertyName("internalName")]
		public string InternalName { get; init; } = string.Empty;

		[JsonPropertyName("abilityName")]
		public string AbilityName { get; init; } = string.Empty;

		[JsonPropertyName("alignment")]
		public string? Alignment { get; init; }

		[JsonPropertyName("family")]
		public List<string>? Family { get; init; }

		[JsonPropertyName("shardId")]
		public string ShardId { get; init; } = string.Empty;
	}
}

internal sealed class RiftSectionParser : IRepoSectionParser
{
	public string Name => "Rift";
	public RepoSectionParserStage Stage => RepoSectionParserStage.ExtendedMetadata;

	public async Task ApplyAsync(RepoSectionLoadContext context, CancellationToken cancellationToken)
	{
		if (!context.HasNeuRepo)
		{
			return;
		}

		var guide = await context.ReadNeuConstantAsync<Dictionary<string, List<RiftGuideTaskSource>>>("rift_guide.json", cancellationToken);
		if (guide is null)
		{
			return;
		}

		var guideData = guide ?? new Dictionary<string, List<RiftGuideTaskSource>>(StringComparer.OrdinalIgnoreCase);
		var areas = new Dictionary<string, SkyblockRiftGuideArea>(StringComparer.OrdinalIgnoreCase);
		foreach (var (areaId, tasks) in guideData)
		{
			areas[areaId] = new SkyblockRiftGuideArea
			{
				AreaId = areaId,
				Name = StaticMetadataParserUtils.TitleCaseId(areaId),
				Tasks = StaticMetadataParserUtils.ToReadOnlyCollection(tasks.Select(ParseTask)),
			};
		}

		var timecharms = context.Data.NeuItems
			.Where(entry =>
				entry.Key.StartsWith("RIFT_TROPHY_", StringComparison.OrdinalIgnoreCase) ||
				(entry.Value.Lore ?? []).Any(line => StaticMetadataParserUtils.CleanText(line).Contains("TIMECHARM", StringComparison.OrdinalIgnoreCase)))
			.ToDictionary(
			entry => entry.Key.Replace("RIFT_TROPHY_", string.Empty, StringComparison.OrdinalIgnoreCase).ToLowerInvariant(),
			entry => new SkyblockRiftUnlockDefinition
			{
				Id = entry.Key.Replace("RIFT_TROPHY_", string.Empty, StringComparison.OrdinalIgnoreCase).ToLowerInvariant(),
				Name = StaticMetadataParserUtils.CleanText(entry.Value.DisplayName),
				Icon = StaticMetadataParserUtils.GetItemIcon(context, entry.Key),
			},
			StringComparer.OrdinalIgnoreCase);

		var eyes = guideData.Values
			.SelectMany(tasks => FlattenTasks(tasks))
			.Where(task => task.Id is not null && task.Id.StartsWith("rift_eye_", StringComparison.OrdinalIgnoreCase))
			.ToDictionary(
			task => task.Id!,
			task => new SkyblockRiftUnlockDefinition
			{
				Id = task.Id!,
				Name = StaticMetadataParserUtils.CleanText(task.Name),
				Icon = new SkyblockDisplayIcon(),
			},
			StringComparer.OrdinalIgnoreCase);

		var enigmaSoulCount = guideData.Values
			.SelectMany(tasks => FlattenTasks(tasks))
			.Count(task => task.Id?.StartsWith("rift_enigma_", StringComparison.OrdinalIgnoreCase) == true);

		context.Data.Rift = new SkyblockRiftData
		{
			Areas = StaticMetadataParserUtils.ToReadOnlyDictionary(areas),
			Timecharms = StaticMetadataParserUtils.ToReadOnlyDictionary(timecharms),
			Eyes = StaticMetadataParserUtils.ToReadOnlyDictionary(eyes),
			EnigmaSoulCount = enigmaSoulCount,
			MaxGrubberStacks = ExtractMaxGrubberStacks(StaticMetadataParserUtils.GetNeuItem(context, "MCGRUBBER_BURGER")?.Lore),
		};
	}

	private static SkyblockRiftGuideTask ParseTask(RiftGuideTaskSource task)
	{
		return new SkyblockRiftGuideTask
		{
			TaskId = task.Id,
			Name = StaticMetadataParserUtils.CleanText(task.Name),
			Description = StaticMetadataParserUtils.CleanText(task.Description),
			WikiUrl = task.Wiki,
			Tasks = StaticMetadataParserUtils.ToReadOnlyCollection((task.Tasks ?? []).Select(ParseTask)),
		};
	}

	private static IEnumerable<RiftGuideTaskSource> FlattenTasks(IEnumerable<RiftGuideTaskSource> tasks)
	{
		foreach (var task in tasks)
		{
			yield return task;

			if (task.Tasks is null)
			{
				continue;
			}

			foreach (var child in FlattenTasks(task.Tasks))
			{
				yield return child;
			}
		}
	}

	private static int ExtractMaxGrubberStacks(IEnumerable<string>? lore)
	{
		foreach (var line in lore ?? [])
		{
			var cleaned = StaticMetadataParserUtils.CleanText(line);
			if (!cleaned.Contains("Max", StringComparison.OrdinalIgnoreCase) ||
			    !cleaned.Contains("stacks", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var digits = new string(cleaned.Where(char.IsDigit).ToArray());
			if (int.TryParse(digits, out var value))
			{
				return value;
			}
		}

		return 0;
	}

	private sealed class RiftGuideTaskSource
	{
		[JsonPropertyName("id")]
		public string? Id { get; init; }

		[JsonPropertyName("name")]
		public string Name { get; init; } = string.Empty;

		[JsonPropertyName("description")]
		public string Description { get; init; } = string.Empty;

		[JsonPropertyName("wiki")]
		public string? Wiki { get; init; }

		[JsonPropertyName("tasks")]
		public List<RiftGuideTaskSource>? Tasks { get; init; }
	}
}

internal sealed class EssencePerksSectionParser : IRepoSectionParser
{
	public string Name => "EssencePerks";
	public RepoSectionParserStage Stage => RepoSectionParserStage.ExtendedMetadata;

	public async Task ApplyAsync(RepoSectionLoadContext context, CancellationToken cancellationToken)
	{
		if (!context.HasNeuRepo)
		{
			return;
		}

		var source = await context.ReadNeuConstantAsync<Dictionary<string, Dictionary<string, EssencePerkSource>>>("essenceshops.json", cancellationToken);
		if (source is null)
		{
			return;
		}

		var byCategory = new Dictionary<string, SkyblockEssenceCategory>(StringComparer.OrdinalIgnoreCase);
		var byPerkId = new Dictionary<string, SkyblockEssencePerkDefinition>(StringComparer.OrdinalIgnoreCase);

		foreach (var (essenceId, perks) in source)
		{
			var perkModels = perks.Select(entry =>
			{
				var definition = new SkyblockEssencePerkDefinition
				{
					PerkId = entry.Key,
					EssenceId = essenceId,
					Name = StaticMetadataParserUtils.CleanText(entry.Value.Name),
					Costs = StaticMetadataParserUtils.ToReadOnlyCollection(entry.Value.Costs ?? []),
					MaxLevel = entry.Value.Costs?.Count ?? 0,
				};

				byPerkId[definition.PerkId] = definition;
				return definition;
			}).ToArray();

			byCategory[essenceId] = new SkyblockEssenceCategory
			{
				EssenceId = essenceId,
				Name = StaticMetadataParserUtils.TitleCaseId(essenceId.Replace("ESSENCE_", string.Empty, StringComparison.OrdinalIgnoreCase)),
				Perks = StaticMetadataParserUtils.ToReadOnlyCollection(perkModels),
			};
		}

		context.Data.EssencePerks = new SkyblockEssencePerksData
		{
			Categories = StaticMetadataParserUtils.ToReadOnlyDictionary(byCategory),
			ByPerkId = StaticMetadataParserUtils.ToReadOnlyDictionary(byPerkId),
		};
	}

	private sealed class EssencePerkSource
	{
		[JsonPropertyName("costs")]
		public List<int>? Costs { get; init; }

		[JsonPropertyName("name")]
		public string Name { get; init; } = string.Empty;
	}
}

internal sealed class GearSectionParser : IRepoSectionParser
{
	public string Name => "Gear";
	public RepoSectionParserStage Stage => RepoSectionParserStage.ExtendedMetadata;

	public Task ApplyAsync(RepoSectionLoadContext context, CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}
}
