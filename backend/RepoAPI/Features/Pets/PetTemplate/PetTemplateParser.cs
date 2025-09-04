using System.Text.RegularExpressions;
using RepoAPI.Core.Models;
using RepoAPI.Features.Pets.Models;
using RepoAPI.Features.Wiki.Templates;
using TimeSpanParserUtil;

namespace RepoAPI.Features.Pets.PetTemplate;

[RegisterService<ITemplateParser<PetTemplateDto>>(LifeTime.Singleton)]
public partial class PetTemplateParser : ITemplateParser<PetTemplateDto>
{
    [GeneratedRegex(@"\{\{Pet/doc\|(?<id>[A-Z0-9_]+)\}\}", RegexOptions.Compiled)]
    private static partial Regex PetInternalId();

    public PetTemplateDto Parse(string wikitext)
    {
        var dto = new PetTemplateDto();
        
        var internalId = PetInternalId().Match(wikitext).Groups["id"].Value;
        if (!string.IsNullOrEmpty(internalId))
        {
            dto.InternalId = internalId;
        }
        
        wikitext = ParserUtils.ExtractIncludeOnlyContent(wikitext);

        var bodyStartIndex = wikitext.IndexOf('|');
        var bodyEndIndex = wikitext.LastIndexOf("}}", StringComparison.Ordinal);

        if (bodyStartIndex == -1 || bodyEndIndex == -1)
        {
            return dto; // Not a valid template
        }

        var templateBody = wikitext.Substring(bodyStartIndex + 1, bodyEndIndex - (bodyStartIndex + 1));
        
        // Parse the key-value pairs using a brace-counting method.
        var properties = ParserUtils.GetTopLevelProperties(templateBody);

        foreach (var prop in properties)
        {
            var key = prop.Key.Trim().ToLowerInvariant();
            var value = prop.Value.Replace("\\r", "\r").Replace("\\n", "\n").Trim();
            // Map the found key to the correct DTO property
            switch (key)
            {
                case "lore":
                {
                    dto.Lore = ParserUtils.GetPropDictionaryFromSwitch(value)
                        .ToDictionary(x => 
                            x.Key, x => 
                            ParserUtils.GetPropDictionaryFromSwitch(x.Value.Replace("| sbpet", "| max = sbpet"))
                                .ToDictionary(l => l.Key, l => ParserUtils.CleanLoreString(l.Value))
                        );
                    break;
                }
                case "kat": {
                    dto.Kat = value;
                    break;
                }
                case "leveling": {
                    dto.Leveling = value;
                    break;
                }
                case "category": {
                    dto.Category = value;
                    break;
                }
                case "name": {
                    dto.Name = value;
                    break;
                }
                case "basestats": {
                    dto.BaseStats = value.Split(["<br>"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(l => ParserUtils.CleanWikitext(l))
                        .ToList();
                    break;
                }
                default: {
                    dto.AdditionalProperties.TryAdd(key, value);
                    break;
                }
            }
        }

        dto.MaxLevel = dto.Leveling?.Contains("200") is true
            ? 200
            : 100;
        dto.MinLevel = dto.MaxLevel - 99;
        
        var props = dto.AdditionalProperties;

        dto.Flags = new PetFlags()
        {
            Auctionable = props.TryGetValue("auctionable", out var auctionable) &&
                          auctionable.ToString()?.Contains("Yes") is true,
            Mountable = props.TryGetValue("mountable", out var mountable) &&
                        mountable.ToString()?.Contains("Yes") is true,
            Tradable = props.TryGetValue("tradable", out var tradable) && tradable.ToString()?.Contains("Yes") is true,
            Museumable = props.TryGetValue("museumable", out var museumable) &&
                         museumable.ToString()?.Contains("Yes") is true,
        };
        
        var katUpgradeable = !dto.Kat.Contains("can't have its rarity upgraded by");
        
        var katMatch = KatRarityRange().Match(dto.Kat);
        var rarityOrder = new List<string> { "common", "uncommon", "rare", "epic", "legendary", "mythic" };
        var katStartIndex = -1;
        var katEndIndex = -1;
        
        if (katMatch.Success)
        {
            var startRarity = katMatch.Groups["start"].Value.ToLowerInvariant();
            var endRarity = katMatch.Groups["end"].Value.ToLowerInvariant();
            katStartIndex = rarityOrder.IndexOf(startRarity);
            katEndIndex = rarityOrder.IndexOf(endRarity);
        }
        
        foreach (var (rarityKey, lore) in dto.Lore)
        {
            var rarity = rarityKey.ToLowerInvariant();
            
            var coinValue = dto.AdditionalProperties.TryGetValue($"{rarity}_value", out var value) 
                ? ParserUtils.GetNumberFromTemplate(value.ToString())
                : 0;

            var katUpgrade = false;
            if (katUpgradeable && katStartIndex != -1 && katEndIndex != -1)
            {
                var rarityIndex = rarityOrder.IndexOf(rarity);
                if (rarityIndex >= katStartIndex && rarityIndex < katEndIndex)
                {
                    katUpgrade = true;
                }
            }

            dto.PetRarities.TryAdd(rarity.ToUpperInvariant(), new PetRarityDto
            {
                Lore = lore,
                Value = coinValue,
                KatUpgradeable = katUpgrade,
            });
        }
        
        var templateStart = dto.Kat.IndexOf("{{Kat Cost", StringComparison.Ordinal);
        if (templateStart == -1) return dto;
        
        var substring = dto.Kat[templateStart..];
        var data = ParserUtils.GetPropDictionary(substring);
        
        foreach (var (key, val) in data)
        {
            var keyLower = key.ToLowerInvariant();
            if (keyLower.StartsWith("cost_") && keyLower.Contains("_level_"))
            {
                var parts = keyLower.Split('_', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 4 || parts[3] != "1") continue;
                
                var rarity = parts[1].ToUpperInvariant();
                if (!dto.PetRarities.TryGetValue(rarity, out var petRarity)) continue;
                
                // Get coins and items from the value
                var lines = val.Split("<br>", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        
                var coinLine = lines.FirstOrDefault(l => l.Contains("{{Coins|"));
                if (coinLine != null)
                {
                    var coinCost = ParserUtils.GetNumberFromTemplate(coinLine);
                    petRarity.KatUpgradeCosts ??= [];
                    petRarity.KatUpgradeCosts.Add(UpgradeCost.CoinCost((int)coinCost));
                }
                        
                foreach (var itemLine in lines.Where(l => !l.Contains("{{Coins|")) )
                {
                    var itemMatch = KatCostItemRegex().Match(itemLine);
                    if (!itemMatch.Success) continue;
                    
                    var qtyStr = itemMatch.Groups["qty"].Value;
                    var itemStr = itemMatch.Groups["item"].Value;
                    if (!int.TryParse(qtyStr, out var qty)) continue;
                    
                    var itemId = itemStr.Replace(" ", "_").ToUpperInvariant();
                    
                    petRarity.KatUpgradeCosts ??= [];
                    petRarity.KatUpgradeCosts.Add(UpgradeCost.ItemCost(itemId, qty));
                }
            }
            else if (keyLower.StartsWith("time_"))
            {
                var parts = keyLower.Split('_', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2) continue;
                
                var rarity = parts[1].ToUpperInvariant();
                if (dto.PetRarities.TryGetValue(rarity, out var petRarity))
                {
                    if (TimeSpanParser.TryParse(val.ToLowerInvariant(), out var valTs)) {
                        petRarity.KatUpgradeSeconds = (int) valTs.TotalSeconds;
                    } else { 
                        petRarity.KatUpgradeTime = val;
                    }
                }
            }
        }
        
        return dto;
    }

    public string GetTemplate(string input)
    {
        return $"Template:Pet/{input}";
    }
    
    /// <summary>
    /// Extract start and end rarities from the kat string.
    /// from {{Common}} all the way up to {{Mythic}} -> Common, Mythic
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"\{\{(?<start>[A-Za-z]+)\}\} all the way up to \{\{(?<end>[A-Za-z]+)\}\}", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex KatRarityRange();
    
    /// <summary>
    /// Regex to extract item cost lines from the kat cost section.
    /// Example line: 128 [[File:Minecraft_items_raw_chicken.png|25px|link=Raw Chicken
    /// Matches quantity = 128, itemId = Raw Chicken (from link=Raw Chicken)
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"(?<qty>\d+)\s+\[\[File:(?<file>[^|\]]+)(\|[^]]*)?\|link=(?<item>[^\]]+)\]\]", RegexOptions.Compiled)]
    private static partial Regex KatCostItemRegex();
}