namespace Ctis.Core;

public enum Dir
{
    Down = 0,
    Left = 1,
    Up = 2,
    Right = 3
}

public enum ItemRarity
{
    Common, Uncommon, Rare, Epic, Legendary, Artifact
}

public enum InventorySortStrategy
{
    Area = 0,
    SlotType = 1,
    Rarity = 2,
    ItemId = 3
}

public enum InventorySlotType
{
    Pocket, Coat, Vest, BackPack, WaistBag, Coffer, Depository,
        LongWeapon, ShortWeapon, LargeConsume, MiddleConsume, SmallConsume,
        Helmet, Pants, Shoes, HeadMountedEquipment, Melee
}

public enum InventoryDropKind
{
    Invalid,
    Vacant,
    Stack,
    Exchange,
    InsertIntoInner
}

public enum InventoryDropCellKind
{
    Empty,
    Occupied,
    Stack,
    Exchange,
    Blocked
}

public enum InventoryPlacementBlockReason
{
    None,
    SelfOwnedContainer,
    OutOfBounds,
    Occupied,
    SlotOccupied,
    SlotTypeMismatch,
    UnknownItem,
    UnknownContainer,
    DuplicateGuid,
    RevisionMismatch,
    InvalidCommand
}
