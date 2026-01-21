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
using Cholopol.TIS.MVVM.ViewModels;
using Cholopol.TIS.SaveLoadSystem;
using System.Collections.Generic;
using UnityEngine;
using Loxodon.Framework.Contexts;

namespace Cholopol.TIS.MVVM
{
    public class InventoryService : IInventoryService
    {
        private void SyncGridToCache(TetrisGridVM grid)
        {
            if (grid == null) return;
            var cache = Context.GetApplicationContext().GetService<IInventoryTreeCache>();
            if (cache == null) return;
            var id = grid.GridGuid;
            if (string.IsNullOrEmpty(id)) return;
            SyncContainerConfig(cache, grid, id);
            SyncContainerItems(cache, grid, id);
        }

        private void SyncContainerConfig(IInventoryTreeCache cache, TetrisGridVM grid, string id)
        {
            if (!cache.TryGetContainer(id, out var container))
            {
                container = cache.GetOrCreateContainer(id);
            }
            if (container == null) return;

            if (container.GridSizeWidth != grid.GridSizeWidth
                || container.GridSizeHeight != grid.GridSizeHeight
                || container.LocalGridTileSizeWidth != grid.LocalGridTileSizeWidth
                || container.LocalGridTileSizeHeight != grid.LocalGridTileSizeHeight)
            {
                cache.SetContainerConfig(id, grid.GridSizeWidth, grid.GridSizeHeight, grid.LocalGridTileSizeWidth, grid.LocalGridTileSizeHeight);
            }
        }

        private void SyncContainerItems(IInventoryTreeCache cache, TetrisGridVM grid, string id)
        {
            if (!cache.TryGetContainer(id, out var container)) return;

            var dic = grid.OwnerItemsDic;
            if (dic == null)
            {
                if (container.ItemsByGuid.Count > 0) cache.ClearContainerItems(id);
                return;
            }

            if (container.ItemsByGuid.Count > 0)
            {
                var staleGuids = new List<string>();
                foreach (var guid in container.ItemsByGuid.Keys)
                {
                    if (!dic.ContainsKey(guid))
                    {
                        staleGuids.Add(guid);
                    }
                }
                for (int i = 0; i < staleGuids.Count; i++)
                {
                    cache.RemoveFromContainer(id, staleGuids[i]);
                }
            }

            foreach (var kv in dic)
            {
                var guid = kv.Key;
                var vm = kv.Value;
                if (vm == null || string.IsNullOrEmpty(guid)) continue;

                var data = GetPersistentData(guid);
                if (data == null) continue;

                var mappedContainerId = cache.GetContainerIdByItem(guid);
                bool isSameContainer = mappedContainerId == id;
                bool hasItemInContainer = container.ItemsByGuid.TryGetValue(guid, out var existing);
                bool isSameDataRef = ReferenceEquals(existing, data);

                if (isSameContainer && hasItemInContainer && isSameDataRef)
                {
                    continue;
                }

                cache.PlaceItem(id, data);
            }
        }

        private TetrisItemPersistentData GetPersistentData(string guid)
        {
            if (InventorySaveLoadService.Instance != null && InventorySaveLoadService.Instance.inventoryData_SO != null)
            {
                return InventorySaveLoadService.Instance.inventoryData_SO.GetTetrisItemPersistentDataByGuid(guid);
            }
            return null;
        }

        private void UpsertGridItemToCache(IInventoryTreeCache cache, TetrisGridVM grid, TetrisItemVM item)
        {
            if (cache == null || grid == null || item == null) return;
            var id = grid.GridGuid;
            if (string.IsNullOrEmpty(id)) return;
            if (string.IsNullOrEmpty(item.Guid)) return;
            SyncContainerConfig(cache, grid, id);
            var data = GetPersistentData(item.Guid);
            if (data == null) return;
            cache.PlaceItem(id, data);
        }

        private void RemoveGridItemFromCache(IInventoryTreeCache cache, TetrisGridVM grid, string itemGuid)
        {
            if (cache == null || grid == null) return;
            var id = grid.GridGuid;
            if (string.IsNullOrEmpty(id)) return;
            if (string.IsNullOrEmpty(itemGuid)) return;
            cache.RemoveFromContainer(id, itemGuid);
        }

        public bool CanPlace(in InventoryPlacementContext context, out InventoryPlacementBlockReason reason)
        {
            return InventoryPlacementConfig_SO.EvaluateActive(context, out reason);
        }

        public bool TryQuickExchange(TetrisGridVM grid, TetrisItemGhostVM ghost, Vector2Int targetPos)
        {
            if (grid == null || ghost == null) return false;
            var originGrid = ghost.SelectedItem != null ? ghost.SelectedItem.CurrentTetrisContainer as TetrisGridVM : null;
            if (!grid.CanQuickExchange(ghost, targetPos))
            {
                return false;
            }
            var ok = grid.TryQuickExchange(ghost, targetPos);
            if (ok)
            {
                SyncGridToCache(grid);
                if (originGrid != null && originGrid != grid) SyncGridToCache(originGrid);
            }
            return ok;
        }

        public bool TryStack(TetrisItemVM source, TetrisItemVM target)
        {
            var ok = TetrisUtilities.InventoryLogicHelper.TryMergeStack(target, source);
            var cache = Context.GetApplicationContext().GetService<IInventoryTreeCache>();
            if (ok && cache != null)
            {
                var bg = target != null ? target.CurrentTetrisContainer as TetrisGridVM : null;
                if (bg != null) UpsertGridItemToCache(cache, bg, target);
            }
            return ok;
        }

        public bool PlaceOnGrid(TetrisItemVM item, TetrisGridVM grid, Vector2Int origin, TetrisSlotVM fromSlot)
        {
            if (item == null || grid == null) return false;
            var placementContext = InventoryPlacementContext.ForItem(item, grid, origin);
            if (!CanPlace(placementContext, out _)) return false;

            var oldContainer = item.CurrentTetrisContainer;
            var oldGrid = oldContainer as TetrisGridVM;
            var oldSlot = oldContainer as TetrisSlotVM;

            if (oldGrid == grid && grid.OwnerItemsDic != null && grid.OwnerItemsDic.ContainsKey(item.Guid))
            {
                var oldX = item.LocalGridCoordinate.x;
                var oldY = item.LocalGridCoordinate.y;
                var oldRotOffset = item.RotationOffset;
                var oldShape = new List<Vector2Int>(item.TetrisCoordinateSet);

                oldGrid.RemoveTetrisItem(item, oldX, oldY, oldRotOffset, oldShape, false);

                if (grid.TryPlaceTetrisItem(item, origin.x, origin.y))
                {
                    var cache = Context.GetApplicationContext().GetService<IInventoryTreeCache>();
                    if (cache != null && !string.IsNullOrEmpty(grid.GridGuid))
                    {
                        UpsertGridItemToCache(cache, grid, item);
                    }
                    return true;
                }
                else
                {
                    grid.PlaceTetrisItem(item, oldX, oldY);
                    return false;
                }
            }

            int oldX_diff = item.LocalGridCoordinate.x;
            int oldY_diff = item.LocalGridCoordinate.y;
            var oldRotOffset_diff = item.RotationOffset;
            var oldShape_diff = new List<Vector2Int>(item.TetrisCoordinateSet);

            if (grid.TryPlaceTetrisItem(item, origin.x, origin.y))
            {
                var cache = Context.GetApplicationContext().GetService<IInventoryTreeCache>();
                if (oldGrid != null && oldGrid != grid && oldGrid.OwnerItemsDic != null && oldGrid.OwnerItemsDic.ContainsKey(item.Guid))
                {
                    oldGrid.RemoveTetrisItem(item, oldX_diff, oldY_diff, oldRotOffset_diff, oldShape_diff, false);
                    if (cache != null) RemoveGridItemFromCache(cache, oldGrid, item.Guid);
                }
                if (oldSlot != null)
                {
                    oldSlot.RemoveTetrisItem(false);
                }

                if (cache != null && !string.IsNullOrEmpty(grid.GridGuid))
                {
                    UpsertGridItemToCache(cache, grid, item);
                }
                return true;
            }

            return false;
        }

        public bool PlaceOnSlot(TetrisItemVM item, TetrisSlotVM slot)
        {
            if (item == null || slot == null) return false;
            var placementContext = InventoryPlacementContext.ForItem(item, slot, Vector2Int.zero);
            if (!CanPlace(placementContext, out _)) return false;

            var oldContainer = item.CurrentTetrisContainer;
            var oldGrid = oldContainer as TetrisGridVM;
            var oldSlot = oldContainer as TetrisSlotVM;

            int oldX = item.LocalGridCoordinate.x;
            int oldY = item.LocalGridCoordinate.y;
            var oldRotOffset = item.RotationOffset;
            var oldShape = new List<Vector2Int>(item.TetrisCoordinateSet);

            var ok = slot.TryPlaceTetrisItem(item);
            if (!ok) return false;

            if (oldGrid != null)
            {
                oldGrid.RemoveTetrisItem(item, oldX, oldY, oldRotOffset, oldShape, false);
            }
            if (oldSlot != null && oldSlot != slot)
            {
                oldSlot.RemoveTetrisItem(false);
            }

            var cache = Context.GetApplicationContext().GetService<IInventoryTreeCache>();
            if (cache != null)
            {
                if (oldGrid != null) RemoveGridItemFromCache(cache, oldGrid, item.Guid);
                cache.RemoveItem(item.Guid);
            }
            return true;
        }

        public bool TrySplit(TetrisItemVM item, int amount)
        {
            if (item == null) return false;

            if (item.CurrentStack <= 1 || amount <= 0 || amount >= item.CurrentStack) return false;
            var grid = item.CurrentTetrisContainer as TetrisGridVM;
            if (grid == null) return false;

            var details = item.ItemDetails;
            var newVm = TetrisItemFactory.GetOrCreateVM(details, null, grid);
            if (newVm == null) return false;
            newVm.Direction = item.Direction;
            newVm.Rotated = item.Rotated;
            newVm.CurrentStack = amount;

            if (!TetrisUtilities.InventoryLogicHelper.HasAnyFreeSpot(grid, newVm))
            {
                if (!string.IsNullOrEmpty(newVm.Guid))
                {
                    newVm.Dispose();
                    TetrisItemFactory.UnregisterVM(newVm.Guid, true);
                }
                return false;
            }

            bool placed = TetrisUtilities.InventoryLogicHelper.TryPlaceAdjacent(grid, newVm, item, this);
            if (!placed)
            {
                for (int row = 0; row < grid.GridSizeHeight && !placed; row++)
                {
                    for (int column = 0; column < grid.GridSizeWidth && !placed; column++)
                    {
                        if (TetrisUtilities.InventoryLogicHelper.CanPlaceAt(grid, newVm, column, row))
                        {
                            placed = PlaceOnGrid(newVm, grid, new Vector2Int(column, row), null);
                        }
                    }
                }
            }

            if (!placed)
            {
                if (!string.IsNullOrEmpty(newVm.Guid))
                {
                    newVm.Dispose();
                    TetrisItemFactory.UnregisterVM(newVm.Guid, true);
                }
                return false;
            }

            item.CurrentStack -= amount;
            item.SetItemData();
            var cache = Context.GetApplicationContext().GetService<IInventoryTreeCache>();
            if (cache != null)
            {
                UpsertGridItemToCache(cache, grid, item);
            }

            return true;
        }

    }

}
