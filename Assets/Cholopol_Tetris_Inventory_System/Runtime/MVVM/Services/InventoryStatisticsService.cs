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

using Cholopol.TIS.MVVM;
using Cholopol.TIS.SaveLoadSystem;
using Loxodon.Framework.Contexts;

namespace Cholopol.TIS.Services
{
    /// <summary>
    /// Service for calculating inventory statistics (Weight, Value, Ergonomics, etc.).
    /// Supports recursive calculation for nested containers using InventoryTreeCache.
    /// This is a pure C# singleton service, not dependent on MonoBehaviour.
    /// </summary>
    public class InventoryStatisticsService
    {
        private static InventoryStatisticsService _instance;
        public static InventoryStatisticsService Instance => _instance ??= new InventoryStatisticsService();

        private IInventoryTreeCache _cache;
        private IInventoryTreeCache Cache
        {
            get
            {
                if (_cache == null)
                {
                    var context = Context.GetApplicationContext();
                    if (context != null)
                        _cache = context.GetService<IInventoryTreeCache>();
                }
                return _cache;
            }
        }

        private ItemDataList_SO _itemDataList;
        private ItemDataList_SO ItemDataList
        {
            get
            {
                if (_itemDataList == null && InventorySaveLoadService.Instance != null)
                {
                    _itemDataList = InventorySaveLoadService.Instance.itemDataList_SO;
                }
                return _itemDataList;
            }
        }

        /// <summary>
        /// Recursively calculate the total weight of an item (including all nested items).
        /// </summary>
        /// <param name="data">The persistent data of the root item</param>
        /// <returns>Total weight in kg (assuming weight unit is kg)</returns>
        public float GetTotalWeight(TetrisItemPersistentData data)
        {
            if (data == null) return 0f;
            var details = GetItemDetails(data.itemID);
            if (details == null) return 0f;

            float weight = details.weight * (data.stack > 0 ? data.stack : 1);

            // Recursively add weight of items inside this item (if it is a container)
            // We check for potential sub-containers (grids) associated with this item.
            // Convention: containerId = itemGuid + ":" + index
            // We check indices 0 to 9 as a heuristic for max sub-grids.
            for (int i = 0; i < 10; i++)
            {
                string containerId = data.itemGuid + ":" + i;
                if (Cache != null && Cache.TryGetContainer(containerId, out var container))
                {
                    foreach (var childItem in container.Items)
                    {
                        weight += GetTotalWeight(childItem);
                    }
                }
                else
                {
                    // Optimization: If index 0 is missing, likely no grids exist. 
                    // But if 0 exists and 1 is missing, we stop there.
                    if (i == 0) break;
                    // If we found 0 but not 1, maybe it only has 1 grid. Stop checking.
                    break;
                }
            }

            return weight;
        }

        /// <summary>
        /// Recursively calculate the total value (price) of an item.
        /// </summary>
        public int GetTotalValue(TetrisItemPersistentData data)
        {
            if (data == null) return 0;
            var details = GetItemDetails(data.itemID);
            if (details == null) return 0;

            int value = details.itemPrice * (data.stack > 0 ? data.stack : 1);

            for (int i = 0; i < 10; i++)
            {
                string containerId = data.itemGuid + ":" + i;
                if (Cache != null && Cache.TryGetContainer(containerId, out var container))
                {
                    foreach (var childItem in container.Items)
                    {
                        value += GetTotalValue(childItem);
                    }
                }
                else
                {
                    if (i == 0) break;
                    break;
                }
            }
            return value;
        }

        /// <summary>
        /// Recursively calculate total ergonomics.
        /// Note: Logic may vary (e.g. sum, average, or base + mods).
        /// Here we implement a simple sum for demonstration.
        /// </summary>
        public float GetTotalErgonomics(TetrisItemPersistentData data)
        {
             if (data == null) return 0f;
            var details = GetItemDetails(data.itemID);
            if (details == null) return 0f;

            float ergo = details.ergonomics;

             for (int i = 0; i < 10; i++)
            {
                string containerId = data.itemGuid + ":" + i;
                if (Cache != null && Cache.TryGetContainer(containerId, out var container))
                {
                    foreach (var childItem in container.Items)
                    {
                        ergo += GetTotalErgonomics(childItem);
                    }
                }
                else
                {
                    if (i == 0) break;
                    break;
                }
            }
            return ergo;
        }

        private ItemDetails GetItemDetails(int id)
        {
            if (ItemDataList == null) return null;
            return ItemDataList.GetItemDetailsByID(id);
        }
    }
}
