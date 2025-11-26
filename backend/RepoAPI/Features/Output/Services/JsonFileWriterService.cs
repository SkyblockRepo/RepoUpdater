using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Quartz.Util;
using RepoAPI.Util;

namespace RepoAPI.Features.Output.Services;

public class JsonFileWriterService(
	JsonWriteQueue queue,
	ILogger<JsonFileWriterService> logger) : BackgroundService, ISelfRegister
{
	private readonly string _outputBasePath = GetOutputBasePath();
	private readonly string _overridesBasePath = Path.Combine(GetOutputBasePath(), "..", "overrides");
	private readonly Dictionary<string, bool> _exclusionCache = [];
	
	private readonly JsonSerializerOptions _jsonOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		// Allows characters like '&' to be written unescaped
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		logger.LogInformation("Json File Writer Service is starting.");
		
		LoadExclusions();

		// This will block and wait until an item is available in the queue
		await foreach (var request in queue.ReadAllAsync(stoppingToken))
		{
			try
			{
				var directory = Path.GetDirectoryName(request.Path) ?? string.Empty;
				var fileName = Path.GetFileName(request.Path);

				var safeFileName = SanitizeFileName(fileName);
				var filePath = Path.Combine(_outputBasePath, directory, safeFileName);
				
				// Skip if in exclusion list
				if (_exclusionCache.ContainsKey(filePath))
				{
					logger.LogDebug("Skipping excluded file: {FilePath}", filePath);
					continue;
				}
				
				var overrideFilePath = Path.Combine(_overridesBasePath, "data", directory, safeFileName);

				Directory.CreateDirectory(Path.Combine(_outputBasePath, directory)!);
                
				// Start with the data from the database
				var finalNode = JsonSerializer.SerializeToNode(request.Data, _jsonOptions);
				
				// If there are properties to keep, read the existing file and merge those properties
				if ((request.KeepProperties is { Count: > 0 } || request.MergeInto) && File.Exists(filePath))
				{
					try {
						var existingJson = await File.ReadAllTextAsync(filePath, stoppingToken);
						var existingNode = JsonNode.Parse(existingJson);

						if (request.MergeInto && finalNode != null && existingNode != null)
						{
							JsonUtils.MergeInto(existingNode, finalNode);
							finalNode = existingNode;
						}
						
						if (request.KeepProperties is { Count: > 0 } && finalNode is JsonObject finalObj && existingNode is JsonObject existingObj)
						{
							foreach (var propToKeep in request.KeepProperties)
							{
								// If the new data doesn't have the property but the old one does, copy it over.
								// Also copy if the new data has the property but it is null (explicit null in JSON).
								if ((!finalObj.ContainsKey(propToKeep) || finalObj[propToKeep] is null) && existingObj.TryGetPropertyValue(propToKeep, out var valueToKeep))
								{
									finalObj[propToKeep] = valueToKeep?.DeepClone();
									logger.LogDebug("Kept property '{Property}' from existing file {FilePath}", propToKeep, filePath);
								}
							}
						}
					}
					catch (Exception ex) {
						logger.LogWarning(ex, "Could not read existing file to keep properties for {FilePath}. Proceeding without them.", filePath);
					}
				}

				// If an override file exists, read and merge it
				if (File.Exists(overrideFilePath))
				{
					logger.LogInformation("Applying override for {InternalId} from {OverridePath}", request.Path, overrideFilePath);
					var overrideJson = await File.ReadAllTextAsync(overrideFilePath, stoppingToken);
					var overrideNode = JsonNode.Parse(overrideJson);

					if (finalNode != null && overrideNode != null)
					{
						JsonUtils.MergeInto(finalNode, overrideNode);
					}
				}

				// Sort properties to ensure stable output
				if (finalNode is JsonObject finalObjToSort)
				{
					finalNode = SortProperties(finalObjToSort);
				}

				// Serialize the final (potentially merged) node to a JSON string
				var jsonString = JsonSerializer.Serialize(finalNode, _jsonOptions);
				if (jsonString.IsNullOrWhiteSpace()) continue;
				
				var tempFilePath = $"{filePath}.tmp";
				
				// Write to temp file first
				await File.WriteAllTextAsync(tempFilePath, jsonString, stoppingToken);

				try
				{
					// Atomically move the temp file to the target location
					File.Move(tempFilePath, filePath, overwrite: true);

				} catch (IOException ioEx)
				{
					// If the move fails, delete the temp file to avoid clutter
					if (File.Exists(tempFilePath)) {
						File.Delete(tempFilePath);
					}
					logger.LogError(ioEx, "Failed to move temp file to {FilePath}", filePath);
				}
				
				logger.LogDebug("Wrote file: {FilePath}", filePath);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to write JSON file for {InternalId}", request.Path);
			}
		}
	}
	
	private static JsonObject SortProperties(JsonObject jsonObject)
	{
		var propertyOrder = new List<string>
		{
			"internalId",
			"name",
			"category",
			"source",
			"npcValue",
			"lore",
			"flags",
			"data",
			"variants",
			"recipes",
			"soldBy"
		};

		var sortedObj = new JsonObject();

		// Add known properties in order
		foreach (var propName in propertyOrder) {
			if (!jsonObject.TryGetPropertyValue(propName, out var value)) continue;
			
			sortedObj.Add(propName, value?.DeepClone());
			jsonObject.Remove(propName);
		}
		
		// Sort the remaining keys alphabetically
		var remainingKeys = jsonObject.Select(x => x.Key).OrderBy(k => k).ToList();
		
		foreach (var key in remainingKeys)
		{
			if (jsonObject.TryGetPropertyValue(key, out var value))
			{
				sortedObj.Add(key, value?.DeepClone());
			}
		}

		return sortedObj;
	}
	
	private static string SanitizeFileName(string fileName)
	{
		var invalidChars = Path.GetInvalidFileNameChars();
		return string.Concat(fileName.Select(c => invalidChars.Contains(c) || c == ':' ? '-' : c));
	}
	
	private static string GetOutputBasePath()
	{
		var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
		var iterations = 0;
		const int maxIterations = 10;
		
		// Search upwards from the bin folder until we find the solution file (.sln)
		while (currentDir != null && currentDir.GetFiles("*.sln").Length == 0)
		{
			currentDir = currentDir.Parent;
			iterations++;
			if (iterations >= maxIterations) throw new InvalidOperationException("Could not find solution root directory.");
		}

		return currentDir != null 
			? Path.Combine(currentDir.FullName, "..", "output") 
			: Path.Combine(AppContext.BaseDirectory, "..", "output");
	}
	
	public void LoadExclusions()
	{
		var exclusionsFile = Path.Combine(_overridesBasePath, "data", "exclusions.txt");
		if (!File.Exists(exclusionsFile)) return;

		foreach (var line in File.ReadAllLines(exclusionsFile))
		{
			if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;

			var target = Path.Combine(_outputBasePath, line.Trim());
			_exclusionCache[target] = true;
		}
	}

	public static void Configure(IServiceCollection services, ConfigurationManager config)
	{
		services.AddHostedService<JsonFileWriterService>();
	}
}