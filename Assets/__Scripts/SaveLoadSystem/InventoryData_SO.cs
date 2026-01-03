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
using System.Collections.Generic;
using UnityEngine;

namespace Cholopol.TIS.SaveLoadSystem
{
    [CreateAssetMenu(fileName = "InventoryData_SO", menuName = "SaveLoad/InventoryDataList")]
    public class InventoryData_SO : ScriptableObject
    {
        public List<TetrisItemPersistentData> inventoryItemList;
        private Dictionary<string, TetrisItemPersistentData> _byGuid;
        private bool _indexDirty = true;

        private void OnEnable()
        {
            _indexDirty = true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _indexDirty = true;
        }
#endif

        public TetrisItemPersistentData GetTetrisItemPersistentDataByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            EnsureIndex();
            if (_byGuid != null && _byGuid.TryGetValue(guid, out var data)) return data;
            return null;
        }

        public bool HasGridItemGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return false;
            EnsureIndex();
            return _byGuid != null && _byGuid.ContainsKey(guid);
        }

        internal void SetInventoryItemList(List<TetrisItemPersistentData> list, bool rebuildNow = false)
        {
            inventoryItemList = list;
            _indexDirty = true;
            if (rebuildNow)
            {
                EnsureIndex();
            }
        }

        private void RebuildIndex()
        {
            _byGuid = new Dictionary<string, TetrisItemPersistentData>();
            if (inventoryItemList != null)
            {
                for (int i = 0; i < inventoryItemList.Count; i++)
                {
                    var data = inventoryItemList[i];
                    if (data == null || string.IsNullOrEmpty(data.itemGuid)) continue;
                    _byGuid[data.itemGuid] = data;
                }
            }
            _indexDirty = false;
        }

        public void UpsertPersistentData(TetrisItemPersistentData data)
        {
            if (data == null || string.IsNullOrEmpty(data.itemGuid)) return;
            if (inventoryItemList == null) inventoryItemList = new List<TetrisItemPersistentData>();
            EnsureIndex();

            if (_byGuid != null && _byGuid.TryGetValue(data.itemGuid, out var existing))
            {
                if (!ReferenceEquals(existing, data))
                {
                    _byGuid[data.itemGuid] = data;
                    for (int i = 0; i < inventoryItemList.Count; i++)
                    {
                        if (inventoryItemList[i] != null && inventoryItemList[i].itemGuid == data.itemGuid)
                        {
                            inventoryItemList[i] = data;
                            return;
                        }
                    }
                    inventoryItemList.Add(data);
                }
                return;
            }

            _byGuid ??= new Dictionary<string, TetrisItemPersistentData>();
            _byGuid[data.itemGuid] = data;
            inventoryItemList.Add(data);
        }

        public bool RemovePersistentDataByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return false;
            EnsureIndex();
            bool removed = false;

            if (_byGuid != null)
            {
                removed = _byGuid.Remove(guid) || removed;
            }

            if (inventoryItemList != null)
            {
                for (int i = inventoryItemList.Count - 1; i >= 0; i--)
                {
                    var data = inventoryItemList[i];
                    if (data != null && data.itemGuid == guid)
                    {
                        inventoryItemList.RemoveAt(i);
                        removed = true;
                        break;
                    }
                }
            }

            return removed;
        }

        private void EnsureIndex()
        {
            if (_byGuid == null || _indexDirty)
            {
                RebuildIndex();
            }
        }
    }
}
