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
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Loxodon.Framework.Views;
using Loxodon.Framework.Binding;
using Loxodon.Framework.ViewModels;
using Loxodon.Framework.Commands;
using Loxodon.Framework.Views.Animations;
using Cholopol.TIS.Events;
using Cholopol.TIS.Windows;

namespace Cholopol.TIS
{
    public class ItemInformationPanel : Window, IBeginDragHandler, IDragHandler, IPointerDownHandler, IFocusableWindow
    {
        public Image itemImage;
        public Sprite itemDefaultSprite;
        public Text ItemName;
        public Text ItemDescription;
        [SerializeField] private Button CloseBtn;
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private RectTransform itemImageParentRect;
        [SerializeField] private RectTransform itemImageRect;
        [SerializeField] private Canvas canvas;
        private Vector2 offset;
        public ItemInformationVM ViewModel { get; private set; }
        private ItemDetails _boundItemDetails;
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

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnCreate(IBundle bundle)
        {
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();

            ViewModel = new ItemInformationVM();
            ViewModel.Init(() => Dismiss());
            this.SetDataContext(ViewModel);

            var bindingSet = this.CreateBindingSet(ViewModel);
            bindingSet.Bind(itemImage).For(v => v.sprite).To(vm => vm.Sprite).OneWay();
            bindingSet.Bind(ItemName).For(v => v.text).To(vm => vm.Name).OneWay();
            bindingSet.Bind(ItemDescription).For(v => v.text).To(vm => vm.Description).OneWay();
            if (CloseBtn != null)
                bindingSet.Bind(CloseBtn).For(v => v.onClick).To(vm => vm.Close).OneWay();
            bindingSet.Build();

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
                Sprite sprite = null;
                string name = null;
                string desc = null;
                if (bundle.Data.ContainsKey("Sprite"))
                    sprite = bundle.Data["Sprite"] as Sprite;
                if (bundle.Data.ContainsKey("Name"))
                    name = bundle.Data["Name"] as string;
                if (bundle.Data.ContainsKey("Description"))
                    desc = bundle.Data["Description"] as string;

                SetContent(sprite, name, desc);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvas.worldCamera,
                out Vector2 localPoint);
            offset = panelRect.anchoredPosition - localPoint;
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            if (FloatingPanelManager.Instance != null)
            {
                FloatingPanelManager.Instance.FocusFocusableWindow(this);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvas.worldCamera,
                out Vector2 localPoint))
            {
                panelRect.anchoredPosition = localPoint + offset;
            }
            ClampToCanvas();
        }

        public void SetVisible(bool isShow)
        {
            if (isShow)
            {
                base.Show(true);
            }
            else
            {
                Hide(true);
            }
        }

        public void SetItemImage(Sprite newSprite)
        {
            itemImage.sprite = newSprite;
            itemImage.SetNativeSize();

            // Force layout update if parent size is invalid
            if (itemImageParentRect.rect.width == 0 || itemImageParentRect.rect.height == 0)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
            }

            Vector2 maxSize = new Vector2(
                itemImageParentRect.rect.width,
                itemImageParentRect.rect.height
            );

            if (maxSize.x > 0 && maxSize.y > 0)
            {
                if (itemImageRect.sizeDelta.x > maxSize.x || itemImageRect.sizeDelta.y > maxSize.y)
                {
                    float widthRatio = maxSize.x / itemImageRect.sizeDelta.x;
                    float heightRatio = maxSize.y / itemImageRect.sizeDelta.y;
                    float scaleFactor = Mathf.Min(widthRatio, heightRatio);

                    itemImageRect.sizeDelta *= scaleFactor;
                }
            }
        }
        public void SetContent(Sprite sprite, string name, string description)
        {
            var s = sprite == null ? itemDefaultSprite : sprite;
            if (ViewModel != null)
            {
                ViewModel.Sprite = s;
                ViewModel.Name = name;
                ViewModel.Description = description;
            }
            SetItemImage(s);
        }

        public static async void OpenAsync(Sprite sprite, string name, string description)
        {
            await OpenAsyncInternal(sprite, name, description, null);
        }

        public static async void OpenAsync(ItemDetails details)
        {
            if (details == null) return;
            var name = details.localizedName != null && !details.localizedName.IsEmpty 
                ? details.localizedName.GetLocalizedString() : string.Empty;
            var desc = details.localizedDescription != null && !details.localizedDescription.IsEmpty 
                ? details.localizedDescription.GetLocalizedString() : string.Empty;
            await OpenAsyncInternal(details.itemIcon, name, desc, details);
        }

        private static async System.Threading.Tasks.Task OpenAsyncInternal(Sprite sprite, string name, string description, ItemDetails details)
        {
            var config = CTISPrefabConfig.Instance;
            if (config == null || config.itemInformationPanel == null || !config.itemInformationPanel.RuntimeKeyIsValid())
            {
                UnityEngine.Debug.LogError("[ItemInformationPanel] CTISPrefabConfig or itemInformationPanel is not configured.");
                return;
            }

            WindowContainer windowContainer = FindWindowContainer();
            if (windowContainer == null)
            {
                UnityEngine.Debug.LogError("[ItemInformationPanel] No WindowContainer found.");
                return;
            }

            var handle = config.itemInformationPanel.InstantiateAsync(windowContainer.transform);
            var go = await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded || go == null)
            {
                UnityEngine.Debug.LogError("[ItemInformationPanel] Failed to instantiate from Addressables.");
                return;
            }

            var window = go.GetComponent<ItemInformationPanel>();
            if (window == null)
            {
                Addressables.ReleaseInstance(go);
                UnityEngine.Debug.LogError("[ItemInformationPanel] Prefab does not have ItemInformationPanel component.");
                return;
            }

            window._instantiateHandle = handle;
            window._uniqueId = System.Guid.NewGuid().ToString();
            window.WindowManager = windowContainer.GetComponent<IWindowManager>() ?? windowContainer.GetComponentInParent<IWindowManager>();

            IBundle bundle = new Bundle();
            bundle.Put("Sprite", sprite);
            bundle.Put("Name", name);
            bundle.Put("Description", description);
            bundle.Put("ItemDetails", details);
            window.Create(bundle);
            window._boundItemDetails = details;

            if (details != null)
                EventBus.Instance.Subscribe(EventNames.LanguageChangedEvent, window.RefreshLocalization);

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
            
            if (FloatingPanelManager.Instance != null)
            {
                FloatingPanelManager.Instance.RegisterInfoPanel(window);
            }
        }

        private static WindowContainer FindWindowContainer()
        {
            var containers = Object.FindObjectsOfType<WindowContainer>();
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

        private void RefreshLocalization()
        {
            if (_boundItemDetails == null || ViewModel == null) return;
            var name = _boundItemDetails.localizedName;
            var desc = _boundItemDetails.localizedDescription;
            ViewModel.Name = name != null && !name.IsEmpty ? name.GetLocalizedString() : string.Empty;
            ViewModel.Description = desc != null && !desc.IsEmpty ? desc.GetLocalizedString() : string.Empty;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            OnFocusableDismissed?.Invoke(this, System.EventArgs.Empty);
            
            EventBus.Instance.Unsubscribe(EventNames.LanguageChangedEvent, RefreshLocalization);
            
            if (_instantiateHandle.IsValid())
            {
                Addressables.ReleaseInstance(_instantiateHandle);
            }
        }
        private void ClampToCanvas()
        {
            Vector3 pos = panelRect.localPosition;
            Vector2 canvasSize = canvas.GetComponent<RectTransform>().rect.size;
            Vector2 panelSize = panelRect.rect.size;

            pos.x = Mathf.Clamp(pos.x,
                -canvasSize.x / 2 + panelSize.x / 2,
                canvasSize.x / 2 - panelSize.x / 2);
            pos.y = Mathf.Clamp(pos.y,
                -canvasSize.y / 2 + panelSize.y / 2,
                canvasSize.y / 2 - panelSize.y / 2);

            panelRect.localPosition = pos;
        }

        public class ItemInformationVM : ViewModelBase
        {
            private Sprite _sprite;
            public Sprite Sprite { get => _sprite; set => Set(ref _sprite, value); }
            private string _name;
            public string Name { get => _name; set => Set(ref _name, value); }
            private string _description;
            public string Description { get => _description; set => Set(ref _description, value); }
            public SimpleCommand Close { get; private set; }
            public void Init(System.Action closeAction)
            {
                Close = new SimpleCommand(() => closeAction?.Invoke());
            }
        }

    }
}
