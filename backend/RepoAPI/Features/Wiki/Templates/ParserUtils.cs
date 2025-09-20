using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Humanizer;
using SkyblockRepo;

namespace RepoAPI.Features.Wiki.Templates;

public static partial class ParserUtils
{
	public static Dictionary<string, string> GetTopLevelProperties(string templateBody)
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

    private static void AddPropertyToDictionary(string segment, Dictionary<string, string> properties)
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
    
    public static string ExtractIncludeOnlyContent(string wikitext)
    {
        const string startTag = "<includeonly>";
        const string endTag = "</includeonly>";

        var startIndex = wikitext.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
        if (startIndex == -1)
        {
            // Replace any <noinclude>...</noinclude> tags with empty string
            return MatchNoIncludeRegex().Replace(wikitext, "");
        }

        startIndex += startTag.Length;

        var endIndex = wikitext.IndexOf(endTag, startIndex, StringComparison.OrdinalIgnoreCase);
        return endIndex == -1 
            ? wikitext 
            : wikitext.Substring(startIndex, endIndex - startIndex);
    }

    [GeneratedRegex(@"\<noinclude\>.*?\<\/noinclude\>", RegexOptions.Singleline)]
    public static partial Regex MatchNoIncludeRegex();

    public static Dictionary<string, string> GetPropDictionary(string wikitext)
    { 
        wikitext = ExtractIncludeOnlyContent(wikitext);
        
        var bodyStartIndex = wikitext.IndexOf('|');
        var bodyEndIndex = wikitext.LastIndexOf("}}", StringComparison.Ordinal);

        if (bodyStartIndex == -1 || bodyEndIndex == -1)
        {
            return new Dictionary<string, string>(); // Not a valid template
        }

        var templateBody = wikitext.Substring(bodyStartIndex + 1, bodyEndIndex - (bodyStartIndex + 1));
        
        // Parse the key-value pairs using a brace-counting method.
        var properties = ParserUtils.GetTopLevelProperties(templateBody);

        var result = new Dictionary<string, string>();
        
        foreach (var prop in properties)
        {
            var key = prop.Key.Trim().ToLowerInvariant();
            var value = prop.Value.Replace("\\r", "\r").Replace("\\n", "\n").Trim();

            result.TryAdd(key, value);
        }
        
        return result;
    }
    
    [GeneratedRegex(@"#switch\: \{\{\{.*?\}\}\}", RegexOptions.Singleline)]
    private static partial Regex SwitchConditionRegex();
    
    public static Dictionary<string, string> GetPropDictionaryFromSwitch(string wikitext)
    {
        return GetPropDictionary(SwitchConditionRegex().Replace(wikitext, ""));
    }
    
    [GeneratedRegex(@"<nowiki>(.*?)<\/nowiki>", RegexOptions.Singleline)]
    private static partial Regex NoWikiContentsRegex();
    
    [GeneratedRegex(@"'''(.*?)'''", RegexOptions.Singleline)]
    private static partial Regex BoldContentsRegex();
    
    [GeneratedRegex(@"<!--(.*?)-->", RegexOptions.Singleline)]
    private static partial Regex HtmlCommentRegex();

    /// <summary>
    /// Parses the special comma-separated lore format from an Item template
    /// into a clean, multi-line string with Minecraft formatting codes.
    /// </summary>
    /// <param name="rawLore">The raw lore string, e.g., "mc,legendary,id:name,1,&7Health..."</param>
    /// <returns>A clean, formatted lore string.</returns>
    public static string CleanLoreString(string rawLore)
    {
        if (string.IsNullOrEmpty(rawLore))
        {
            return string.Empty;
        }

        // Split the string into a maximum of 5 parts. The 5th part will contain
        // the entire rest of the string, preserving any commas within the lore.
        var parts = rawLore.Split([','], 5);
        
        if (parts.Length < 5) {
            return rawLore;
        }

        var loreText = parts[4];
        
        loreText = loreText.Replace(@"\\n", "\n");
        
        // Remove any <nowiki>...</nowiki> tags, preserving their inner content
        loreText = NoWikiContentsRegex().Replace(loreText, match => match.Groups[1].Value);
        // Convert bold formatting to Minecraft's &l code
        loreText = BoldContentsRegex().Replace(loreText, match => $"&l{match.Groups[1].Value}&r");

        return loreText;
    }

    public record struct LoreParseResult(
        string? CleanLore,
        string? ItemName,
        string? Material,
        string? Rarity,
        int Count = 0);
    
    /// <summary>
    /// Parses the special comma-separated lore format from an Item template
    /// into a clean, multi-line string with Minecraft formatting codes.
    /// </summary>
    /// <param name="rawLore">The raw lore string, e.g., "mc,legendary,id:name,1,&7Health..."</param>
    /// <returns>A LoreParseResult containing the clean lore, item name, identifier, and rarity.</returns>
    public static LoreParseResult ParseLoreString(string rawLore)
    {
        // Split the string into a maximum of 5 parts. The 5th part will contain
        // the entire rest of the string, preserving any commas within the lore.
        rawLore = rawLore.Replace("<nowiki>,", "<nowiki>|");

        if (rawLore.StartsWith("{{"))
        {
            var lore = rawLore;
        
            lore = lore.Replace("<nowiki>|", "<nowiki>,").Replace(@"\\n", "\n");
        
            // Remove any <nowiki>...</nowiki> tags, preserving their inner content
            lore = NoWikiContentsRegex().Replace(lore, match => match.Groups[1].Value);
            // Convert bold formatting to Minecraft's &l code
            lore = BoldContentsRegex().Replace(lore, match => $"&l{match.Groups[1].Value}&r");
            
            // Check for quantity suffix and extract it
            var quantityMatch = LoreQuantityRegex().Match(lore);
            if (quantityMatch.Success && int.TryParse(quantityMatch.Groups[1].Value, out var count))
            {
                lore = lore[..quantityMatch.Index];
                return new LoreParseResult(lore, null, null, null, count);
            }
            
            return new LoreParseResult(lore, null, null, null);
        }
        
        var parts = rawLore.Split([','], 5);
        
        var rarity = parts.Length > 1 ? parts[1].Trim().ToUpperInvariant().Replace(" ", "") : null;
        var itemIdentifierPart = parts.Length > 2 ? parts[2].Trim() : null;
        var itemIdentifierParts = itemIdentifierPart?.Split([':'], 2);
        var material = itemIdentifierParts is { Length: > 0 } ? itemIdentifierParts[0] : null;
        var itemName = itemIdentifierParts is { Length: > 1 } ? itemIdentifierParts[1].Replace("_", " ") : null;

        var loreText = parts.LastOrDefault() ?? "";
        
        loreText = loreText.Replace("<nowiki>|", "<nowiki>,").Replace(@"\\n", "\n");
        
        // Remove any <nowiki>...</nowiki> tags, preserving their inner content
        loreText = NoWikiContentsRegex().Replace(loreText, match => match.Groups[1].Value);
        // Convert bold formatting to Minecraft's &l code
        loreText = BoldContentsRegex().Replace(loreText, match => $"&l{match.Groups[1].Value}&r");

        if (loreText == material) {
            loreText = null;
        }

        return new LoreParseResult(loreText, itemName, material, rarity);
    }

    /// <summary>
    /// Gets a numeric value from a wikitext template, stripping formatting and links.
    /// Example: {{Coins|500,000}} -> 500000
    /// </summary>
    /// <param name="wikitext"></param>
    /// <returns></returns>
    public static double GetNumberFromTemplate(string? wikitext)
    {
        if (string.IsNullOrWhiteSpace(wikitext)) return 0;

        var text = CleanWikitext(wikitext);
        text = text.Replace(",", "").Replace(" ", "").Trim();

        if (double.TryParse(text, out var value))
        {
            return value;
        }

        return 0;
    }
    
    [GeneratedRegex(@",(\d+)$")]
    private static partial Regex LoreQuantityRegex();
    
    [GeneratedRegex(@"^\s*style[^|]+\|")]
    private static partial Regex StylePrefixRegex();

    [GeneratedRegex(@"\[\[File:.*\]\]\s*")]
    private static partial Regex FileLinkRegex();

    [GeneratedRegex(@"\[\[(?:[^|\]]+\|)?([^\]]+)\]\]")]
    private static partial Regex WikiLinkRegex();

    [GeneratedRegex(@"\{\{([^{}]+?)\}\}")]
    private static partial Regex InnermostTemplateRegex();
    
    /// <summary>
    /// Srips wikitext of its formatting, links, and templates to return clean plain text.
    /// </summary>
    public static string CleanWikitext(string wikitext, bool preserveBreaks = false)
    {
        if (string.IsNullOrWhiteSpace(wikitext) || wikitext.Contains("'''—'''")) return "—";

        var text = wikitext;
        
        text = StylePrefixRegex().Replace(text, "");
        text = FileLinkRegex().Replace(text, "");

        while (WikiLinkRegex().IsMatch(text))
        {
            text = WikiLinkRegex().Replace(text, "$1");
        }
        
        while (InnermostTemplateRegex().IsMatch(text))
        {
            text = InnermostTemplateRegex().Replace(text, match => match.Groups[1].Value.Split('|').LastOrDefault() ?? "");
        }
        
        text = preserveBreaks ? text.Replace("<br>", "\n") : text.Replace("<br>", " ");
        text = text.Replace("'''", "").Replace("''", "");
        text = SpaceRegex().Replace(text, " ").Trim();
        
        text = HtmlCommentRegex().Replace(text, "").Trim();
        text = NoWikiContentsRegex().Replace(text, match => match.Groups[1].Value);
        
        return text;
    }

    public static string BasicCleanWikitext(string wikitext)
    {
        wikitext = wikitext.Replace("\\n", "\n");
        wikitext = HtmlCommentRegex().Replace(wikitext, "").Trim();
        wikitext = NoWikiContentsRegex().Replace(wikitext, match => match.Groups[1].Value);
        
        return wikitext;
    }
    
    /// <summary>
    /// Extracts the colored text from the first color template found in the wikitext.
    /// Example: {{color|red|Some Text}} -> "Some Text"
    /// </summary>
    /// <param name="wikitext"></param>
    /// <returns></returns>
    public static string GetStringFromColorTemplate(string? wikitext)
    {
        if (string.IsNullOrWhiteSpace(wikitext)) return string.Empty;

        var match = ColorTextRegex().Match(wikitext);
        return match.Success ? CleanWikitext(match.Groups[1].Value.Trim()) : CleanWikitext(wikitext);
    }
    
    public static string GetNameFromFileLink(string? wikitext)
    {
        if (string.IsNullOrWhiteSpace(wikitext)) return string.Empty;

        var text = wikitext;
        
        text = FileLinkRegex().Replace(text, "");
        
        while (WikiLinkRegex().IsMatch(text))
        {
            text = WikiLinkRegex().Replace(text, "$1");
        }

        text = text.Replace("'''", "").Replace("''", "");
        text = SpaceRegex().Replace(text, " ").Trim();
        
        return text;
    }
    
    public static string FillInItemLoreTemplates(string wikitext)
    {
        if (string.IsNullOrWhiteSpace(wikitext)) return wikitext;
        
        var match = ItemLoreTemplateNameRegex().Match(wikitext);
        while (match.Success)
        {
            var itemName = match.Groups[1].Value.Trim();
            var item = SkyblockRepoClient.Instance.FindItem(itemName);
            if (item is null) break;
            wikitext = wikitext.Replace(match.Value, item.NameAndLore);
            match = ItemLoreTemplateNameRegex().Match(wikitext);
        }
        return wikitext;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex SpaceRegex();
        
    [GeneratedRegex(@"\{\{Item[_\/](.*?)(?:\|lore)?\}\}", RegexOptions.IgnoreCase | RegexOptions.Singleline, "en-US")]
    private static partial Regex ItemLoreTemplateNameRegex();
    
    /// <summary>
    /// Gets a list item from a bulleted line
    /// Example: * [[File:Minecraft_sprite_entity_spider.png|15px|link=Dasher Spider]] [[Spider]]\n
    /// Output: { Link = "Dasher Spider", Name = "Spider" }
    /// </summary>
    [GeneratedRegex(@"^\*\s*(?:\[\[File:[^\]]+\|(?:15px\|)?link=([^\]]+?)(?:\|.*?\]\]|\]\])\s*)?(?:\[\[(?:[^|\]]+\|)?([^\]]+)\]\])", RegexOptions.Multiline)]
    private static partial Regex WikiLinkItemRegex();
    
    /// <summary>
    /// Gets a list of referenced items from a property
    /// Example: * [[File:Minecraft_sprite_entity_spider.png|15px|link=Dasher Spider]] [[Spider]]\n* [[File:Minecraft_sprite_entity_bat.png|15px|link=Mega Bat]] [[Mega Bat]]
    /// Output: [ { Link = "Dasher Spider", Name = "Spider" }, { Link = "Mega Bat", Name = "Mega Bat" } ]
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static List<ListItem> GetListFromProperty(string? input)
    {
        var list = new List<ListItem>();
        if (string.IsNullOrWhiteSpace(input)) return list;

        var matches = WikiLinkItemRegex().Matches(input);
        foreach (Match match in matches)
        {
            var link = match.Groups[1].Success ? match.Groups[1].Value.Trim() : match.Groups[2].Value.Trim();
            var name = match.Groups[2].Value.Trim();
            if (!string.IsNullOrEmpty(name))
            {
                list.Add(new ListItem(link, name));
            }
        }

        return list;
    }
    
    public static bool TryParseRoman(this string roman, out int value)
    {
        try {
            value = roman.FromRoman();
            return value > 0;
        } catch {
            value = 0;
            return false;
        }
    }

    public static string ToRomanOrDefault(this int number, string defaultValue = "I")
    {
        try {
            return number.ToRoman();
        } catch {
            return defaultValue;
        }
    }
    
    public static string ToRomanOrDefault(this string? number, string defaultValue = "I")
    {
        if (number is null || !int.TryParse(number, out var numberValue)) return defaultValue;
        try {
            return numberValue.ToRoman();
        } catch {
            return defaultValue;
        }
    }
    
    public static bool DeepJsonEquals(object? oldData, object? newData)
    {
        if (oldData == null && newData == null) return true;
        if (oldData == null || newData == null) return false;
        
        var newJson = JsonNode.Parse(JsonSerializer.Serialize(newData, newData.GetType()));
        var oldJson = JsonNode.Parse(JsonSerializer.Serialize(oldData, oldData.GetType()));

        return JsonNode.DeepEquals(newJson, oldJson);
    }
    
    public static bool DeepJsonEquals(string oldData, JsonNode newData)
    {
        var newJson = JsonNode.Parse(JsonSerializer.Serialize(oldData));
        return JsonNode.DeepEquals(newJson, newData);
    }
    
    public static bool DeepJsonEquals(JsonNode oldData, JsonNode newData)
    {
        return JsonNode.DeepEquals(oldData, newData);
    }

    [GeneratedRegex(@"\{\{color\|[^|]+\|([^}]+)\}\}", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ColorTextRegex();
}

public record struct ListItem(string Link, string Name);