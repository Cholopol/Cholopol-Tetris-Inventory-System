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
using Cholopol.TIS.MVVM.ViewModels;
using Loxodon.Framework.Binding;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Cholopol.TIS.MVVM.Views
{
    public class TetrisSlotView : TetrisItemContainerView<TetrisSlotVM>
    {
        [SerializeField] private UnityEvent<TetrisSlotView> OnPointerEnterEvent = new UnityEvent<TetrisSlotView>();
        [SerializeField] private UnityEvent<TetrisSlotView> OnPointerExitEvent = new UnityEvent<TetrisSlotView>();

        [SerializeField] private InventorySlotType inventorySlotType;
        [SerializeField] private Image activeUIImage;

        public Transform GridPanelParent { get; private set; }

        private TetrisSlotVM _viewModel;
        public override TetrisSlotVM ViewModel
        {
            get { return _viewModel; }
            set
            {
                if (_viewModel == value)
                    return;
                if (_viewModel != null)
                {
                    _viewModel.PlaceItemViewRequested -= OnPlaceItemViewRequested;
                    _viewModel.RemoveItemViewRequested -= OnRemoveItemViewRequested;
                }
                _viewModel = value;
                if (_viewModel != null)
                {
                    this.SetDataContext(_viewModel);
                    _viewModel.SlotType = inventorySlotType;
                    Bind(_viewModel);
                    _viewModel.PlaceItemViewRequested += OnPlaceItemViewRequested;
                    _viewModel.RemoveItemViewRequested += OnRemoveItemViewRequested;
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();
            var p = transform.parent != null ? transform.parent.Find("GridPanel") : null;
            GridPanelParent = p;
            if (ViewModel == null)
            {
                ViewModel = new TetrisSlotVM();
            }
        }

        protected override void OnDestroy()
        {
            if (_viewModel != null)
            {
                _viewModel.PlaceItemViewRequested -= OnPlaceItemViewRequested;
                _viewModel.RemoveItemViewRequested -= OnRemoveItemViewRequested;
            }
            base.OnDestroy();
        }

        protected virtual void Bind(TetrisSlotVM viewModel)
        {
            viewModel.SlotSize = RectTransform.sizeDelta;
            var bindingSet = this.CreateBindingSet(viewModel);
            bindingSet.Bind(RectTransform).For(v => v.sizeDelta).To(vm => vm.SlotSize).OneWayToSource();
            bindingSet.Build();
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            if (ViewModel != null)
                TetrisItemMediator.Instance.SyncGhostTargetDropedSlot(ViewModel);
            OnPointerEnterEvent.Invoke(this);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            OnPointerExitEvent.Invoke(this);
        }

        private void OnPlaceItemViewRequested(TetrisItemVM vm)
        {
            if (vm == null) return;
            TetrisItemView targetView = null;

            if (TetrisItemFactory.TryGetViews(vm, out var views) && views != null)
            {
                for (int i = 0; i < views.Count; i++)
                {
                    var v = views[i];
                    if (v != null && v.transform.parent == this.RectTransform)
                    {
                        targetView = v;
                        break;
                    }
                }
            }

            if (targetView == null)
            {
                targetView = TetrisItemFactory.CreateViewByGuid(vm.Guid);
            }

            if (targetView == null) return;

            var rt = targetView.RectTransform;
            if (rt.parent != this.RectTransform)
                rt.SetParent(this.RectTransform, false);
            rt.localScale = Vector3.one;

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            rt.localRotation = Quaternion.Euler(0, 0, 0);
            rt.sizeDelta = this.RectTransform.sizeDelta;

            if (targetView.itemImage != null && targetView.itemImage.transform != targetView.transform)
            {
                var imgRt = targetView.itemImage.rectTransform;
                targetView.itemImage.preserveAspect = true;
                imgRt.localScale = Vector3.one;
                imgRt.localRotation = Quaternion.identity;
                imgRt.anchorMin = Vector2.zero;
                imgRt.anchorMax = Vector2.one;
                imgRt.pivot = new Vector2(0.5f, 0.5f);
                imgRt.anchoredPosition = Vector2.zero;
                imgRt.sizeDelta = Vector2.zero;
            }
            else if (targetView.itemImage != null)
            {
                targetView.itemImage.preserveAspect = true;
                var imgRt = targetView.itemImage.rectTransform;
                imgRt.localScale = Vector3.one;
                imgRt.localRotation = Quaternion.identity;
                imgRt.anchorMin = Vector2.zero;
                imgRt.anchorMax = Vector2.one;
                imgRt.pivot = new Vector2(0.5f, 0.5f);
                imgRt.anchoredPosition = Vector2.zero;
                imgRt.sizeDelta = Vector2.zero;
            }

            targetView.InitializeGridPanel();
            if (GridPanelParent != null)
                targetView.SetGridPanelParent(GridPanelParent);

            if (activeUIImage != null) activeUIImage.enabled = false;
        }

        private void OnRemoveItemViewRequested(TetrisItemVM vm)
        {
            if (activeUIImage != null) activeUIImage.enabled = true;

            if (vm == null) return;

            bool removedAny = false;

            if (TetrisItemFactory.TryGetViews(vm, out var views) && views != null)
            {
                for (int i = views.Count - 1; i >= 0; i--)
                {
                    var v = views[i];
                    if (v == null) continue;

                    if (v.transform.parent == this.RectTransform)
                    {
                        TetrisItemFactory.ReleaseView(v);
                        removedAny = true;
                        continue;
                    }

                    if (GridPanelParent != null &&
                        v.EquipmentTypeGridsPanel != null &&
                        v.EquipmentTypeGridsPanel.parent == GridPanelParent)
                    {
                        v.DestroyGridPanel();
                    }
                }
            }

            if (!removedAny)
            {
                for (int i = this.transform.childCount - 1; i >= 0; i--)
                {
                    var child = this.transform.GetChild(i);
                    var itemView = child.GetComponent<TetrisItemView>();
                    if (itemView != null && itemView.ViewModel != null && itemView.ViewModel.Guid == vm.Guid)
                    {
                        TetrisItemFactory.ReleaseView(itemView);
                    }
                }
            }
        }
    }
}
