using Ctis.Core;
using Godot;

#pragma warning disable CS0618 // EditorSpinSlider.HideSlider — ControlState name differs across Godot 4.x

namespace Ctis.Presentation.Editor;

internal static class InspectorWidgets
{
    public const float LabelWidth = 140f;
    public const int SwatchWidth = 160;
    public const int SwatchHeight = 32;
    public const int IconSize = 32;

    public static void NoTranslate(Node node)
    {
        node.AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled;
        foreach (var child in node.GetChildren())
            NoTranslate(child);
    }

    public static Label Caption(string text, float width = LabelWidth)
    {
        var label = new Label
        {
            Text = text,
            CustomMinimumSize = new Vector2(width, 0),
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            MouseFilter = Control.MouseFilterEnum.Stop,
            AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled
        };
        label.AddThemeColorOverride("font_color", CtisEditorTheme.Label);
        label.AddThemeFontSizeOverride("font_size", CtisEditorTheme.FontCaption);
        label.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        return label;
    }

    public static HBoxContainer Labeled(string label, Control field, float labelWidth = LabelWidth, bool expandField = true)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled;
        row.AddChild(Caption(label, labelWidth));
        field.SizeFlagsHorizontal = expandField ? Control.SizeFlags.ExpandFill : Control.SizeFlags.ShrinkBegin;
        field.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        row.AddChild(field);
        return row;
    }

    public static InspectorIntField IntField(string label, int min, int max, int value)
        => new(label, min, max, value);

    public static InspectorFloatField FloatField(string label, float min, float max, float value, float step = 0.01f)
        => new(label, min, max, value, step);

    public static OptionButton EnumField<T>(T value) where T : struct, Enum
    {
        var box = new OptionButton
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, SwatchHeight),
            AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled
        };
        CtisEditorTheme.ApplyOption(box);
        foreach (var item in Enum.GetValues<T>())
            box.AddItem(item.ToString());
        var values = Enum.GetValues<T>();
        var index = Array.IndexOf(values, value);
        box.Select(index < 0 ? 0 : index);
        return box;
    }

    public static T ReadEnum<T>(OptionButton box) where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        var index = box.Selected;
        if ((uint)index >= (uint)values.Length) return values[0];
        return values[index];
    }

    public static void SelectEnum<T>(OptionButton box, T value) where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        var index = Array.IndexOf(values, value);
        box.Select(index < 0 ? 0 : index);
    }

    public static OptionButton StringField()
    {
        var box = new OptionButton
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, SwatchHeight),
            AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled
        };
        CtisEditorTheme.ApplyOption(box);
        return box;
    }

    public static void FillStrings(OptionButton box, IEnumerable<string> items, string selected)
    {
        box.Clear();
        int index = 0;
        int selectedIndex = -1;
        foreach (var item in items)
        {
            box.AddItem(item);
            if (item == selected) selectedIndex = index;
            index++;
        }
        if (selectedIndex < 0 && !string.IsNullOrEmpty(selected))
        {
            box.AddItem(selected);
            selectedIndex = index;
        }
        if (box.ItemCount == 0) return;
        box.Select(selectedIndex < 0 ? 0 : selectedIndex);
    }

    public static string ReadSelected(OptionButton box)
    {
        if (box.Selected < 0 || box.Selected >= box.ItemCount) return "";
        return box.GetItemText(box.Selected);
    }

    public static CheckBox Check(bool value)
    {
        var box = new CheckBox
        {
            ButtonPressed = value,
            AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled
        };
        box.AddThemeColorOverride("font_color", CtisEditorTheme.Text);
        box.AddThemeFontSizeOverride("font_size", CtisEditorTheme.FontCaption);
        return box;
    }

    public static ColorPickerButton ColorField(Rgba value)
    {
        var button = new ColorPickerButton
        {
            Color = ToColor(value),
            EditAlpha = true,
            CustomMinimumSize = new Vector2(SwatchWidth, SwatchHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled
        };
        return button;
    }

    public static LineEdit TextField(string value, bool multiline = false)
    {
        var edit = new LineEdit
        {
            Text = value,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled
        };
        CtisEditorTheme.ApplyLineEdit(edit);
        return edit;
    }

    public static FoldableContainer Foldout(string title, bool open, Control content)
    {
        var fold = new FoldableContainer
        {
            Title = title,
            Folded = !open,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled
        };
        fold.AddThemeStyleboxOverride("panel", CtisEditorTheme.Flat(CtisEditorTheme.SectionBg, CtisEditorTheme.Border, 6, 0, 0));
        fold.AddThemeStyleboxOverride("title_panel", CtisEditorTheme.Flat(CtisEditorTheme.FoldHeader, radius: 6, marginX: 12, marginY: 10));
        fold.AddThemeStyleboxOverride("title_collapsed_panel", CtisEditorTheme.Flat(CtisEditorTheme.FoldHeader, radius: 6, marginX: 12, marginY: 10));
        fold.AddThemeColorOverride("font_color", CtisEditorTheme.Text);
        fold.AddThemeFontSizeOverride("font_size", CtisEditorTheme.FontTitle);
        content.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        fold.AddChild(content);
        return fold;
    }

    public static MarginContainer Padded(Control child, int all = 12)
    {
        var margin = new MarginContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        margin.AddThemeConstantOverride("margin_left", all);
        margin.AddThemeConstantOverride("margin_right", all);
        margin.AddThemeConstantOverride("margin_top", all);
        margin.AddThemeConstantOverride("margin_bottom", all);
        child.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        margin.AddChild(child);
        return margin;
    }

    public static Button IconButton(string text, Color bg, Color hover)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(IconSize, IconSize),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            FocusMode = Control.FocusModeEnum.None,
            AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled
        };
        CtisEditorTheme.ApplyButton(button, bg, hover, Colors.White, 4, 4, 4);
        return button;
    }

    public static Button ActionButton(string text, Color bg, Color hover, int ratioW = 2, int height = 32)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(height * ratioW, height),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            FocusMode = Control.FocusModeEnum.None,
            ClipText = false,
            AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled
        };
        CtisEditorTheme.ApplyButton(button, bg, hover, Colors.White, 4);
        return button;
    }

    public static Color ToColor(Rgba color) => new(color.R, color.G, color.B, color.A);

    public static Rgba ToRgba(Color color) => new(color.R, color.G, color.B, color.A);
}

internal sealed partial class InspectorIntField : HBoxContainer
{
    private readonly EditorSpinSlider _spin;
    private bool _suppress;

    public event Action<int>? Changed;

    public int Value
    {
        get => (int)Math.Round(_spin.Value);
        set => SetValueWithoutNotify(value);
    }

    public InspectorIntField(string label, int min, int max, int value)
    {
        AddThemeConstantOverride("separation", 8);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AutoTranslateMode = AutoTranslateModeEnum.Disabled;
        _spin = new EditorSpinSlider
        {
            Label = label,
            MinValue = min,
            MaxValue = max,
            Step = 1,
            Rounded = true,
            HideSlider = true,
            Value = value,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, InspectorWidgets.SwatchHeight),
            AutoTranslateMode = AutoTranslateModeEnum.Disabled
        };
        _spin.AddThemeColorOverride("label_color", CtisEditorTheme.Label);
        _spin.AddThemeFontSizeOverride("font_size", CtisEditorTheme.FontBody);
        _spin.ValueChanged += OnValueChanged;
        AddChild(_spin);
    }

    public void SetLabel(string label) => _spin.Label = label;

    public void SetValueWithoutNotify(int value)
    {
        _suppress = true;
        _spin.SetValueNoSignal(value);
        _suppress = false;
    }

    private void OnValueChanged(double value)
    {
        if (_suppress) return;
        Changed?.Invoke((int)Math.Round(value));
    }
}

internal sealed partial class InspectorFloatField : HBoxContainer
{
    private readonly EditorSpinSlider _spin;
    private bool _suppress;

    public event Action<float>? Changed;

    public float Value
    {
        get => (float)_spin.Value;
        set => SetValueWithoutNotify(value);
    }

    public InspectorFloatField(string label, float min, float max, float value, float step)
    {
        AddThemeConstantOverride("separation", 8);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AutoTranslateMode = AutoTranslateModeEnum.Disabled;
        _spin = new EditorSpinSlider
        {
            Label = label,
            MinValue = min,
            MaxValue = max,
            Step = step,
            Rounded = false,
            HideSlider = true,
            Value = value,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, InspectorWidgets.SwatchHeight),
            AutoTranslateMode = AutoTranslateModeEnum.Disabled
        };
        _spin.AddThemeColorOverride("label_color", CtisEditorTheme.Label);
        _spin.AddThemeFontSizeOverride("font_size", CtisEditorTheme.FontBody);
        _spin.ValueChanged += OnValueChanged;
        AddChild(_spin);
    }

    public void SetLabel(string label) => _spin.Label = label;

    public void SetValueWithoutNotify(float value)
    {
        _suppress = true;
        _spin.SetValueNoSignal(value);
        _suppress = false;
    }

    private void OnValueChanged(double value)
    {
        if (_suppress) return;
        Changed?.Invoke((float)value);
    }
}
