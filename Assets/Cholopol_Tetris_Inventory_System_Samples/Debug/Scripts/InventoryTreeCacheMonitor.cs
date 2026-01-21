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
using Cholopol.TIS.MVVM;
using Cholopol.TIS.SaveLoadSystem;
using Loxodon.Framework.Contexts;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;

namespace Cholopol.TIS.Debug
{
    public class InventoryTreeCacheMonitor : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text outputText;

        [Header("Data (Optional)")]
        [SerializeField] private ItemDataList_SO itemDataList;

        [Header("Refresh")]
        [SerializeField] private float refreshInterval = 0.2f;
        [SerializeField] private bool includeEmptyContainers = false;
        [SerializeField] private bool showExtraFields = false;
        [SerializeField] private int maxLines = 200;

        private float _nextRefreshTime;
        private readonly StringBuilder _sb = new StringBuilder(4096);
        private readonly List<ContainerNode> _containersBuffer = new List<ContainerNode>(64);
        private readonly List<TetrisItemPersistentData> _itemsBuffer = new List<TetrisItemPersistentData>(256);

        private System.Type _reflectedCacheType;
        private FieldInfo _reflectedContainersField;
        private FieldInfo _reflectedItemsField;
        private FieldInfo _reflectedItemToContainerField;

        private const string ColorHeader = "#00D4FF";
        private const string ColorSection = "#FFA500";
        private const string ColorKey = "#FFD966";
        private const string ColorValue = "#B6FFB6";
        private const string ColorDim = "#9AA0A6";
        private const string ColorWarn = "#FF6B6B";

        private const string SeparatorLine = "------------------------------------------------";

        private void Update()
        {
            if (outputText == null) return;
            if (refreshInterval <= 0f)
            {
                RefreshNow();
                return;
            }

            if (Time.unscaledTime < _nextRefreshTime) return;
            _nextRefreshTime = Time.unscaledTime + refreshInterval;
            RefreshNow();
        }

        public void RefreshNow()
        {
            if (outputText == null) return;

            var cache = Context.GetApplicationContext().GetService<IInventoryTreeCache>();
            if (cache == null)
            {
                outputText.text = Colorize("InventoryTreeCache: <null>", ColorWarn);
                return;
            }

            var resolvedItemDataList = ResolveItemDataList();

            _containersBuffer.Clear();
            foreach (var c in cache.GetAllContainers())
            {
                if (c == null) continue;
                if (!includeEmptyContainers && (c.ItemsByGuid == null || c.ItemsByGuid.Count == 0)) continue;
                _containersBuffer.Add(c);
            }
            _containersBuffer.Sort((a, b) => string.CompareOrdinal(a.ContainerId, b.ContainerId));

            int containerCount = _containersBuffer.Count;
            int itemCount = 0;

            _sb.Clear();
            _sb.Append(Colorize("InventoryTreeCache", ColorHeader));
            _sb.Append("  ");
            _sb.Append(Colorize("containers=", ColorKey));
            _sb.Append(Colorize(containerCount.ToString(), ColorValue));

            for (int i = 0; i < _containersBuffer.Count; i++)
            {
                var c = _containersBuffer[i];
                int cItemCount = c.ItemsByGuid != null ? c.ItemsByGuid.Count : 0;
                itemCount += cItemCount;
            }

            _sb.Append(" ");
            _sb.Append(Colorize("items=", ColorKey));
            _sb.Append(Colorize(itemCount.ToString(), ColorValue));
            _sb.Append('\n');

            int lineCount = 1;
            AppendSeparator(ref lineCount);
            AppendLine(Colorize("Containers (from cache.GetAllContainers)", ColorSection), ref lineCount);
            for (int i = 0; i < _containersBuffer.Count; i++)
            {
                var c = _containersBuffer[i];
                if (c == null) continue;

                if (lineCount >= maxLines) break;

                AppendSeparator(ref lineCount);
                AppendLine(
                    "[" + Colorize(c.ContainerId, ColorKey) + "] " +
                    Colorize("owner=", ColorDim) + Colorize(string.IsNullOrEmpty(c.OwnerItemGuid) ? "-" : c.OwnerItemGuid, ColorValue) + " " +
                    Colorize("items=", ColorDim) + Colorize((c.ItemsByGuid != null ? c.ItemsByGuid.Count : 0).ToString(), ColorValue),
                    ref lineCount);

                _itemsBuffer.Clear();
                if (c.ItemsByGuid != null)
                {
                    foreach (var kv in c.ItemsByGuid)
                    {
                        if (kv.Value != null) _itemsBuffer.Add(kv.Value);
                    }
                }
                _itemsBuffer.Sort((a, b) => string.CompareOrdinal(a.itemGuid, b.itemGuid));

                for (int j = 0; j < _itemsBuffer.Count; j++)
                {
                    if (lineCount >= maxLines) break;

                    var data = _itemsBuffer[j];
                    var guid = data.itemGuid;
                    var details = resolvedItemDataList != null ? resolvedItemDataList.GetItemDetailsByID(data.itemID) : null;
                    var itemName = details != null && details.localizedName != null && !details.localizedName.IsEmpty ? details.localizedName.GetLocalizedString() : ("ItemID=" + data.itemID.ToString());
                    var itemType = details != null ? details.inventorySlotType.ToString() : "Unknown";

                    _sb.Append("  ");
                    _sb.Append(Colorize(guid, ColorKey));
                    _sb.Append(" ");
                    _sb.Append(Colorize("|", ColorDim));
                    _sb.Append(" ");
                    _sb.Append(Colorize(itemName, ColorValue));
                    _sb.Append(" ");
                    _sb.Append(Colorize("|", ColorDim));
                    _sb.Append(" ");
                    _sb.Append(Colorize(itemType, ColorHeader));

                    if (showExtraFields)
                    {
                        _sb.Append(" | pos=(");
                        _sb.Append(data.orginPosition.x);
                        _sb.Append(',');
                        _sb.Append(data.orginPosition.y);
                        _sb.Append(") stack=");
                        _sb.Append(data.stack);
                        _sb.Append(" dir=");
                        _sb.Append(data.direction.ToString());
                    }

                    _sb.Append('\n');
                    lineCount++;
                }
            }

            if (lineCount < maxLines)
            {
                AppendSeparator(ref lineCount);
                AppendLine(Colorize("Raw Dictionaries (InventoryTreeCache private fields)", ColorSection), ref lineCount);
                AppendInventoryTreeCacheInternals(cache, resolvedItemDataList, ref lineCount);
            }

            outputText.text = _sb.ToString();
        }

        private void AppendInventoryTreeCacheInternals(IInventoryTreeCache cache, ItemDataList_SO resolvedItemDataList, ref int lineCount)
        {
            if (cache == null) return;
            if (lineCount >= maxLines) return;

            var type = cache.GetType();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;

            if (_reflectedCacheType != type)
            {
                _reflectedCacheType = type;
                _reflectedContainersField = type.GetField("_containers", flags);
                _reflectedItemsField = type.GetField("_items", flags);
                _reflectedItemToContainerField = type.GetField("_itemToContainer", flags);
            }

            if (_reflectedContainersField == null || _reflectedItemsField == null || _reflectedItemToContainerField == null)
            {
                AppendLine(Colorize("Cannot reflect _containers/_items/_itemToContainer from: ", ColorWarn) + Colorize(type.FullName, ColorKey), ref lineCount);
                return;
            }

            var containers = _reflectedContainersField.GetValue(cache) as Dictionary<string, ContainerNode>;
            var items = _reflectedItemsField.GetValue(cache) as Dictionary<string, ItemNode>;
            var itemToContainer = _reflectedItemToContainerField.GetValue(cache) as Dictionary<string, string>;

            AppendLine(
                Colorize("_containers", ColorKey) + "=" + Colorize(containers != null ? containers.Count.ToString() : "null", containers != null ? ColorValue : ColorWarn) + "  " +
                Colorize("_items", ColorKey) + "=" + Colorize(items != null ? items.Count.ToString() : "null", items != null ? ColorValue : ColorWarn) + "  " +
                Colorize("_itemToContainer", ColorKey) + "=" + Colorize(itemToContainer != null ? itemToContainer.Count.ToString() : "null", itemToContainer != null ? ColorValue : ColorWarn),
                ref lineCount);

            if (containers != null && lineCount < maxLines)
            {
                AppendSeparator(ref lineCount);
                AppendLine(Colorize("_containers entries", ColorSection), ref lineCount);

                foreach (var kv in containers)
                {
                    if (lineCount >= maxLines) break;
                    var c = kv.Value;
                    AppendSeparator(ref lineCount);
                    if (c == null)
                    {
                        AppendLine(Colorize(kv.Key, ColorKey) + " => " + Colorize("<null>", ColorWarn), ref lineCount);
                        continue;
                    }

                    AppendLine(
                        Colorize(kv.Key, ColorKey) + " => " +
                        Colorize("id=", ColorDim) + Colorize(c.ContainerId ?? "-", ColorValue) + " " +
                        Colorize("owner=", ColorDim) + Colorize(string.IsNullOrEmpty(c.OwnerItemGuid) ? "-" : c.OwnerItemGuid, ColorValue) + " " +
                        Colorize("size=", ColorDim) + Colorize(c.GridSizeWidth.ToString(), ColorValue) + "x" + Colorize(c.GridSizeHeight.ToString(), ColorValue) + " " +
                        Colorize("tile=", ColorDim) + Colorize(c.LocalGridTileSizeWidth.ToString("0.##"), ColorValue) + "," + Colorize(c.LocalGridTileSizeHeight.ToString("0.##"), ColorValue) + " " +
                        Colorize("items=", ColorDim) + Colorize((c.ItemsByGuid != null ? c.ItemsByGuid.Count : 0).ToString(), ColorValue),
                        ref lineCount);

                    if (lineCount >= maxLines) break;
                    if (c.ItemsByGuid == null || c.ItemsByGuid.Count == 0) continue;

                    _itemsBuffer.Clear();
                    foreach (var ikv in c.ItemsByGuid)
                    {
                        if (ikv.Value != null) _itemsBuffer.Add(ikv.Value);
                    }
                    _itemsBuffer.Sort((a, b) => string.CompareOrdinal(a.itemGuid, b.itemGuid));

                    for (int i = 0; i < _itemsBuffer.Count && lineCount < maxLines; i++)
                    {
                        var data = _itemsBuffer[i];
                        var details = resolvedItemDataList != null ? resolvedItemDataList.GetItemDetailsByID(data.itemID) : null;
                        var itemName = details != null && details.localizedName != null && !details.localizedName.IsEmpty ? details.localizedName.GetLocalizedString() : ("ItemID=" + data.itemID.ToString());
                        var itemType = details != null ? details.inventorySlotType.ToString() : "Unknown";
                        AppendLine(
                            "  " + Colorize(data.itemGuid, ColorKey) + " " + Colorize("|", ColorDim) + " " + Colorize(itemName, ColorValue) + " " + Colorize("|", ColorDim) + " " + Colorize(itemType, ColorHeader),
                            ref lineCount);
                    }
                }
            }

            if (items != null && lineCount < maxLines)
            {
                AppendSeparator(ref lineCount);
                AppendLine(Colorize("_items entries", ColorSection), ref lineCount);

                foreach (var kv in items)
                {
                    if (lineCount >= maxLines) break;
                    var n = kv.Value;
                    AppendSeparator(ref lineCount);
                    if (n == null || n.Data == null)
                    {
                        AppendLine(Colorize(kv.Key, ColorKey) + " => " + Colorize("<null>", ColorWarn), ref lineCount);
                        continue;
                    }

                    var d = n.Data;
                    var details = resolvedItemDataList != null ? resolvedItemDataList.GetItemDetailsByID(d.itemID) : null;
                    var itemName = details != null && details.localizedName != null && !details.localizedName.IsEmpty ? details.localizedName.GetLocalizedString() : ("ItemID=" + d.itemID.ToString());
                    var itemType = details != null ? details.inventorySlotType.ToString() : "Unknown";

                    AppendLine(
                        Colorize(kv.Key, ColorKey) + " => " +
                        Colorize("id=", ColorDim) + Colorize(d.itemID.ToString(), ColorValue) + " " +
                        Colorize("name=", ColorDim) + Colorize(itemName, ColorValue) + " " +
                        Colorize("type=", ColorDim) + Colorize(itemType, ColorHeader) + " " +
                        Colorize("container=", ColorDim) + Colorize(string.IsNullOrEmpty(d.persistentGridGuid) ? "-" : d.persistentGridGuid, ColorValue) + " " +
                        Colorize("parent=", ColorDim) + Colorize(string.IsNullOrEmpty(d.parentItemGuid) ? "-" : d.parentItemGuid, ColorValue) + " " +
                        Colorize("slot=", ColorDim) + Colorize(d.isOnSlot ? "true" : "false", d.isOnSlot ? ColorValue : ColorDim),
                        ref lineCount);
                }
            }

            if (itemToContainer != null && lineCount < maxLines)
            {
                AppendSeparator(ref lineCount);
                AppendLine(Colorize("_itemToContainer entries", ColorSection), ref lineCount);

                foreach (var kv in itemToContainer)
                {
                    if (lineCount >= maxLines) break;
                    AppendSeparator(ref lineCount);
                    AppendLine(Colorize(kv.Key, ColorKey) + " => " + Colorize(kv.Value ?? "<null>", kv.Value != null ? ColorValue : ColorWarn), ref lineCount);
                }
            }
        }

        private void AppendSeparator(ref int lineCount)
        {
            if (lineCount >= maxLines) return;
            _sb.Append(Colorize(SeparatorLine, ColorDim));
            _sb.Append('\n');
            lineCount++;
        }

        private void AppendLine(string line, ref int lineCount)
        {
            if (lineCount >= maxLines) return;
            _sb.Append(line);
            _sb.Append('\n');
            lineCount++;
        }

        private static string Colorize(string text, string hex)
        {
            if (string.IsNullOrEmpty(text)) text = "";
            if (string.IsNullOrEmpty(hex)) return text;
            return "<color=" + hex + ">" + text + "</color>";
        }

        private ItemDataList_SO ResolveItemDataList()
        {
            if (itemDataList != null) return itemDataList;
            if (InventorySaveLoadService.Instance != null && InventorySaveLoadService.Instance.itemDataList_SO != null)
                return InventorySaveLoadService.Instance.itemDataList_SO;
            if (InventoryManager.Instance != null && InventoryManager.Instance.itemDataList_SO != null)
                return InventoryManager.Instance.itemDataList_SO;
            return null;
        }
    }
}
