using System.Text.Json.Nodes;

namespace RepoAPI.Util;

public static class JsonUtils
{
	/// <summary>
	/// Recursively merges values from <paramref name="overrideNode"/> into <paramref name="baseNode"/>.
	/// </summary>
	public static void MergeInto(JsonNode baseNode, JsonNode overrideNode)
	{
		if (baseNode is not JsonObject baseObj || overrideNode is not JsonObject overrideObj)
			return;

		foreach (var property in overrideObj)
		{
			if (baseObj.ContainsKey(property.Key) &&
			    baseObj[property.Key] is JsonObject baseChild &&
			    property.Value is JsonObject overrideChild)
			{
				MergeInto(baseChild, overrideChild);
			}
			else
			{
				baseObj[property.Key] = property.Value?.DeepClone();
			}
		}
	}
}