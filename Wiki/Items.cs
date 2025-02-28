using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Web;

namespace TombstoneWikiExtractor.Wiki;

public partial class Items
{
	private List<string> internalIds = [];

	public async Task Load(Bot bot)
	{
		Console.WriteLine("📦 Loading items and enemies...");

		var catalog = File.ReadAllText($"{Config.GAME_PATH}/app/Tombstone_Data/StreamingAssets/aa/catalog.json");
		var catalogJson = JsonSerializer.Deserialize<JsonElement>(catalog);
		internalIds = catalogJson.GetProperty("m_InternalIds").EnumerateArray().Select(id => id.GetString()).ToList()!;
		var itemIds = internalIds.Where(id => id.StartsWith("Assets/ScriptableObjects/Items/"));
		var enemyIds = internalIds.Where(id => id.StartsWith("Assets/ScriptableObjects/EnemyData/"));

		Console.WriteLine($"🔍 Found {itemIds.Count()} items and {enemyIds.Count()} enemies");

		Console.WriteLine("📥 Downloading item files...");
		await DownloadItemFiles();
		// await DownloadAllBundleFiles();

		Console.WriteLine("🔪 Editing items...");
		Parallel.ForEach(itemIds, id => EditPage(id, bot).Wait());
		// foreach (var id in itemIds)
		// {
		// 	await EditPage(id, bot);
		// }
	}

	private async Task DownloadItemFiles()
	{
		var itemDataId = internalIds.First(id => id.EndsWith("items_assets_all.bundle"));
		var client = new HttpClient();
		var itemDataResponse = await client.GetAsync(itemDataId);
		File.WriteAllBytes("items_assets_all.bundle", await itemDataResponse.Content.ReadAsByteArrayAsync());

		// Check if AssetStudioModCLI is installed
		if (!Directory.Exists("AssetStudioModCLI"))
		{
			Directory.CreateDirectory("AssetStudioModCLI");
			var assetStudioModCli = await client.GetAsync("https://github.com/aelurum/AssetStudio/releases/download/v0.18.0/AssetStudioModCLI_net8_portable.zip");
			File.WriteAllBytes("AssetStudioModCLI.zip", await assetStudioModCli.Content.ReadAsByteArrayAsync());
			ZipFile.ExtractToDirectory("AssetStudioModCLI.zip", "AssetStudioModCLI");
			File.Delete("AssetStudioModCLI.zip");
			Console.WriteLine("🛠️ AssetStudioModCLI installed");
		}

		Console.WriteLine("📁 Extracting item files...");
		var process = new Process();
		process.StartInfo.FileName = "dotnet";
		process.StartInfo.Arguments = "AssetStudioModCLI/AssetStudioModCLI.dll items_assets_all.bundle -o . --log-output file";
		process.StartInfo.RedirectStandardOutput = true;
		process.Start();
		process.WaitForExit();
		File.Delete("items_assets_all.bundle");
		Console.WriteLine("📦 Item files extracted");
	}

	private async Task DownloadAllBundleFiles()
	{
		var client = new HttpClient();
		var allBundleIds = internalIds.Where(id => id.StartsWith("http") && id.EndsWith(".bundle"));
		foreach (var id in allBundleIds)
		{
			var fileName = id.Split('/').Last();
			Console.WriteLine($"📥 Downloading {fileName}...");
			var response = await client.GetAsync(id);
			File.WriteAllBytes(fileName, await response.Content.ReadAsByteArrayAsync());
			var process = new Process();
			process.StartInfo.FileName = "dotnet";
			process.StartInfo.Arguments = $"AssetStudioModCLI/AssetStudioModCLI.dll {fileName} -o Assets/test --log-output file";
			process.Start();
			process.WaitForExit();
			File.Delete(fileName);
		}
	}

	private async Task EditPage(string itemId, Bot bot)
	{
		var itemLocation = itemId.Replace(".asset", ".json");
		if (!File.Exists(itemLocation))
		{
			Console.WriteLine($"❌ Item file {itemLocation} not found");
			return;
		}
		var itemText = File.ReadAllText(itemLocation);
		var itemJson = JsonSerializer.Deserialize<JsonElement>(itemText);
		var itemName = itemJson.GetProperty("m_Name").GetString()!;
		var itemDescription = itemJson.GetProperty("Description").GetString();
		var itemValue = itemJson.GetProperty("Value").GetInt32();
		var itemType = itemJson.GetProperty("ItemType").GetInt32();
		var itemTypes = ItemType.GetTypes(itemType);
		var joinedTypes = string.Join(", ", itemTypes);
		var client = new HttpClient();
		var existingPageResponse = await client.GetAsync($"https://tombstonemmowiki.org/index.php?action=raw&title={HttpUtility.UrlEncode(itemName)}");
		if (!existingPageResponse.IsSuccessStatusCode)
		{
			Console.WriteLine($"❌ Failed to get {itemName}, retrying...");
			await EditPage(itemId, bot);
			return;
		}
		var pageContent = await existingPageResponse.Content.ReadAsStringAsync();
		if (pageContent.Contains("<html>") || pageContent.Contains("MediaWiki"))
		{
			Console.WriteLine($"⚠️ Page for {itemName} is corrupted, might need a manual fix");
		}
		var newPage = pageContent == "";

		if (pageContent.Contains("{{ItemAcquisition}}") && pageContent.Contains("{{ItemUsedIn}}")
			&& !pageContent.Contains("{{Stub}}"))
		{
			pageContent = "{{Stub}}\n\n" + pageContent;
		}

		// Update ItemFloat
		if (!pageContent.Contains("{{ItemFloat"))
		{
			pageContent = $"\n\n{{{{ItemFloat|name={itemName}\n|type={joinedTypes}\n|description={itemDescription}\n|value={itemValue}\n}}}}\n\n" + pageContent;
		}
		else
		{
			var regex = new Regex("{{ItemFloat[a-zA-Z\r\n|{}= .0-9,']*?}}");
			pageContent = regex.Replace(pageContent, $"{{{{ItemFloat\n|name={itemName}\n|type={joinedTypes}\n|description={itemDescription}\n|value={itemValue}\n}}}}\n\n");
		}

		// Update ItemAcquisition
		if (!pageContent.Contains("{{ItemAcquisition"))
		{
			pageContent += "\n\n{{ItemAcquisition}}";
		}

		// Update ItemUsedIn
		if (!pageContent.Contains("{{ItemUsedIn"))
		{
			pageContent += "\n\n{{ItemUsedIn}}";
		}

		// Format page content
		while (pageContent.Contains("\n\n\n"))
		{
			pageContent = pageContent.Replace("\n\n\n", "\n\n");
		}
		pageContent = pageContent.Trim();

		// Post page content
		var editContent = new FormUrlEncodedContent([
			new KeyValuePair<string, string>("action", "edit"),
			new KeyValuePair<string, string>("format", "json"),
			new KeyValuePair<string, string>("title", itemName),
			new KeyValuePair<string, string>("text", pageContent),
			new KeyValuePair<string, string>("token", bot.EditToken),
			new KeyValuePair<string, string>("bot", "True")
		]);
		var editRequest = new HttpRequestMessage(HttpMethod.Post, "https://tombstonemmowiki.org/api.php")
		{
			Content = editContent
		};
		editRequest.Headers.Add("Cookie", bot.BotCookie);
		var response = await client.SendAsync(editRequest);
		if (!response.IsSuccessStatusCode)
		{
			Console.WriteLine($"❌ Failed to edit {itemName}, retrying...");
			await EditPage(itemId, bot);
			return;
		}
		// if (newPage)
		// {
		// 	Console.WriteLine($"📝 Created {itemName}");
		// 	return;
		// }
		// Console.WriteLine($"📝 Edited {itemName}");
	}
}