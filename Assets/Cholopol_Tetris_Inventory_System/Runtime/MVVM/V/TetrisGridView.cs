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

namespace Cholopol.TIS.MVVM.Views
{
    public class TetrisGridView : TetrisItemContainerView<TetrisGridVM>
    {
        [SerializeField] private int _gridSizeWidth = 1;
        [SerializeField] private int _gridSizeHeight = 1;
        [SerializeField] private float _localGridUnitSizeWidth = 20f;
        [SerializeField] private float _localGridUnitSizeHeight = 20f;
        [SerializeField] private UnityEvent<TetrisGridView> OnPointerEnterEvent = new UnityEvent<TetrisGridView>();
        [SerializeField] private UnityEvent<TetrisGridView> OnPointerExitEvent = new UnityEvent<TetrisGridView>();

        private TetrisGridVM _viewModel;
        public override TetrisGridVM ViewModel
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
                    Bind(_viewModel);
                    _viewModel.PlaceItemViewRequested += OnPlaceItemViewRequested;
                    _viewModel.RemoveItemViewRequested += OnRemoveItemViewRequested;
                }
            }
        }

        protected void Bind(TetrisGridVM viewModel)
        {
            var unitW = _localGridUnitSizeWidth > 0f ? _localGridUnitSizeWidth : Settings.gridTileSizeWidth;
            var unitH = _localGridUnitSizeHeight > 0f ? _localGridUnitSizeHeight : Settings.gridTileSizeHeight;
            var gw = _gridSizeWidth > 0 ? _gridSizeWidth : 1;
            var gh = _gridSizeHeight > 0 ? _gridSizeHeight : 1;
            viewModel.ApplyConfig(gw, gh, unitW, unitH);
            var bindingSet = this.CreateBindingSet(viewModel);
            bindingSet.Bind(RectTransform).For(v => v.sizeDelta).To(vm => vm.Size).OneWay();
            bindingSet.Build();
            viewModel.GridPIndex = transform.GetSiblingIndex();
            var guidComp = this.gameObject.GetComponent<DataGUID>();
            if (guidComp != null && !string.IsNullOrEmpty(guidComp.guid))
                viewModel.GridGuid = guidComp.guid;

            if (viewModel.OwnerItemsDic != null)
            {
                foreach (var itemVM in viewModel.OwnerItemsDic.Values)
                {
                    if (itemVM != null)
                    {
                        if (itemVM.CurrentTetrisContainer == null)
                            itemVM.CurrentTetrisContainer = viewModel;
                        OnPlaceItemViewRequested(itemVM, itemVM.LocalGridCoordinate.x, itemVM.LocalGridCoordinate.y);
                    }
                }
            }
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            if (ViewModel != null)
                TetrisItemMediator.Instance.SyncGhostTargetDropedGrid(ViewModel);
            OnPointerEnterEvent.Invoke(this);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            OnPointerExitEvent.Invoke(this);
        }

        private void OnPlaceItemViewRequested(TetrisItemVM vm, int posX, int posY)
        {
            if (vm == null) return;

            TetrisItemView targetView = null;

            if (TetrisItemFactory.TryGetViews(vm, out var views) && views != null)
            {
                foreach (var v in views)
                {
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

            if (targetView != null)
            {
                var rt = targetView.RectTransform;
                if (rt.parent != this.RectTransform)
                    rt.SetParent(this.RectTransform, false);
                rt.localScale = Vector3.one;

                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0.5f, 0.5f);

                var pos = ViewModel.CalculatePositionOnGrid(vm, posX, posY);
                rt.anchoredPosition = pos;
                rt.sizeDelta = new Vector2(vm.Width * ViewModel.LocalGridTileSizeWidth, vm.Height * ViewModel.LocalGridTileSizeHeight);

                rt.localRotation = Quaternion.identity;

                if (targetView.itemImage != null && targetView.itemImage.gameObject != targetView.gameObject)
                {
                    var imgRt = targetView.itemImage.rectTransform;
                    imgRt.localScale = Vector3.one;
                    var angle = TetrisUtilities.RotationHelper.GetRotationAngle(vm.Direction);
                    imgRt.localRotation = Quaternion.Euler(0, 0, -angle);

                    float originalW = vm.ItemDetails.xWidth * ViewModel.LocalGridTileSizeWidth;
                    float originalH = vm.ItemDetails.yHeight * ViewModel.LocalGridTileSizeHeight;
                    imgRt.sizeDelta = new Vector2(originalW, originalH);

                    imgRt.anchorMin = new Vector2(0.5f, 0.5f);
                    imgRt.anchorMax = new Vector2(0.5f, 0.5f);
                    imgRt.pivot = new Vector2(0.5f, 0.5f);
                    imgRt.anchoredPosition = Vector2.zero;
                }
                else
                {
                    var angle = TetrisUtilities.RotationHelper.GetRotationAngle(vm.Direction);
                    rt.localRotation = Quaternion.Euler(0, 0, -angle);
                }

                vm.CurrentTetrisContainer = ViewModel;
                vm.SetItemData();
            }
        }

        private void OnRemoveItemViewRequested(TetrisItemVM vm)
        {
            if (vm == null) return;
            bool found = false;

            if (TetrisItemFactory.TryGetViews(vm, out var views) && views != null)
            {
                for (int i = views.Count - 1; i >= 0; i--)
                {
                    var v = views[i];
                    if (v != null && v.transform.parent == this.RectTransform)
                    {
                        TetrisItemFactory.ReleaseView(v);
                        found = true;
                    }
                }
            }

            if (!found)
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

        protected override void OnDestroy()
        {
            if (_viewModel != null)
            {
                _viewModel.PlaceItemViewRequested -= OnPlaceItemViewRequested;
                _viewModel.RemoveItemViewRequested -= OnRemoveItemViewRequested;
            }
            base.OnDestroy();
        }
    }
}
