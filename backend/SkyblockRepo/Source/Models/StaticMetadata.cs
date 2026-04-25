using System.Collections.ObjectModel;

namespace SkyblockRepo.Models;

internal static class StaticMetadataDefaults
{
	public static ReadOnlyCollection<T> EmptyList<T>() => Array.AsReadOnly(Array.Empty<T>());

	public static ReadOnlyDictionary<string, T> EmptyDictionary<T>() =>
		new(new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase));

	public static ReadOnlyDictionary<int, T> EmptyIntDictionary<T>() =>
		new(new Dictionary<int, T>());
}

/// <summary>
/// Represents a package-stable display reference for a SkyBlock entity.
/// </summary>
public class SkyblockDisplayIcon
{
	/// <summary>
	/// Gets or sets the item identifier to use when the icon should be rendered from an item instead of a skull texture.
	/// </summary>
	public string? ItemId { get; init; }

	/// <summary>
	/// Gets or sets the skull owner UUID when the icon comes from a player head.
	/// </summary>
	public string? SkullOwner { get; init; }

	/// <summary>
	/// Gets or sets the Base64 skin texture value when the icon comes from a player head.
	/// </summary>
	public string? Texture { get; init; }
}

/// <summary>
/// Static reference data for SkyBlock bestiary categories and mobs.
/// </summary>
public class SkyblockBestiaryData
{
	/// <summary>
	/// Gets the bestiary bracket tables keyed by bracket id.
	/// </summary>
	public ReadOnlyDictionary<int, ReadOnlyCollection<int>> Brackets { get; internal set; } = StaticMetadataDefaults.EmptyIntDictionary<ReadOnlyCollection<int>>();

	/// <summary>
	/// Gets the top-level bestiary categories keyed by bestiary id.
	/// </summary>
	public ReadOnlyDictionary<string, SkyblockBestiaryCategory> ByBestiaryId { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockBestiaryCategory>();

	/// <summary>
	/// Gets the bestiary mob definitions keyed by Hypixel mob id.
	/// </summary>
	public ReadOnlyDictionary<string, SkyblockBestiaryMob> ByMobId { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockBestiaryMob>();

	/// <summary>
	/// Gets a bestiary category by id.
	/// </summary>
	public SkyblockBestiaryCategory? GetCategory(string bestiaryId) => ByBestiaryId.GetValueOrDefault(bestiaryId);

	/// <summary>
	/// Gets a bestiary mob by mob id.
	/// </summary>
	public SkyblockBestiaryMob? GetMob(string mobId) => ByMobId.GetValueOrDefault(mobId);
}

/// <summary>
/// A bestiary category or subcategory.
/// </summary>
public class SkyblockBestiaryCategory
{
	/// <summary>
	/// Gets or sets the category id from the upstream repo.
	/// </summary>
	public string BestiaryId { get; init; } = string.Empty;

	/// <summary>
	/// Gets or sets the human-readable category name.
	/// </summary>
	public string Name { get; init; } = string.Empty;

	/// <summary>
	/// Gets or sets the display icon for the category.
	/// </summary>
	public SkyblockDisplayIcon Icon { get; init; } = new();

	/// <summary>
	/// Gets or sets the bestiary mobs directly under this category.
	/// </summary>
	public ReadOnlyCollection<SkyblockBestiaryMob> Mobs { get; init; } = StaticMetadataDefaults.EmptyList<SkyblockBestiaryMob>();

	/// <summary>
	/// Gets or sets nested bestiary subcategories keyed by subcategory id.
	/// </summary>
	public ReadOnlyDictionary<string, SkyblockBestiaryCategory> Subcategories { get; init; } = StaticMetadataDefaults.EmptyDictionary<SkyblockBestiaryCategory>();
}

/// <summary>
/// A bestiary mob definition.
/// </summary>
public class SkyblockBestiaryMob
{
	/// <summary>
	/// Gets or sets the top-level bestiary category id.
	/// </summary>
	public string CategoryId { get; init; } = string.Empty;

	/// <summary>
	/// Gets or sets the optional subcategory id when the mob belongs to a nested category.
	/// </summary>
	public string? SubcategoryId { get; init; }

	/// <summary>
	/// Gets or sets the mob display name.
	/// </summary>
	public string Name { get; init; } = string.Empty;

	/// <summary>
	/// Gets or sets the mob icon.
	/// </summary>
	public SkyblockDisplayIcon Icon { get; init; } = new();

	/// <summary>
	/// Gets or sets the kill cap for the mob family.
	/// </summary>
	public int Cap { get; init; }

	/// <summary>
	/// Gets or sets the bracket id used for milestone lookups.
	/// </summary>
	public int BracketId { get; init; }

	/// <summary>
	/// Gets or sets the Hypixel mob ids represented by this entry.
	/// </summary>
	public ReadOnlyCollection<string> MobIds { get; init; } = StaticMetadataDefaults.EmptyList<string>();
}

/// <summary>
/// Static reference data for SkyBlock accessories and magical power metadata.
/// </summary>
public class SkyblockAccessoriesData
{
	/// <summary>
	/// Gets the canonical accessory catalog keyed by internal item id.
	/// </summary>
	public ReadOnlyDictionary<string, SkyblockAccessoryDefinition> ByItemId { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockAccessoryDefinition>();

	/// <summary>
	/// Gets alias-to-canonical id mappings.
	/// </summary>
	public ReadOnlyDictionary<string, string> AliasToBaseId { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<string>();

	/// <summary>
	/// Gets magical power values keyed by rarity.
	/// </summary>
	public ReadOnlyDictionary<string, int> MagicalPowerByRarity { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<int>();

	/// <summary>
	/// Gets enrichment key to stat name mappings.
	/// </summary>
	public ReadOnlyDictionary<string, string> EnrichmentKeyToStatName { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<string>();

	/// <summary>
	/// Gets ignored accessory ids.
	/// </summary>
	public ReadOnlyCollection<string> IgnoredIds { get; internal set; } = StaticMetadataDefaults.EmptyList<string>();

	/// <summary>
	/// Gets a canonical accessory definition by id or alias.
	/// </summary>
	public SkyblockAccessoryDefinition? GetAccessory(string itemIdOrAlias)
	{
		var canonicalId = AliasToBaseId.GetValueOrDefault(itemIdOrAlias) ?? itemIdOrAlias;
		return ByItemId.GetValueOrDefault(canonicalId);
	}

	/// <summary>
	/// Gets the magical power for an accessory rarity, honoring accessory-specific overrides when present.
	/// </summary>
	public int GetMagicalPower(string rarity, string? itemId = null)
	{
		if (!string.IsNullOrWhiteSpace(itemId) && ByItemId.TryGetValue(itemId, out var accessory))
		{
			if (accessory.MagicalPowerOverride is int overrideValue)
			{
				return overrideValue;
			}

			if (accessory.MagicalPowerMultiplier is double multiplier)
			{
				return (int)Math.Round((MagicalPowerByRarity.GetValueOrDefault(rarity)) * multiplier, MidpointRounding.AwayFromZero);
			}
		}

		return MagicalPowerByRarity.GetValueOrDefault(rarity);
	}
}

/// <summary>
/// A canonical accessory definition.
/// </summary>
public class SkyblockAccessoryDefinition
{
	public string ItemId { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public string? BaseRarity { get; init; }
	public SkyblockDisplayIcon Icon { get; init; } = new();
	public ReadOnlyCollection<string> Aliases { get; init; } = StaticMetadataDefaults.EmptyList<string>();
	public ReadOnlyCollection<string> UpgradeChain { get; init; } = StaticMetadataDefaults.EmptyList<string>();
	public SkyblockAccessorySpecialCase? SpecialCase { get; init; }
	public int? MagicalPowerOverride { get; init; }
	public double? MagicalPowerMultiplier { get; init; }
}

/// <summary>
/// Special accessory behavior metadata.
/// </summary>
public class SkyblockAccessorySpecialCase
{
	public bool AllowsRecombobulation { get; init; } = true;
	public bool AllowsEnrichment { get; init; } = true;
	public ReadOnlyCollection<string> AlternateRarities { get; init; } = StaticMetadataDefaults.EmptyList<string>();
	public bool UsesCustomPrice { get; init; }
	public SkyblockAccessoryUpgradeCost? UpgradeCost { get; init; }
}

/// <summary>
/// Custom upgrade metadata for an accessory special case.
/// </summary>
public class SkyblockAccessoryUpgradeCost
{
	public string ItemId { get; init; } = string.Empty;
	public ReadOnlyDictionary<string, int> CostsByRarity { get; init; } = StaticMetadataDefaults.EmptyDictionary<int>();
}

/// <summary>
/// Static reference data for attribute shards.
/// </summary>
public class SkyblockAttributeShardsData
{
	public ReadOnlyDictionary<string, SkyblockAttributeShardDefinition> ByInternalId { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockAttributeShardDefinition>();
	public ReadOnlyDictionary<string, SkyblockAttributeShardDefinition> ByShardId { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockAttributeShardDefinition>();
	public ReadOnlyDictionary<string, SkyblockAttributeShardDefinition> ByBazaarId { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<SkyblockAttributeShardDefinition>();
	public ReadOnlyDictionary<string, ReadOnlyCollection<int>> LevellingByRarity { get; internal set; } = StaticMetadataDefaults.EmptyDictionary<ReadOnlyCollection<int>>();
	public ReadOnlyCollection<string> UnconsumableIds { get; internal set; } = StaticMetadataDefaults.EmptyList<string>();

	public SkyblockAttributeShardDefinition? GetByInternalId(string internalId) => ByInternalId.GetValueOrDefault(internalId);
	public SkyblockAttributeShardDefinition? GetByShardId(string shardId) => ByShardId.GetValueOrDefault(shardId);
}

/// <summary>
/// A static attribute shard definition.
/// </summary>
public class SkyblockAttributeShardDefinition
{
	public string InternalId { get; init; } = string.Empty;
	public string StackId { get; init; } = string.Empty;
	public string OwnedId { get; init; } = string.Empty;
	public string ShardId { get; init; } = string.Empty;
	public string BazaarId { get; init; } = string.Empty;
	public string DisplayName { get; init; } = string.Empty;
	public string AbilityName { get; init; } = string.Empty;
	public string Rarity { get; init; } = string.Empty;
	public string? Alignment { get; init; }
	public ReadOnlyCollection<string> Family { get; init; } = StaticMetadataDefaults.EmptyList<string>();
	public SkyblockDisplayIcon Icon { get; init; } = new();
	public ReadOnlyCollection<string> Lore { get; init; } = StaticMetadataDefaults.EmptyList<string>();
}
