using Ctis.Core;
using Ctis.Presentation;
using DotPudica.Godot.Views;
using Godot;
using AppContext = DotPudica.Godot.AppContext;

namespace Ctis.Demo;

public sealed class InventorySession : IInventorySession
{
    private readonly ISaveLoadService _save;
    private readonly InventoryPageVM _page;
    private readonly IFloatingInventoryWindows _floating;

    public InventorySession(
        ISaveLoadService save,
        InventoryPageVM page,
        IFloatingInventoryWindows floating)
    {
        _save = save;
        _page = page;
        _floating = floating;
        _save.Restored += _page.RebuildFromCache;
    }

    public bool HasEnteredGame { get; private set; }
    public bool IsInventoryOpen => AppContext.Current.WindowManager.Find<InventoryWindow>()?.IsWindowVisible == true;
    public bool IsSavePanelOpen => AppContext.Current.WindowManager.Find<SaveLoadWindow>()?.IsWindowVisible == true;

    /// <summary>Clears inventory and begins an empty session without opening UI.</summary>
    public void EnterNewGame()
    {
        RecycleInventoryUi();
        _page.DetachItems();
        _save.Restore("");
        HasEnteredGame = true;
    }

    /// <summary>Loads a save slot into a session without opening UI; false when missing or corrupt.</summary>
    public bool EnterLoadedGame(int slotIndex)
    {
        var slot = _save.GetSlot(slotIndex);
        if (!slot.HasData || slot.IsCorrupt)
            return false;
        RecycleInventoryUi();
        _page.DetachItems();
        if (!_save.LoadSlot(slotIndex))
            return false;
        HasEnteredGame = true;
        return true;
    }

    /// <summary>Loads a slot if it has data, otherwise starts an empty game.</summary>
    public bool LoadOrStart(int slotIndex)
    {
        var slot = _save.GetSlot(slotIndex);
        if (slot.IsCorrupt)
            return false;
        if (slot.HasData)
            return EnterLoadedGame(slotIndex);
        EnterNewGame();
        return true;
    }

    /// <summary>Writes the current session into a numbered slot.</summary>
    public void SaveSlot(int index)
    {
        HasEnteredGame = true;
        _save.SaveSlot(index);
    }

    /// <summary>Deletes a numbered save slot.</summary>
    public void DeleteSlot(int index) => _save.DeleteSlot(index);

    /// <summary>Toggles inventory window visibility.</summary>
    public void ToggleInventory()
    {
        if (IsInventoryOpen)
        {
            RecycleInventoryUi();
            return;
        }
        InstantiateInventoryUi();
    }

    /// <summary>Toggles save/load panel visibility.</summary>
    public void ToggleSavePanel()
    {
        if (IsSavePanelOpen)
        {
            DismissSavePanel();
            return;
        }
        ShowSavePanel();
    }

    /// <summary>Dismisses inventory and floating item windows.</summary>
    public void RecycleInventoryUi()
    {
        _floating.DismissAll();
        var wm = AppContext.Current.WindowManager;
        var inventory = wm.Find<InventoryWindow>();
        if (inventory != null && !inventory.Dismissed)
            wm.Dismiss(inventory, ignoreAnimation: true);
        _page.IsOpen = false;
    }

    /// <summary>Shows the pooled inventory window.</summary>
    public void InstantiateInventoryUi()
    {
        HasEnteredGame = true;
        var wm = AppContext.Current.WindowManager;
        var existing = wm.Find<InventoryWindow>();
        if (existing != null && existing.IsWindowVisible) return;
        wm.ShowPooled<InventoryWindow>(new Bundle());
        _page.IsOpen = true;

        var debugWin = wm.Find<DebugItemListWindow>();
        if (debugWin != null && debugWin.IsWindowVisible)
            debugWin.MoveToFront();
    }

    /// <summary>Shows the save/load panel.</summary>
    public void ShowSavePanel()
    {
        var wm = AppContext.Current.WindowManager;
        var existing = wm.Find<SaveLoadWindow>();
        if (existing != null && existing.IsWindowVisible) return;
        wm.ShowPooled<SaveLoadWindow>(new Bundle());

        var debugWin = wm.Find<DebugItemListWindow>();
        if (debugWin != null && debugWin.IsWindowVisible)
            debugWin.MoveToFront();
    }

    /// <summary>Dismisses the save/load panel if open.</summary>
    public void DismissSavePanel()
    {
        var wm = AppContext.Current.WindowManager;
        var panel = wm.Find<SaveLoadWindow>();
        if (panel != null && !panel.Dismissed)
            wm.Dismiss(panel, ignoreAnimation: true);
    }
}
