using System.Text.RegularExpressions;

namespace RepoAPI.Features.Wiki.Templates.PetTemplate;

[RegisterService<ITemplateParser<PetTemplateDto>>(LifeTime.Singleton)]
public partial class PetTemplateParser : ITemplateParser<PetTemplateDto>
{
    // Regex to match property names in the format |property_name=
    [GeneratedRegex(@"\|([^=]+?)\s*=", RegexOptions.Compiled)]
    private static partial Regex PropertyNameRegex();
    
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
                // case "name": dto.Name = value; break;
                // case "rift_name": dto.RiftName = value; break;
                // case "internal_id": dto.InternalId = value; break;
                // case "image": dto.Image = value; break;
                // case "infobox_image": dto.InfoboxImage = value; break;
                // case "ref": dto.Ref = value; break;
                // case "cr": dto.CollectionReference = value; break;
                // case "bcr": dto.CollectionReferenceB = value; break;
                // case "itemlorecolumns": dto.ItemLoreColumns = value; break;
                // case "lore": dto.Lore = value; break;
                // case "lore2": dto.Lore2 = value; break;
                // case "real_lore": dto.RealLore = value; break;
                // case "category": dto.Category = value; break;
                // case "categoryb": dto.CategoryB = value; break;
                // case "tier": dto.Tier = value; break;
                // case "crafting_requirements": dto.CraftingRequirements = value; break;
                // case "value": dto.Value = value; break;
                // case "motes_value": dto.MotesValue = value; break;
                // case "power": dto.Power = value; break;
                // case "stats": dto.Stats = value; break;
                // case "rift_stats": dto.RiftStats = value; break;
                // case "ability_stats": dto.AbilityStats = value; break;
                // case "requirements": dto.Requirements = value; break;
                // case "essence": dto.Essence = value; break;
                // case "essence_cost": dto.EssenceCost = value; break;
                // case "dungeon_requirements": dto.DungeonRequirements = value; break;
                // case "gemslots": dto.Gemslots = value; break;
                // case "tradable": dto.Tradable = value; break;
                // case "auctionable": dto.Auctionable = value; break;
                // case "reforgeable": dto.Reforgeable = value; break;
                // case "enchantable": dto.Enchantable = value; break;
                // case "museumable": dto.Museumable = value; break;
                // case "bazaarable": dto.Bazaarable = value; break;
                // case "salvageable": dto.Salvageable = value; break;
                // case "sackable": dto.Sackable = value; break;
                // case "soulboundable": dto.Soulboundable = value; break;
                // case "soulboundtype": dto.SoulboundType = value; break;
                // case "rift_item": dto.RiftItem = value; break;
                // case "rift_transferrable": dto.RiftTransferrable = value; break;
                // case "item_color": dto.ItemColor = value; break;
                // case "reforge": dto.Reforge = value; break;
                // case "reforge_type": dto.ReforgeType = value; break;
                // case "reforge_requirements": dto.ReforgeRequirements = value; break;
                // case "collection": dto.Collection = value; break;
                // case "collection_menu": dto.CollectionMenu = value; break;
                // case "skill_xp": dto.SkillXp = value; break;
                // case "rawmaterials": dto.RawMaterials = value; break;
                // case "recipetree": dto.RecipeTree = value; break;
                // case "upgrading": dto.Upgrading = value; break;
                // case "scaled_stats": dto.ScaledStats = value; break;
                // case "essence_upgrading": dto.EssenceUpgrading = value; break;
                // case "skins": dto.Skins = value; break;
                // case "sources": dto.Sources = value; break;
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
        return $"Template:Pet/{input}";
    }
}