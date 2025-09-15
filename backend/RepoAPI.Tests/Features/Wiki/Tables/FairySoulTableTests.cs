using RepoAPI.Features.Wiki.Templates;

namespace RepoAPI.Tests.Features.Wiki.Tables;

public class FairySoulTableTests
{
	[Fact]
	public void FairySoulTable_ParsesCorrectly()
	{
		var input = """
		            {{{!}} class="wikitable" style="text-align: center;"
		            ! style="width:25px;" rowspan="2" {{!}} No.
		            ! style="width:100px;" rowspan="2" {{!}} Reference
		            ! style="width:120px;" colspan="3" {{!}} Coordinates
		            {{!}}-
		            {{!}} style="width:40px;" {{!}} {{Color|green|X}}
		            {{!}} style="width:40px;" {{!}} {{Color|green|Y}}
		            {{!}} style="width:40px;" {{!}} {{Color|green|Z}}
		            {{!}}-
		            {{!}} 22
		            {{!}} {{Image|SkyBlock_location_fairysoul_hub_22.png|100px}}
		            {{!}} -187
		            {{!}} 92
		            {{!}} -104
		            {{!}}-
		            {{!}} 53
		            {{!}} {{Image|SkyBlock_location_fairysoul_hub_53.png|100px}}
		            {{!}} -94
		            {{!}} 72
		            {{!}} -129
		            {{!}}-
		            {{!}}}
		            """;

		var table = WikiTableParser.Parse(input);

		table.Rows.Count.ShouldBe(2);

		table.Rows[0]["No."].ShouldBe("22");
		table.Rows[0]["Reference"].ShouldBe("{{Image|SkyBlock_location_fairysoul_hub_22.png|100px}}");
		table.Rows[0]["X"].ShouldBe("-187");
		table.Rows[0]["Y"].ShouldBe("92");
		table.Rows[0]["Z"].ShouldBe("-104");

		table.Rows[1]["No."].ShouldBe("53");
		table.Rows[1]["Reference"].ShouldBe("{{Image|SkyBlock_location_fairysoul_hub_53.png|100px}}");
		table.Rows[1]["X"].ShouldBe("-94");
		table.Rows[1]["Y"].ShouldBe("72");
		table.Rows[1]["Z"].ShouldBe("-129");
	}
}