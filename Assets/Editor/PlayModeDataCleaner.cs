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
using Cholopol.TIS.SaveLoadSystem;
using UnityEditor;

[InitializeOnLoad] // Ensure that classes are automatically initialized during script recompilation
public static class PlayModeDataCleaner
{
    static PlayModeDataCleaner()
    {
        // Register Play Mode status change event
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Trigger reset only when exiting Play Mode
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            ResetScriptableObjectData();
        }
    }

    private static void ResetScriptableObjectData()
    {
        // Load the target ScriptableObject resource
        InventoryData_SO data = AssetDatabase.LoadAssetAtPath<InventoryData_SO>("Assets/GameData/SlotItemData/InventoryData_SO.asset");
        if (data != null)
        {
            // Clear the list data
            data.inventoryItemList.Clear();
            // Mark data as dirty (needs to be saved)
            EditorUtility.SetDirty(data);
            // Force saving of resource modifications
            AssetDatabase.SaveAssets();
        }
    }
}
