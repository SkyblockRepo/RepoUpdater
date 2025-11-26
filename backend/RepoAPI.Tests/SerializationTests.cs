using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using SkyblockRepo.Models;
using Xunit;

namespace RepoAPI.Tests;

public class SerializationTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    [Fact]
    public void TestSkyblockItemData_NullRecipes_ShouldBeMissing_Or_Null()
    {
        // Arrange
        var itemData = new SkyblockItemData
        {
            InternalId = "TEST_ITEM",
            Name = "Test Item",
            Recipes = null // Explicitly null
        };

        // Act
        var node = JsonSerializer.SerializeToNode(itemData, _jsonOptions);
        var jsonString = JsonSerializer.Serialize(itemData, _jsonOptions);
        
        // Assert
        Assert.NotNull(node);
        var obj = node.AsObject();
        
        // Check if key exists
        bool hasKey = obj.ContainsKey("recipes");
        
        if (hasKey)
        {
            var val = obj["recipes"];
            Assert.Null(val); // Should be null if present
        }
    }

    [Fact]
    public void TestMergeLogic_WithNullInNew_AndValueInOld()
    {
        // Simulate the logic in JsonFileWriterService
        
        // Arrange
        var propToKeep = "recipes";
        
        // Existing data has recipes
        var existingJson = """
        {
            "internalId": "TEST_ITEM",
            "recipes": [ { "type": "crafting" } ]
        }
        """;
        var existingNode = JsonNode.Parse(existingJson)!.AsObject();
        
        // New data has explicit null for recipes (simulating the case where serialization produces null)
        var newJsonWithNull = """
        {
            "internalId": "TEST_ITEM",
            "recipes": null
        }
        """;
        var finalNodeWithNull = JsonNode.Parse(newJsonWithNull)!.AsObject();
        
        // New data is missing recipes (simulating the case where serialization omits it)
        var newJsonMissing = """
        {
            "internalId": "TEST_ITEM"
        }
        """;
        var finalNodeMissing = JsonNode.Parse(newJsonMissing)!.AsObject();

        // Act & Assert 1: Case where new data has explicit null
        ApplyKeepLogic(finalNodeWithNull, existingNode, propToKeep);
        
        Assert.True(finalNodeWithNull.ContainsKey(propToKeep), "Should contain key after merge");
        Assert.NotNull(finalNodeWithNull[propToKeep]); // Should not be null anymore
        Assert.Equal(existingNode[propToKeep]!.ToJsonString(), finalNodeWithNull[propToKeep]!.ToJsonString());

        // Act & Assert 2: Case where new data is missing key
        ApplyKeepLogic(finalNodeMissing, existingNode, propToKeep);
        
        Assert.True(finalNodeMissing.ContainsKey(propToKeep), "Should contain key after merge");
        Assert.NotNull(finalNodeMissing[propToKeep]);
        Assert.Equal(existingNode[propToKeep]!.ToJsonString(), finalNodeMissing[propToKeep]!.ToJsonString());
    }

    private void ApplyKeepLogic(JsonObject finalObj, JsonObject existingObj, string propToKeep)
    {
        if ((!finalObj.ContainsKey(propToKeep) || finalObj[propToKeep] is null) && existingObj.TryGetPropertyValue(propToKeep, out var valueToKeep))
        {
            finalObj[propToKeep] = valueToKeep?.DeepClone();
        }
    }
    
    [Fact]
    public void TestMergeLogic_WithNewValue_ShouldOverwrite()
    {
        // Arrange
        var propToKeep = "recipes";
        
        var existingJson = """
        {
            "internalId": "TEST_ITEM",
            "recipes": [ { "type": "old" } ]
        }
        """;
        var existingNode = JsonNode.Parse(existingJson)!.AsObject();
        
        var newJson = """
        {
            "internalId": "TEST_ITEM",
            "recipes": [ { "type": "new" } ]
        }
        """;
        var finalNode = JsonNode.Parse(newJson)!.AsObject();

        // Act
        ApplyKeepLogic(finalNode, existingNode, propToKeep);
        
        // Assert
        Assert.Equal("""[{"type":"new"}]""", finalNode[propToKeep]!.ToJsonString());
    }
}
