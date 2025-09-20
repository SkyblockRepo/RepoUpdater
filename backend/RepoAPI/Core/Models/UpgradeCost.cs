using SkyblockRepo.Models;

namespace RepoAPI.Core.Models;

public class ShopInputOutput
{
	public List<UpgradeCost> Cost { get; set; } = [];
	public List<UpgradeCost> Output { get; set; } = [];
}