namespace SkyblockRepo.Models;

public record struct PetFlags()
{
	public bool Auctionable { get; set; }
	public bool Mountable { get; set; }
	public bool Tradable { get; set; }
	public bool Museumable { get; set; }
}