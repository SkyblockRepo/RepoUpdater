using System.Text.RegularExpressions;

namespace RepoAPI.Features.Wiki.Templates.ItemTemplate;

[RegisterService<ITemplateParser<ItemTemplateDto>>(LifeTime.Singleton)]
public partial class ItemTemplateParser : ITemplateParser<ItemTemplateDto>
{
    public ItemTemplateDto Parse(string wikitext)
    {
        wikitext = ParserUtils.ExtractIncludeOnlyContent(wikitext);
        
        var dto = new ItemTemplateDto();

        var bodyStartIndex = wikitext.IndexOf('|');
        var bodyEndIndex = wikitext.LastIndexOf("}}", StringComparison.Ordinal);

        if (bodyStartIndex == -1 || bodyEndIndex == -1)
        {
            return dto; // Not a valid template
        }

        var templateBody = wikitext.Substring(bodyStartIndex + 1, bodyEndIndex - (bodyStartIndex + 1));
        
        // Parse the key-value pairs using a brace-counting method.
        var properties = GetTopLevelProperties(templateBody);

        foreach (var prop in properties)
        {
            var key = prop.Key.Trim().ToLowerInvariant();
            var value = prop.Value.Replace("\\r", "\r").Replace("\\n", "\n").Trim();
            // Map the found key to the correct DTO property
            switch (key)
            {
                case "name": dto.Name = value; break;
                case "rift_name": dto.RiftName = value; break;
                case "internal_id": dto.InternalId = value; break;
                case "image": dto.Image = value; break;
                case "infobox_image": dto.InfoboxImage = value; break;
                case "ref": dto.Ref = value; break;
                case "cr": dto.CollectionReference = value; break;
                case "bcr": dto.CollectionReferenceB = value; break;
                case "itemlorecolumns": dto.ItemLoreColumns = value; break;
                case "lore": dto.Lore = value; break;
                case "lore2": dto.Lore2 = value; break;
                case "real_lore": dto.RealLore = value; break;
                case "category": dto.Category = value; break;
                case "categoryb": dto.CategoryB = value; break;
                case "tier": dto.Tier = value; break;
                case "crafting_requirements": dto.CraftingRequirements = value; break;
                case "value": dto.Value = value; break;
                case "motes_value": dto.MotesValue = value; break;
                case "power": dto.Power = value; break;
                case "stats": dto.Stats = value; break;
                case "rift_stats": dto.RiftStats = value; break;
                case "ability_stats": dto.AbilityStats = value; break;
                case "requirements": dto.Requirements = value; break;
                case "essence": dto.Essence = value; break;
                case "essence_cost": dto.EssenceCost = value; break;
                case "dungeon_requirements": dto.DungeonRequirements = value; break;
                case "gemslots": dto.Gemslots = value; break;
                case "tradable": dto.Tradable = value; break;
                case "auctionable": dto.Auctionable = value; break;
                case "reforgeable": dto.Reforgeable = value; break;
                case "enchantable": dto.Enchantable = value; break;
                case "museumable": dto.Museumable = value; break;
                case "bazaarable": dto.Bazaarable = value; break;
                case "salvageable": dto.Salvageable = value; break;
                case "sackable": dto.Sackable = value; break;
                case "soulboundable": dto.Soulboundable = value; break;
                case "soulboundtype": dto.SoulboundType = value; break;
                case "rift_item": dto.RiftItem = value; break;
                case "rift_transferrable": dto.RiftTransferrable = value; break;
                case "item_color": dto.ItemColor = value; break;
                case "reforge": dto.Reforge = value; break;
                case "reforge_type": dto.ReforgeType = value; break;
                case "reforge_requirements": dto.ReforgeRequirements = value; break;
                case "collection": dto.Collection = value; break;
                case "collection_menu": dto.CollectionMenu = value; break;
                case "skill_xp": dto.SkillXp = value; break;
                case "rawmaterials": dto.RawMaterials = value; break;
                case "recipetree":
                {
                    dto.RecipeTree ??= new ItemTemplateRecipeTreeDto();
                    dto.RecipeTree.Raw = value;
                    
                    // Extract item id and recipe name from {{Recipe Tree/CHICKEN_GENERATOR_1|first}} or {{CollapsibleTree/Item/rift_trophy_wyldly_supreme|Base|1}}
                    var match = RecipeTreeRegex().Match(value);
                    if (match.Success)
                    {
                        dto.RecipeTree.ItemId = match.Groups[1].Value.ToUpperInvariant();
                        dto.RecipeTree.RecipeName = match.Groups[2].Value;
                    }
                    
                    break;
                }
                case "upgrading": dto.Upgrading = value; break;
                case "scaled_stats": dto.ScaledStats = value; break;
                case "essence_upgrading": dto.EssenceUpgrading = value; break;
                case "skins": dto.Skins = value; break;
                case "sources": dto.Sources = value; break;
                default: {
                    dto.AdditionalProperties.TryAdd(key, value);
                    break;
                }
            }
        }
        return dto;
    }

    public string GetTemplate(string input)
    {
        return $"Template:Item/{input}";
    }
    
    private Dictionary<string, string> GetTopLevelProperties(string templateBody)
    {
        var properties = new Dictionary<string, string>();
        var nestingLevel = 0;
        var lastSplitIndex = 0;

        for (int i = 0; i < templateBody.Length; i++)
        {
            if (templateBody[i] == '{' && i + 1 < templateBody.Length && templateBody[i+1] == '{')
            {
                nestingLevel++;
                i++; // Skip the second brace
            }
            else if (templateBody[i] == '}' && i + 1 < templateBody.Length && templateBody[i+1] == '}')
            {
                nestingLevel--;
                i++; // Skip the second brace
            }
            else if (templateBody[i] == '[' && i + 1 < templateBody.Length && templateBody[i+1] == '[')
            {
                nestingLevel++;
                i++; // Skip the second bracket
            }
            else if (templateBody[i] == ']' && i + 1 < templateBody.Length && templateBody[i+1] == ']')
            {
                nestingLevel--;
                i++; // Skip the second bracket
            }
            // A pipe at the top level (nestingLevel 0) is a property delimiter
            else if (templateBody[i] == '|' && nestingLevel == 0)
            {
                // Extract the previous property segment
                var propertySegment = templateBody.Substring(lastSplitIndex, i - lastSplitIndex);
                AddPropertyToDictionary(propertySegment, properties);
                
                // Set the start for the next segment
                lastSplitIndex = i + 1;
            }
        }

        // Add the final property after the last pipe
        var finalSegment = templateBody.Substring(lastSplitIndex);
        AddPropertyToDictionary(finalSegment, properties);

        return properties;
    }

    private void AddPropertyToDictionary(string segment, Dictionary<string, string> properties)
    {
        var parts = segment.Split(['='], 2);
        if (parts.Length == 2)
        {
            var key = parts[0].Trim();
            var value = parts[1].Trim();
            if (!string.IsNullOrEmpty(key) && !key.Contains('{')) // Final check for template params
            {
                properties[key] = value;
            }
        }
    }

    [GeneratedRegex(@"\{\{(?:Recipe Tree|CollapsibleTree/Item)/([^|}]+)\|?([^}]*)\}\}")]
    private static partial Regex RecipeTreeRegex();
}