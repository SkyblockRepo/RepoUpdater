using System.Text.RegularExpressions;
using Quartz.Util;
using RepoAPI.Core.Models;
using RepoAPI.Features.Pets.Models;
using RepoAPI.Features.Wiki.Templates;
using TimeSpanParserUtil;

namespace RepoAPI.Features.NPCs.NpcTemplate;

[RegisterService<ITemplateParser<NpcTemplateDto>>(LifeTime.Singleton)]
public partial class NpcTemplateParser : ITemplateParser<NpcTemplateDto>
{
    public NpcTemplateDto Parse(string wikitext, string backupId = "")
    {
        var dto = new NpcTemplateDto();
        
        var properties = ParserUtils.GetPropDictionary(wikitext);

        foreach (var prop in properties)
        {
            var key = prop.Key.Trim().ToLowerInvariant();
            var value = prop.Value.Replace("\\r", "\r").Replace("\\n", "\n").Trim();
            // Map the found key to the correct DTO property
            switch (key)
            {
                case "npc_id": {
                    dto.InternalId = value;
                    break;
                }
                case "name": {
                    dto.Name = ParserUtils.GetStringFromColorTemplate(value);
                    break;
                }
                case "location": {
                    dto.Location.Zone = ParserUtils.GetStringFromColorTemplate(value);
                    break;
                }
                case "location_x":
                {
                    var x = ParserUtils.GetNumberFromTemplate(value);
                    dto.Location.Coordinates = dto.Location.Coordinates with { X = x };
                    break;
                }
                case "location_y":
                {
                    var y = ParserUtils.GetNumberFromTemplate(value);
                    dto.Location.Coordinates = dto.Location.Coordinates with { Y = y };
                    break;
                }
                case "location_z":
                {
                    var z = ParserUtils.GetNumberFromTemplate(value);
                    dto.Location.Coordinates = dto.Location.Coordinates with { Z = z };
                    break;
                }
                default: {
                    dto.AdditionalProperties.TryAdd(key, value);
                    break;
                }
            }
        }
        
        
        var props = dto.AdditionalProperties;
        
        if (dto.InternalId is null && props.TryGetValue("internal_id", out var id)) {
            dto.InternalId = id.ToString();
        }

        dto.Flags = new NpcFlags()
        {
            Merchant = props.TryGetValue("merchant", out var merchant) &&
                          merchant.ToString()?.Contains("Yes") is true,
            Abiphone = props.TryGetValue("abiphone", out var abiphone) &&
                        abiphone.ToString()?.Contains("Yes") is true,
            Shop = props.TryGetValue("shop", out var shop) && shop.ToString()?.Contains("Yes") is true,
            Garden = props.TryGetValue("garden", out var garden) &&
                         garden.ToString()?.Contains("Yes") is true,
        };

        if (dto.Flags.Garden)
        {
            var visitor = new NpcGardenVisitor();
            var gardenProps = ParserUtils.GetPropDictionaryFromSwitch(props.GetValueOrDefault("garden", "").ToString() ?? "");
            
            if (gardenProps.TryGetValue("rarity", out var rarity)) {
                visitor.Rarity = ParserUtils.CleanWikitext(rarity);;
            }
           
            if (gardenProps.TryGetValue("desire", out var desire)) {
                visitor.Desire = ParserUtils.GetNameFromFileLink(desire);
                if (visitor.Desire.IsNullOrWhiteSpace()) visitor.Desire = null;
            }
            
            if (gardenProps.TryGetValue("level_requirement", out var gardenLevel)) {
                visitor.GardenLevel = (int)ParserUtils.GetNumberFromTemplate(gardenLevel);
            }
            
            if (gardenProps.TryGetValue("copper_reward", out var copper)) {
                visitor.Copper = ParserUtils.GetNumberFromTemplate(copper);
            }
            
            if (gardenProps.TryGetValue("farming_xp_reward", out var farmingXp)) {
                visitor.FarmingXp = ParserUtils.GetNumberFromTemplate(farmingXp);
            }
            
            dto.Visitor = visitor;
        }
        
        return dto;
    }

    public string GetTemplate(string input)
    {
        return $"Template:NPC/{input}";
    }
}