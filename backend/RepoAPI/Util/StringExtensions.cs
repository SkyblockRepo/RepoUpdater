namespace RepoAPI.Util;

public static class StringExtensions
{
	/// <summary>
	/// Converts SCREAMING_SNAKE_CASE to Title Case.
	/// Example: "ENCHANTMENT_LURE_2" -> "Enchantment Lure 2"
	/// </summary>
	public static string ToTitleCase(this string input)
	{
		if (string.IsNullOrWhiteSpace(input))
			return input;

		// Split by underscore and convert each word
		var words = input.Split('_');
		var result = new List<string>();

		foreach (var word in words)
		{
			if (string.IsNullOrWhiteSpace(word))
				continue;

			// If the word is all uppercase, convert to title case
			// Otherwise keep it as-is (for mixed case like "NPC")
			if (word.All(char.IsUpper) || word.All(char.IsDigit))
			{
				result.Add(char.ToUpper(word[0]) + word.Substring(1).ToLower());
			}
			else
			{
				result.Add(word);
			}
		}

		return string.Join(" ", result);
	}
}
