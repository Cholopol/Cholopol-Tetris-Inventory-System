using Ctis.Core;
using Godot;

namespace Ctis.Presentation;

public static class CtisArt
{
    public const string AddonRoot = "res://addons/ctis/Art/";
    public const string HostRoot = "res://Art/";

    public static string DefaultIcon => Locate("Icons/DefaultIcon.png");
    public static string EditorLogo => Locate("Icons/LOGO_Gray.png");
    public static string CloseIcon => Locate("Icons/Close.png");
    public static string WindowBanner => Locate("Sprites/UI_Inventory/UI Banner.png");
    public static string InventoryBackground => Locate("Sprites/UI_Inventory/Bag UI.png");
    public static string SlotFallback => Locate("Sprites/UI_Inventory/BagSlot.png");
    public static string SlotPlate => Locate("Sprites/UI_Inventory/UI Banner 1.png");
    public static string ColumnPlate => Locate("Sprites/UI_Inventory/UI Banner 2.png");
    public const int SlotPlatePatch = 1;
    public const int EmbeddedBagPatch = 2;

    public static string Packet => Locate("Sprites/UI_Inventory/SquareBG_Packet.png");

    public static string SlotBackground(InventorySlotType type) => type switch
    {
        InventorySlotType.SmallConsume => Locate("Sprites/UI_Inventory/SquareBG_Packet.png"),
        InventorySlotType.MiddleConsume => Locate("Sprites/UI_Inventory/SquareBG_CIG.png"),
        InventorySlotType.LargeConsume => Locate("Sprites/UI_Inventory/SquareBG_CIG.png"),
        InventorySlotType.LongWeapon => Locate("Sprites/UI_Inventory/BG_Weapon.png"),
        InventorySlotType.ShortWeapon => Locate("Sprites/UI_Inventory/SquareBG_Pistol.png"),
        InventorySlotType.Vest => Locate("Sprites/UI_Inventory/SquareBG_Vest.png"),
        InventorySlotType.BackPack => Locate("Sprites/UI_Inventory/SquareBG_Bag.png"),
        InventorySlotType.WaistBag => Locate("Sprites/UI_Inventory/SquareBG_WaistBag.png"),
        InventorySlotType.Helmet => Locate("Sprites/UI_Inventory/SquareBG_Helmet.png"),
        InventorySlotType.Coat => Locate("Sprites/UI_Inventory/SquareBG_Jacket.png"),
        InventorySlotType.Pants => Locate("Sprites/UI_Inventory/SquareBG_Pants.png"),
        InventorySlotType.Shoes => Locate("Sprites/UI_Inventory/SquareBG_Shoes.png"),
        InventorySlotType.Pocket => Packet,
        InventorySlotType.Coffer => Locate("Sprites/UI_Inventory/SquareBG_Safe Box.png"),
        InventorySlotType.HeadMountedEquipment => Locate("Sprites/UI_Inventory/SquareBG_Visual.png"),
        InventorySlotType.Melee => Locate("Sprites/UI_Inventory/SquareBG_Knife.png"),
        _ => SlotFallback
    };

    private static readonly Dictionary<string, Texture2D> Loaded = new();
    private static readonly Dictionary<string, Texture2D> Copied = new();
    private static readonly Dictionary<string, Image> SourceImages = new();

    /// <summary>Resolves a relative art path, preferring the plugin folder then the host project.</summary>
    public static string Locate(string relative)
    {
        var addon = AddonRoot + relative;
        if (ResourceLoader.Exists(addon)) return addon;
        var host = HostRoot + relative;
        if (ResourceLoader.Exists(host)) return host;
        return addon;
    }

    /// <summary>Loads a texture or atlas slice, sharing GPU textures across callers.</summary>
    public static Texture2D? Load(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (Loaded.TryGetValue(path, out var cached) && GodotObject.IsInstanceValid(cached))
            return cached;
        var icon = ItemIconRef.Parse(path);
        var resolved = icon.Path.StartsWith("res://", StringComparison.Ordinal) ? icon.Path : Locate(icon.Path);
        if (!ResourceLoader.Exists(resolved))
        {
            GD.PushError($"[CTIS] Missing texture: {resolved}");
            return null;
        }
        var baseTex = ResourceLoader.Load<Texture2D>(resolved);
        if (baseTex == null) return null;
        Texture2D result = icon.HasRegion
            ? new AtlasTexture
            {
                Atlas = baseTex,
                Region = new Rect2(icon.X, icon.Y, icon.Width, icon.Height),
                FilterClip = true
            }
            : baseTex;
        Loaded[path] = result;
        return result;
    }

    /// <summary>Loads the unsliced source texture for an icon key.</summary>
    public static Texture2D? LoadBase(string path)
        => Load(ItemIconRef.Parse(path).Path);

    /// <summary>Returns a standalone small texture for UI lists that cannot draw atlas slices.</summary>
    public static Texture2D? LoadCopied(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (Copied.TryGetValue(path, out var cached) && GodotObject.IsInstanceValid(cached))
            return cached;
        var tex = Load(path);
        if (tex is not AtlasTexture atlas || atlas.Atlas == null)
            return tex;
        var copy = CopyRegion(atlas.Atlas, ItemIconRef.Parse(path)) ?? tex;
        Copied[path] = copy;
        return copy;
    }

    private static Texture2D? CopyRegion(Texture2D source, ItemIconRef icon)
    {
        var image = GetSourceImage(source);
        if (image == null || image.IsEmpty()) return null;
        var bounds = new Rect2I(0, 0, image.GetWidth(), image.GetHeight());
        var region = new Rect2I(icon.X, icon.Y, icon.Width, icon.Height).Intersection(bounds);
        if (region.Size.X <= 0 || region.Size.Y <= 0)
            return source;
        return ImageTexture.CreateFromImage(image.GetRegion(region));
    }

    private static Image? GetSourceImage(Texture2D source)
    {
        var key = source.ResourcePath;
        if (string.IsNullOrEmpty(key))
            key = source.GetInstanceId().ToString();
        if (SourceImages.TryGetValue(key, out var cached) && GodotObject.IsInstanceValid(cached))
            return cached;
        var image = source.GetImage();
        if (image == null || image.IsEmpty()) return null;
        if (image.IsCompressed())
            image.Decompress();
        SourceImages[key] = image;
        return image;
    }

    public static NinePatchRect CreateSlotPlate()
        => CreateNinePatch("SlotPlate", SlotPlate);

    public static Control CreateSlotFace(string patternPath, int cellsW = 2, int cellsH = 2)
    {
        var face = new Control
        {
            CustomMinimumSize = new Vector2(
                CtisSettings.GridTileSizeWidth * cellsW,
                CtisSettings.GridTileSizeHeight * cellsH),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            ClipContents = true
        };
        face.AddChild(CreateSlotPlate());
        var pattern = new TextureRect
        {
            Name = "SlotPattern",
            Texture = Load(patternPath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        };
        pattern.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        face.AddChild(pattern);
        return face;
    }

    public static NinePatchRect CreateColumnPlate()
        => CreateNinePatch("ColumnPlate", ColumnPlate);

    public static NinePatchRect CreateEmbeddedBagPlate()
        => CreateNinePatch(
            "EmbeddedBag",
            InventoryBackground,
            EmbeddedBagPatch,
            EmbeddedBagPatch,
            EmbeddedBagPatch,
            EmbeddedBagPatch);

    public static NinePatchRect CreateNinePatch(string name, string path)
        => CreateNinePatch(name, path, SlotPlatePatch, SlotPlatePatch, SlotPlatePatch, SlotPlatePatch);

    public static NinePatchRect CreateNinePatch(string name, string path, int left, int top, int right, int bottom)
    {
        var plate = new NinePatchRect
        {
            Name = name,
            Texture = Load(path),
            PatchMarginLeft = left,
            PatchMarginTop = top,
            PatchMarginRight = right,
            PatchMarginBottom = bottom,
            AxisStretchHorizontal = NinePatchRect.AxisStretchMode.Stretch,
            AxisStretchVertical = NinePatchRect.AxisStretchMode.Stretch,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        };
        plate.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        return plate;
    }
}
