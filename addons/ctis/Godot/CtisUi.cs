using Ctis.Core;
using Godot;

namespace Ctis.Presentation;

public static class CtisUi
{
    public static void PlaceFloatClose(Control close, float size = 16f)
    {
        close.CustomMinimumSize = new Vector2(size, size);
        close.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        close.AnchorLeft = 1;
        close.AnchorTop = 0;
        close.AnchorRight = 1;
        close.AnchorBottom = 0;
        close.OffsetLeft = -size - 3;
        close.OffsetTop = 3;
        close.OffsetRight = -3;
        close.OffsetBottom = size + 3;
    }

    public static Label KeyLabel(string key, int fontSize, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        var label = new Label
        {
            Text = key,
            AutoTranslateMode = Node.AutoTranslateModeEnum.Always,
            HorizontalAlignment = align,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin
        };
        if (align == HorizontalAlignment.Center)
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }

    public static void ApplyLabelOutline(Label label, int size = 2)
    {
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", size);
    }

    public static Button KeyButton(string key)
    {
        return new Button
        {
            Text = key,
            AutoTranslateMode = Node.AutoTranslateModeEnum.Always
        };
    }

    public static OptionButton LocaleSelector()
    {
        var box = new OptionButton { Alignment = HorizontalAlignment.Center };
        box.AddThemeFontSizeOverride("font_size", 16);
        box.AddItem("English (en)", 0);
        box.AddItem("中文 (zh)", 1);
        box.Select(CtisLocale.IsChinese ? 1 : 0);
        box.ItemSelected += OnLocaleSelectorItemSelected;
        return box;
    }

    private static void OnLocaleSelectorItemSelected(long index)
        => CtisLocale.SetLocale(index == 1 ? "zh" : "en");

    public static SpinBox IntSpin(double min, double max, double value)
    {
        return new SpinBox
        {
            MinValue = min,
            MaxValue = max,
            Value = value,
            Step = 1,
            Rounded = true,
            CustomMinimumSize = new Vector2(70, 26)
        };
    }

    public static Button CreateOrganizeButton(Action<InventorySortStrategy> onOrganize, float minWidth = 44f, float minHeight = 20f)
    {
        var button = new MenuButton
        {
            Text = "CTIS_SORT",
            AutoTranslateMode = Node.AutoTranslateModeEnum.Always,
            CustomMinimumSize = new Vector2(minWidth, minHeight),
            MouseFilter = Control.MouseFilterEnum.Stop,
            FocusMode = Control.FocusModeEnum.None,
            Flat = false
        };

        var popup = button.GetPopup();
        popup.Clear();
        popup.AddItem("CTIS_SORT_AREA", (int)InventorySortStrategy.Area);
        popup.AddItem("CTIS_SORT_TYPE", (int)InventorySortStrategy.SlotType);
        popup.AddItem("CTIS_SORT_RARITY", (int)InventorySortStrategy.Rarity);
        popup.AddItem("CTIS_SORT_ITEM_ID", (int)InventorySortStrategy.ItemId);
        popup.AutoTranslateMode = Node.AutoTranslateModeEnum.Always;

        var bridge = new OrganizeMenuBridge(onOrganize);
        popup.IdPressed += bridge.OnIdPressed;
        return button;
    }

    public sealed class OrganizeMenuBridge
    {
        private readonly Action<InventorySortStrategy> _onOrganize;
        public OrganizeMenuBridge(Action<InventorySortStrategy> onOrganize) => _onOrganize = onOrganize;
        public void OnIdPressed(long id) => _onOrganize((InventorySortStrategy)id);
    }

    public static TextureButton CloseButton(Action pressed)
    {
        var close = new TextureButton
        {
            TextureNormal = CtisArt.Load(CtisArt.CloseIcon),
            IgnoreTextureSize = true,
            StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(16, 16),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        close.Pressed += pressed;
        return close;
    }

    public static void CenterWindowOnScreen(Control window)
    {
        var viewport = window.GetViewport()?.GetVisibleRect().Size ?? window.GetViewportRect().Size;
        if (viewport.X <= 0 || viewport.Y <= 0)
        {
            viewport = new Vector2(
                (float)ProjectSettings.GetSetting("display/window/size/viewport_width", 1152),
                (float)ProjectSettings.GetSetting("display/window/size/viewport_height", 648));
        }
        var size = window.Size.X > 0 && window.Size.Y > 0 ? window.Size : window.CustomMinimumSize;
        var pos = (viewport - size) * 0.5f;
        pos.X = MathF.Round(Math.Clamp(pos.X, 0, Math.Max(0, viewport.X - size.X)));
        pos.Y = MathF.Round(Math.Clamp(pos.Y, 0, Math.Max(0, viewport.Y - size.Y)));
        window.Position = pos;
    }
}


