using RepoAPI.Features.Wiki.Templates.ItemTemplate;

namespace RepoAPI.Tests.Features.Wiki.Templates;

public class ItemTemplateParserTests
{
	[Fact]
	public void Parse_WithItemWikitext_ReturnsPopulatedDto()
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
    
		// Verify properties are no longer cut off
		result.Image.ShouldBe("[[File:SkyBlock_items_mutant_nether_stalk.png|{{{is|25}}}px|link=Mutant Nether Wart]]");
		result.RecipeTree.ShouldBe("{{Recipe Tree/MUTANT_NETHER_STALK|first}}");
		result.CraftingRequirements.ShouldBe("""
		{{#switch: {{{2|}}}
		 | Short = [[File:Minecraft_items_nether_wart.png|25px|link=Nether Wart#Collection]] [[Nether Wart#Collection|Nether Wart XI]]
		 | [[File:Minecraft_items_nether_wart.png|25px|link=Nether Wart#Collection]] [[Nether Wart#Collection|Nether Wart Collection XI]]
		}}
		""".Replace("\r\n", "\n"));
	}
}