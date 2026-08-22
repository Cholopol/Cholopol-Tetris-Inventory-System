using Ctis.Presentation.Editor;
using Godot;

namespace Ctis.Presentation.Editor;

/// <summary>
/// Compiled by the consuming Godot project (not Ctis.Godot.dll) so plugin.gd can
/// <c>load()</c> this script. Godot does not register <c>[GlobalClass]</c> types
/// from referenced assemblies into ClassDB.
/// </summary>
[Tool]
public partial class CtisDataEditorHost : Window
{
    public override void _Ready()
    {
        Title = "CTIS Data Editor";
        AutoTranslateMode = AutoTranslateModeEnum.Disabled;
        MinSize = new Vector2I(900, 600);
        Unresizable = false;
        WrapControls = true;
        Exclusive = false;
        var editor = new CtisDataEditorWindow();
        editor.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(editor);
        CloseRequested += QueueFree;
    }
}
