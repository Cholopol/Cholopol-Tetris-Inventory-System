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
using Cholopol.TIS.MVVM.Views;
using Cholopol.TIS.MVVM.ViewModels;
using Loxodon.Framework.Contexts;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cholopol.TIS.Debug
{
    public class InfiniteItemListWindow : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        [Header("Data")]
        [SerializeField] private ItemDataList_SO itemDataList_SO;

        [Header("UI")]
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform content;
        [SerializeField] private InfiniteItemListRowView rowPrefab;
        [SerializeField] private float scrollSensitivity = 20f;

        [Header("Stats")]
        [SerializeField] private Text statsText;
        [SerializeField] private float statsUpdateInterval = 0.5f;

        [Header("Placement")]
        [SerializeField] private TetrisGridView depository;

        [Header("Pool")]
        [SerializeField] private int poolSize = 0;
        [SerializeField] private int extraPool = 2;

        private readonly List<InfiniteItemListRowView> rows = new();
        private bool built;
        private bool adjusting;
        private float rowHeight;
        private float rowStep;
        private int topDataIndex;
        private int dataCount;
        private bool dragging;
        private Vector2 lastLocalCursor;
        private float nextStatsUpdate;
        private readonly Dictionary<int, (int vm, int view)> perItemCounts = new();

        private void Awake()
        {
            if (viewport == null) viewport = transform as RectTransform;
        }

        private void OnEnable()
        {
            if (!built) Build();
            ResetToTop();
        }

        private void OnDisable()
        {
            dragging = false;
        }

        private void Update()
        {
            if (!built) return;
            if (Time.unscaledTime < nextStatsUpdate) return;
            nextStatsUpdate = Time.unscaledTime + Mathf.Max(0.05f, statsUpdateInterval);
            RefreshStats();
        }

        public void Build()
        {
            if (content == null || rowPrefab == null) return;
            if (itemDataList_SO == null || itemDataList_SO.itemDetailsList == null || itemDataList_SO.itemDetailsList.Count == 0) return;
            dataCount = itemDataList_SO.itemDetailsList.Count;

            rows.Clear();
            if (rowPrefab != null && rowPrefab.transform != null && rowPrefab.transform.IsChildOf(content))
            {
                rowPrefab.gameObject.SetActive(false);
            }
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i);
                if (child == null) continue;
                if (rowPrefab != null && child == rowPrefab.transform) continue;
                Destroy(child.gameObject);
            }

            rowHeight = ResolveRowHeight();
            if (rowHeight <= 0f) rowHeight = 60f;

            var viewportHeight = viewport != null ? viewport.rect.height : 0f;
            var visibleCount = viewportHeight > 0f ? Mathf.CeilToInt(viewportHeight / rowHeight) : 8;
            var requiredPool = Mathf.Max(2, visibleCount + Mathf.Max(0, extraPool));
            if (poolSize <= 0) poolSize = requiredPool;
            poolSize = Mathf.Max(requiredPool, poolSize);

            for (int i = 0; i < poolSize; i++)
            {
                var row = Instantiate(rowPrefab, content);
                row.gameObject.SetActive(true);
                rows.Add(row);
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            Canvas.ForceUpdateCanvases();

            built = true;
            rowStep = ResolveRowStep();
            if (rowStep <= 0f) rowStep = rowHeight;
            ResetToTop();
            RefreshStats();
        }

        public void ResetToTop()
        {
            if (!built) return;
            if (content == null) return;
            if (rows.Count == 0) return;

            topDataIndex = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                var details = itemDataList_SO.itemDetailsList[(topDataIndex + i) % dataCount];
                rows[i].SetData(details, OnAddClicked);
                ApplyCountsToRow(rows[i]);
            }

            content.anchoredPosition = Vector2.zero;
            Canvas.ForceUpdateCanvases();
        }

        private void HandleLooping()
        {
            if (!built) return;
            if (adjusting) return;
            if (content == null) return;
            if (rows.Count == 0) return;
            if (rowStep <= 0f) return;
            if (dataCount <= 0) return;

            adjusting = true;
            var pos = content.anchoredPosition;
            var y = pos.y;

            var shifted = false;
            while (y >= rowStep)
            {
                ShiftDown();
                y -= rowStep;
                shifted = true;
            }

            while (y < 0f)
            {
                ShiftUp();
                y += rowStep;
                shifted = true;
            }

            if (shifted)
            {
                pos.y = y;
                content.anchoredPosition = pos;
                Canvas.ForceUpdateCanvases();
            }
            adjusting = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!built) return;
            if (viewport == null) return;

            dragging = RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, eventData.position, eventData.pressEventCamera, out lastLocalCursor);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!built) return;
            if (!dragging) return;
            if (viewport == null) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, eventData.position, eventData.pressEventCamera, out var localCursor))
                return;

            var delta = localCursor - lastLocalCursor;
            lastLocalCursor = localCursor;

            var pos = content.anchoredPosition;
            pos.y += delta.y;
            content.anchoredPosition = pos;
            HandleLooping();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            dragging = false;
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (!built) return;
            if (content == null) return;

            var pos = content.anchoredPosition;
            pos.y += -eventData.scrollDelta.y * scrollSensitivity;
            content.anchoredPosition = pos;
            HandleLooping();
        }

        private float ResolveRowHeight()
        {
            if (rowPrefab == null) return 0f;
            var le = rowPrefab.GetComponent<LayoutElement>();
            if (le != null && le.preferredHeight > 0f) return le.preferredHeight;
            var rt = rowPrefab.transform as RectTransform;
            if (rt == null) return 0f;
            var h = rt.rect.height;
            if (h > 0f) return h;
            return 0f;
        }

        private float ResolveRowStep()
        {
            if (rows.Count >= 2)
            {
                var a = rows[0].transform as RectTransform;
                var b = rows[1].transform as RectTransform;
                if (a != null && b != null)
                {
                    var d = Mathf.Abs(a.anchoredPosition.y - b.anchoredPosition.y);
                    if (d > 0f) return d;
                }
            }

            var layout = content != null ? content.GetComponent<VerticalLayoutGroup>() : null;
            if (layout != null)
            {
                var d = rowHeight + Mathf.Max(0f, layout.spacing);
                if (d > 0f) return d;
            }

            return rowHeight;
        }

        private void ShiftDown()
        {
            if (rows.Count == 0) return;
            var first = rows[0];
            rows.RemoveAt(0);
            rows.Add(first);
            first.transform.SetAsLastSibling();

            topDataIndex = (topDataIndex + 1) % dataCount;
            var dataIndex = (topDataIndex + rows.Count - 1) % dataCount;
            first.SetData(itemDataList_SO.itemDetailsList[dataIndex], OnAddClicked);
            ApplyCountsToRow(first);
        }

        private void ShiftUp()
        {
            if (rows.Count == 0) return;
            var lastIndex = rows.Count - 1;
            var last = rows[lastIndex];
            rows.RemoveAt(lastIndex);
            rows.Insert(0, last);
            last.transform.SetAsFirstSibling();

            topDataIndex = (topDataIndex - 1 + dataCount) % dataCount;
            last.SetData(itemDataList_SO.itemDetailsList[topDataIndex], OnAddClicked);
            ApplyCountsToRow(last);
        }

        private void OnAddClicked(ItemDetails details)
        {
            PlaceItem(details);
            RefreshStats();
        }

        private void RefreshStats()
        {
            perItemCounts.Clear();

            foreach (var kv in TetrisItemFactory.VmRegistry)
            {
                var vm = kv.Value;
                var id = vm != null && vm.ItemDetails != null ? vm.ItemDetails.itemID : int.MinValue;
                if (id == int.MinValue) continue;
                if (!perItemCounts.TryGetValue(id, out var c)) c = (0, 0);
                c.vm++;
                perItemCounts[id] = c;
            }

            var itemViews = FindObjectsOfType<TetrisItemView>(true);
            for (int i = 0; i < itemViews.Length; i++)
            {
                var v = itemViews[i];
                var id = v != null && v.ViewModel != null && v.ViewModel.ItemDetails != null ? v.ViewModel.ItemDetails.itemID : int.MinValue;
                if (id == int.MinValue) continue;
                if (!perItemCounts.TryGetValue(id, out var c)) c = (0, 0);
                c.view++;
                perItemCounts[id] = c;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                ApplyCountsToRow(rows[i]);
            }

            if (statsText != null)
            {
                var itemVmCount = TetrisItemFactory.VmRegistry.Count;
                var gridVmCount = TetrisGridFactory.VmRegistry.Count;
                var slotViews = FindObjectsOfType<TetrisSlotView>(true);
                var slotVmCount = 0;
                for (int i = 0; i < slotViews.Length; i++)
                {
                    var v = slotViews[i];
                    if (v != null && v.ViewModel != null) slotVmCount++;
                }

                var totalVm = itemVmCount + gridVmCount + slotVmCount;
                var sb = new StringBuilder();
                sb.AppendLine("ViewModel Total: " + totalVm);
                sb.AppendLine("ItemVM: " + itemVmCount);
                sb.AppendLine("GridVM: " + gridVmCount);
                sb.AppendLine("SlotVM: " + slotVmCount);
                statsText.text = sb.ToString();
            }
        }

        private void ApplyCountsToRow(InfiniteItemListRowView row)
        {
            if (row == null) return;
            var id = row.CurrentItemId;
            if (id < 0)
            {
                row.SetCounts(0, 0);
                return;
            }
            if (perItemCounts.TryGetValue(id, out var c))
            {
                row.SetCounts(c.vm, c.view);
            }
            else
            {
                row.SetCounts(0, 0);
            }
        }

        private void EnsureDepositoryVM()
        {
            if (depository == null) return;
            if (depository.ViewModel != null) return;

            var guidComp = depository.gameObject.GetComponent<DataGUID>();
            if (guidComp == null) guidComp = depository.gameObject.AddComponent<DataGUID>();
            if (string.IsNullOrEmpty(guidComp.guid)) guidComp.guid = System.Guid.NewGuid().ToString();

            var size = depository.RectTransform != null ? depository.RectTransform.sizeDelta : new Vector2(0, 0);
            int defaultW = 8;
            int defaultH = 6;
            int w = Mathf.Max(1, Mathf.RoundToInt(size.x / 20f));
            int h = Mathf.Max(1, Mathf.RoundToInt(size.y / 20f));
            if (w <= 0) w = defaultW;
            if (h <= 0) h = defaultH;
            var vm = new TetrisGridVM(w, h);
            depository.ViewModel = vm;
        }

        private void PlaceItem(ItemDetails details)
        {
            if (details == null || depository == null) return;

            EnsureDepositoryVM();
            var gridVm = depository.ViewModel;
            if (gridVm == null) return;

            var vm = TetrisItemFactory.GetOrCreateVM(details, null, gridVm);
            if (vm == null) return;
            bool placed = false;

            for (int row = 0; row < gridVm.GridSizeHeight && !placed; row++)
            {
                for (int column = 0; column < gridVm.GridSizeWidth && !placed; column++)
                {
                    if (!gridVm.IsAreaVacantForItem(vm, column, row)) continue;

                    var service = Context.GetApplicationContext().GetService<IInventoryService>();
                    if (service != null)
                    {
                        placed = service.PlaceOnGrid(vm, gridVm, new Vector2Int(column, row), null);
                    }
                    else
                    {
                        placed = gridVm.TryPlaceTetrisItem(vm, column, row);
                    }
                }
            }

            if (!placed)
            {
                if (!string.IsNullOrEmpty(vm.Guid))
                {
                    vm.Dispose();
                    TetrisItemFactory.UnregisterVM(vm.Guid, true);
                }
            }
        }
    }
}

