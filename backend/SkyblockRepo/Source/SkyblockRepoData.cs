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

	public TaylorCollection TaylorCollection { get; internal set; } = new();
	public TaylorCollection SeasonalBundles { get; internal set; } = new();
	
	// NEU Data
	public ReadOnlyDictionary<string, NeuItemData> NeuItems { get; set; } = new(new Dictionary<string, NeuItemData>());
}