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

namespace Cholopol.TIS.MVVM
{
    /// <summary>
    /// Represents an item node used to store persistent data of a single item in cache.
    /// Only save data references, not responsible for view or instance management.
    /// </summary>
    public class ItemNode
    {
        public TetrisItemPersistentData Data { get; private set; }
        public string ItemGuid => Data.itemGuid;
        public int ItemID => Data.itemID;
        /// <summary>
        /// Initialize the node using the incoming persistent data.
        /// </summary>
        /// <param name="data">Persistent data of items</param>
        public ItemNode(TetrisItemPersistentData data)
        {
            Data = data;
        }
        /// <summary>
        /// Update the data references bound to this node (such as changes in position, orientation, stacking, etc.).
        /// </summary>
        /// <param name="data">New persistent data</param>
        public void Update(TetrisItemPersistentData data)
        {
            Data = data;
        }
    }

    /// <summary>
    /// Represents a container node (grid, subgrid, etc.) that maintains the configuration of the container and its collection of items.
    /// </summary>
    public class ContainerNode
    {
        private readonly Dictionary<string, TetrisItemPersistentData> _items = new Dictionary<string, TetrisItemPersistentData>();
        public string ContainerId { get; internal set; }
        public int GridSizeWidth { get; internal set; } = 1;
        public int GridSizeHeight { get; internal set; } = 1;
        public float LocalGridTileSizeWidth { get; internal set; } = 20f;
        public float LocalGridTileSizeHeight { get; internal set; } = 20f;
        public string OwnerItemGuid { get; internal set; }
        public IReadOnlyDictionary<string, TetrisItemPersistentData> ItemsByGuid => _items;
        public IEnumerable<TetrisItemPersistentData> Items => _items.Values;
        /// <summary>
        /// Set the grid width, height, and unit size configuration of the container.
        /// </summary>
        /// <param name="w">Grid width (number of columns)</param>
        /// <param name="h">Grid height (number of rows)</param>
        /// <param name="tileW">Cell width</param>
        /// <param name="tileH">Cell height</param>
        public void SetConfig(int w, int h, float tileW, float tileH)
        {
            GridSizeWidth = w;
            GridSizeHeight = h;
            LocalGridTileSizeWidth = tileW;
            LocalGridTileSizeHeight = tileH;
        }
        /// <summary>
        /// Insert or update item data in the container based on the GUID.
        /// </summary>
        /// <param name="data">Persistent data of items</param>
        public void Upsert(TetrisItemPersistentData data)
        {
            _items[data.itemGuid] = data;
        }
        /// <summary>
        /// Retrieve corresponding data through the item's GUID.
        /// </summary>
        /// <param name="itemGuid">item GUID</param>
        /// <param name="data">Output item data</param>
        /// <returns>Whether it hits</returns>
        public bool TryGet(string itemGuid, out TetrisItemPersistentData data)
        {
            return _items.TryGetValue(itemGuid, out data);
        }
        /// <summary>
        /// Remove the specified item from the container.
        /// </summary>
        /// <param name="itemGuid">item GUID</param>
        /// <returns>Whether the removal was successful</returns>
        public bool Remove(string itemGuid)
        {
            return _items.Remove(itemGuid);
        }
        /// <summary>
        /// Empty all item data from the container.
        /// </summary>
        public void Clear()
        {
            _items.Clear();
        }
    }

    /// <summary>
    /// Runtime caching of item container relationships, providing fast queries and updates,
    /// Used to reduce the overhead of frequently searching and rebuilding data in the UI and service layers.
    /// </summary>
    public class InventoryTreeCache : IInventoryTreeCache
    {
        public const bool DebugEnabled = false;
        private readonly Dictionary<string, ContainerNode> _containers = new Dictionary<string, ContainerNode>();
        private readonly Dictionary<string, ItemNode> _items = new Dictionary<string, ItemNode>();
        private readonly Dictionary<string, string> _itemToContainer = new Dictionary<string, string>();

        /// <summary>
        /// Retrieve or create container nodes (by container unique ID).
        /// </summary>
        /// <param name="containerId">Unique identification of container（ Grid GUID or parentGuid:index）</param>
        /// <returns>Container Node</returns>
        public ContainerNode GetOrCreateContainer(string containerId)
        {
            if (string.IsNullOrEmpty(containerId)) return null;
            if (_containers.TryGetValue(containerId, out var c)) return c;
            c = new ContainerNode { ContainerId = containerId };
            _containers[containerId] = c;
            return c;
        }

        /// <summary>
        /// Retrieve the container node (if it exists) through the container ID.
        /// </summary>
        /// <param name="containerId">Unique identification of container</param>
        /// <param name="container">Output container node</param>
        /// <returns>Whether it hits</returns>
        public bool TryGetContainer(string containerId, out ContainerNode container)
        {
            return _containers.TryGetValue(containerId, out container);
        }

        /// <summary>
        /// Set the owner item GUID of the container (for scenarios where the parent item has a child grid).
        /// </summary>
        /// <param name="containerId">Unique identification of container</param>
        /// <param name="ownerItemGuid">parent item GUID</param>
        public void SetContainerOwner(string containerId, string ownerItemGuid)
        {
            var c = GetOrCreateContainer(containerId);
            if (c != null) c.OwnerItemGuid = ownerItemGuid;
        }

        /// <summary>
        /// Set the grid configuration of the container (width, height, and unit size).
        /// </summary>
        /// <param name="containerId">Unique identification of container</param>
        /// <param name="w">grid width</param>
        /// <param name="h">Grid height</param>
        /// <param name="tileW">Cell width</param>
        /// <param name="tileH">Cell height</param>
        public void SetContainerConfig(string containerId, int w, int h, float tileW, float tileH)
        {
            var c = GetOrCreateContainer(containerId);
            if (c != null) c.SetConfig(w, h, tileW, tileH);
        }

        /// <summary>
        /// Insert or update item nodes (global dimension, independent of containers).
        /// </summary>
        /// <param name="data">Persistent data of items</param>
        /// <returns>Item Node</returns>
        public ItemNode UpsertItem(TetrisItemPersistentData data)
        {
            if (data == null) return null;
            if (string.IsNullOrEmpty(data.itemGuid)) return null;
            var guid = data.itemGuid;
            if (_items.TryGetValue(guid, out var node))
            {
                node.Update(data);
                return node;
            }
            node = new ItemNode(data);
            _items[guid] = node;
            return node;
        }

        /// <summary>
        /// Place items in designated containers and maintain bidirectional mapping between containers and items.
        /// </summary>
        /// <param name="containerId">Unique identification of container</param>
        /// <param name="data">Persistent data of items</param>
        public void PlaceItem(string containerId, TetrisItemPersistentData data)
        {
            var c = GetOrCreateContainer(containerId);
            if (c == null || data == null)
            {
                return;
            }
            data.persistentGridGuid = containerId;
            UpsertItem(data);
            c.Upsert(data);
            _itemToContainer[data.itemGuid] = containerId;
        }

        /// <summary>
        /// Global removal of items: Remove and clean up global item nodes and mappings from their respective containers.
        /// </summary>
        /// <param name="itemGuid">item GUID</param>
        /// <returns>Whether the removal was successful (hit entry)</returns>
        public bool RemoveItem(string itemGuid)
        {
            if (string.IsNullOrEmpty(itemGuid))
            {
                return false;
            }
            string containerId = null;
            if (_itemToContainer.TryGetValue(itemGuid, out containerId))
            {
                if (_containers.TryGetValue(containerId, out var c))
                {
                    c.Remove(itemGuid);
                }
                _itemToContainer.Remove(itemGuid);
            }
            var removed = _items.Remove(itemGuid);
            return removed;
        }

        /// <summary>
        /// Only remove an item from the specified container (without deleting the global item node).
        /// </summary>
        /// <param name="containerId">Unique identification of container</param>
        /// <param name="itemGuid">item GUID</param>
        /// <returns>Was the removal successful</returns>
        public bool RemoveFromContainer(string containerId, string itemGuid)
        {
            if (!_containers.TryGetValue(containerId, out var c))
            {
                return false;
            }
            var removed = c.Remove(itemGuid);
            if (removed)
            {
                _itemToContainer.Remove(itemGuid);
            }
            return removed;
        }

        /// <summary>
        /// Retrieve the dataset of all items inside the container.
        /// </summary>
        /// <param name="containerId">Unique identification of container</param>
        /// <returns>The persistent dataset in the container (returns an empty array if it does not exist)</returns>
        public IEnumerable<TetrisItemPersistentData> GetItems(string containerId)
        {
            if (_containers.TryGetValue(containerId, out var c)) return c.Items;
            return System.Array.Empty<TetrisItemPersistentData>();
        }

        /// <summary>
        /// Retrieve the global item node through the GUID.
        /// </summary>
        /// <param name="itemGuid">item GUID</param>
        /// <param name="node">Output item node</param>
        /// <returns>Whether it hits</returns>
        public bool TryGetItem(string itemGuid, out ItemNode node)
        {
            return _items.TryGetValue(itemGuid, out node);
        }

        /// <summary>
        /// Obtain the unique identifier of the container to which an item belongs.
        /// </summary>
        /// <param name="itemGuid">item GUID</param>
        /// <returns>Container ID (returns null if not recorded)</returns>
        public string GetContainerIdByItem(string itemGuid)
        {
            if (_itemToContainer.TryGetValue(itemGuid, out var id)) return id;
            return null;
        }

        /// <summary>
        /// Clear all containers, items, and mappings (available for rebuilding runtime cache).
        /// </summary>
        public void Clear()
        {
            _containers.Clear();
            _items.Clear();
            _itemToContainer.Clear();
        }

        /// <summary>
        /// Empty all items in the specified container and clear the corresponding items ->container mapping.
        /// </summary>
        /// <param name="containerId">Unique identification of container</param>
        public void ClearContainerItems(string containerId)
        {
            if (string.IsNullOrEmpty(containerId))
            {
                return;
            }
            if (_containers.TryGetValue(containerId, out var c))
            {
                var toRemove = new List<string>();
                foreach (var kv in _itemToContainer)
                {
                    if (kv.Value == containerId) toRemove.Add(kv.Key);
                }
                for (int i = 0; i < toRemove.Count; i++)
                {
                    _itemToContainer.Remove(toRemove[i]);
                }
                c.Clear();
            }
        }

        /// <summary>
        /// Get a collection of all container nodes (for debugging or monitoring).
        /// </summary>
        public IEnumerable<ContainerNode> GetAllContainers()
        {
            return _containers.Values;
        }

        /// <summary>
        /// Get a collection of all container IDs (for traversal or debugging).
        /// </summary>
        public IEnumerable<string> GetAllContainerIds()
        {
            return _containers.Keys;
        }
    }
}
