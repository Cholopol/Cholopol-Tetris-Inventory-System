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
using UnityEditor;
using UnityEngine;
using System.IO;
using Cholopol.TIS.Debug;

namespace Cholopol.TIS.Editor
{
    public static class SaveLoadMenu
    {
        /// <summary>
        /// Opens the persistent data path where save files are stored.
        /// </summary>
        [MenuItem("CTIS/SaveLoad/Open Save Folder")]
        public static void OpenSaveFolder()
        {
            string path = Path.Combine(Application.persistentDataPath, "Cholopol_TIS_Data");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            EditorUtility.RevealInFinder(path);
        }

        /// <summary>
        /// Deletes all save data in the persistent data path.
        /// </summary>
        [MenuItem("CTIS/SaveLoad/Delete All Save Data")]
        public static void DeleteAllSaveData()
        {
            string path = Path.Combine(Application.persistentDataPath, "Cholopol_TIS_Data");
            if (Directory.Exists(path))
            {
                if (EditorUtility.DisplayDialog("Delete All Save Data", 
                    "Are you sure you want to delete all save data? This cannot be undone.", "Yes", "No"))
                {
                    Directory.Delete(path, true);
                    GameDebug.Log(DebugChannel.SaveLoad, DebugLevel.Info, "Deleted all save data: " + path);
                    AssetDatabase.Refresh();
                }
            }
            else
            {
                GameDebug.Log(DebugChannel.SaveLoad, DebugLevel.Warning, "Save folder not found: " + path);
            }
        }
    }
}
