using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkyblockRepo;

/// <summary>
/// When applied to an enum, serializes its values as camelCase strings.
/// </summary>
public class JsonStringEnumAttribute : JsonConverterAttribute
{
    public override JsonConverter CreateConverter(Type typeToConvert)
    {
        return new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower);
    }
}

/// <summary>
/// When applied to an enum, serializes its values as camelCase strings.
/// </summary>
public class JsonStringEnumCapitalizeAttribute : JsonConverterAttribute
{
    public override JsonConverter CreateConverter(Type typeToConvert)
    {
        return new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper);
    }
}