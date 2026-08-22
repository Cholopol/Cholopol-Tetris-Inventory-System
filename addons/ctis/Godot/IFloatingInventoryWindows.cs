using Ctis.Core;
using Godot;

namespace Ctis.Presentation;

public interface IFloatingInventoryWindows
{
    void ShowItemGrid(TetrisItemVM item);
    void ShowItemInfo(ItemDetails? details, int stack);
    void ShowContextMenu(TetrisItemVM item, Vector2 globalPosition);
    void DismissContextMenu();
    void Focus(string uniqueId);
    void DismissAll();
}
