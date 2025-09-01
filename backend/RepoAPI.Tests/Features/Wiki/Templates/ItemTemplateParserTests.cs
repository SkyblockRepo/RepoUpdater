using Microsoft.Extensions.Logging;
using RepoAPI.Core;
using RepoAPI.Features.Items.Services;
using RepoAPI.Features.Wiki.Services;
using RepoAPI.Features.Wiki.Templates;
using RepoAPI.Features.Wiki.Templates.ItemTemplate;

namespace RepoAPI.Tests.Features.Wiki.Templates;

public class ItemTemplateParserTests
{
	[Fact]
	public async Task Parse_WithItemWikitext_ReturnsPopulatedDto()
	{
		var parser = new ItemTemplateParser();
		var complexWikitext = """
			{{Item|{{{1|ref}}}\n|name = Mutant Nether Wart\n|internal_id = MUTANT_NETHER_STALK\n|infobox_image = [[File:SkyBlock_items_mutant_nether_stalk.png|160px|link=]]\n|image = [[File:SkyBlock_items_mutant_nether_stalk.png|{{{is|25}}}px|link=Mutant Nether Wart]]\n|ref = [[File:SkyBlock_items_mutant_nether_stalk.png|{{{is|25}}}px|link=Mutant Nether Wart]] [[Mutant Nether Wart]]\n|lore = sb,rare,mutant_nether_stalk:Mutant Nether Wart,1,&7&8Collection Item\\n\\n&9'''RARE'''\n|real_lore = sb,rare,mutant_nether_stalk:Mutant Nether Wart,1,&7&8Collection Item\\n\\n&9'''RARE'''\n|category = Item\n|tier = Rare\n|texture = {{#switch: {{{2|}}}\n|link = [http://textures.minecraft.net/texture/77efc2bf3297bade7391dfbd50eff556a4e9d3920d5615be23b22d667e0533 77efc2bf3297bade7391dfbd50eff556a4e9d3920d5615be23b22d667e0533]\n|#default = eyJ0aW1lc3RhbXAiOjE1NzE3NzY3NDAzMzYsInByb2ZpbGVJZCI6ImVkNTNkZDgxNGY5ZDRhM2NiNGViNjUxZGNiYTc3ZTY2IiwicHJvZmlsZU5hbWUiOiJGb3J5eExPTCIsInNpZ25hdHVyZVJlcXVpcmVkIjp0cnVlLCJ0ZXh0dXJlcyI6eyJTS0lOIjp7InVybCI6Imh0dHA6Ly90ZXh0dXJlcy5taW5lY3JhZnQubmV0L3RleHR1cmUvNzdlZmMyYmYzMjk3YmFkZTczOTFkZmJkNTBlZmY1NTZhNGU5ZDM5MjBkNTYxNWJlMjNiMjJkNjY3ZTA1MzMiLCJtZXRhZGF0YSI6eyJtb2RlbCI6InNsaW0ifX19fQ==\n}}\n|value = 102,400\n|crafting_requirements = {{#switch: {{{2|}}}\n | Short = [[File:Minecraft_items_nether_wart.png|25px|link=Nether Wart#Collection]] [[Nether Wart#Collection|Nether Wart XI]]\n | [[File:Minecraft_items_nether_wart.png|25px|link=Nether Wart#Collection]] [[Nether Wart#Collection|Nether Wart Collection XI]]\n}}\n|tradable = Yes\n|auctionable = No\n|reforgeable = No\n|enchantable = No\n|museumable = No\n|bazaarable = Yes\n|soulboundable = No\n|sackable = No\n|rawmaterials =\n*25,600 [[File:Minecraft_items_nether_wart.png|15px|link=Nether Wart]] [[Nether Wart]]\n|recipetree = {{Recipe Tree/MUTANT_NETHER_STALK|first}}\n|recipes = '''Mutant Nether Wart''' can be used to craft the {{Item/FERMENTO_CHESTPLATE}}, {{Item/FERMENTO_LEGGINGS}}, {{Item/THEORETICAL_HOE_WARTS_3}}, and {{Item/AMALGAMATED_CRIMSONITE_NEW}}.\n}}
			""";

		
		var result = parser.Parse(complexWikitext);

		// ASSERT
		result.ShouldNotBeNull();
		result.Name.ShouldBe("Mutant Nether Wart");
		result.InternalId.ShouldBe("MUTANT_NETHER_STALK");
		
		result.Image.ShouldBe("[[File:SkyBlock_items_mutant_nether_stalk.png|{{{is|25}}}px|link=Mutant Nether Wart]]");
		result.RecipeTree?.Raw.ShouldBe("{{Recipe Tree/MUTANT_NETHER_STALK|first}}");
		result.RecipeTree?.ItemId.ShouldBe("MUTANT_NETHER_STALK");
		result.RecipeTree?.RecipeName.ShouldBe("first");
		result.CraftingRequirements.ShouldBe("""
		{{#switch: {{{2|}}}
		 | Short = [[File:Minecraft_items_nether_wart.png|25px|link=Nether Wart#Collection]] [[Nether Wart#Collection|Nether Wart XI]]
		 | [[File:Minecraft_items_nether_wart.png|25px|link=Nether Wart#Collection]] [[Nether Wart#Collection|Nether Wart Collection XI]]
		}}
		""".Replace("\r\n", "\n"));
	}

	[Fact]
	public void LoreParserTest()
	{
		var raw = """
		          mc,legendary,leather_boots_lime:Fermento Boots,1,&7Health: &a+130\\n&7Defense: &a+40\\n&7Speed: &a+5\\n&7Farming Fortune: &a+30\\n&7 &8[&8☘&8] &8[&8☘&8]\\n\\n&8Tiered Bonus: Feast (0/4)\\n&7Combines the Tiered Bonuses of\\n&7wearing &a0 pieces &7of the Melon Armor<nowiki>,</nowiki>\\n&7Cropie Armor<nowiki>,</nowiki> and Squash Armor.\\n&7&7Grants &60☘ Farming Fortune&7.\\n\\n&6Ability: Farmer's Grace \\n&7Grants immunity to trampling crops.\\n\\n&7&8This item can be reforged!\\n&7&4❣ &cRequires &aFarming Skill 40&c.\\n&6'''LEGENDARY BOOTS'''
		          """;
		
		var expected = """
		           &7Health: &a+130
		           &7Defense: &a+40
		           &7Speed: &a+5
		           &7Farming Fortune: &a+30
		           &7 &8[&8☘&8] &8[&8☘&8]
		           
		           &8Tiered Bonus: Feast (0/4)
		           &7Combines the Tiered Bonuses of
		           &7wearing &a0 pieces &7of the Melon Armor,
		           &7Cropie Armor, and Squash Armor.
		           &7&7Grants &60☘ Farming Fortune&7.
		           
		           &6Ability: Farmer's Grace 
		           &7Grants immunity to trampling crops.
		           
		           &7&8This item can be reforged!
		           &7&4❣ &cRequires &aFarming Skill 40&c.
		           &6&lLEGENDARY BOOTS&r
		           """.Replace("\r\n", "\n");
		
		var cleaned = ParserUtils.CleanLoreString(raw);
		
		cleaned.ShouldBe(expected);
	}

	[Fact]
	public void EnchantmentLoreTest()
	{
		var raw = """
		          "{{#switch: {{{2|I}}}\n| I = !mc,common,enchanted_book:Enchanted Book,1,&7&9Smarty Pants I\n&7Grants &b+5✎ Intelligence&7.\n\n&7Applicable on: &9Leggings\n&7Apply Cost: &320 Exp Levels\n\n&7Use this on an item in an Anvil to\n&7apply it!\n\n&f'''COMMON'''\n| II = !mc,common,enchanted_book:Enchanted Book,1,&7&9Smarty Pants II\n&7Grants &b+10✎ Intelligence&7.\n\n&7Applicable on: &9Leggings\n&7Apply Cost: &340 Exp Levels\n\n&7Use this on an item in an Anvil to\n&7apply it!\n\n&f'''COMMON'''\n| III = !mc,common,enchanted_book:Enchanted Book,1,&7&9Smarty Pants III\n&7Grants &b+15✎ Intelligence&7.\n\n&7Applicable on: &9Leggings\n&7Apply Cost: &360 Exp Levels\n\n&7Use this on an item in an Anvil to\n&7apply it!\n\n&f'''COMMON'''\n| IV = !mc,common,enchanted_book:Enchanted Book,1,&7&9Smarty Pants IV\n&7Grants &b+20✎ Intelligence&7.\n\n&7Applicable on: &9Leggings\n&7Apply Cost: &380 Exp Levels\n\n&7Use this on an item in an Anvil to\n&7apply it!\n\n&f'''COMMON'''\n| V = !mc,uncommon,enchanted_book:Enchanted Book,1,&7&9Smarty Pants V\n&7Grants &b+25✎ Intelligence&7.\n\n&7Applicable on: &9Leggings\n&7Apply Cost: &3100 Exp Levels\n\n&7Use this on an item in an Anvil to\n&7apply it!\n\n&a'''UNCOMMON'''\n}}"
		          """;
		
		var properties = ParserUtils.GetPropDictionaryFromSwitch(raw);
		properties.ShouldContainKey("i");
		properties.ShouldContainKey("ii");
		properties.ShouldContainKey("iii");
		properties.ShouldContainKey("iv");
		properties.ShouldContainKey("v");
		
		ParserUtils.CleanLoreString(properties["i"]).ShouldBe(
		"""
		&7&9Smarty Pants I
		&7Grants &b+5✎ Intelligence&7.
		
		&7Applicable on: &9Leggings
		&7Apply Cost: &320 Exp Levels
		
		&7Use this on an item in an Anvil to
		&7apply it!
		
		&f&lCOMMON&r
		""".Replace("\r\n", "\n")
		);
		
		ParserUtils.CleanLoreString(properties["ii"]).ShouldBe(
			"""
			&7&9Smarty Pants II
			&7Grants &b+10✎ Intelligence&7.
			
			&7Applicable on: &9Leggings
			&7Apply Cost: &340 Exp Levels
			
			&7Use this on an item in an Anvil to
			&7apply it!
			
			&f&lCOMMON&r
			""".Replace("\r\n", "\n")
		);
		
		ParserUtils.CleanLoreString(properties["v"]).ShouldBe(
			"""
			&7&9Smarty Pants V
			&7Grants &b+25✎ Intelligence&7.
			
			&7Applicable on: &9Leggings
			&7Apply Cost: &3100 Exp Levels
			
			&7Use this on an item in an Anvil to
			&7apply it!
			
			&a&lUNCOMMON&r
			""".Replace("\r\n", "\n")
		);
	}
}