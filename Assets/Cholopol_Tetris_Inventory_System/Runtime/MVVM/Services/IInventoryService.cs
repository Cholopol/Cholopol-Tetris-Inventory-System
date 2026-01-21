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
using Cholopol.TIS;
using Cholopol.TIS.MVVM.ViewModels;
using UnityEngine;

namespace Cholopol.TIS.MVVM
{
    public interface IInventoryService
    {
        bool CanPlace(in InventoryPlacementContext context, out InventoryPlacementBlockReason reason);
        bool TryQuickExchange(TetrisGridVM grid, TetrisItemGhostVM ghost, Vector2Int targetPos);
        bool TryStack(TetrisItemVM a, TetrisItemVM b);
        bool PlaceOnGrid(TetrisItemVM item, TetrisGridVM grid, Vector2Int origin, TetrisSlotVM fromSlot);
        bool PlaceOnSlot(TetrisItemVM item, TetrisSlotVM slot);
        bool TrySplit(TetrisItemVM item, int amount);

    }
}
