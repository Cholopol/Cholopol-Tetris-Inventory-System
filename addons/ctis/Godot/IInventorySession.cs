namespace Ctis.Presentation;

public interface IInventorySession
{
    bool HasEnteredGame { get; }
    bool IsInventoryOpen { get; }
    bool IsSavePanelOpen { get; }
    void EnterNewGame();
    bool EnterLoadedGame(int slotIndex);
    bool LoadOrStart(int slotIndex);
    void SaveSlot(int index);
    void DeleteSlot(int index);
    void ToggleInventory();
    void ToggleSavePanel();
    void RecycleInventoryUi();
    void InstantiateInventoryUi();
    void ShowSavePanel();
    void DismissSavePanel();
}
