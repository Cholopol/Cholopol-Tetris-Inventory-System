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
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cholopol.TIS.Debug;

namespace Cholopol.TIS
{
    /// <summary>
    /// CTIS Resource Preloader - Preloads all necessary prefabs when the game starts.
    /// </summary>
    public class CTISResourcePreloader : MonoBehaviour
    {
        [SerializeField] 
        [Tooltip("CTIS prefab configuration file (can be placed anywhere)")]
        private CTISPrefabConfig config;
        
        [SerializeField] private bool preloadOnAwake = true;
        
        public static bool IsPreloaded { get; private set; }
        
        public static event Action OnPreloadComplete;

        private void Awake()
        {
            if (config != null)
            {
                CTISPrefabConfig.SetInstance(config);
            }
            else
            {
                GameDebug.Log(DebugChannel.Other, DebugLevel.Error, "CTISPrefabConfig not assigned", this);
                return;
            }
            
            if (preloadOnAwake)
            {
                PreloadAllAsync();
            }
        }

        /// <summary>
        /// Asynchronously preload all CTIS resources
        /// </summary>
        public static async void PreloadAllAsync()
        {
            if (IsPreloaded) return;
            
            var config = CTISPrefabConfig.Instance;
            if (config == null)
            {
                GameDebug.Log(DebugChannel.Other, DebugLevel.Error, "CTISPrefabConfig missing");
                return;
            }

            if (!config.Validate())
            {
                GameDebug.Log(DebugChannel.Other, DebugLevel.Error, "CTISPrefabConfig invalid");
                return;
            }

            try
            {
                GameDebug.Log(DebugChannel.Other, DebugLevel.Info, "Preload start");
                
                var itemTask = TetrisItemFactory.PreloadPrefabAsync();
                var floatingTask = PreloadAssetAsync(config.floatingGridPanelTemplate, "FloatingGridPanelTemplate");
                var infoTask = PreloadAssetAsync(config.itemInformationPanel, "ItemInformationPanel");

                await Task.WhenAll(itemTask, floatingTask, infoTask);
                
                IsPreloaded = true;
                GameDebug.Log(DebugChannel.Other, DebugLevel.Info, "Preload complete");
                OnPreloadComplete?.Invoke();
            }
            catch (Exception e)
            {
                GameDebug.Log(DebugChannel.Other, DebugLevel.Error, "Preload failed: " + e.Message);
            }
        }

        private static async Task PreloadAssetAsync(AssetReferenceGameObject assetRef, string name)
        {
            if (assetRef == null || !assetRef.RuntimeKeyIsValid())
            {
                GameDebug.Log(DebugChannel.Other, DebugLevel.Warning, name + " invalid");
                return;
            }

            var handle = assetRef.LoadAssetAsync<GameObject>();
            await handle.Task;
            
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameDebug.Log(DebugChannel.Other, DebugLevel.Info, "Preloaded " + name);
            }
            else
            {
                GameDebug.Log(DebugChannel.Other, DebugLevel.Error, "Preload failed " + name);
            }
        }

        /// <summary>
        /// Release all CTIS resources (called when the game exits or the scene is completely unloaded).
        /// </summary>
        public static void ReleaseAll()
        {
            TetrisItemFactory.ReleasePrefab();
            
            var config = CTISPrefabConfig.Instance;
            if (config != null)
            {
                if (config.floatingGridPanelTemplate != null && config.floatingGridPanelTemplate.IsValid())
                {
                    config.floatingGridPanelTemplate.ReleaseAsset();
                }
                if (config.itemInformationPanel != null && config.itemInformationPanel.IsValid())
                {
                    config.itemInformationPanel.ReleaseAsset();
                }
            }
            
            IsPreloaded = false;
            GameDebug.Log(DebugChannel.Other, DebugLevel.Info, "Resources released");
        }

        private void OnDestroy()
        {
            // Optional: Release resources when this object is destroyed.
            // ReleaseAll();
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            IsPreloaded = false;
            OnPreloadComplete = null;
        }
#endif
    }
}
