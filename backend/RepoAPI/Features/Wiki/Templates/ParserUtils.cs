namespace RepoAPI.Features.Wiki.Templates;

public static class ParserUtils
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
        if (startIndex == -1) return wikitext; // Fallback to full text if tag not found

        startIndex += startTag.Length;

        var endIndex = wikitext.IndexOf(endTag, startIndex, StringComparison.OrdinalIgnoreCase);
        return endIndex == -1 
            ? wikitext 
            : wikitext.Substring(startIndex, endIndex - startIndex);
    }

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
}