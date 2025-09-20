using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Humanizer;
using RepoAPI.Core.Models;

namespace RepoAPI.Features.Wiki.Templates;

public static partial class ParserUtils
{
	/// <summary>
	/// Turn a wikitext string representing upgrade costs into a list of UpgradeCost objects.
	/// {{Item/SAND:1|lore}}\n\n&7Cost\n&620 Coins
	/// [ { "type": UpgradeCostType.Coins, "amount": 20 } ]
	/// </summary>
	/// <param name="wikitext"></param>
	/// <returns></returns>
	public static List<UpgradeCost> ParseUpgradeCost(string wikitext)
	{
		var costs = new List<UpgradeCost>();
		if (string.IsNullOrWhiteSpace(wikitext)) return costs;

		// Remove any item links or other templates
		wikitext = BasicCleanWikitext(wikitext);
		
		// Find the "Cost" section
		var costSectionMatch = CostsSectionRegex().Match(wikitext);
		if (!costSectionMatch.Success) return costs;
		
		var costSection = costSectionMatch.Groups[1].Value;
		// Split the section into lines and parse each line
		var lines = costSection.Split('\n', StringSplitOptions.RemoveEmptyEntries);
		foreach (var line in lines)
		{
			var cleanLine = RemoveColorCodesRegex().Replace(line, "").Trim();
			if (string.IsNullOrWhiteSpace(cleanLine)) continue;
			
			// Check for coins
			var coinsMatch = CoinsRegex().Match(cleanLine);
			if (coinsMatch.Success) {
				var amountStr = coinsMatch.Groups[1].Value.Replace(",", "").Replace(".", "");
				if (!int.TryParse(amountStr, out var amount)) continue;
				costs.Add(UpgradeCost.CoinCost(amount));
				continue;
			}
			
			// Check for item with optional quantity
			var quantityMatch = QuantityRegex().Match(cleanLine);
			var quantity = 1;
			var itemName = cleanLine;
			if (quantityMatch.Success) {
				var qtyStr = quantityMatch.Groups[1].Value;
				if (int.TryParse(qtyStr, out var qty))
				{
					quantity = qty;
					itemName = cleanLine.Substring(0, quantityMatch.Index).Trim();
				}
			}
			
			if (!string.IsNullOrWhiteSpace(itemName)) {
				if (itemName is "Gold medal" or "Silver medal" or "Bronze medal")
				{
					costs.Add(UpgradeCost.JacobMedalCost(itemName.ToLowerInvariant().Replace(" medal", ""), quantity));
				} else if (itemName.EndsWith(" Pelts"))
				{
					var peltCountStr = itemName.Replace(" Pelts", "").Trim();
					if (int.TryParse(peltCountStr, out var peltCount) && peltCount > 0)
					{
						costs.Add(UpgradeCost.PeltCost(peltCount * quantity));
					}
				} else if (itemName.EndsWith(" Motes"))
				{
					var motesCountSr = itemName.Replace(" Motes", "").Trim();
					if (int.TryParse(motesCountSr, out var motesCount) && motesCount > 0)
					{
						costs.Add(UpgradeCost.MoteCost(motesCount * quantity));
					}
				} else {
					var item = SkyblockRepo.SkyblockRepo.Cache.FindItem(itemName);
					costs.Add(UpgradeCost.ItemCost(item?.InternalId ?? itemName, quantity));
				}
			}
		}
		
		return costs;
	}
	
	/// <summary>
	/// Regex to remove color codes from Minecraft text.
	/// </summary>
	[GeneratedRegex("&[0-9A-Fa-fK-Ok-oRr]")]
	public static partial Regex RemoveColorCodesRegex();
	
    [GeneratedRegex(@"Cost\n\s*([\S\s]*?)(?=\n\n|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline, "en-US")]
    private static partial Regex CostsSectionRegex();
    
    /// <summary>
    /// Regex to get quantity from a string like "Item Name x32" or "Item Name 32"
    ///	 </summary>
    [GeneratedRegex(@"\s*x?(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Singleline, "en-US")]
	public static partial Regex QuantityRegex();
    
    /// <summary>
    /// Regex to get coins from a string like "20 Coins" or "1,500 Coins"
    /// </summary>
	[GeneratedRegex(@"([\d,\.]+)\s*Coins?", RegexOptions.IgnoreCase | RegexOptions.Singleline, "en-US")]
	public static partial Regex CoinsRegex();
}