using Ctis.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Ctis.Presentation;

public static class CtisGodotServiceExtensions
{
    /// <summary>Registers Godot views, pointer session, and save store.</summary>
    public static IServiceCollection AddCtisGodot(this IServiceCollection services)
    {
        services.AddSingleton<PointerGridSession>();
        services.AddSingleton<IPointerGridSession>(sp => sp.GetRequiredService<PointerGridSession>());
        services.AddSingleton<IPointerGridViews>(sp => sp.GetRequiredService<PointerGridSession>());
        services.AddSingleton<TetrisItemGhostVM>();
        services.AddSingleton<IIconAtlas, IconAtlas>();
        services.AddSingleton<ItemViewRegistry>();
        services.AddSingleton<GridViewRegistry>();
        services.AddSingleton<ISaveSlotStore, GodotSaveSlotStore>();
        services.AddSingleton<IInnerGridLayout, SceneInnerGridLayout>();
        services.AddSingleton<MobileSettingsManager>();
        return services;
    }
}
