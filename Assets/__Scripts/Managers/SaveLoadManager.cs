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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Cholopol.TIS.Debug;
using Cholopol.TIS.Events;

namespace Cholopol.TIS.SaveLoadSystem
{
    public class SaveLoadManager : Singleton<SaveLoadManager>, ISaveLoadService
    {
        private const string OkHex = "#00E676";
        private const string WarnHex = "#FFC107";
        private const string FailHex = "#FF5252";
        private const string SaveHex = "#00E676";
        private const string LoadHex = "#40C4FF";
        private const string DeleteHex = "#FF5252";

        private List<ISaveable> saveableList = new List<ISaveable>();
        public List<DataSlot> dataSlots = new List<DataSlot>(new DataSlot[3]);
        public List<string> slotTimestamps = new List<string>(new string[3]);
        private string jsonFolder;

        protected override void Awake()
        {
            base.Awake();
            jsonFolder = Path.Combine(Application.persistentDataPath, "Cholopol_TIS_Data");
            ReadSaveData();
            ServiceLocator.Register<ISaveLoadService>(this);
        }

        private void OnEnable()
        {
            EventBus.Instance.Subscribe<int>(EventNames.StartGameEvent, OnStartGameEvent);
            EventBus.Instance.Subscribe<int>(EventNames.SaveGameEvent, OnSaveGameEvent);
            EventBus.Instance.Subscribe<int>(EventNames.DeleteDataEvent, OnDeleteGameEvent);
        }

        private void OnDisable()
        {
            EventBus.Instance.Unsubscribe<int>(EventNames.StartGameEvent, OnStartGameEvent);
            EventBus.Instance.Unsubscribe<int>(EventNames.SaveGameEvent, OnSaveGameEvent);
            EventBus.Instance.Unsubscribe<int>(EventNames.DeleteDataEvent, OnDeleteGameEvent);
        }

        private void OnStartGameEvent(int index)
        {
            Load(index);
        }

        public void OnSaveGameEvent(int index)
        {
            Save(index);
        }

        public void OnDeleteGameEvent(int index)
        {
            Delete(index);
        }

        public void RegisterSaveable(ISaveable saveable)
        {
            if (!saveableList.Contains(saveable))
            {
                saveableList.Add(saveable);
            }
        }

        private static string Rich(string hex, string text)
        {
            return "<color=" + hex + ">" + text + "</color>";
        }

        private int GetSlotCount()
        {
            var dataCount = dataSlots != null ? dataSlots.Count : 0;
            var tsCount = slotTimestamps != null ? slotTimestamps.Count : 0;
            if (dataCount <= 0) return 0;
            if (tsCount <= 0) return dataCount;
            return Math.Min(dataCount, tsCount);
        }

        private bool TryValidateSlotIndex(int index, DebugLevel level, string opName, string chainId)
        {
            var count = GetSlotCount();
            if (index >= 0 && index < count) return true;
            var hint = count > 0 ? "0-" + (count - 1) : "no slots configured";
            GameDebug.Log(DebugChannel.SaveLoad, level, opName + " invalid slot index: " + index + " (" + hint + ")", this, chainId);
            return false;
        }

        private string GetSlotPath(int index)
        {
            return Path.Combine(jsonFolder, "RecordFile" + index + ".json");
        }

        private void ReadSaveData(string chainId = null)
        {
            var slotCount = GetSlotCount();
            if (slotCount <= 0) return;

            if (string.IsNullOrEmpty(jsonFolder) || !Directory.Exists(jsonFolder))
            {
                for (int i = 0; i < slotCount; i++)
                {
                    dataSlots[i] = null;
                    slotTimestamps[i] = null;
                }
                return;
            }

            for (int i = 0; i < slotCount; i++)
            {
                var resultPath = GetSlotPath(i);
                if (!File.Exists(resultPath))
                {
                    dataSlots[i] = null;
                    slotTimestamps[i] = null;
                    continue;
                }

                try
                {
                    var stringData = File.ReadAllText(resultPath);
                    var wrapped = JsonConvert.DeserializeObject<SaveFileWrapper<DataSlot>>(stringData);
                    var payload = Cholopol.TIS.TetrisUtilities.MigrationHandler.Process(wrapped);
                    dataSlots[i] = payload;
                    slotTimestamps[i] = wrapped != null ? wrapped.Timestamp : null;
                }
                catch (Exception e)
                {
                    dataSlots[i] = null;
                    slotTimestamps[i] = null;
                    GameDebug.Log(DebugChannel.SaveLoad, DebugLevel.Warning, "ReadSaveData failed for slot=" + i + " " + Rich(WarnHex, e.GetType().Name) + " " + e.Message, this, chainId);
                }
            }
        }

        public string GetSlotTimestamp(int index)
        {
            if (index < 0 || index >= slotTimestamps.Count) return null;
            return slotTimestamps[index];
        }

        public void Save(int index)
        {
            var chainId = GameDebug.NewChain("SAVE");
            var sw = Stopwatch.StartNew();

            if (!TryValidateSlotIndex(index, DebugLevel.Error, "SAVE", chainId))
            {
                return;
            }

            var slotPath = GetSlotPath(index);
            GameDebug.LogBlockHeader(DebugChannel.SaveLoad, DebugLevel.Info, Rich(SaveHex, "SAVE") + " slot=" + index, this, chainId);
            GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Info, "Folder: " + jsonFolder, this, chainId);
            GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Info, "File: " + slotPath, this, chainId);

            var data = new DataSlot();
            int total = saveableList != null ? saveableList.Count : 0;
            int generated = 0;
            int failed = 0;
            int duplicates = 0;

            for (int i = 0; i < total; i++)
            {
                var saveable = saveableList[i];
                if (saveable == null) continue;

                var guid = saveable.GUID;
                if (string.IsNullOrEmpty(guid))
                {
                    failed++;
                    GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Warning, "Skip: empty GUID (" + saveable.GetType().Name + ")", this, chainId);
                    continue;
                }

                try
                {
                    var saveData = saveable.GenerateSaveData();
                    if (data.dataDict.ContainsKey(guid))
                    {
                        duplicates++;
                        GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Warning, "Duplicate GUID overwrite: " + guid + " (" + saveable.GetType().Name + ")", this, chainId);
                    }
                    data.dataDict[guid] = saveData;
                    generated++;
                }
                catch (Exception e)
                {
                    failed++;
                    GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Error, "Generate failed: " + guid + " (" + saveable.GetType().Name + ") " + Rich(FailHex, e.GetType().Name) + " " + e.Message, this, chainId);
                }
            }

            var now = DateTime.Now;
            var timestamp = now.ToString("yyyy/MM/dd HH:mm:ss");
            dataSlots[index] = data;
            slotTimestamps[index] = timestamp;

            var wrapper = new SaveFileWrapper<DataSlot>
            {
                Version = Settings.CURRENT_VERSION,
                Timestamp = timestamp,
                Payload = data
            };

            string jsonData;
            try
            {
                jsonData = JsonConvert.SerializeObject(wrapper, Formatting.Indented);
            }
            catch (Exception e)
            {
                GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Error, "Serialize failed " + Rich(FailHex, e.GetType().Name) + " " + e.Message, this, chainId);
                GameDebug.LogBlockFooter(DebugChannel.SaveLoad, DebugLevel.Error, Rich(FailHex, "FAIL") + " elapsed=" + sw.ElapsedMilliseconds + "ms", this, chainId);
                return;
            }

            try
            {
                Directory.CreateDirectory(jsonFolder);
                File.WriteAllText(slotPath, jsonData);
            }
            catch (Exception e)
            {
                GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Error, "Write failed " + Rich(FailHex, e.GetType().Name) + " " + e.Message, this, chainId);
                GameDebug.LogBlockFooter(DebugChannel.SaveLoad, DebugLevel.Error, Rich(FailHex, "FAIL") + " elapsed=" + sw.ElapsedMilliseconds + "ms", this, chainId);
                return;
            }

            GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Info, "Timestamp: " + timestamp, this, chainId);
            GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Info, "Saveables: " + total + ", Generated: " + generated + ", Failed: " + failed + ", Duplicates: " + duplicates, this, chainId);
            GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Info, "Json chars: " + (jsonData != null ? jsonData.Length : 0), this, chainId);
            GameDebug.LogBlockFooter(DebugChannel.SaveLoad, DebugLevel.Info, Rich(OkHex, "OK") + " elapsed=" + sw.ElapsedMilliseconds + "ms", this, chainId);
        }

        public void Load(int index)
        {
            var chainId = GameDebug.NewChain("LOAD");
            var sw = Stopwatch.StartNew();

            if (!TryValidateSlotIndex(index, DebugLevel.Error, "LOAD", chainId))
            {
                return;
            }

            var slotPath = GetSlotPath(index);
            GameDebug.LogBlockHeader(DebugChannel.SaveLoad, DebugLevel.Info, Rich(LoadHex, "LOAD") + " slot=" + index, this, chainId);
            GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Info, "Folder: " + jsonFolder, this, chainId);
            GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Info, "File: " + slotPath, this, chainId);

            if (!File.Exists(slotPath))
            {
                GameDebug.LogBlockFooter(DebugChannel.SaveLoad, DebugLevel.Warning, Rich(WarnHex, "NOT FOUND") + " elapsed=" + sw.ElapsedMilliseconds + "ms", this, chainId);
                return;
            }

            string stringData;
            SaveFileWrapper<DataSlot> wrapped;
            try
            {
                stringData = File.ReadAllText(slotPath);
                wrapped = JsonConvert.DeserializeObject<SaveFileWrapper<DataSlot>>(stringData);
            }
            catch (Exception e)
            {
                GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Error, "Read/Deserialize failed " + Rich(FailHex, e.GetType().Name) + " " + e.Message, this, chainId);
                GameDebug.LogBlockFooter(DebugChannel.SaveLoad, DebugLevel.Error, Rich(FailHex, "FAIL") + " elapsed=" + sw.ElapsedMilliseconds + "ms", this, chainId);
                return;
            }

            var payload = Cholopol.TIS.TetrisUtilities.MigrationHandler.Process(wrapped);
            slotTimestamps[index] = wrapped != null ? wrapped.Timestamp : null;
            if (payload == null || payload.dataDict == null)
            {
                GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Warning, "Payload is empty", this, chainId);
                GameDebug.LogBlockFooter(DebugChannel.SaveLoad, DebugLevel.Warning, Rich(WarnHex, "EMPTY") + " elapsed=" + sw.ElapsedMilliseconds + "ms", this, chainId);
                return;
            }

            GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Info, "Version: " + (wrapped != null ? wrapped.Version : -1) + ", Timestamp: " + (wrapped != null ? wrapped.Timestamp : "null"), this, chainId);
            GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Info, "Json chars: " + (stringData != null ? stringData.Length : 0), this, chainId);
            GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Info, "Entries: " + payload.dataDict.Count, this, chainId);

            int total = saveableList != null ? saveableList.Count : 0;
            int restored = 0;
            int missing = 0;
            int failed = 0;

            for (int i = 0; i < total; i++)
            {
                var saveable = saveableList[i];
                if (saveable == null) continue;
                var guid = saveable.GUID;
                if (string.IsNullOrEmpty(guid))
                {
                    missing++;
                    GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Warning, "Missing GUID (" + saveable.GetType().Name + ")", this, chainId);
                    continue;
                }

                if (!payload.dataDict.TryGetValue(guid, out var saveData))
                {
                    missing++;
                    GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Warning, "No data for GUID: " + guid + " (" + saveable.GetType().Name + ")", this, chainId);
                    continue;
                }

                try
                {
                    saveable.RestoreData(saveData);
                    restored++;
                }
                catch (Exception e)
                {
                    failed++;
                    GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Error, "Restore failed: " + guid + " (" + saveable.GetType().Name + ") " + Rich(FailHex, e.GetType().Name) + " " + e.Message, this, chainId);
                }
            }

            GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Info, "Saveables: " + total + ", Restored: " + restored + ", Missing: " + missing + ", Failed: " + failed, this, chainId);
            GameDebug.LogBlockFooter(DebugChannel.SaveLoad, DebugLevel.Info, Rich(OkHex, "OK") + " elapsed=" + sw.ElapsedMilliseconds + "ms", this, chainId);
        }

        public void Delete(int index)
        {
            var chainId = GameDebug.NewChain("DELETE");
            var sw = Stopwatch.StartNew();

            if (!TryValidateSlotIndex(index, DebugLevel.Error, "DELETE", chainId))
            {
                return;
            }

            var slotPath = GetSlotPath(index);
            GameDebug.LogBlockHeader(DebugChannel.SaveLoad, DebugLevel.Info, Rich(DeleteHex, "DELETE") + " slot=" + index, this, chainId);
            GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Info, "Folder: " + jsonFolder, this, chainId);
            GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Info, "File: " + slotPath, this, chainId);

            if (!File.Exists(slotPath))
            {
                GameDebug.LogBlockFooter(DebugChannel.SaveLoad, DebugLevel.Warning, Rich(WarnHex, "NOT FOUND") + " elapsed=" + sw.ElapsedMilliseconds + "ms", this, chainId);
                return;
            }

            try
            {
                File.Delete(slotPath);
            }
            catch (Exception e)
            {
                GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Error, "Delete failed " + Rich(FailHex, e.GetType().Name) + " " + e.Message, this, chainId);
                GameDebug.LogBlockFooter(DebugChannel.SaveLoad, DebugLevel.Error, Rich(FailHex, "FAIL") + " elapsed=" + sw.ElapsedMilliseconds + "ms", this, chainId);
                return;
            }

            ReadSaveData(chainId);
            GameDebug.LogBlockLine(DebugChannel.SaveLoad, DebugLevel.Info, "Slot timestamp after delete: " + (slotTimestamps != null ? slotTimestamps[index] : "null"), this, chainId);
            GameDebug.LogBlockFooter(DebugChannel.SaveLoad, DebugLevel.Info, Rich(OkHex, "OK") + " elapsed=" + sw.ElapsedMilliseconds + "ms", this, chainId);
        }
    }
}
