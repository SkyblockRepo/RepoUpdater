namespace SkyblockRepo.Models.Misc;

public class TaylorCollection
{
	public List<TaylorCollectionItem> Items { get; set; } = [];
}

public class TaylorCollectionItem
{
	public string Name { get; set; } = string.Empty;
	public List<UpgradeCost> Output { get; set; } = [];
	public List<UpgradeCost> Cost { get; set; } = [];
	public string Released { get; set; } = string.Empty;
}