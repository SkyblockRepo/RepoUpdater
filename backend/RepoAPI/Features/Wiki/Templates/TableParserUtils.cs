using System.Text.RegularExpressions;

namespace RepoAPI.Features.Wiki.Templates;

public class WikiTable
{
    public List<string> Headers { get; set; } = new();
    public List<Dictionary<string, string>> Rows { get; set; } = new();
}

public static partial class WikiTableParser
{
    [GeneratedRegex(@"(rowspan|colspan|style)=""[^""]*""", RegexOptions.Compiled)]
    private static partial Regex AttributeRegex();

    [GeneratedRegex(@"rowspan=""(?<rowspan>\d*?)""", RegexOptions.Compiled)]
    private static partial Regex RowspanRegex();

    [GeneratedRegex(@"colspan=""(?<colspan>\d*?)""", RegexOptions.Compiled)]
    private static partial Regex ColspanRegex();

    private static string CleanCellValue(string value)
    {
        // Remove attributes first
        var cleaned = AttributeRegex().Replace(value, "").Trim();
        
        // If it contains {{!}}, extract content after it
        var pipeIndex = cleaned.IndexOf("{{!}}", StringComparison.Ordinal);
        if (pipeIndex >= 0)
        {
            cleaned = cleaned.Substring(pipeIndex + 5).Trim();
        }
        
        return cleaned;
    }

    public static WikiTable Parse(string wikitext, string backupId = "")
    {
        var table = new WikiTable();
        wikitext = ParserUtils.ExtractIncludeOnlyContent(wikitext).Replace("\r\n", "\n").Replace("\\n", "\n");
        
        // Split into logical lines
        var lines = wikitext.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var headerLines = new List<string>();
        var rows = new List<List<string>>();
        var currentRow = new List<string>();
        var headerRowSpan = 1;
        var headerColSpan = 1;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (RowspanRegex().Match(trimmed) is { Success: true } match)
            {
                if (int.TryParse(match.Groups["rowspan"].Value, out var span))
                {
                    headerRowSpan = Math.Max(headerRowSpan, span);
                }
            }
            
            if (ColspanRegex().Match(trimmed) is { Success: true } colspanMatch)
            {
                if (int.TryParse(colspanMatch.Groups["colspan"].Value, out var span))
                {
                    headerColSpan = Math.Max(headerColSpan, span);
                }
            }
            
            if (trimmed.StartsWith('!'))
            {
                headerLines.Add(trimmed.TrimStart('!').Trim());
                continue;
            }

            if (headerRowSpan > 0)
            {
                if (trimmed.StartsWith("{{!}}-") && headerLines.Count >= headerColSpan)
                {
                    headerRowSpan--;
                    continue; // Still in header rows
                }

                if (trimmed.StartsWith("{{!}}") && !trimmed.StartsWith("{{!}}-"))
                {
                    headerLines.Add(trimmed.Replace("{{!}}", "").Trim());
                    continue;
                }
            }
            else
            {
                if (trimmed.StartsWith("{{!}}-"))
                {
                    if (currentRow.Count > 0)
                    {
                        rows.Add([..currentRow]);
                        currentRow.Clear();
                    }
                }
                else if (trimmed.StartsWith("{{!}}"))
                {
                    currentRow.Add(trimmed.Replace("{{!}}", "").Trim());
                } else {
                    // Add this string to the last cell if it exists
                    if (currentRow.Count > 0) {
                        currentRow[^1] += "\n" + trimmed;
                    }
                }
            }
        }

        // Clean headers and flatten them
        var cleanedHeaders = new List<string>();
        
        foreach (var header in headerLines)
        {
            var cleanHeader = CleanCellValue(header);
            
            // Check if this header has colspan
            var colspanMatch = ColspanRegex().Match(header);
            if (colspanMatch.Success && int.TryParse(colspanMatch.Groups["colspan"].Value, out var colspan) && colspan > 1)
            {
                // Skip this header as it's a parent header, its children will be added instead
                continue;
            }
            
            cleanedHeaders.Add(ParserUtils.GetStringFromColorTemplate(cleanHeader));
        }
        
        table.Headers = cleanedHeaders;

        // Parse rows
        foreach (var columns in rows)
        {
            var rowDict = new Dictionary<string, string>();

            for (var i = 0; i < columns.Count && i < table.Headers.Count; i++)
            {
                var clean = CleanCellValue(columns[i]);
                if (string.IsNullOrEmpty(clean)) continue;
                
                var header = table.Headers[i];
                rowDict[header] = clean;
            }
            table.Rows.Add(rowDict);
        }

        return table;
    }
}