using RepoAPI.Features.Wiki.Templates.ItemTemplate;

namespace RepoAPI.Tests.Features.Wiki.Templates;

public class ItemTemplateParserTests
{
	[Fact]
	public void Parse_WithItemWikitext_ReturnsPopulatedDto()
	{
		var parser = new ItemTemplateParser();
		var mockWikitext = """
		                   {{Item
		                   |name = Mutant Nether Wart
		                   |internal_id = MUTANT_NETHER_STALK
		                   |bazaarable = Yes
		                   |value = 102400
		                   |tier = RARE
		                   |lore = This is a
		                   multi-line lore string.
		                   }}
		                   """;
		
		var result = parser.Parse(mockWikitext);
		
		result.ShouldNotBeNull();
		result.Name.ShouldBe("Mutant Nether Wart");
		result.InternalId.ShouldBe("MUTANT_NETHER_STALK");
		result.Bazaarable.ShouldBe("Yes");
		result.Value.ShouldBe("102400");
		result.Tier.ShouldBe("RARE");
		result.Lore.ShouldBe($"This is a{Environment.NewLine}multi-line lore string.");
		
		result.Auctionable.ShouldBeNull();
	}
}