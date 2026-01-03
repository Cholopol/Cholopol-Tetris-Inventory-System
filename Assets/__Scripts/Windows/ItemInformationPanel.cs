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
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Loxodon.Framework.Views;
using Loxodon.Framework.Binding;
using Loxodon.Framework.ViewModels;
using Loxodon.Framework.Commands;
using Loxodon.Framework.Contexts;
using Loxodon.Framework.Views.Animations;

namespace Cholopol.TIS
{
    public class ItemInformationPanel : Window, IBeginDragHandler, IDragHandler
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
        private const string ResourcePath = "Prefabs/Windows/ItemInformation";
        public ItemInformationVM ViewModel { get; private set; }

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
        public static ItemInformationPanel Open(Sprite sprite, string name, string description)
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
            bundle.Put("Sprite", sprite);
            bundle.Put("Name", name);
            bundle.Put("Description", description);

            var window = locator.LoadWindow<ItemInformationPanel>(windowContainer, ResourcePath);
            if (window == null)
                return null;

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
