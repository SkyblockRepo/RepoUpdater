using System.Text.RegularExpressions;
using RepoAPI.Core.Models;
using SkyblockRepo;
using SkyblockRepo.Models;

namespace RepoAPI.Features.Wiki.Templates;


public static partial class ParserUtils
{
	/// <summary>
	/// Turn a wikitext string representing upgrade costs into a list of UpgradeCost objects.
	/// {{Item/SAND:1|lore}}\n\n&7Cost\n&620 Coins
	/// [ { "type": UpgradeCostType.Coins, "amount": 20 } ]
	/// </summary>
	/// <param name="parsedLore"></param>
	/// <param name="outputCount"></param>
	/// <returns></returns>
	public static ShopInputOutput ParseUpgradeCost(string parsedLore, int outputCount = 1)
	{
		var result = new ShopInputOutput();
		var costs = result.Cost;
		if (string.IsNullOrWhiteSpace(parsedLore)) return result;

		// Remove any item links or other templates
		parsedLore = BasicCleanWikitext(parsedLore);
		
		// Find the "Cost" section
		var costSectionMatch = CostsSectionRegex().Match(parsedLore);
		if (!costSectionMatch.Success) return result;
		
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
					var item = SkyblockRepoClient.Instance.FindItem(itemName);
					costs.Add(UpgradeCost.ItemCost(item?.InternalId ?? itemName, quantity));
				}
			}
		}
		
		
		// Check for what item is being sold, if any
		var linesBeforeCost = parsedLore.Substring(0, costSectionMatch.Index);
		var itemMatch = ItemTemplateNameRegex().Match(linesBeforeCost);
		if (itemMatch.Success)
		{
			var itemName = itemMatch.Groups[1].Value;
			var item = SkyblockRepoClient.Instance.FindItem(itemName);
			result.Output.Add(UpgradeCost.ItemCost(item?.InternalId ?? itemName, outputCount));
		}
		
		return result;
	}
	
	/// <summary>
	/// Regex to remove color codes from Minecraft text.
	/// </summary>
	[GeneratedRegex("&[0-9A-Fa-fK-Ok-oRr]")]
	public static partial Regex RemoveColorCodesRegex();
	
    [GeneratedRegex(@"Cost\n\s*([\S\s]*?)(?=\n\n|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline, "en-US")]
    private static partial Regex CostsSectionRegex();
    
    [GeneratedRegex(@"\{\{Item[_/](.*?)[\|\}]", RegexOptions.IgnoreCase | RegexOptions.Singleline, "en-US")]
    private static partial Regex ItemTemplateNameRegex();
    
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