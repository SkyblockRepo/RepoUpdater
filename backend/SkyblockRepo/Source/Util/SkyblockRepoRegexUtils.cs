using System.Text.RegularExpressions;

namespace SkyblockRepo;

public static partial class SkyblockRepoRegexUtils
{
	public record SkullTextureInfo(string Value, string Signature);

	public static SkullTextureInfo? ExtractSkullTexture(string nbtTag)
	{
		if (string.IsNullOrWhiteSpace(nbtTag))
		{
			return null;
		}

		var match = SkullTextureRegex().Match(nbtTag);

		if (!match.Success) return null;
		
		var signature = match.Groups["signature"].Value;
		var value = match.Groups["value"].Value;
		return new SkullTextureInfo(value, signature);

	}

	[GeneratedRegex(@"textures:\[0:\{(?=.*?Signature:""(?<signature>.*?)"")(?=.*?Value:""(?<value>.*?)"").*?\}", RegexOptions.Compiled)]
	private static partial Regex SkullTextureRegex();
}