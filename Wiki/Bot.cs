namespace TombstoneWikiExtractor.Wiki;

public class Bot
{
	public string BotCookie { get; private set; } = "";
	public string EditToken { get; private set; } = "";

	public async Task Login()
	{
		Console.WriteLine("😑 Logging the bot in...");

		// Get login token
		var client = new HttpClient();
		var loginResponse = await client.GetAsync("https://tombstonemmowiki.org/api.php?action=query&format=json&meta=tokens&type=login");
		var loginCookie = loginResponse.Headers.GetValues("Set-Cookie").FirstOrDefault();
		var loginToken = JsonSerializer.Deserialize<JsonElement>(await loginResponse.Content.ReadAsStringAsync())
			.GetProperty("query").GetProperty("tokens").GetProperty("logintoken").GetString()
			?? throw new Exception("Failed to get login token");

		// Log the bot in
		var loginContent = new FormUrlEncodedContent([
			new KeyValuePair<string, string>("action", "login"),
			new KeyValuePair<string, string>("format", "json"),
			new KeyValuePair<string, string>("lgname", Config.BOT_USERNAME),
			new KeyValuePair<string, string>("lgpassword", Config.BOT_PASSWORD),
			new KeyValuePair<string, string>("lgtoken", loginToken)
		]);
		var loginRequest = new HttpRequestMessage(HttpMethod.Post, "https://tombstonemmowiki.org/api.php")
		{
			Content = loginContent
		};
		loginRequest.Headers.Add("Cookie", loginCookie);
		var loginResult = await client.SendAsync(loginRequest);
		var botCookie = loginResult.Headers.GetValues("Set-Cookie").FirstOrDefault()
			?? throw new Exception("Failed to get bot cookie");
		BotCookie = botCookie;

		// Get edit token
		var editRequest = new HttpRequestMessage(HttpMethod.Get, "https://tombstonemmowiki.org/api.php?action=query&meta=tokens&format=json");
		editRequest.Headers.Add("Cookie", botCookie);
		var editResult = await client.SendAsync(editRequest);
		var editToken = JsonSerializer.Deserialize<JsonElement>(await editResult.Content.ReadAsStringAsync())
			.GetProperty("query").GetProperty("tokens").GetProperty("csrftoken").GetString()
			?? throw new Exception("Failed to get edit token");
		EditToken = editToken;

		Console.WriteLine("🙂 Login successful, ready to edit");
	}
}