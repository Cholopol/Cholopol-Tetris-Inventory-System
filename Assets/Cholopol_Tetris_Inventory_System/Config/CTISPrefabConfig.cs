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
using UnityEngine.AddressableAssets;
using Cholopol.TIS.Debug;

namespace Cholopol.TIS
{
    /// <summary>
    /// CTIS 预制体配置 - 使用 Addressables AssetReference 管理预制体引用
    /// </summary>
    [CreateAssetMenu(fileName = "CTISPrefabConfig", menuName = "CTIS/Prefab Config")]
    public class CTISPrefabConfig : ScriptableObject
    {
        private static CTISPrefabConfig _instance;

        [Header("物品预制体")]
        [Tooltip("通用俄罗斯方块物品预制体")]
        public AssetReferenceGameObject generalTetrisItemPrefab;

        [Header("窗口预制体")]
        [Tooltip("浮动网格面板模板")]
        public AssetReferenceGameObject floatingGridPanelTemplate;
        
        [Tooltip("物品信息面板")]
        public AssetReferenceGameObject itemInformationPanel;

        [Header("编辑器资源")]
        [Tooltip("默认物品图标")]
        public AssetReferenceSprite defaultItemIcon;

        [Tooltip("编辑器 Logo")]
        public AssetReferenceSprite editorLogoSprite;

        /// <summary>
        /// 获取配置实例（由 CTISResourcePreloader 注入）
        /// </summary>
        public static CTISPrefabConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameDebug.Log(DebugChannel.Other, DebugLevel.Error, "CTISPrefabConfig missing");
                }
                return _instance;
            }
        }

        /// <summary>
        /// 设置配置实例（由 CTISResourcePreloader 在启动时调用）
        /// </summary>
        public static void SetInstance(CTISPrefabConfig config)
        {
            _instance = config;
        }

        /// <summary>
        /// 验证配置是否完整
        /// </summary>
        public bool Validate()
        {
            bool isValid = true;
            
            if (generalTetrisItemPrefab == null || !generalTetrisItemPrefab.RuntimeKeyIsValid())
            {
                GameDebug.Log(DebugChannel.Other, DebugLevel.Error, "Missing generalTetrisItemPrefab");
                isValid = false;
            }
            
            if (floatingGridPanelTemplate == null || !floatingGridPanelTemplate.RuntimeKeyIsValid())
            {
                GameDebug.Log(DebugChannel.Other, DebugLevel.Error, "Missing floatingGridPanelTemplate");
                isValid = false;
            }
            
            if (itemInformationPanel == null || !itemInformationPanel.RuntimeKeyIsValid())
            {
                GameDebug.Log(DebugChannel.Other, DebugLevel.Error, "Missing itemInformationPanel");
                isValid = false;
            }
            
            return isValid;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器下重置单例（用于热重载）
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            _instance = null;
        }
#endif
    }
}
