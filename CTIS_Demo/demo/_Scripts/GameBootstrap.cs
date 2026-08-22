using Ctis.Core;
using Ctis.Presentation;
using DotPudica.Godot.Views;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using AppContext = DotPudica.Godot.AppContext;

namespace Ctis.Demo;

public partial class GameBootstrap : Node
{
    private AppContext? _app;

    public override void _Ready()
    {
        var wm = GetNodeOrNull<GodotWindowManager>("WindowManager")
            ?? new GodotWindowManager { Name = "WindowManager" };
        if (wm.GetParent() == null)
            AddChild(wm);

        _app = new AppContext().Initialize(services =>
        {
            services.AddCtis();
            services.AddCtisGodot();
            services.AddSingleton<IFloatingInventoryWindows, FloatingInventoryWindows>();
            services.AddSingleton<IInventorySession, InventorySession>();
            services.AddTransient<DebugItemListVM>();
        }, wm);

        CtisLocale.LoadCsv();
        CtisLocale.SetLocale("zh");
        ItemCatalogLoader.LoadInto(_app.Services.GetRequiredService<IItemCatalog>());
        PlacementConfigLoader.LoadInto(_app.Services.GetRequiredService<PlacementConfig>());
        EquipmentLayoutLoader.LoadInto(_app.Services.GetRequiredService<EquipmentLayout>());

        CtisRuntime.Attach(this, wm);

        wm.ConfigurePool<SaveLoadWindow>("res://CTIS_Demo/demo/SaveLoadWindow.tscn", 1);
        wm.ConfigurePool<InventoryWindow>("res://CTIS_Demo/demo/InventoryWindow.tscn", 1);
        wm.ConfigurePool<FloatingGridWindow>("res://CTIS_Demo/demo/FloatingGridWindow.tscn", 8);
        wm.ConfigurePool<ContextMenuWindow>("res://CTIS_Demo/demo/ContextMenuWindow.tscn", 2);
        wm.ConfigurePool<ItemInformationWindow>("res://CTIS_Demo/demo/ItemInformationWindow.tscn", 5);
        wm.ConfigurePool<DebugItemListWindow>("res://CTIS_Demo/demo/DebugItemListWindow.tscn", 1);

        var overlayScene = GD.Load<PackedScene>("res://CTIS_Demo/demo/MobileControlsOverlay.tscn");
        var overlay = overlayScene.Instantiate<MobileControlsOverlay>();
        AddChild(overlay);

        _app.Services.GetRequiredService<IInventorySession>().ShowSavePanel();
    }

    public override void _ExitTree()
    {
        _app?.Dispose();
        _app = null;
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;
        var session = AppContext.Current.Services.GetRequiredService<IInventorySession>();
        if (key.PhysicalKeycode == Key.B)
        {
            session.ToggleInventory();
            GetViewport().SetInputAsHandled();
        }
        else if (key.PhysicalKeycode == Key.Escape)
        {
            session.ToggleSavePanel();
            GetViewport().SetInputAsHandled();
        }
        else if (key.PhysicalKeycode == Key.F1)
        {
            ToggleDebugItemList();
            GetViewport().SetInputAsHandled();
        }
    }

    private static void ToggleDebugItemList()
    {
        var wm = AppContext.Current.WindowManager;
        var existing = wm.Find<DebugItemListWindow>();
        if (existing != null && existing.IsWindowVisible)
            wm.Dismiss(existing, ignoreAnimation: true);
        else
            wm.ShowPooled<DebugItemListWindow>(new Bundle());
    }
}
