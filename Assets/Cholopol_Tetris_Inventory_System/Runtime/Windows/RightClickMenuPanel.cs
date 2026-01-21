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
using Cholopol.TIS.MVVM.Views;
using Loxodon.Framework.Binding;
using Loxodon.Framework.Interactivity;
using Loxodon.Framework.Views;
using UnityEngine;
using UnityEngine.UI;
using Cholopol.TIS.Windows;

namespace Cholopol.TIS
{
    public class RightClickMenuPanel : UIView
    {
        [SerializeField] private ItemInformationPanel ItemInformationPanel;
        [SerializeField] private Button CheckBtn, SplitBtn, UseBtn, OpenBtn;
        [SerializeField] private float distanceThreshold = 150f;

        private Canvas _canvas;
        private RightClickMenuVM _viewModel;
        public RightClickMenuVM ViewModel
        {
            get { return _viewModel; }
            set
            {
                if (_viewModel == value)
                    return;
                _viewModel = value;
                if (_viewModel != null)
                {
                    this.SetDataContext(_viewModel);
                    Bind(_viewModel);
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();
            ViewModel = new RightClickMenuVM();
            _canvas = GetComponentInParent<Canvas>();
        }

        private void Update()
        {
            float currentThreshold = distanceThreshold * (_canvas != null ? _canvas.scaleFactor : 1f);
            if (Vector2.Distance(Input.mousePosition, transform.position) > currentThreshold)
            {
                Show(false);
            }
        }

        protected virtual void Bind(RightClickMenuVM vm)
        {
            var bindingSet = this.CreateBindingSet(vm);
            bindingSet.Bind(CheckBtn).For(v => v.onClick).To(m => m.CheckCommand);
            bindingSet.Bind(SplitBtn).For(v => v.onClick).To(m => m.SplitCommand);
            bindingSet.Bind(UseBtn).For(v => v.onClick).To(m => m.UseCommand);
            bindingSet.Bind(OpenBtn).For(v => v.onClick).To(m => m.OpenCommand);
            bindingSet.Bind().For(v => v.OnShowInfo).To(m => m.ShowInfoRequest);
            bindingSet.Bind().For(v => v.OnOpenPanel).To(m => m.OpenPanelRequest);
            bindingSet.Bind().For(v => v.OnClose).To(m => m.CloseRequest);
            bindingSet.Build();
        }

        public void OnShowInfo(object sender, InteractionEventArgs args)
        {
            var details = args.Context as ItemDetails;
            if (ItemInformationPanel == null || details == null) return;
            ItemInformationPanel.OpenAsync(details);
        }

        public void OnOpenPanel(object sender, InteractionEventArgs args)
        {
            var vm = args.Context as TetrisItemVM;
            if (vm == null) return;
            FloatingTetrisGridWindow.OpenAsync(vm);
        }

        public void OnClose(object sender, InteractionEventArgs args)
        {
            Show(false);
        }

        public void Show(bool isClick)
        {
            gameObject.SetActive(isClick);
            if (isClick)
            {
                RectTransform.SetAsLastSibling();
            }
        }

        public void SetContext(TetrisItemView itemView)
        {
            if (ViewModel != null) ViewModel.CurrentItem = itemView != null ? itemView.ViewModel : null;
            SetBtnActive();
        }

        public void SetBtnActive()
        {
            var item = ViewModel != null ? ViewModel.CurrentItem : null;
            if (item == null)
            {
                CheckBtn.gameObject.SetActive(false);
                SplitBtn.gameObject.SetActive(false);
                OpenBtn.gameObject.SetActive(false);
                return;
            }
            bool hasFloating = item.ItemDetails != null && item.ItemDetails.gridUIPrefab != null;
        bool isOpen = FloatingPanelManager.Instance != null && FloatingPanelManager.Instance.IsGridWindowOpen(item);
            CheckBtn.gameObject.SetActive(true);
            SplitBtn.gameObject.SetActive(item.IsStackable);
            OpenBtn.gameObject.SetActive(hasFloating && !isOpen);
        }
    }
}
