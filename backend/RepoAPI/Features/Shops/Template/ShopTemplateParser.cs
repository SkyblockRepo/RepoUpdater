using System.Text.RegularExpressions;
using RepoAPI.Features.Wiki.Templates;

namespace RepoAPI.Features.Shops.Template;

[RegisterService<ITemplateParser<ShopTemplateDto>>(LifeTime.Singleton)]
public partial class ShopTemplateParser : ITemplateParser<ShopTemplateDto>
{
    public ShopTemplateDto Parse(string wikitext, string backupId = "")
    {
        var dto = new ShopTemplateDto();
        
        var properties = ParserUtils.GetPropDictionary(wikitext);

        foreach (var prop in properties)
        {
            var key = prop.Key.Trim().ToLowerInvariant();
            var value = prop.Value.Replace("\\r", "\r").Replace("\\n", "\n").Trim();
            // Map the found key to the correct DTO property
            switch (key)
            {
                case "internal_id": {
                    dto.InternalId = value;
                    break;
                }
                case "name": {
                    dto.Name = ParserUtils.CleanWikitext(value);
                    break;
                }
                default: {
                    dto.AdditionalProperties.TryAdd(key, value);
                    break;
                }
            }
        }
        
        var props = dto.AdditionalProperties;

        foreach (var (key, value) in props)
        {
            if (!SlotPropertyRegex().IsMatch(key)) continue;
            
            var parsed = ParserUtils.ParseLoreString(value.ToString() ?? "");
            var cost = ParserUtils.ParseUpgradeCost(parsed.CleanLore ?? "");
            
            dto.Slots[key] = new InventorySlot()
            {
                Lore = parsed.CleanLore,
                Name = parsed.ItemName,
                Material = parsed.Material,
                Cost = cost.Count > 0 ? cost : null
            };
        }

        foreach (var key in dto.Slots.Keys)
        {
            props.Remove(key);
        }
        
        if (dto.InternalId is null && props.TryGetValue("shop_id", out var id)) {
            dto.InternalId = id.ToString();
        }
        
        if (dto.InternalId is null && !string.IsNullOrEmpty(backupId)) {
            dto.InternalId = backupId;
        }

        return dto;
    }

    public string GetTemplate(string input)
    {
        return $"Template:{input}";
    }
    
    /// <summary>
    /// Regex for getting shop id out of template
    /// Input: {{Shop/ExampleShop}}
    /// Output: ExampleShop
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"(\w|item)\d+")]
    private static partial Regex SlotPropertyRegex();
}