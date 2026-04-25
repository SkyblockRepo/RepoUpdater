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

	public static void WriteExtendedRepoContents(string repoRoot)
	{
		WriteRepoContents(repoRoot);

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "items", "RIFT_PRISM.json"), """
		{
		  "internalId": "RIFT_PRISM",
		  "name": "Rift Prism",
		  "category": "Accessory",
		  "data": {
		    "id": "RIFT_PRISM",
		    "name": "Rift Prism",
		    "category": "ACCESSORY",
		    "tier": "EPIC"
		  }
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "items", "HEGEMONY_ARTIFACT.json"), """
		{
		  "internalId": "HEGEMONY_ARTIFACT",
		  "name": "Hegemony Artifact",
		  "category": "Accessory",
		  "data": {
		    "id": "HEGEMONY_ARTIFACT",
		    "name": "Hegemony Artifact",
		    "category": "ACCESSORY",
		    "tier": "LEGENDARY"
		  }
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "items", "INK_SACK-3.json"), """
		{
		  "internalId": "INK_SACK:3",
		  "name": "Cocoa Beans",
		  "category": "Item",
		  "data": {
		    "id": "INK_SACK:3",
		    "name": "Cocoa Beans",
		    "tier": "COMMON"
		  }
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "npcs", "GUIDE_VISITOR.json"), """
		{
		  "internalId": "GUIDE_VISITOR",
		  "name": "Guide Visitor"
		}
		""");
	}

	public static void WriteNeuRepoContents(string repoRoot)
	{
		Directory.CreateDirectory(repoRoot);
		Directory.CreateDirectory(System.IO.Path.Combine(repoRoot, "items"));
		Directory.CreateDirectory(System.IO.Path.Combine(repoRoot, "constants"));

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "items", "ATTRIBUTE_SHARD_SPEED;1.json"), """
		{
		  "itemid": "minecraft:skull",
		  "displayname": "Speed Attribute Shard",
		  "nbttag": "{SkullOwner:{Properties:{textures:[0:{Value:\"speed-texture\"}]}}}",
		  "damage": 3,
		  "lore": [
		    "Speed lore line"
		  ],
		  "internalname": "ATTRIBUTE_SHARD_SPEED;1"
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "items", "RIFT_PRISM.json"), """
		{
		  "itemid": "minecraft:skull",
		  "displayname": "Rift Prism",
		  "nbttag": "{SkullOwner:{Properties:{textures:[0:{Value:\"rift-prism-texture\"}]}}}",
		  "lore": [
		    "Travel to the Rift.",
		    "",
		    "RARE ACCESSORY"
		  ],
		  "internalname": "RIFT_PRISM"
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "items", "HEGEMONY_ARTIFACT.json"), """
		{
		  "itemid": "minecraft:skull",
		  "displayname": "Hegemony Artifact",
		  "nbttag": "{SkullOwner:{Properties:{textures:[0:{Value:\"hegemony-texture\"}]}}}",
		  "lore": [
		    "Counts for twice the Magical Power in",
		    "your Accessory Bag.",
		    "",
		    "LEGENDARY ACCESSORY"
		  ],
		  "internalname": "HEGEMONY_ARTIFACT"
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "items", "TALISMAN_ENRICHMENT_DEFENSE.json"), """
		{
		  "itemid": "minecraft:skull",
		  "displayname": "Defense Enrichment",
		  "nbttag": "{SkullOwner:{Properties:{textures:[0:{Value:\"defense-enrichment-texture\"}]}}}",
		  "lore": [
		    "Enriches an accessory with Defense."
		  ],
		  "internalname": "TALISMAN_ENRICHMENT_DEFENSE"
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "items", "CHOCOBERRY.json"), """
		{
		  "itemid": "minecraft:skull",
		  "displayname": "Chocoberry",
		  "nbttag": "{SkullOwner:{Properties:{textures:[0:{Value:\"chocoberry-texture\"}]}}}",
		  "lore": [
		    "Analyze this crop!",
		    "",
		    "UNCOMMON MUTATION"
		  ],
		  "internalname": "CHOCOBERRY"
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "items", "BLOBFISH_BRONZE.json"), """
		{
		  "itemid": "minecraft:skull",
		  "displayname": "Blobfish BRONZE",
		  "nbttag": "{SkullOwner:{Properties:{textures:[0:{Value:\"blobfish-bronze-texture\"}]}}}",
		  "lore": [
		    "Caught everywhere.",
		    "",
		    "COMMON TROPHY FISH"
		  ],
		  "internalname": "BLOBFISH_BRONZE"
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "items", "BLOBFISH_SILVER.json"), """
		{
		  "itemid": "minecraft:skull",
		  "displayname": "Blobfish SILVER",
		  "nbttag": "{SkullOwner:{Properties:{textures:[0:{Value:\"blobfish-silver-texture\"}]}}}",
		  "lore": [
		    "Caught everywhere.",
		    "",
		    "COMMON TROPHY FISH"
		  ],
		  "internalname": "BLOBFISH_SILVER"
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "items", "BLOBFISH_GOLD.json"), """
		{
		  "itemid": "minecraft:skull",
		  "displayname": "Blobfish GOLD",
		  "nbttag": "{SkullOwner:{Properties:{textures:[0:{Value:\"blobfish-gold-texture\"}]}}}",
		  "lore": [
		    "Caught everywhere.",
		    "",
		    "COMMON TROPHY FISH"
		  ],
		  "internalname": "BLOBFISH_GOLD"
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "items", "BLOBFISH_DIAMOND.json"), """
		{
		  "itemid": "minecraft:skull",
		  "displayname": "Blobfish DIAMOND",
		  "nbttag": "{SkullOwner:{Properties:{textures:[0:{Value:\"blobfish-diamond-texture\"}]}}}",
		  "lore": [
		    "Caught everywhere.",
		    "",
		    "COMMON TROPHY FISH"
		  ],
		  "internalname": "BLOBFISH_DIAMOND"
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "items", "WHEAT_GENERATOR_1.json"), """
		{
		  "itemid": "minecraft:skull",
		  "displayname": "Wheat Minion I",
		  "nbttag": "{SkullOwner:{Properties:{textures:[0:{Value:\"wheat-minion-texture\"}]}}}",
		  "lore": [
		    "Helps with wheat."
		  ],
		  "internalname": "WHEAT_GENERATOR_1"
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "items", "DROPLET_WISP;1.json"), """
		{
		  "itemid": "minecraft:skull",
		  "displayname": "[Lvl {LVL}] Droplet Wisp",
		  "nbttag": "{SkullOwner:{Properties:{textures:[0:{Value:\"droplet-wisp-texture\"}]}}}",
		  "lore": [
		    "Gabagool Pet",
		    "",
		    "UNCOMMON"
		  ],
		  "internalname": "DROPLET_WISP;1"
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "items", "SUBZERO_WISP;4.json"), """
		{
		  "itemid": "minecraft:skull",
		  "displayname": "[Lvl {LVL}] Subzero Wisp",
		  "nbttag": "{SkullOwner:{Properties:{textures:[0:{Value:\"subzero-wisp-texture\"}]}}}",
		  "lore": [
		    "Gabagool Pet",
		    "",
		    "MYTHIC"
		  ],
		  "internalname": "SUBZERO_WISP;4"
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "items", "RIFT_TROPHY_WYLDLY_SUPREME.json"), """
		{
		  "itemid": "minecraft:spruce_leaves",
		  "displayname": "Supreme Timecharm",
		  "nbttag": "{ExtraAttributes:{id:\"RIFT_TROPHY_WYLDLY_SUPREME\"}}",
		  "lore": [
		    "Bring this back to Elise.",
		    "",
		    "SPECIAL RIFT TIMECHARM"
		  ],
		  "internalname": "RIFT_TROPHY_WYLDLY_SUPREME"
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "items", "MCGRUBBER_BURGER.json"), """
		{
		  "itemid": "minecraft:skull",
		  "displayname": "McGrubber's Burger",
		  "nbttag": "{SkullOwner:{Properties:{textures:[0:{Value:\"mcgrubber-burger-texture\"}]}}}",
		  "lore": [
		    "Each stack grants bonuses.",
		    "Max 5 stacks!",
		    "",
		    "EPIC"
		  ],
		  "internalname": "MCGRUBBER_BURGER"
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "constants", "bestiary.json"), """
		{
		  "brackets": {
		    "1": [10, 25, 50]
		  },
		  "spiders": {
		    "name": "Spiders",
		    "icon": {
		      "texture": "category-texture"
		    },
		    "mobs": [
		      {
		        "name": "Brood Mother",
		        "texture": "mob-texture",
		        "cap": 50,
		        "mobs": ["brood_mother"],
		        "bracket": 1
		      }
		    ]
		  }
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "constants", "attribute_shards.json"), """
		{
		  "attribute_levelling": {
		    "COMMON": [1, 2, 3]
		  },
		  "unconsumable_attributes": ["mana_pool"],
		  "attributes": [
		    {
		      "bazaarName": "SHARD_SPEED",
		      "displayName": "Speed Attribute Shard",
		      "rarity": "COMMON",
		      "internalName": "ATTRIBUTE_SHARD_SPEED;1",
		      "abilityName": "Fleet",
		      "alignment": "neutral",
		      "family": ["speed"],
		      "shardId": "speed"
		    }
		  ]
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "constants", "garden.json"), """
		{
		  "garden_exp": [0, 100, 250],
		  "crop_milestones": {
		    "WHEAT": [100, 500]
		  },
		  "visitors": {
		    "guide_visitor": "COMMON"
		  },
		  "plots": {
		    "plot_1": {
		      "name": "Plot 1",
		      "x": 0,
		      "y": 0
		    }
		  },
		  "plot_costs": {
		    "plot_2": [
		      {
		        "item": "COMPOST",
		        "amount": 5
		      }
		    ]
		  },
		  "barn": {
		    "default": {
		      "name": "Default Barn",
		      "item": "BARN_SKIN"
		    }
		  },
		  "crop_upgrades": [5, 10],
		  "composter_upgrades": {
		    "fuel_cap": {
		      "1": {
		        "upgrade": 100,
		        "items": {
		          "COMPOST": 1
		        },
		        "copper": 10
		      }
		    }
		  },
		  "composter_tooltips": {
		    "fuel_cap": "Increase fuel cap."
		  }
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "constants", "misc.json"), """
		{
		  "talisman_upgrades": {
		    "WEDDING_RING_0": ["WEDDING_RING_1"]
		  },
		  "ignored_talisman": ["CAMPFIRE_BADGE_1"],
		  "minions": {
		    "WHEAT_GENERATOR": 12
		  }
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "constants", "parents.json"), """
		{
		  "WHEAT_GENERATOR_1": [
		    "WHEAT_GENERATOR_2",
		    "WHEAT_GENERATOR_12"
		  ],
		  "BLOBFISH_DIAMOND": [
		    "BLOBFISH_GOLD",
		    "BLOBFISH_SILVER",
		    "BLOBFISH_BRONZE"
		  ],
		  "SUBZERO_WISP;4": [
		    "DROPLET_WISP;1"
		  ]
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "constants", "trophyfish.json"), """
		{
		  "BLOBFISH": [1, 5, 10, 25]
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "constants", "sblevels.json"), """
		{
		  "fishing_task": {
		    "dolphin_milestone_required": [250, 1000, 2500]
		  }
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "constants", "pets.json"), """
		{
		  "pet_rarity_offset": {
		    "COMMON": 0
		  },
		  "pet_levels": [100, 200, 300],
		  "custom_pet_leveling": {
		    "DROPLET_WISP": {
		      "type": 2,
		      "pet_levels": [10, 20],
		      "max_level": 2,
		      "xp_multiplier": 1.5
		    }
		  },
		  "pet_types": {
		    "DROPLET_WISP": "combat"
		  },
		  "id_to_display_name": {
		    "DROPLET_WISP": "Droplet Wisp"
		  },
		  "pet_item_display_name_to_id": {
		    "Lucky Clover": "LUCKY_CLOVER"
		  }
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "constants", "petnums.json"), """
		{
		  "DROPLET_WISP": {
		    "COMMON": {
		      "1": {
		        "otherNums": [1],
		        "statNums": {
		          "speed": 1
		        }
		      },
		      "100": {
		        "otherNums": [100],
		        "statNums": {
		          "speed": 100
		        }
		      },
		      "stats_levelling_curve": "101:200:1"
		    }
		  }
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "constants", "rift_guide.json"), """
		{
		  "castle": [
		    {
		      "id": "enter_castle",
		      "name": "Enter the Castle",
		      "description": "Reach the castle courtyard."
		    },
		    {
		      "id": "rift_eye_1",
		      "name": "First Eye",
		      "description": "Find the first eye."
		    },
		    {
		      "id": "rift_enigma_bark",
		      "name": "Enigma Bark",
		      "description": "Solve the bark puzzle."
		    }
		  ]
		}
		""");

		File.WriteAllText(System.IO.Path.Combine(repoRoot, "constants", "essenceshops.json"), """
		{
		  "ESSENCE_UNDEAD": {
		    "fortitude": {
		      "name": "Fortitude",
		      "costs": [10, 25]
		    }
		  }
		}
		""");
	}

	public static void WriteCollectionsPayload(
		string filePath,
		long lastUpdated = 1710000000000,
		int firstThreshold = 75,
		int secondThreshold = 200)
	{
		Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath)!);
		File.WriteAllText(filePath, $$"""
		{
		  "success": true,
		  "lastUpdated": {{lastUpdated}},
		  "version": "test",
		  "collections": {
		    "FARMING": {
		      "name": "Farming",
		      "items": {
		        "INK_SACK:3": {
		          "name": "Cocoa Beans",
		          "maxTiers": 2,
		          "tiers": [
		            {
		              "tier": 1,
		              "amountRequired": {{firstThreshold}}
		            },
		            {
		              "tier": 2,
		              "amountRequired": {{secondThreshold}}
		            }
		          ]
		        }
		      }
		    }
		  }
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
