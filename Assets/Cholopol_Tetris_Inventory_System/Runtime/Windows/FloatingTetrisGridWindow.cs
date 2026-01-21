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
using Cholopol.TIS.Events;
using Loxodon.Framework.Views;
using Loxodon.Framework.Views.Animations;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace Cholopol.TIS.Windows
{
    public class FloatingTetrisGridWindow : Window, IFocusableWindow
    {
        public Text ItemName;
        public RectTransform GridPanelContainer;
        public RectTransform HeadBanner;
        [SerializeField] private Button CloseBtn;
        [SerializeField] private Canvas canvas;
        private Vector2 offset;
        private TetrisItemVM _boundVM; // Store VM for language refresh
        private AsyncOperationHandle<GameObject> _instantiateHandle;
        private string _uniqueId;
        
        #region IFocusableWindow Implementation
        
        public Window WindowInstance => this;
        
        public string UniqueId => _uniqueId;
        
        public event System.EventHandler OnFocusableDismissed;
        
        public void SetFocused(bool focused)
        {
            if (CloseBtn != null) CloseBtn.interactable = focused;
        }
        
        #endregion

        protected override void OnCreate(IBundle bundle)
        {
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();

            if (CloseBtn != null)
                CloseBtn.onClick.AddListener(() => { Dismiss(); });
            SetupDragHandler();

            var enter = gameObject.AddComponent<AlphaAnimation>();
            enter.AnimationType = AnimationType.EnterAnimation;
            enter.from = 0f;
            enter.to = 1f;
            enter.duration = 0.15f;
            var exit = gameObject.AddComponent<AlphaAnimation>();
            exit.AnimationType = AnimationType.ExitAnimation;
            exit.from = 1f;
            exit.to = 0f;
            exit.duration = 0.12f;
            var act = gameObject.AddComponent<AlphaAnimation>();
            act.AnimationType = AnimationType.ActivationAnimation;
            act.from = 1f;
            act.to = 1f;
            act.duration = 0.01f;
            var pass = gameObject.AddComponent<AlphaAnimation>();
            pass.AnimationType = AnimationType.PassivationAnimation;
            pass.from = 1f;
            pass.to = 1f;
            pass.duration = 0.01f;

            if (bundle != null && bundle.Data != null)
            {
                TetrisItemVM vm = null;
                if (bundle.Data.ContainsKey("VM"))
                    vm = bundle.Data["VM"] as TetrisItemVM;
                if (vm != null)
                    Initialize(vm);
            }
        }

        /// <summary>
        /// Open a floating window asynchronously (loaded using Addressables).
        /// </summary>
        /// <param name="vm">item ViewModel</param>
        /// <param name="onCreated">Callback after window creation is complete</param>
        public static async void OpenAsync(TetrisItemVM vm, System.Action<FloatingTetrisGridWindow> onCreated = null)
        {
            var config = CTISPrefabConfig.Instance;
            if (config == null || config.floatingGridPanelTemplate == null || !config.floatingGridPanelTemplate.RuntimeKeyIsValid())
            {
                UnityEngine.Debug.LogError("[FloatingTetrisGridWindow] CTISPrefabConfig or floatingGridPanelTemplate is not configured.");
                return;
            }

            WindowContainer windowContainer = FindWindowContainer();
            if (windowContainer == null)
            {
                UnityEngine.Debug.LogError("[FloatingTetrisGridWindow] No WindowContainer found.");
                return;
            }

            var handle = config.floatingGridPanelTemplate.InstantiateAsync(windowContainer.transform);
            var go = await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded || go == null)
            {
                UnityEngine.Debug.LogError("[FloatingTetrisGridWindow] Failed to instantiate from Addressables.");
                return;
            }

            var window = go.GetComponent<FloatingTetrisGridWindow>();
            if (window == null)
            {
                Addressables.ReleaseInstance(go);
                UnityEngine.Debug.LogError("[FloatingTetrisGridWindow] Prefab does not have FloatingTetrisGridWindow component.");
                return;
            }

            window._instantiateHandle = handle;
            window.WindowManager = windowContainer.GetComponent<IWindowManager>() ?? windowContainer.GetComponentInParent<IWindowManager>();

            IBundle bundle = new Bundle();
            bundle.Put("VM", vm);
            window.Create(bundle);

            var rt = window.GetComponent<RectTransform>();
            if (rt != null) rt.position = Settings.centerOfScreen;

            var wm = window.WindowManager as WindowManager;
            if (wm != null)
            {
                var count = wm.VisibleCount;
                var rtt = window.RectTransform;
                rtt.anchoredPosition += new Vector2(24 * count, -24 * count);
            }

            if (FloatingPanelManager.Instance != null)
            {
                FloatingPanelManager.Instance.RegisterGridWindow(window);
            }

            // Calling callbacks allows the caller to register events.
            onCreated?.Invoke(window);

            var transition = window.Show().Overlay((prev, curr) => ActionType.None);
        }

        private static WindowContainer FindWindowContainer()
        {
            var containers = FindObjectsOfType<WindowContainer>();
            for (int i = 0; i < containers.Length; i++)
            {
                var c = containers[i];
                if (c != null && c.name == "FLOATING")
                {
                    return c;
                }
            }
            return null;
        }

        public void Initialize(TetrisItemVM vm)
        {
            if (vm == null) return;
            _boundVM = vm;
            _uniqueId = vm.Guid;
            
            if (ItemName != null && vm.ItemDetails != null)
                ItemName.text = vm.ItemDetails.localizedName != null && !vm.ItemDetails.localizedName.IsEmpty ? vm.ItemDetails.localizedName.GetLocalizedString() : string.Empty;
            if (GridPanelContainer == null || vm.ItemDetails == null || vm.ItemDetails.gridUIPrefab == null) return;
            var content = Object.Instantiate(vm.ItemDetails.gridUIPrefab, GridPanelContainer);
            AdjustPanelSize(content.GetComponent<RectTransform>());
            var gridViews = content.GetComponentsInChildren<TetrisGridView>(true);
            if (gridViews != null && gridViews.Length > 0)
            {
                for (int i = 0; i < gridViews.Length; i++)
                {
                    var gv = gridViews[i];
                    var guidComp = gv.gameObject.GetComponent<DataGUID>();
                    if (guidComp == null) guidComp = gv.gameObject.AddComponent<DataGUID>();
                    var gridVM = vm.GetOrCreateGridVM(i);
                    guidComp.guid = gridVM.GridGuid;
                    TetrisGridFactory.BindViewToGuid(gv, gridVM.GridGuid);
                }
            }
            
            EventBus.Instance.Subscribe(EventNames.LanguageChangedEvent, RefreshItemName);
        }

        private void RefreshItemName()
        {
            if (_boundVM?.ItemDetails?.localizedName != null && !_boundVM.ItemDetails.localizedName.IsEmpty)
                ItemName.text = _boundVM.ItemDetails.localizedName.GetLocalizedString();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            OnFocusableDismissed?.Invoke(this, System.EventArgs.Empty);
            
            EventBus.Instance.Unsubscribe(EventNames.LanguageChangedEvent, RefreshItemName);
            
            if (_instantiateHandle.IsValid())
            {
                Addressables.ReleaseInstance(_instantiateHandle);
            }
        }

        private void SetupDragHandler()
        {
            if (HeadBanner == null) return;
            var eventTrigger = HeadBanner.GetComponent<EventTrigger>();
            if (eventTrigger == null) eventTrigger = HeadBanner.gameObject.AddComponent<EventTrigger>();
            eventTrigger.triggers.Clear();

            var pointerDownEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDownEntry.callback.AddListener((data) => { FloatingPanelManager.Instance.FocusFocusableWindow(this); });
            eventTrigger.triggers.Add(pointerDownEntry);

            var beginDragEntry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            beginDragEntry.callback.AddListener((data) => { OnBeginDrag((PointerEventData)data); });
            eventTrigger.triggers.Add(beginDragEntry);

            var dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            dragEntry.callback.AddListener((data) => { OnDrag((PointerEventData)data); });
            eventTrigger.triggers.Add(dragEntry);
            
            SetupContentClickHandler();
        }
        
        private void SetupContentClickHandler()
        {
            if (GridPanelContainer == null) return;
            var contentTrigger = GridPanelContainer.GetComponent<EventTrigger>();
            if (contentTrigger == null) contentTrigger = GridPanelContainer.gameObject.AddComponent<EventTrigger>();
            
            var pointerDownEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDownEntry.callback.AddListener((data) => { FloatingPanelManager.Instance.FocusFocusableWindow(this); });
            contentTrigger.triggers.Add(pointerDownEntry);
        }

        private void OnBeginDrag(PointerEventData eventData)
        {
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvas.worldCamera,
                out Vector2 localPoint);
            offset = RectTransform.anchoredPosition - localPoint;
        }

        private void OnDrag(PointerEventData eventData)
        {
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvas.worldCamera,
                out Vector2 localPoint))
            {
                RectTransform.anchoredPosition = localPoint + offset;
            }
            ClampToCanvas();
        }

        private void AdjustPanelSize(RectTransform gridUI)
        {
            RectTransform containerRect = GridPanelContainer;
            RectTransform bannerRect = HeadBanner;

            if (containerRect != null && bannerRect != null && gridUI != null)
            {
                containerRect.sizeDelta = gridUI.sizeDelta;
                bannerRect.sizeDelta = new Vector2(containerRect.rect.width, 15);
                
                if (ItemName != null)
                {
                    var rt = ItemName.rectTransform;
                    var size = rt.sizeDelta;
                    size.x = bannerRect.rect.width;
                    rt.sizeDelta = size;
                }
            }
        }

        private void ClampToCanvas()
        {
            Vector3 pos = RectTransform.localPosition;
            var cRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
            if (cRect == null) return;
            Vector2 canvasSize = cRect.rect.size;
            Vector2 panelSize = RectTransform.rect.size;
            pos.x = Mathf.Clamp(pos.x, -canvasSize.x / 2 + panelSize.x / 2, canvasSize.x / 2 - panelSize.x / 2);
            pos.y = Mathf.Clamp(pos.y, -canvasSize.y / 2 + panelSize.y / 2, canvasSize.y / 2 - panelSize.y / 2);
            RectTransform.localPosition = pos;
        }
    }
}
