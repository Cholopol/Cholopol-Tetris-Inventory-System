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
using UnityEngine;

namespace Cholopol.TIS.Debug
{
    public class DebugCenter : Singleton<DebugCenter>
    {
        [SerializeField] private bool globalEnabled = true;
        [SerializeField] private bool inventoryFlowEnabled = true;
        [SerializeField] private bool saveLoadEnabled = true;
        [SerializeField] private bool viewLifecycleEnabled = true;
        [SerializeField] private bool viewModelLifecycleEnabled = true;
        [SerializeField] private bool uiOverlayEnabled = true;
        [SerializeField] private bool cacheSyncEnabled = true;
        [SerializeField] private DebugLevel minLevel = DebugLevel.Info;

        public bool GlobalEnabled => globalEnabled;

        public bool IsChannelEnabled(DebugChannel channel)
        {
            if (!globalEnabled) return false;
            switch (channel)
            {
                case DebugChannel.InventoryFlow:
                    return inventoryFlowEnabled;
                case DebugChannel.SaveLoad:
                    return saveLoadEnabled;
                case DebugChannel.ViewLifecycle:
                    return viewLifecycleEnabled;
                case DebugChannel.ViewModelLifecycle:
                    return viewModelLifecycleEnabled;
                case DebugChannel.UIOverlay:
                    return uiOverlayEnabled;
                case DebugChannel.CacheSync:
                    return cacheSyncEnabled;
                default:
                    return true;
            }
        }

        public bool IsLevelEnabled(DebugLevel level)
        {
            return level >= minLevel;
        }
    }
}

