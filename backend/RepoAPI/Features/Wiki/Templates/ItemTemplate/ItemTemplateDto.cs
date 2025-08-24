using System.Text.Json.Serialization;

namespace RepoAPI.Features.Wiki.Templates.ItemTemplate;

/// <summary>
/// Represents the parsed data from a Hypixel SkyBlock Item template.
/// </summary>
public class ItemTemplateDto
{
	/// <summary>Displays the item's name.</summary>
	public string? Name { get; set; }

	/// <summary>Displays the item's Rift name.</summary>
	public string? RiftName { get; set; }

	/// <summary>Displays the item's internal id.</summary>
	public string? InternalId { get; set; }

	/// <summary>Displays the item's image.</summary>
	public string? Image { get; set; }

	/// <summary>Displays the item's image at 160px.</summary>
	public string? InfoboxImage { get; set; }

	/// <summary>Displays the item's reference (Name + Image linked).</summary>
	public string? Ref { get; set; }

	/// <summary>Displays the item's collection reference including the word Collection.</summary>
	public string? CollectionReference { get; set; }

	/// <summary>Displays the item's collection reference excluding the word Collection.</summary>
	public string? CollectionReferenceB { get; set; }

	/// <summary>Displays the total number of columns needed to display this item's lore.</summary>
	public string? ItemLoreColumns { get; set; }

	/// <summary>Displays the item's lore template.</summary>
	public string? Lore { get; set; }

	/// <summary>Displays the item's second lore template.</summary>
	public string? Lore2 { get; set; }

	/// <summary>Displays the item's category.</summary>
	public string? Category { get; set; }

	/// <summary>Displays the item's second category.</summary>
	public string? CategoryB { get; set; }

	/// <summary>Displays the item's tier.</summary>
	public string? Tier { get; set; }

	/// <summary>Displays the item's crafting requirements.</summary>
	public string? CraftingRequirements { get; set; }

	/// <summary>Displays how many Coins an item will sell for to an NPC.</summary>
	public string? Value { get; set; }

	/// <summary>Displays how many Motes an item will sell for to Motes_Grubber.</summary>
	public string? MotesValue { get; set; }

	/// <summary>Displays the item's Power.</summary>
	public string? Power { get; set; }

	/// <summary>Displays the item's Stats.</summary>
	public string? Stats { get; set; }

	/// <summary>Displays the item's Rift Stats.</summary>
	public string? RiftStats { get; set; }

	/// <summary>Displays the item's Ability Stats.</summary>
	public string? AbilityStats { get; set; }

	/// <summary>Displays the requirements to use the item.</summary>
	public string? Requirements { get; set; }

	/// <summary>Displays the Essence used to upgrade the item.</summary>
	public string? Essence { get; set; }

	/// <summary>Displays the amount of Essence needed to convert the item to a Dungeon item.</summary>
	public string? EssenceCost { get; set; }

	/// <summary>Displays the requirements needed to "use" the item in Dungeons.</summary>
	public string? DungeonRequirements { get; set; }

	/// <summary>Displays the item's Gemstone slots.</summary>
	public string? Gemslots { get; set; }

	/// <summary>Displays whether the item can be traded. (Yes or No)</summary>
	public string? Tradable { get; set; }

	/// <summary>Displays whether the item can be put up for an Auction. (Yes or No)</summary>
	public string? Auctionable { get; set; }

	/// <summary>Displays whether the item can be Reforged. (Yes or No)</summary>
	public string? Reforgeable { get; set; }

	/// <summary>Displays whether the item can be Enchanted. (Yes or No)</summary>
	public string? Enchantable { get; set; }

	/// <summary>Displays whether the item can be added to the Museum. (Yes or No)</summary>
	public string? Museumable { get; set; }

	/// <summary>Displays whether the item can be sold and purchased on the Bazaar. (Yes or No)</summary>
	public string? Bazaarable { get; set; }

	/// <summary>Displays the item's Salvage data.</summary>
	public string? Salvageable { get; set; }

	/// <summary>Displays whether the item is Soulbound. (Yes or No)</summary>
	public string? Soulboundable { get; set; }

	/// <summary>Displays the item's Soulbound type.</summary>
	public string? SoulboundType { get; set; }

	/// <summary>Displays whether the item is a Rift Item. (Yes or No)</summary>
	public string? RiftItem { get; set; }

	/// <summary>Displays whether the item can be transferred in or out the Rift. (Yes or No)</summary>
	public string? RiftTransferrable { get; set; }

	/// <summary>Displays the item's color.</summary>
	public string? ItemColor { get; set; }

	/// <summary>Displays the Reforge Stone's reforge name.</summary>
	public string? Reforge { get; set; }

	/// <summary>Displays the Reforge Stone's reforge type.</summary>
	public string? ReforgeType { get; set; }

	/// <summary>Displays the Reforge Stone's reforge skill requirement.</summary>
	public string? ReforgeRequirements { get; set; }

	/// <summary>Displays the item's Collection.</summary>
	public string? Collection { get; set; }

	/// <summary>Displays the item's Collection menu.</summary>
	public string? CollectionMenu { get; set; }

	/// <summary>Displays the item's Skill XP gained from Minions.</summary>
	public string? SkillXp { get; set; }

	/// <summary>Displays the total amount of items needed to craft or forge this item.</summary>
	public string? RawMaterials { get; set; }

	/// <summary>Displays the item's recipe tree template.</summary>
	public string? RecipeTree { get; set; }

	/// <summary>Displays the item's upgrading template.</summary>
	public string? Upgrading { get; set; }

	/// <summary>Displays the item's scaled stats.</summary>
	public string? ScaledStats { get; set; }

	/// <summary>Displays the item's Essence upgrading table.</summary>
	public string? EssenceUpgrading { get; set; }

	/// <summary>Displays the item's Skins.</summary>
	public string? Skins { get; set; }

	/// <summary>Displays the sources this item is obtained from.</summary>
	public string? Sources { get; set; }
}