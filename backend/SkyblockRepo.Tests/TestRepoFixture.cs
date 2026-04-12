using System.IO.Compression;

namespace SkyblockRepo.Tests;

internal sealed class TempDirectory : IDisposable
{
	public TempDirectory()
	{
		Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SkyblockRepoTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(Path);
	}

	public string Path { get; }

	public void Dispose()
	{
		if (Directory.Exists(Path))
		{
			Directory.Delete(Path, true);
		}
	}
}

internal static class TestRepoFixture
{
	public static void WriteRepoContents(string repoRoot, string itemName = "Brown Mushroom")
	{
		Directory.CreateDirectory(repoRoot);
		Directory.CreateDirectory(System.IO.Path.Combine(repoRoot, "items"));
		Directory.CreateDirectory(System.IO.Path.Combine(repoRoot, "pets"));
		Directory.CreateDirectory(System.IO.Path.Combine(repoRoot, "enchantments"));
		Directory.CreateDirectory(System.IO.Path.Combine(repoRoot, "npcs"));
		Directory.CreateDirectory(System.IO.Path.Combine(repoRoot, "shops"));
		Directory.CreateDirectory(System.IO.Path.Combine(repoRoot, "zones"));
		Directory.CreateDirectory(System.IO.Path.Combine(repoRoot, "misc"));

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "manifest.json"), """
		{
		  "version": 1,
		  "paths": {
		    "items": "items",
		    "pets": "pets",
		    "enchantments": "enchantments",
		    "npcs": "npcs",
		    "zones": "zones",
		    "misc": "misc"
		  }
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "items", "BROWN_MUSHROOM.json"), $$"""
		{
		  "internalId": "BROWN_MUSHROOM",
		  "name": "{{itemName}}"
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "pets", "MUSHROOM_PET.json"), """
		{
		  "internalId": "MUSHROOM_PET",
		  "name": "Mushroom Pet"
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "enchantments", "SHARPNESS.json"), """
		{
		  "internalId": "SHARPNESS",
		  "name": "Sharpness"
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "npcs", "MUSHROOM_FARM_MERCHANT.json"), """
		{
		  "internalId": "MUSHROOM_FARM_MERCHANT",
		  "name": "Mushroom Merchant",
		  "location": {
		    "zone": "MUSHROOM_DESERT",
		    "coordinates": {
		      "x": 0,
		      "y": 70,
		      "z": 0
		    }
		  }
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "shops", "MUSHROOM_SHOP.json"), """
		{
		  "internalId": "MUSHROOM_SHOP",
		  "name": "Mushroom Shop",
		  "slots": {}
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "zones", "MUSHROOM_DESERT.json"), """
		{
		  "internalId": "MUSHROOM_DESERT",
		  "name": "Mushroom Desert"
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "misc", "taylors_collection.json"), """
		{
		  "items": [
		    {
		      "name": "Autumn Bundle",
		      "output": [],
		      "cost": [],
		      "released": "2025-01-01"
		    }
		  ]
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "misc", "seasonal_bundles.json"), """
		{
		  "items": [
		    {
		      "name": "Winter Bundle",
		      "output": [],
		      "cost": [],
		      "released": "2025-12-01"
		    }
		  ]
		}
		""");
	}

	public static byte[] CreateGithubZipBytes(string sourceDirectory, string rootFolderName = "repo-main")
	{
		var files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);

		using var memoryStream = new MemoryStream();
		using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
		{
			AddFilesToArchive(sourceDirectory, files, archive, rootFolderName);
		}

		return memoryStream.ToArray();
	}

	public static void CreateGithubZip(string sourceDirectory, string zipPath, string rootFolderName = "repo-main")
	{
		var fullZipPath = System.IO.Path.GetFullPath(zipPath);
		var files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories)
			.Where(path => !string.Equals(System.IO.Path.GetFullPath(path), fullZipPath, StringComparison.OrdinalIgnoreCase))
			.ToArray();

		Directory.CreateDirectory(System.IO.Path.GetDirectoryName(zipPath)!);

		using var archiveStream = File.Create(zipPath);
		using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create);
		AddFilesToArchive(sourceDirectory, files, archive, rootFolderName);
	}

	private static void AddFilesToArchive(string sourceDirectory, IEnumerable<string> files, ZipArchive archive, string rootFolderName)
	{
		foreach (var file in files)
		{
			var relativePath = System.IO.Path.GetRelativePath(sourceDirectory, file).Replace('\\', '/');
			archive.CreateEntryFromFile(file, $"{rootFolderName}/{relativePath}");
		}
	}
}
