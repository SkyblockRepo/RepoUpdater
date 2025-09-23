using System.Collections.ObjectModel;
using SkyblockRepo.Models;
using SkyblockRepo.Models.Neu;

namespace SkyblockRepo;

public class SkyblockRepoData
{
	public Manifest? Manifest { get; set; }
	public ReadOnlyDictionary<string, SkyblockItemData> Items { get; set; } = new(new Dictionary<string, SkyblockItemData>());
	public ReadOnlyDictionary<string, SkyblockItemNameSearch> ItemNameSearch { get; set; } = new(new Dictionary<string, SkyblockItemNameSearch>());
	public ReadOnlyDictionary<string, SkyblockPetData> Pets { get; set; } = new(new Dictionary<string, SkyblockPetData>());
	
	// NEU Data
	public ReadOnlyDictionary<string, NeuItemData> NeuItems { get; set; } = new(new Dictionary<string, NeuItemData>());
}