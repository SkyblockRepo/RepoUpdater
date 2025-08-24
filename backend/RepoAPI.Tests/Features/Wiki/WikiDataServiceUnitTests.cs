using NSubstitute;
using RepoAPI.Features.Wiki.Responses;
using RepoAPI.Features.Wiki.Services;
using RepoAPI.Features.Wiki.Templates.ItemTemplate;
using RepoAPI.Features.Wiki.Templates.RecipeTemplate;

namespace RepoAPI.Tests.Features.Wiki;

public class WikiDataServiceUnitTests
{
	[Fact]
    public async Task GetRecipeAsync_WithValidItemId_ReturnsParsedRecipe()
    {
        var itemId = "MUTANT_NETHER_STALK";
        var templateTitle = $"Template:Recipe/{itemId}";
        var fakeApiResponse = new WikiApiResponse
        {
            Query = new Query
            {
                Pages = new Dictionary<string, Page>
                {
                    { "59272", new Page
                        {
                            Pageid = 59272, Ns = 10, Title = templateTitle,
                            Revisions =
                            [
                                new Revision
                                {
                                    Slots = new Slots
                                    {
                                        Main = new MainSlot
                                        {
                                            Content = """
                                                        <noinclude>[[Category:DataRecipe]]{{Recipe/doc|MUTANT_NETHER_STALK}}</noinclude><includeonly>{{Recipe|{{{1|first}}}\\n|first = {{Craft Item\\n|in2 = {{Item/ENCHANTED_NETHER_STALK|lore}},32\\n|in4 = {{Item/ENCHANTED_NETHER_STALK|lore}},32\\n|in5 = {{Item/ENCHANTED_NETHER_STALK|lore}},32\\n|in6 = {{Item/ENCHANTED_NETHER_STALK|lore}},32\\n|in8 = {{Item/ENCHANTED_NETHER_STALK|lore}},32\\n|out = {{Item/MUTANT_NETHER_STALK|lore}}\\n}}\\n|second = {{Craft Item\\n|in1 = {{Item/ENCHANTED_NETHER_STALK|lore}},32\\n|in2 = {{Item/ENCHANTED_NETHER_STALK|lore}},32\\n|in3 = {{Item/ENCHANTED_NETHER_STALK|lore}},32\\n|in4 = {{Item/ENCHANTED_NETHER_STALK|lore}},32\\n|in5 = {{Item/ENCHANTED_NETHER_STALK|lore}},32\\n|out = {{Item/MUTANT_NETHER_STALK|lore}}\\n}}\\n}}</includeonly>
                                                      """
                                        }
                                    }
                                }
                            ]
                        }
                    }
                }
            }
        };

        var mockWikiApi = Substitute.For<IWikiApi>();

        mockWikiApi
            .GetTemplateContentAsync(templateTitle)
            .Returns(fakeApiResponse);
        
        var realParser = new RecipeTemplateParser();
        var wikiDataService = new WikiDataService(mockWikiApi, realParser, new ItemTemplateParser());
        
        var result = await wikiDataService.GetRecipeData(itemId);
        
        result.ShouldNotBeNull();
        
        // Assert that two distinct recipes were found
        result.Recipes.Count.ShouldBe(2);

        // Assert the "first" recipe
        var firstRecipe = result.Recipes.FirstOrDefault(r => r.Name == "first");
        firstRecipe.ShouldNotBeNull();
        firstRecipe.Result.ShouldNotBeNull();
        firstRecipe.Result.ItemId.ShouldBe(itemId);
        firstRecipe.Result.Quantity.ShouldBe(1);
        firstRecipe.Ingredients.Count.ShouldBe(5);
        firstRecipe.Ingredients.ShouldContain(i => i.Slot == "in2" && i.Quantity == 32 && i.ItemId == "ENCHANTED_NETHER_STALK");
        firstRecipe.Ingredients.ShouldContain(i => i.Slot == "in4" && i.Quantity == 32 && i.ItemId == "ENCHANTED_NETHER_STALK");
        firstRecipe.Ingredients.ShouldContain(i => i.Slot == "in5" && i.Quantity == 32 && i.ItemId == "ENCHANTED_NETHER_STALK");
        firstRecipe.Ingredients.ShouldContain(i => i.Slot == "in6" && i.Quantity == 32 && i.ItemId == "ENCHANTED_NETHER_STALK");
        firstRecipe.Ingredients.ShouldContain(i => i.Slot == "in8" && i.Quantity == 32 && i.ItemId == "ENCHANTED_NETHER_STALK");

        // Assert the "second" recipe
        var secondRecipe = result.Recipes.FirstOrDefault(r => r.Name == "second");
        secondRecipe.ShouldNotBeNull();
        secondRecipe.Result.ShouldNotBeNull();
        secondRecipe.Result.ItemId.ShouldBe(itemId);
        secondRecipe.Result.Quantity.ShouldBe(1);
        secondRecipe.Ingredients.Count.ShouldBe(5);
        secondRecipe.Ingredients.ShouldContain(i => i.Slot == "in1" && i.Quantity == 32 && i.ItemId == "ENCHANTED_NETHER_STALK");
        secondRecipe.Ingredients.ShouldContain(i => i.Slot == "in2" && i.Quantity == 32 && i.ItemId == "ENCHANTED_NETHER_STALK");
        secondRecipe.Ingredients.ShouldContain(i => i.Slot == "in3" && i.Quantity == 32 && i.ItemId == "ENCHANTED_NETHER_STALK");
        secondRecipe.Ingredients.ShouldContain(i => i.Slot == "in4" && i.Quantity == 32 && i.ItemId == "ENCHANTED_NETHER_STALK");
        secondRecipe.Ingredients.ShouldContain(i => i.Slot == "in5" && i.Quantity == 32 && i.ItemId == "ENCHANTED_NETHER_STALK");
    }
}