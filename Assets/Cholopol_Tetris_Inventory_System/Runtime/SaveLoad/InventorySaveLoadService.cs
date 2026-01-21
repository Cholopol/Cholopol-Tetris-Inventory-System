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
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cholopol.TIS.Events;
using Cholopol.TIS.MVVM.Views;
using Cholopol.TIS.MVVM;
using Loxodon.Framework.Contexts;
using Cholopol.TIS.Utility;

namespace Cholopol.TIS.SaveLoadSystem
{
    public class InventorySaveLoadService : Singleton<InventorySaveLoadService>, ISaveable
    {
        [Header("ItemDetails Data")]
        public ItemDataList_SO itemDataList_SO;
        [Header("TetrisItem Database")]
        public InventoryData_SO inventoryData_SO;
        [Header("Slot Panels")]
        public TetrisSlotView[] inventorySlots;
        [Header("PersistentGrid")]
        public TetrisGridView[] inventoryPersistentGrids;
        private Dictionary<string, TetrisItemView> slotItemGuidDic = new();
        private Dictionary<string, TetrisItemView> parentItemGuidDic = new();
        private Dictionary<string, TetrisItemView> normalItemGuidDic = new();
        private readonly Dictionary<string, TetrisGridView> gridGuidMap = new();
        private Coroutine _lazyInstantiateUICoroutine;
        private bool _uiRefreshPending;
        private bool _closedStateCleared;

        public string GUID => GetComponent<DataGUID>().guid;

        private void OnEnable()
        {
            EventBus.Instance.Subscribe(EventNames.InstantiateInventoryItemUI, HandleLazyInstantiateReservedUI);
            EventBus.Instance.Subscribe(EventNames.RecycleInventoryItemUI, HandleRecycleInventoryItemUI);
            EventBus.Instance.Subscribe(EventNames.DeleteObjectEvent, HandleDeleteItem);
            EventBus.Instance.Subscribe<int>(EventNames.DeleteDataEvent, HandleDeleteSaveData);
        }

        private void OnDisable()
        {
            EventBus.Instance.Unsubscribe(EventNames.InstantiateInventoryItemUI, HandleLazyInstantiateReservedUI);
            EventBus.Instance.Unsubscribe(EventNames.RecycleInventoryItemUI, HandleRecycleInventoryItemUI);
            EventBus.Instance.Unsubscribe(EventNames.DeleteObjectEvent, HandleDeleteItem);
            EventBus.Instance.Unsubscribe<int>(EventNames.DeleteDataEvent, HandleDeleteSaveData);
        }

        private void Start()
        {
            ISaveable saveable = this;
            saveable.RegisterSaveable();
            BuildGridGuidMap();
            AssignSlotIndices();
            BuildRuntimeCache();
        }

        public GameSaveData GenerateSaveData()
        {
            NormalizeItemRelations();
            GameSaveData saveData = new()
            {
                inventoryDict = new Dictionary<string, List<TetrisItemPersistentData>>
                {
                    { inventoryData_SO.name, inventoryData_SO.inventoryItemList }
                }
            };

            return saveData;
        }

        public void RestoreData(GameSaveData saveData)
        {
            inventoryData_SO.SetInventoryItemList(saveData.inventoryDict[inventoryData_SO.name], rebuildNow: true);
            BuildGridGuidMap();
            BuildRuntimeCache();
        }

        /// <summary>
        /// Lazy loading: After the inventory UI is actually activated, render the items in the archive onto the reserved persistent Grid/Slot.
        /// </summary>
        public void HandleLazyInstantiateReservedUI()
        {
            _uiRefreshPending = true;
            _closedStateCleared = false;
            if (_lazyInstantiateUICoroutine == null)
            {
                _lazyInstantiateUICoroutine = StartCoroutine(LazyInstantiateReservedUICoroutine());
            }
        }

        /// <summary>
        /// Recycle the item UI when the inventory is closed, and clear the cache related to View (keep the ViewModel and runtime data).
        /// </summary>
        private void HandleRecycleInventoryItemUI()
        {
            if (_closedStateCleared) return;
            _uiRefreshPending = false;
            if (_lazyInstantiateUICoroutine != null)
            {
                StopCoroutine(_lazyInstantiateUICoroutine);
                _lazyInstantiateUICoroutine = null;
            }
            ClearAllItemViewsAndCaches(true);
        }

        private void HandleDeleteItem()
        {
            if (inventoryData_SO == null || inventoryData_SO.inventoryItemList == null)
            {
                BuildRuntimeCache();
                RefreshReservedPersistentGridViews();
                RestoreSlotVMsFromPersistentData();
                return;
            }

            // Delete non reserved items, keep items such as safes, etc
            List<TetrisItemPersistentData> retained = new List<TetrisItemPersistentData>();
            foreach (TetrisItemPersistentData item in inventoryData_SO.inventoryItemList)
            {
                if (IsItemRetainedOnDeath(item))
                {
                    retained.Add(item);
                    continue;
                }
                if (!string.IsNullOrEmpty(item.itemGuid))
                {
                    TetrisItemFactory.UnregisterAndDestroyUIByGuid(item.itemGuid, true);
                }
            }
            inventoryData_SO.inventoryItemList = retained;
            BuildRuntimeCache();
            RefreshReservedPersistentGridViews();
            RestoreSlotVMsFromPersistentData();
        }

        private void HandleDeleteSaveData(int slotIndex)
        {
            if (_lazyInstantiateUICoroutine != null)
            {
                StopCoroutine(_lazyInstantiateUICoroutine);
                _lazyInstantiateUICoroutine = null;
            }
            _uiRefreshPending = false;
            _closedStateCleared = false;

            var list = inventoryData_SO != null ? inventoryData_SO.inventoryItemList : null;
            if (list != null)
            {
                var uniqueGuids = new HashSet<string>();
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    if (item == null || string.IsNullOrEmpty(item.itemGuid)) continue;
                    uniqueGuids.Add(item.itemGuid);
                }
                foreach (var guid in uniqueGuids)
                {
                    TetrisItemFactory.UnregisterAndDestroyUIByGuid(guid, true);
                }
            }

            ClearAllItemViewsAndCaches(false);

            if (inventoryData_SO != null)
            {
                inventoryData_SO.SetInventoryItemList(new List<TetrisItemPersistentData>(), rebuildNow: true);
            }

            BuildRuntimeCache();
            RefreshReservedPersistentGridViews();
            RestoreSlotVMsFromPersistentData();
        }

        /// <summary>
        /// Asynchronous generation of item UI during archive loading
        /// </summary>
        /// <returns></returns>
        public IEnumerator InstantiateInventoryItemUICoroutine()
        {
            var operations = new Action[]
            {
                () => ClearAllItemViewsAndCaches(false),
                BuildRuntimeCache,
                BindReservedPersistentGrids,
                RefreshReservedPersistentGridViews
            };

            foreach (var op in operations)
            {
                op();
                yield return null;
            }

            RestoreSlotVMsFromPersistentData();
        }

        /// <summary>
        /// Wait until the reserved UI (persistent Grid/Slot) is activated in the hierarchy, and then execute the restoration of the item UI.
        /// </summary>
        private IEnumerator LazyInstantiateReservedUICoroutine()
        {
            while (_uiRefreshPending)
            {
                if ((inventoryPersistentGrids == null || inventoryPersistentGrids.Length == 0) &&
                    (inventorySlots == null || inventorySlots.Length == 0))
                {
                    _uiRefreshPending = false;
                    break;
                }

                if (IsReservedInventoryUIActiveInHierarchy())
                {
                    _uiRefreshPending = false;
                    yield return InstantiateInventoryItemUICoroutine();
                    break;
                }
                yield return null;
            }
            _lazyInstantiateUICoroutine = null;
        }

        /// <summary>
        /// Determine whether the reserved persistent grid/slot has been activated in the hierarchy (usually indicates that the inventory UI has been opened).
        /// </summary>
        private bool IsReservedInventoryUIActiveInHierarchy()
        {
            if (inventoryPersistentGrids != null)
            {
                for (int i = 0; i < inventoryPersistentGrids.Length; i++)
                {
                    var g = inventoryPersistentGrids[i];
                    if (g != null && g.gameObject.activeInHierarchy) return true;
                }
            }
            if (inventorySlots != null)
            {
                for (int i = 0; i < inventorySlots.Length; i++)
                {
                    var s = inventorySlots[i];
                    if (s != null && s.gameObject.activeInHierarchy) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Recycle all TetrisItemViews and clear the cache related to the view.
        /// </summary>
        /// <param name="markClosedCleared">Whether it is marked as "inventory closed status has been cleared" for idempotent anti reentry.</param>
        private void ClearAllItemViewsAndCaches(bool markClosedCleared)
        {
            if (markClosedCleared && _closedStateCleared) return;

            var root = InventoryManager.Instance != null && InventoryManager.Instance.InventorySystemRoot != null
                ? InventoryManager.Instance.InventorySystemRoot.transform
                : null;
            if (root != null)
            {
                var itemViews = root.GetComponentsInChildren<TetrisItemView>(true);
                for (int i = 0; i < itemViews.Length; i++)
                {
                    var v = itemViews[i];
                    if (v == null) continue;
                    if (IsPooled(v)) continue;
                    TetrisItemFactory.ReleaseView(v);
                }
            }
            else
            {
                var itemViews = FindObjectsOfType<TetrisItemView>(true);
                for (int i = 0; i < itemViews.Length; i++)
                {
                    var v = itemViews[i];
                    if (v == null) continue;
                    if (IsPooled(v)) continue;
                    TetrisItemFactory.ReleaseView(v);
                }
            }

            slotItemGuidDic.Clear();
            parentItemGuidDic.Clear();
            normalItemGuidDic.Clear();

            if (markClosedCleared) _closedStateCleared = true;
        }

        private bool IsPooled(Component c)
        {
            var pool = PoolManager.Instance;
            return pool != null && pool.IsPooled(c);
        }

        /// <summary>
        /// Bind the manually configured persistent GridView with its GUID to ensure that the ViewModel is available and can render items from the cache.
        /// </summary>
        private void BindReservedPersistentGrids()
        {
            if (inventoryPersistentGrids == null) return;
            for (int i = 0; i < inventoryPersistentGrids.Length; i++)
            {
                var gridView = inventoryPersistentGrids[i];
                if (gridView == null) continue;
                var guidComp = gridView.GetComponent<DataGUID>();
                var guid = guidComp != null ? guidComp.guid : null;
                if (string.IsNullOrEmpty(guid)) continue;
                TetrisGridFactory.BindViewToGuid(gridView, guid);
            }
        }

        private void RestoreSlotVMsFromPersistentData()
        {
            AssignSlotIndices();
            if (inventorySlots != null)
            {
                for (int i = 0; i < inventorySlots.Length; i++)
                {
                    var slot = inventorySlots[i];
                    var vm = slot != null ? slot.ViewModel : null;
                    if (vm != null) vm.RemoveTetrisItem(true);
                }
            }

            var list = inventoryData_SO != null ? inventoryData_SO.inventoryItemList : null;
            if (list == null) return;

            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (item == null || !item.isOnSlot) continue;

                var details = itemDataList_SO != null ? itemDataList_SO.GetItemDetailsByID(item.itemID) : null;
                if (details == null) continue;

                var targetSlot = ResolveSlotView(item);
                if (targetSlot == null || targetSlot.ViewModel == null) continue;

                var itemVm = TetrisItemFactory.GetOrCreateVM(details, item, targetSlot.ViewModel);
                if (itemVm == null) continue;

                targetSlot.ViewModel.TryPlaceTetrisItem(itemVm, 0, 0);
            }
        }

        /// <summary>
        /// Refresh the manually configured persistent GridView to trigger rebinding and rendering of existing items from the cache.
        /// </summary>
        private void RefreshReservedPersistentGridViews()
        {
            if (inventoryPersistentGrids == null) return;
            for (int i = 0; i < inventoryPersistentGrids.Length; i++)
            {
                var gv = inventoryPersistentGrids[i];
                if (gv == null) continue;
                var vm = gv.ViewModel;
                if (vm == null) continue;
                gv.ViewModel = null;
                gv.ViewModel = vm;
            }
        }

        private void BuildRuntimeCache()
        {
            var cache = Context.GetApplicationContext().GetService<IInventoryTreeCache>();
            if (cache == null) return;
            cache.Clear();
            if (inventoryPersistentGrids != null)
            {
                for (int i = 0; i < inventoryPersistentGrids.Length; i++)
                {
                    var g = inventoryPersistentGrids[i];
                    if (g == null) continue;
                    var guidComp = g.GetComponent<DataGUID>();
                    var id = guidComp != null ? guidComp.guid : null;
                    if (string.IsNullOrEmpty(id)) continue;
                    cache.GetOrCreateContainer(id);
                    var vm = g.ViewModel;
                    if (vm != null)
                    {
                        cache.SetContainerConfig(id, vm.GridSizeWidth, vm.GridSizeHeight, vm.LocalGridTileSizeWidth, vm.LocalGridTileSizeHeight);
                    }
                }
            }

            var list = inventoryData_SO != null ? inventoryData_SO.inventoryItemList : null;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    string containerId = null;
                    if (!string.IsNullOrEmpty(item.persistentGridGuid)) containerId = item.persistentGridGuid;
                    else if (!string.IsNullOrEmpty(item.parentItemGuid)) containerId = item.parentItemGuid + ":" + item.gridPIndex.ToString();
                    if (string.IsNullOrEmpty(item.parentItemGuid) && !string.IsNullOrEmpty(item.persistentGridGuid))
                    {
                        int sep = item.persistentGridGuid.IndexOf(':');
                        if (sep > 0)
                        {
                            item.parentItemGuid = item.persistentGridGuid.Substring(0, sep);
                            int gp = 0;
                            int.TryParse(item.persistentGridGuid.Substring(sep + 1), out gp);
                            item.gridPIndex = gp;
                            item.isOnSlot = false;
                        }
                    }
                    if (!string.IsNullOrEmpty(containerId))
                    {
                        cache.PlaceItem(containerId, item);
                    }
                }
            }
        }

        /// <summary>
        /// Build a lookup table of GUID ->TetrisItemGrid (supporting dynamic/multi grid).
        /// </summary>
        private void BuildGridGuidMap()
        {
            gridGuidMap.Clear();
            if (inventoryPersistentGrids != null)
            {
                for (int i = 0; i < inventoryPersistentGrids.Length; i++)
                {
                    var g = inventoryPersistentGrids[i];
                    if (g == null) continue;
                    var guidComp = g.GetComponent<DataGUID>();
                    if (guidComp != null && !string.IsNullOrEmpty(guidComp.guid))
                    {
                        gridGuidMap[guidComp.guid] = g;
                    }
                }
            }

            var grids = UnityEngine.Object.FindObjectsOfType<TetrisGridView>(true);
            foreach (var g in grids)
            {
                if (g == null) continue;
                var guidComp = g.GetComponent<DataGUID>();
                if (guidComp != null && !string.IsNullOrEmpty(guidComp.guid))
                {
                    if (!gridGuidMap.ContainsKey(guidComp.guid))
                        gridGuidMap[guidComp.guid] = g;
                }
            }
        }

        /// <summary>
        /// Whether the item should be retained in the event of death (e.g. safe deposit box).
        /// </summary>
        private bool IsItemRetainedOnDeath(TetrisItemPersistentData item)
        {
            var grid = ResolvePersistentGrid(item);
            if (grid == null) return false;
            var desc = grid.GetComponent<InventoryGridDescriptor>();
            return desc != null && desc.retainedOnDeath;
        }

        /// <summary>
        /// Resolve the target persistent grid according to the archive entry (priority GUID, compatible with the old index).
        /// </summary>
        private TetrisGridView ResolvePersistentGrid(TetrisItemPersistentData item)
        {
            if (!string.IsNullOrEmpty(item.persistentGridGuid))
            {
                if (gridGuidMap.TryGetValue(item.persistentGridGuid, out var grid))
                    return grid;

                // The mapping may be rebuilt once after the UI grid is instantiated at Start
                BuildGridGuidMap();
                if (gridGuidMap.TryGetValue(item.persistentGridGuid, out grid))
                    return grid;
            }
            return null;
        }

        private void NormalizeItemRelations()
        {
            var list = inventoryData_SO != null ? inventoryData_SO.inventoryItemList : null;
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (string.IsNullOrEmpty(item.parentItemGuid) && !string.IsNullOrEmpty(item.persistentGridGuid))
                {
                    int sep = item.persistentGridGuid.IndexOf(':');
                    if (sep > 0)
                    {
                        item.parentItemGuid = item.persistentGridGuid.Substring(0, sep);
                        int gp = 0;
                        int.TryParse(item.persistentGridGuid.Substring(sep + 1), out gp);
                        item.gridPIndex = gp;
                        item.isOnSlot = false;
                    }
                }
            }
        }

        private void AssignSlotIndices()
        {
            if (inventorySlots == null) return;
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                var s = inventorySlots[i];
                if (s == null) continue;
                var vm = s.ViewModel;
                if (vm != null) vm.SlotIndex = i;
            }
        }

        private TetrisSlotView ResolveSlotView(TetrisItemPersistentData item)
        {
            if (inventorySlots == null || item == null) return null;
            if (item.slotIndex < 0 || item.slotIndex >= inventorySlots.Length) return null;
            return inventorySlots[item.slotIndex];
        }
        
    }
}
