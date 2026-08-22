using Ctis.Core;
using Godot;

namespace Ctis.Presentation;

public static class ItemCatalogLoader
{
	public const string SettingKey = "ctis/item_catalog";

	public static string CatalogPath
		=> CtisJsonFileStore.PathSetting(SettingKey, "");

	/// <summary>Loads catalog JSON from project settings. Missing or empty files yield an empty catalog.</summary>
	public static void LoadInto(IItemCatalog catalog)
		=> catalog.ReplaceAll(LoadOrDefault());

	/// <summary>Reads the catalog file, or an empty list when missing or unconfigured.</summary>
	public static List<ItemDetails> LoadOrDefault()
	{
		using var _ = CtisTrace.Scope("Catalog.Load");
		var path = CatalogPath;
		if (string.IsNullOrEmpty(path) || !Godot.FileAccess.FileExists(path))
			return new List<ItemDetails>();
		return CtisJsonFileStore.Read(path, ItemCatalogJson.Parse) ?? new List<ItemDetails>();
	}

	/// <summary>Writes the catalog JSON to the configured path.</summary>
	public static Error Save(IReadOnlyList<ItemDetails> items)
	{
		using var _ = CtisTrace.Scope("Catalog.Save");
		var path = CatalogPath;
		if (string.IsNullOrEmpty(path))
			return Error.FileNotFound;
		return CtisJsonFileStore.Write(path, ItemCatalogJson.Serialize(items));
	}
}
