using Microsoft.Extensions.DependencyInjection;

namespace Ctis.Core;

public static class CtisServiceExtensions
{
    /// <summary>Registers Core inventory services and view-models.</summary>
    public static IServiceCollection AddCtis(this IServiceCollection services)
    {
        services.AddSingleton<IItemCatalog, ItemCatalog>();
        services.AddSingleton<IInventoryTreeCache, InventoryTreeCache>();
        services.AddSingleton<PlacementConfig>();
        services.AddSingleton<IItemVmRegistry, ItemVmRegistry>();
        services.AddSingleton<IItemIdFactory, GuidItemIdFactory>();
        services.AddSingleton<IInventoryService, InventoryService>();
        services.AddSingleton<IItemDragMediator, TetrisItemMediator>();
        services.AddSingleton<ISaveSlotStore, InMemorySaveSlotStore>();
        services.AddSingleton<JsonSaveLoadService>();
        services.AddSingleton<ISaveLoadService>(sp =>
            new ProfiledSaveLoadService(
                sp.GetRequiredService<JsonSaveLoadService>(),
                DotPudica.Core.Logging.LogManager.GetLogger("SaveLoad")));
        services.AddSingleton<EquipmentLayout>();
        services.AddTransient<ContextMenuVM>();
        services.AddTransient<ItemInformationVM>();
        services.AddSingleton<InventoryPageVM>();
        services.AddTransient<FloatingGridVM>();
        services.AddTransient<SaveSlotListVM>();
        services.AddSingleton<IGridFactory, GridFactory>();
        services.AddTransient<TetrisGridVM>(sp => sp.GetRequiredService<IGridFactory>().Create(1, 1));
        return services;
    }
}
