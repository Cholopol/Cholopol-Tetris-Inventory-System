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
using Cholopol.TIS;
using Cholopol.TIS.MVVM.ViewModels;
using Cholopol.TIS.MVVM.Views;
using Loxodon.Framework.Views;
using Loxodon.Framework.Contexts;
using Loxodon.Framework.Views.Animations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cholopol.TIS.Windows
{
    public class FloatingTetrisGridWindow : Window
    {
        public Text ItemName;
        public RectTransform GridPanelContainer;
        public RectTransform HeadBanner;
        [SerializeField] private Button CloseBtn;
        [SerializeField] private Canvas canvas;
        private Vector2 offset;
        private const string ResourcePath = "Prefabs/Windows/FloatingGridPanelTemplate";

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

        public static FloatingTetrisGridWindow Open(TetrisItemVM vm)
        {
            var applicationContext = Context.GetApplicationContext();
            var locator = applicationContext.GetService<IUIViewLocator>();

            WindowContainer windowContainer = null;
            var containers = Object.FindObjectsOfType<WindowContainer>();
            for (int i = 0; i < containers.Length; i++)
            {
                var c = containers[i];
                if (c != null && c.name == "FLOATING")
                {
                    windowContainer = c;
                    break;
                }
            }

            if (windowContainer == null)
            {
                for (int i = 0; i < containers.Length; i++)
                {
                    var c = containers[i];
                    if (c != null && c.name == "MAIN")
                    {
                        windowContainer = c;
                        break;
                    }
                }
            }

            IBundle bundle = new Bundle();
            bundle.Put("VM", vm);

            var window = locator.LoadWindow<FloatingTetrisGridWindow>(windowContainer, ResourcePath);
            if (window == null)
            {
                return null;
            }

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
            var transition = window.Show().Overlay((prev, curr) => ActionType.None);
            if (wm != null)
            {
                transition.OnFinish(() =>
                {
                    var visibles = wm.Find(true);
                    for (int i = 0; i < visibles.Count; i++)
                    {
                        var w = visibles[i] as Window;
                        if (w != null) w.Activate(true);
                    }
                });
            }
            return window;
        }

        public void Initialize(TetrisItemVM vm)
        {
            if (vm == null) return;
            if (ItemName != null && vm.ItemDetails != null)
                ItemName.text = vm.ItemDetails.itemName;
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
        }

        private void SetupDragHandler()
        {
            if (HeadBanner == null) return;
            var eventTrigger = HeadBanner.GetComponent<EventTrigger>();
            if (eventTrigger == null) eventTrigger = HeadBanner.gameObject.AddComponent<EventTrigger>();
            eventTrigger.triggers.Clear();

            var beginDragEntry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            beginDragEntry.callback.AddListener((data) => { OnBeginDrag((PointerEventData)data); });
            eventTrigger.triggers.Add(beginDragEntry);

            var dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            dragEntry.callback.AddListener((data) => { OnDrag((PointerEventData)data); });
            eventTrigger.triggers.Add(dragEntry);
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
