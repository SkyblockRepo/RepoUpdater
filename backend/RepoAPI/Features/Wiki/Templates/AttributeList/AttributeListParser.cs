using System.Text;
using System.Text.RegularExpressions;

namespace RepoAPI.Features.Wiki.Templates.AttributeList;

[RegisterService<ITemplateParser<AttributeListTemplateDto>>(LifeTime.Singleton)]
public partial class AttributeListParser : ITemplateParser<AttributeListTemplateDto>
{
	[GeneratedRegex(@"❮❮\s*(.*?)\s*❯❯", RegexOptions.Compiled)]
	private static partial Regex AttributeNameRegex();

	[GeneratedRegex(@"([\+\d\.%\w]+)\s*➔\s*([\d\.%\w]+)\s*(.*?)(?=\s*[\+\d]+\s*➔|$)", RegexOptions.Singleline | RegexOptions.Compiled)]
	private static partial Regex BonusRegex();

	public AttributeListTemplateDto Parse(string wikitext)
	{
		var attributes = new List<AttributeTemplateDto>();
		var rows = wikitext.Split(["{{!}}-"], StringSplitOptions.RemoveEmptyEntries)
			.Skip(1);

		foreach (var rowString in rows)
		{
			var columns = SplitRowIntoColumns(rowString);

			if (columns.Count < 11) continue; // Not a valid data row

			var dto = new AttributeTemplateDto
			{
				ShardName = ParserUtils.CleanWikitext(columns[0]),
				Rarity = ParserUtils.CleanWikitext(columns[1]),
				Id = int.TryParse(ParserUtils.CleanWikitext(columns[2]), out var id) ? id : 0,
				Category = ParserUtils.CleanWikitext(columns[3]),
				Family = ParserUtils.CleanWikitext(columns[4]),
				Obtaining = ParserUtils.CleanWikitext(columns[6], preserveBreaks: true),
				FusionInput1 = ParserUtils.CleanWikitext(columns[7], preserveBreaks: true),
				FusionInput2 = ParserUtils.CleanWikitext(columns[8], preserveBreaks: true),
				FusionResult = ParserUtils.CleanWikitext(columns[9]),
				FusionOrigin = ParserUtils.CleanWikitext(columns[10])
			};

			dto.AttributeEffect = ParseAttributeEffect(dto, columns[5]);

			attributes.Add(dto);
		}

		return new AttributeListTemplateDto { Attributes = attributes };
	}

	/// <summary>
	/// Uses a bracket-counting algorithm to safely split a wiki table row into columns,
	/// ignoring delimiters inside nested templates.
	/// </summary>
	private List<string> SplitRowIntoColumns(string rowString)
	{
		var columns = new List<string>();
		var currentColumn = new StringBuilder();
		var nestingLevel = 0;
		const string delimiter = "{{!}}";

		for (var i = 0; i < rowString.Length; i++)
		{
			// Check for the delimiter ONLY at the top nesting level
			if (nestingLevel == 0 && rowString.Substring(i).StartsWith(delimiter))
			{
				// Finalize the previous column and add it to the list
				columns.Add(currentColumn.ToString().Trim());
				// Reset for the next column
				currentColumn.Clear();
				// Skip the parser index past the delimiter
				i += delimiter.Length - 1;
				continue;
			}
            
			var c = rowString[i];
			currentColumn.Append(c);

			// Check for template/link openings
			if ((c == '{' && i + 1 < rowString.Length && rowString[i+1] == '{') ||
			    (c == '[' && i + 1 < rowString.Length && rowString[i+1] == '['))
			{
				currentColumn.Append(rowString[i + 1]);
				nestingLevel++;
				i++; // Skip the second character
			}
			// Check for template/link closings
			else if ((c == '}' && i + 1 < rowString.Length && rowString[i+1] == '}') ||
			         (c == ']' && i + 1 < rowString.Length && rowString[i+1] == ']'))
			{
				currentColumn.Append(rowString[i + 1]);
				nestingLevel--;
				i++; // Skip the second character
			}
		}

		// Add the final column after the loop finishes
		columns.Add(currentColumn.ToString().Trim());

		// The first "column" is often empty or styling before the real data, so we remove it.
		if (columns.Count != 0 && string.IsNullOrWhiteSpace(columns[0]))
		{
			columns.RemoveAt(0);
		}
        
		return columns;
	}
	
	public string GetTemplate(string input)
	{
		throw new NotSupportedException(
			"AttributeListParser handles a fixed set of 5 templates and does not support generating a name from a single input.");
	}

	private AttributeEffectDto? ParseAttributeEffect(AttributeTemplateDto template, string rawWikitext)
	{
		var cleanText = ParserUtils.CleanWikitext(rawWikitext);
		var nameMatch = AttributeNameRegex().Match(cleanText);
		if (!nameMatch.Success) return null;
		
		template.Description = cleanText;

		var effect = new AttributeEffectDto { Name = nameMatch.Groups[1].Value };

		var bonusMatches = BonusRegex().Matches(cleanText);
        
		// Loop through each match found.
		foreach (Match bonusMatch in bonusMatches)
		{
			var description = bonusMatch.Groups[3].Value.Trim();
			if (description.EndsWith(" and")) {
				description = description[..^4].TrimEnd();
			}
			
			effect.Bonuses.Add(new StatBonusDto
			{
				FromValue = bonusMatch.Groups[1].Value.TrimStart('+').Trim(),
				ToValue = bonusMatch.Groups[2].Value.Trim(),
				Description = description,
			});
		}

		return effect;
	}
}