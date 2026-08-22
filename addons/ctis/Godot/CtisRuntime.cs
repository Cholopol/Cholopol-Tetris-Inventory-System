using DotPudica.Core.ObjectPool;
using DotPudica.Godot.ObjectPool;
using DotPudica.Godot.Views;
using Godot;

namespace Ctis.Presentation;

public sealed class CtisHud
{
    public required CanvasLayer WindowLayer { get; init; }
    public required GodotWindowManager WindowManager { get; init; }
    public required CanvasLayer GhostLayer { get; init; }
    public required TetrisItemGhostView Ghost { get; init; }
}

public static class CtisRuntime
{
    public const int WindowCanvasLayer = 10;
    public const int GhostCanvasLayer = 100;

    public const string BuiltinPocketScenePath = "res://addons/ctis/Godot/Scenes/GridPanels/GP_Pocket.tscn";
    public const string BuiltinCofferScenePath = "res://addons/ctis/Godot/Scenes/GridPanels/GP_Coffer.tscn";
    public const string BuiltinGridPanelsDir = "res://addons/ctis/Godot/Scenes/GridPanels/";

    public static string PersistentPocketScenePath { get; set; } = BuiltinPocketScenePath;
    public static string PersistentCofferScenePath { get; set; } = BuiltinCofferScenePath;

    /// <summary>Resolves active pocket container scene path with fallbacks.</summary>
    public static string GetPocketScenePath()
    {
        if (!string.IsNullOrEmpty(PersistentPocketScenePath) && ResourceLoader.Exists(PersistentPocketScenePath))
            return PersistentPocketScenePath;
        if (ProjectSettings.HasSetting("ctis/scenes/pocket"))
        {
            var configured = (string)ProjectSettings.GetSetting("ctis/scenes/pocket");
            if (!string.IsNullOrEmpty(configured) && ResourceLoader.Exists(configured))
                return configured;
        }
        return BuiltinPocketScenePath;
    }

    /// <summary>Resolves active coffer container scene path with fallbacks.</summary>
    public static string GetCofferScenePath()
    {
        if (!string.IsNullOrEmpty(PersistentCofferScenePath) && ResourceLoader.Exists(PersistentCofferScenePath))
            return PersistentCofferScenePath;
        if (ProjectSettings.HasSetting("ctis/scenes/coffer"))
        {
            var configured = (string)ProjectSettings.GetSetting("ctis/scenes/coffer");
            if (!string.IsNullOrEmpty(configured) && ResourceLoader.Exists(configured))
                return configured;
        }
        return BuiltinCofferScenePath;
    }

    /// <summary>Resolves grid panel scene with three-tier fallback.</summary>
    public static string ResolveGridPanelScene(string? sceneKey)
    {
        if (string.IsNullOrEmpty(sceneKey)) return string.Empty;
        if (ResourceLoader.Exists(sceneKey)) return sceneKey;

        var filename = System.IO.Path.GetFileName(sceneKey);
        var builtinCandidate = BuiltinGridPanelsDir + filename;
        if (ResourceLoader.Exists(builtinCandidate)) return builtinCandidate;

        return sceneKey;
    }

    public static readonly IObjectPool<TetrisItemView> ItemViews = NodePool.Create<TetrisItemView>(128);
    public static readonly IObjectPool<TetrisGridView> GridViews = NodePool.Create<TetrisGridView>(32);
    public static readonly IObjectPool<TetrisSlotView> SlotViews = NodePool.Create<TetrisSlotView>(32);
    public static readonly IObjectPool<ColorRect> HighlightTiles = NodePool.Create<ColorRect>(128);

    /// <summary>Attaches HUD layers and the drag ghost view.</summary>
    public static CtisHud Attach(Node parent, GodotWindowManager? windowManager = null)
    {
        var windowLayer = parent.GetNodeOrNull<CanvasLayer>("CtisWindowLayer")
            ?? new CanvasLayer { Name = "CtisWindowLayer", Layer = WindowCanvasLayer };
        if (windowLayer.GetParent() == null)
            parent.AddChild(windowLayer);

        var wm = windowManager ?? windowLayer.GetNodeOrNull<GodotWindowManager>("WindowManager");
        if (wm == null)
        {
            wm = new GodotWindowManager { Name = "WindowManager" };
            windowLayer.AddChild(wm);
        }
        else if (wm.GetParent() != windowLayer)
        {
            wm.GetParent()?.RemoveChild(wm);
            windowLayer.AddChild(wm);
        }

        var ghostLayer = parent.GetNodeOrNull<CanvasLayer>("CtisGhostLayer")
            ?? new CanvasLayer { Name = "CtisGhostLayer", Layer = GhostCanvasLayer };
        if (ghostLayer.GetParent() == null)
            parent.AddChild(ghostLayer);

        var ghost = ghostLayer.GetNodeOrNull<TetrisItemGhostView>("TetrisItemGhost")
            ?? CreateGhost();
        if (ghost.GetParent() == null)
            ghostLayer.AddChild(ghost);

        return new CtisHud
        {
            WindowLayer = windowLayer,
            WindowManager = wm,
            GhostLayer = ghostLayer,
            Ghost = ghost
        };
    }

    /// <summary>Creates the drag ghost in C# because plugin scripts cannot be assigned on .tscn files.</summary>
    public static TetrisItemGhostView CreateGhost()
    {
        var ghost = new TetrisItemGhostView { Name = "TetrisItemGhost" };
        ghost.BuildTree();
        return ghost;
    }

    /// <summary>Allocates a pooled item view.</summary>
    public static TetrisItemView CreateItemView()
    {
        var view = ItemViews.Allocate();
        view.Name = "TetrisItem";
        return view;
    }

    /// <summary>Allocates a pooled grid view.</summary>
    public static TetrisGridView CreateGridView()
    {
        var view = GridViews.Allocate();
        view.Name = "TetrisGrid";
        return view;
    }

    /// <summary>Allocates a pooled slot view.</summary>
    public static TetrisSlotView CreateSlotView()
    {
        var view = SlotViews.Allocate();
        view.Name = "TetrisSlot";
        return view;
    }

    /// <summary>Returns a grid view to the pool.</summary>
    public static void Recycle(TetrisGridView view)
    {
        view.GetParent()?.RemoveChild(view);
        GridViews.Free(view);
    }

    /// <summary>Returns a slot view to the pool.</summary>
    public static void Recycle(TetrisSlotView view)
    {
        view.GetParent()?.RemoveChild(view);
        SlotViews.Free(view);
    }

    /// <summary>Returns an item view to the pool.</summary>
    public static void Recycle(TetrisItemView view)
    {
        view.GetParent()?.RemoveChild(view);
        ItemViews.Free(view);
    }
}
