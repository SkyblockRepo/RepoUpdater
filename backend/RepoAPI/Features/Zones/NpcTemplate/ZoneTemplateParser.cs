using System.Text.RegularExpressions;
using RepoAPI.Features.Wiki.Templates;

namespace RepoAPI.Features.Zones.NpcTemplate;

[RegisterService<ITemplateParser<ZoneTemplateDto>>(LifeTime.Singleton)]
public partial class ZoneTemplateParser : ITemplateParser<ZoneTemplateDto>
{
    public ZoneTemplateDto Parse(string wikitext)
    {
        var dto = new ZoneTemplateDto();
        
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
                    dto.Name = ParserUtils.GetStringFromColorTemplate(value);
                    break;
                }
                case "discovery_text": {
                    dto.DiscoveryText = string.Join("\n", ParserUtils.GetPropDictionaryFromSwitch(value).Select(x => ParserUtils.CleanWikitext(x.Value)));
                    break;
                }
                case "npcs": {
                    dto.Npcs = ParserUtils.GetListFromProperty(value);
                    break;
                }
                case "mobs": {
                    dto.Mobs = ParserUtils.GetListFromProperty(value);
                    break;
                }
                case "mob_drops": {
                    dto.MobDrops = ParserUtils.GetListFromProperty(value);
                    break;
                }
                default: {
                    dto.AdditionalProperties.TryAdd(key, value);
                    break;
                }
            }
        }
        
        var props = dto.AdditionalProperties;
        
        if (dto.InternalId is null && props.TryGetValue("zone_id", out var id)) {
            dto.InternalId = id.ToString();
        }
        
        if (props.TryGetValue("fairy_souls", out var fairySouls)) {
            var souls = ParserUtils.GetPropDictionaryFromSwitch(fairySouls.ToString() ?? "");
            // if (souls.TryGetValue("total", out var total)) {
            //     props["fairy_souls_total"] = int.TryParse(total, out var count) ? count : 0;
            // }

            if (souls.TryGetValue("#default", out var tableValue))
            {
                var table = WikiTableParser.Parse(tableValue);
                var list = new List<FairySoul>();
                foreach (var row in table.Rows)
                {
                    var soul = new FairySoul();
                    if (row.TryGetValue("Zone", out var coords))
                    {
                        var match = ZoneTemplateRegex().Match(coords);
                        if (match.Success) {
                            soul.Location = match.Groups[1].Value;
                        }
                    }

                    soul.Location ??= dto.InternalId;
                    
                    soul.Number = row.TryGetValue("No.", out var number) ? int.Parse(number) : 0;
                    
                    var x = row.GetValueOrDefault("X");
                    var y = row.GetValueOrDefault("Y");
                    var z = row.GetValueOrDefault("Z");
                    if (int.TryParse(x, out var xi) && int.TryParse(y, out var yi) && int.TryParse(z, out var zi))
                    {
                        soul.Coordinates = new Coordinates { X = xi, Y = yi, Z = zi };
                    }
                    
                    list.Add(soul);
                }
                dto.FairySouls = list;
            }
        }
        
        return dto;
    }

    public string GetTemplate(string input)
    {
        return $"Template:Zone/{input}";
    }
    
    /// <summary>
    /// Regex for getting zone id out of template
    /// Input: {{Zone/ExampleZone}}
    /// Output: ExampleZone
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"\{\{Zone\/(.*?)\}\}")]
    private static partial Regex ZoneTemplateRegex();
}