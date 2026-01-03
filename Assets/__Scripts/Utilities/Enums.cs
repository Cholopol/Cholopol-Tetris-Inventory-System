/*
 * Copyright 2026 Cholopol
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
public enum TetrisPieceShape
{
    Frame,
    //Domino
    Domino,
    //Tromino
    Tromino_I, Tromino_L, Tromino_J,
    //Tetromino
    Tetromino_I, Tetromino_O, Tetromino_T, Tetromino_J, Tetromino_L, Tetromino_S, Tetromino_Z,
    //Pentomino
    Pentomino_I, Pentomino_L, Pentomino_J, Pentomino_U, Pentomino_T, Pentomino_P,

    Cells9_Square, Cells16_Square,
    S_Sword,
    S_Rifle, 
    S_Shotgun,
    S_Like
}

public enum Dir
{
    Down,
    Left,
    Up,
    Right,
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Artifact
}

public enum InventorySlotType
{
    Pocket, Coat, Vest, BackPack, WaistBag, Coffer, Depository,
    LongWeapon, ShortWeapon, LargeConsume, MiddleConsume, SmallConsume,
    Helmet, Pants, Shoes, HeadMountedEquipment,
}

public enum PersistentGridType
{
    None,
    Pocket, Coffer, Depository
}

public enum PlaceState
{
    OnGridHasItem, OnGridNoItem,
    OnSlotHasItem, OnSlotNoItem,
    InvalidPos
}

public enum InventoryPlacementBlockReason
{
    None,
    SelfOwnedContainer,
    OutOfBounds,
    SlotOccupied,
    SlotTypeMismatch,
}
