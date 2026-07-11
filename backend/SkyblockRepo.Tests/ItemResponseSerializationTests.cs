using System.Text.Json;
using SkyblockRepo.Models;

namespace SkyblockRepo.Tests;

public sealed class ItemResponseSerializationTests
{
	[Fact]
	public void ItemModelIsFirstClassInRepoOutput() {
		const string json = """
		                    {
		                      "id": "ARACK",
		                      "item_model": "hypixel_skyblock:item/combat_1/arack"
		                    }
		                    """;

		var item = JsonSerializer.Deserialize<SkyblockItemResponse>(json);

		Assert.NotNull(item);
		Assert.Equal("hypixel_skyblock:item/combat_1/arack", item.ItemModel);
		Assert.False(item.ExtensionData?.ContainsKey("item_model") ?? false);
		Assert.Contains("\"item_model\":\"hypixel_skyblock:item/combat_1/arack\"", JsonSerializer.Serialize(item));
	}
}
