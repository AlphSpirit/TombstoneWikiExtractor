using TombstoneWikiExtractor.Wiki;

Config.Load();
var bot = new Bot();
await bot.Login();
var items = new Items();
await items.Load(bot);