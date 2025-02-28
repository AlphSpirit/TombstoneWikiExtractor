namespace TombstoneWikiExtractor.Wiki;

public abstract class Config
{
	public static string BOT_USERNAME { get; private set; } = null!;
	public static string BOT_PASSWORD { get; private set; } = null!;
	public static string GAME_PATH { get; private set; } = null!;

	public static void Load()
	{
		var configText = File.ReadAllText("config.json");
		var config = JsonSerializer.Deserialize<JsonElement>(configText);
		BOT_USERNAME = config.GetProperty("bot_username").GetString() ?? throw new Exception("bot_username is missing");
		BOT_PASSWORD = config.GetProperty("bot_password").GetString() ?? throw new Exception("bot_password is missing");
		GAME_PATH = config.GetProperty("game_path").GetString() ?? throw new Exception("game_path is missing");
	}
}