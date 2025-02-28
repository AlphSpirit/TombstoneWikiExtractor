namespace TombstoneWikiExtractor.Wiki;

public static class ItemType
{
	public static string[] GetTypes(int flags)
	{
		var types = new List<string>();

		if ((flags & 1) != 0)
			types.Add("Farm Animal Feed");
		if ((flags & 2) != 0)
			types.Add("Water");
		if ((flags & 4) != 0)
			types.Add("Fishing Bait");
		if ((flags & 8) != 0)
			types.Add("Fire Starter");
		if ((flags & 16) != 0)
			types.Add("Sugary Food");
		if ((flags & 32) != 0)
			types.Add("Cooked");
		if ((flags & 64) != 0)
			types.Add("Fish");
		if ((flags & 128) != 0)
			types.Add("Ring Cast");
		if ((flags & 256) != 0)
			types.Add("Earring Cast");
		if ((flags & 512) != 0)
			types.Add("Necklace Cast");
		if ((flags & 1024) != 0)
			types.Add("Ore");
		if ((flags & 2048) != 0)
			types.Add("Bone");
		if ((flags & 4096) != 0)
			types.Add("Leather");

		return [.. types];
	}
}