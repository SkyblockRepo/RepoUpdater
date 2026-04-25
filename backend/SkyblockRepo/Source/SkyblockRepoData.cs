using System.Collections.ObjectModel;
using SkyblockRepo.Models;
using SkyblockRepo.Models.Misc;
using SkyblockRepo.Models.Neu;

namespace SkyblockRepo;

public class SkyblockRepoData
{
	public Manifest? Manifest { get; set; }
	public ReadOnlyDictionary<string, SkyblockItemData> Items { get; set; } = new(new Dictionary<string, SkyblockItemData>());
	public ReadOnlyDictionary<string, SkyblockItemNameSearch> ItemNameSearch { get; set; } = new(new Dictionary<string, SkyblockItemNameSearch>());
	public ReadOnlyDictionary<string, SkyblockPetData> Pets { get; set; } = new(new Dictionary<string, SkyblockPetData>());
	public ReadOnlyDictionary<string, SkyblockEnchantmentData> Enchantments { get; set; } = new(new Dictionary<string, SkyblockEnchantmentData>());
	public ReadOnlyDictionary<string, SkyblockNpcData> Npcs { get; set; } = new(new Dictionary<string, SkyblockNpcData>());
	public ReadOnlyDictionary<string, SkyblockShopData> Shops { get; set; } = new(new Dictionary<string, SkyblockShopData>());
	public ReadOnlyDictionary<string, SkyblockZoneData> Zones { get; set; } = new(new Dictionary<string, SkyblockZoneData>());

	public TaylorCollection TaylorCollection { get; internal set; } = new();
	public TaylorCollection SeasonalBundles { get; internal set; } = new();
	
	// NEU Data
	public ReadOnlyDictionary<string, NeuItemData> NeuItems { get; set; } = new(new Dictionary<string, NeuItemData>());

	/// <summary>
	/// Static bestiary reference data loaded from NEU constants and package-owned supplemental metadata.
	/// </summary>
	public SkyblockBestiaryData Bestiary { get; internal set; } = new();

	/// <summary>
	/// Static accessory and magical power reference data loaded from NEU constants and package-owned supplemental metadata.
	/// </summary>
	public SkyblockAccessoriesData Accessories { get; internal set; } = new();

	/// <summary>
	/// Static attribute shard reference data loaded from NEU constants.
	/// </summary>
	public SkyblockAttributeShardsData AttributeShards { get; internal set; } = new();

	/// <summary>
	/// Static Garden reference data loaded from NEU constants and package-owned supplemental metadata.
	/// </summary>
	public SkyblockGardenData Garden { get; internal set; } = new();

	/// <summary>
	/// Static fishing reference data loaded from NEU constants and package-owned supplemental metadata.
	/// </summary>
	public SkyblockFishingData Fishing { get; internal set; } = new();

	/// <summary>
	/// Static collection reference data loaded from the cached Hypixel collections resource plus package-owned supplemental metadata.
	/// </summary>
	public SkyblockCollectionsData Collections { get; internal set; } = new();

	/// <summary>
	/// Static minion reference data loaded from NEU constants and package-owned supplemental metadata.
	/// </summary>
	public SkyblockMinionsData Minions { get; internal set; } = new();

	/// <summary>
	/// Static pet progression and score reference data loaded from NEU constants and package-owned supplemental metadata.
	/// </summary>
	public SkyblockPetCatalogData PetCatalog { get; internal set; } = new();

	/// <summary>
	/// Static Rift guide reference data loaded from NEU constants and package-owned supplemental metadata.
	/// </summary>
	public SkyblockRiftData Rift { get; internal set; } = new();

	/// <summary>
	/// Static essence shop reference data loaded from NEU constants.
	/// </summary>
	public SkyblockEssencePerksData EssencePerks { get; internal set; } = new();

	/// <summary>
	/// Curated static gear groupings for farming, fishing, and mining.
	/// </summary>
	public SkyblockGearData Gear { get; internal set; } = new();
}
