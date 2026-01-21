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
    public interface IInventoryTreeCache
    {
        ContainerNode GetOrCreateContainer(string containerId);
        bool TryGetContainer(string containerId, out ContainerNode container);
        void SetContainerOwner(string containerId, string ownerItemGuid);
        void SetContainerConfig(string containerId, int w, int h, float tileW, float tileH);
        ItemNode UpsertItem(TetrisItemPersistentData data);
        void PlaceItem(string containerId, TetrisItemPersistentData data);
        bool RemoveItem(string itemGuid);
        bool RemoveFromContainer(string containerId, string itemGuid);
        IEnumerable<TetrisItemPersistentData> GetItems(string containerId);
        bool TryGetItem(string itemGuid, out ItemNode node);
        string GetContainerIdByItem(string itemGuid);
        void Clear();
        void ClearContainerItems(string containerId);
        IEnumerable<ContainerNode> GetAllContainers();
        IEnumerable<string> GetAllContainerIds();
    }
}
