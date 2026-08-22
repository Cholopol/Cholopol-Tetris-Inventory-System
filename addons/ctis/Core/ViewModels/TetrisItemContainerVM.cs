using DotPudica.Core.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Ctis.Core;

public abstract partial class TetrisItemContainerVM : ViewModelBase
{
    [ObservableProperty]
    private TetrisItemVM? _relatedTetrisItem;

    public virtual Dictionary<string, TetrisItemVM> OwnerItemsDic { get; set; } = new();

    public abstract bool TryPlaceTetrisItem(TetrisItemVM tetrisItem, int posX = 0, int posY = 0);
    public abstract void PlaceTetrisItem(TetrisItemVM tetrisItem, int posX = 0, int posY = 0);
}
