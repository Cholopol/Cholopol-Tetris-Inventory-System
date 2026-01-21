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
using Cholopol.TIS.Utility;
using System.Collections.Generic;
using Loxodon.Framework.Binding;
using Loxodon.Framework.Views;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cholopol.TIS.MVVM.Views
{
    public class TetrisItemView : UIView, IPointerEnterHandler
    {
        [SerializeField] private Font _defaultFont;
        public Image itemImage;
        public Image rarityBackground;
        public Text stackNumText;
        public Text itemNameText;
        private RectTransform rarityBackgroundRoot;
        private readonly List<Image> rarityBackgroundTiles = new List<Image>();
        private static GameObject s_rarityTilePrefab;
        public Transform EquipmentTypeGridsPanel { get; private set; }
        public List<TetrisGridView> OwnedTetrisGrids { get; private set; } = new List<TetrisGridView>();
        private Transform _initialGridPanelParent;
        private TetrisItemVM _viewModel;
        public TetrisItemVM ViewModel
        {
            get { return _viewModel; }
            set
            {
                if (_viewModel == value)
                    return;
                if (_viewModel != null)
                    _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel = value;
                if (_viewModel != null)
                {
                    this.SetDataContext(_viewModel);
                    Bind(_viewModel);
                    _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                    if (_viewModel.CurrentStack < 1)
                    {
                        TetrisItemFactory.ReleaseView(this);
                        return;
                    }
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        protected override void OnDestroy()
        {
            ReleaseAllRarityBackgroundTiles();
            if (EquipmentTypeGridsPanel != null)
            {
                Destroy(EquipmentTypeGridsPanel.gameObject);
                EquipmentTypeGridsPanel = null;
            }
            if (_viewModel != null)
                _viewModel.HasActivedGridPanel = false;
            if (_viewModel != null)
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            base.OnDestroy();
        }

        private void Initialize()
        {
            var rootImage = GetComponent<Image>();
            if (rootImage == null)
            {
                rootImage = gameObject.AddComponent<Image>();
            }
            rootImage.color = new Color(0, 0, 0, 0);
            rootImage.raycastTarget = false;

            var contentTrans = transform.Find("Content");
            if (contentTrans == null)
            {
                var contentObj = new GameObject("Content");
                contentObj.transform.SetParent(this.transform, false);
                contentTrans = contentObj.transform;
            }

            var contentImage = contentTrans.GetComponent<Image>();
            if (contentImage == null)
            {
                contentImage = contentTrans.gameObject.AddComponent<Image>();
                contentImage.color = Color.white;
            }

            var contentRect = contentImage.rectTransform;
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.sizeDelta = Vector2.zero;
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.pivot = new Vector2(0.5f, 0.5f);

            contentImage.raycastTarget = true;
            contentImage.preserveAspect = false;
            itemImage = contentImage;

            if (itemImage != null)
            {
                var filter = itemImage.GetComponent<SpriteMeshRaycastFilter>();
                if (filter == null) itemImage.gameObject.AddComponent<SpriteMeshRaycastFilter>();
            }

            var rarityBgTrans = transform.Find("RarityBackground");
            if (rarityBgTrans != null)
            {
                rarityBackgroundRoot = rarityBgTrans as RectTransform;
            }
            else
            {
                var bgObj = new GameObject("RarityBackground");
                bgObj.transform.SetParent(this.transform, false);
                bgObj.transform.SetAsFirstSibling(); 
                
                var bgRect = bgObj.AddComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
                bgRect.offsetMin = new Vector2(0.5f, 0.5f);
                bgRect.offsetMax = new Vector2(-0.5f, -0.5f);

                rarityBackgroundRoot = bgRect;
            }

            EnsureRarityBackgroundRoot();

            stackNumText = ResolveOrCreateText(stackNumText, "StackNum", "1", TextAnchor.LowerRight, Color.white);

            if (stackNumText != null) stackNumText.transform.SetAsLastSibling();

            itemNameText = ResolveOrCreateText(itemNameText, "ItemName", string.Empty, TextAnchor.UpperRight, Color.white);
            if (itemNameText != null) itemNameText.transform.SetAsLastSibling();
        }

        private Text ResolveOrCreateText(Text existing, string childName, string defaultText, TextAnchor alignment, Color color)
        {
            if (existing != null) return existing;

            Text t = null;
            var trans = transform.Find(childName);
            if (trans != null)
            {
                t = trans.GetComponent<Text>();
            }

            if (t == null)
            {
                var obj = new GameObject(childName);
                obj.transform.SetParent(this.transform, false);
                t = obj.AddComponent<Text>();
            }

            if (string.IsNullOrEmpty(t.text)) t.text = defaultText;
            t.alignment = alignment;
            t.color = color;
            t.raycastTarget = false;

            if (t.font == null) t.font = _defaultFont;

            var rt = t.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return t;
        }

        private void ApplyTextStyle(Text text, ItemTextStyle style, bool applyLayout)
        {
            if (text == null) return;

            if (text.font == null) text.font = _defaultFont;

            if (applyLayout)
            {
                text.alignment = style.Alignment;
                text.raycastTarget = false;
                text.resizeTextForBestFit = style.BestFit;
                if (style.BestFit)
                {
                    text.resizeTextMaxSize = style.MaxFontSize;
                    text.resizeTextMinSize = style.MinFontSize;
                }
                text.alignByGeometry = true;

                var rt = text.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
                rt.offsetMin = new Vector2(style.LeftOffset, style.BottomOffset);
                rt.offsetMax = new Vector2(-style.RightOffset, -style.TopOffset);

                var outline = text.GetComponent<Outline>();
                if (outline == null) outline = text.gameObject.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = style.OutlineDistance;
                outline.useGraphicAlpha = true;
            }

            if (!style.BestFit)
            {
                text.fontSize = style.MaxFontSize;
            }
            text.horizontalOverflow = style.HorizontalOverflow;
            text.verticalOverflow = style.VerticalOverflow;
            text.lineSpacing = style.LineSpacing;
        }

        protected virtual void Bind(TetrisItemVM viewModel)
        {
            var bindingSet = this.CreateBindingSet(viewModel);
            bindingSet.Bind(itemImage).For(v => v.sprite).To(vm => vm.Icon).OneWay();
            bindingSet.Bind(itemImage).For(v => v.color).To(vm => vm.ImageColor).OneWay();
            bindingSet.Bind(itemImage).For(v => v.raycastTarget).To(vm => vm.IsRaycastTargetEnabled).OneWay();
            bindingSet.Bind(RectTransform).For(v => v.sizeDelta).To(vm => vm.Size).OneWay();
            if (stackNumText != null)
            {
                bindingSet.Bind(stackNumText).For(v => v.text).To(vm => vm.CurrentStack).OneWay();
            }
            if (itemNameText != null)
            {
                bindingSet.Bind(itemNameText).For(v => v.text).To(vm => vm.ItemName).OneWay();
            }
            bindingSet.Build();
            RefreshRarityBackgroundTiles();
            if (stackNumText != null)
            {
                stackNumText.gameObject.SetActive(viewModel.IsStackable);
            }
            if (itemNameText != null)
            {
                itemNameText.gameObject.SetActive(true);
            }
            ApplyTextStyle(stackNumText, viewModel.StackNumTextStyle, true);
            ApplyTextStyle(itemNameText, viewModel.ItemNameTextStyle, true);
            viewModel.HasActivedGridPanel = EquipmentTypeGridsPanel != null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_viewModel == null) return;

            var w = RectTransform.sizeDelta.x;
            var h = RectTransform.sizeDelta.y;
            var dir = _viewModel.Direction;
            var size = TetrisUtilities.RotationHelper.IsRotated(dir) ? new Vector2(h, w) : new Vector2(w, h);

            var initData = new TetrisItemGhostVM.GhostInitData
            {
                WorldPosition = Transform.position,
                Pivot = RectTransform.pivot,
                Size = size,
                Direction = dir
            };
            TetrisItemMediator.Instance.TrySyncGhostFromItem(_viewModel, initData);
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TetrisItemVM.CurrentStack))
            {
                if (_viewModel != null && _viewModel.CurrentStack < 1)
                {
                    TetrisItemFactory.ReleaseView(this);
                    return;
                }
            }
            else if (e.PropertyName == nameof(TetrisItemVM.Size) ||
                e.PropertyName == nameof(TetrisItemVM.Direction) ||
                e.PropertyName == nameof(TetrisItemVM.Rotated) ||
                e.PropertyName == nameof(TetrisItemVM.RotationOffset) ||
                e.PropertyName == nameof(TetrisItemVM.Width) ||
                e.PropertyName == nameof(TetrisItemVM.Height) ||
                e.PropertyName == nameof(TetrisItemVM.RarityColor) ||
                e.PropertyName == nameof(TetrisItemVM.ItemDetails))
            {
                if (_viewModel != null)
                {
                    ApplyTextStyle(stackNumText, _viewModel.StackNumTextStyle, false);
                    ApplyTextStyle(itemNameText, _viewModel.ItemNameTextStyle, false);
                    RefreshRarityBackgroundTiles();
                }
            }
        }

        private void EnsureRarityBackgroundRoot()
        {
            if (rarityBackgroundRoot == null)
            {
                var trans = transform.Find("RarityBackground");
                rarityBackgroundRoot = trans as RectTransform;
            }

            if (rarityBackgroundRoot == null)
            {
                var bgObj = new GameObject("RarityBackground");
                bgObj.transform.SetParent(this.transform, false);
                bgObj.transform.SetAsFirstSibling();
                rarityBackgroundRoot = bgObj.AddComponent<RectTransform>();
            }

            rarityBackgroundRoot.SetAsFirstSibling();
            rarityBackgroundRoot.anchorMin = Vector2.zero;
            rarityBackgroundRoot.anchorMax = Vector2.one;
            rarityBackgroundRoot.sizeDelta = Vector2.zero;
            rarityBackgroundRoot.pivot = new Vector2(0f, 1f);
            rarityBackgroundRoot.offsetMin = new Vector2(0.5f, 0.5f);
            rarityBackgroundRoot.offsetMax = new Vector2(-0.5f, -0.5f);
        }

        private void RefreshRarityBackgroundTiles()
        {
            EnsureRarityBackgroundRoot();
            var vm = _viewModel;
            if (vm == null) return;
            if (vm.ItemDetails == null) return;

            float w = RectTransform != null ? RectTransform.sizeDelta.x : vm.Size.x;
            float h = RectTransform != null ? RectTransform.sizeDelta.y : vm.Size.y;
            if (w <= 0f || h <= 0f) return;

            bool inSlot = vm.CurrentTetrisContainer is TetrisSlotVM;
            int gridW = inSlot ? vm.ItemDetails.xWidth : vm.Width;
            int gridH = inSlot ? vm.ItemDetails.yHeight : vm.Height;
            if (gridW <= 0 || gridH <= 0)
            {
                ReleaseAllRarityBackgroundTiles();
                return;
            }

            List<Vector2Int> points = null;
            if (!inSlot)
            {
                points = vm.TetrisCoordinateSet;
            }
            int required = inSlot ? (gridW * gridH) : (points != null ? points.Count : 0);
            if (required <= 0)
            {
                ReleaseAllRarityBackgroundTiles();
                return;
            }

            float tileW = w / gridW;
            float tileH = h / gridH;
            if (tileW <= 0f) tileW = Settings.gridTileSizeWidth;
            if (tileH <= 0f) tileH = Settings.gridTileSizeHeight;

            EnsureRarityTileCount(required);

            var color = vm.RarityColor;
            var offset = inSlot ? Vector2Int.zero : vm.RotationOffset;
            if (inSlot)
            {
                int idx = 0;
                for (int y = 0; y < gridH; y++)
                {
                    for (int xCell = 0; xCell < gridW; xCell++)
                    {
                        var tile = rarityBackgroundTiles[idx++];
                        if (tile == null) continue;
                        float x = (xCell + offset.x) * tileW;
                        float yDown = (y + offset.y) * tileH;
                        float centerX = x + (tileW * 0.5f) - (w * 0.5f);
                        float centerY = (h * 0.5f) - (yDown + (tileH * 0.5f));
                        ConfigureRarityTile(tile, tileW, tileH, new Vector2(centerX, centerY), color);
                    }
                }
            }
            else
            {
                for (int i = 0; i < required; i++)
                {
                    var tile = rarityBackgroundTiles[i];
                    if (tile == null) continue;

                    var p = points[i];
                    float x = (p.x + offset.x) * tileW;
                    float yDown = (p.y + offset.y) * tileH;
                    float centerX = x + (tileW * 0.5f) - (w * 0.5f);
                    float centerY = (h * 0.5f) - (yDown + (tileH * 0.5f));
                    ConfigureRarityTile(tile, tileW, tileH, new Vector2(centerX, centerY), color);
                }
            }
        }

        private static void ConfigureRarityTile(Image tile, float tileW, float tileH, Vector2 anchoredPos, Color color)
        {
            tile.color = color;
            var rt = tile.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(tileW, tileH);
            rt.anchoredPosition = anchoredPos;
        }

        private static GameObject EnsureRarityTilePrefab()
        {
            if (s_rarityTilePrefab != null) return s_rarityTilePrefab;
            var prefab = new GameObject("RarityTile");
            // prefab.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(prefab);
            var img = prefab.AddComponent<Image>();
            img.raycastTarget = false;
            prefab.SetActive(false);
            s_rarityTilePrefab = prefab;
            return s_rarityTilePrefab;
        }

        private static Image AcquireRarityTile(Transform parent)
        {
            GameObject go = null;
            var prefab = EnsureRarityTilePrefab();
            if (PoolManager.Instance != null)
            {
                go = PoolManager.Instance.GetObject(prefab);
            }
            else
            {
                go = Instantiate(prefab);
            }

            if (go == null) return null;
            go.SetActive(true);
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        private static void ReleaseRarityTile(Image tile)
        {
            if (tile == null) return;
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.PushObject(tile.gameObject);
                return;
            }
            Destroy(tile.gameObject);
        }

        private void EnsureRarityTileCount(int required)
        {
            if (required < 0) required = 0;
            while (rarityBackgroundTiles.Count > required)
            {
                var lastIndex = rarityBackgroundTiles.Count - 1;
                var tile = rarityBackgroundTiles[lastIndex];
                rarityBackgroundTiles.RemoveAt(lastIndex);
                ReleaseRarityTile(tile);
            }

            while (rarityBackgroundTiles.Count < required)
            {
                var tile = AcquireRarityTile(rarityBackgroundRoot);
                rarityBackgroundTiles.Add(tile);
            }
        }

        private void ReleaseAllRarityBackgroundTiles()
        {
            for (int i = rarityBackgroundTiles.Count - 1; i >= 0; i--)
            {
                ReleaseRarityTile(rarityBackgroundTiles[i]);
            }
            rarityBackgroundTiles.Clear();
        }

        public void InitializeGridPanel()
        {
            var vm = this.ViewModel;
            if (vm == null || vm.ItemDetails == null || vm.ItemDetails.gridUIPrefab == null) return;
            if (EquipmentTypeGridsPanel != null) return;
            EquipmentTypeGridsPanel = Instantiate(vm.ItemDetails.gridUIPrefab);
            _initialGridPanelParent = transform;
            EquipmentTypeGridsPanel.SetParent(_initialGridPanelParent, false);
            var rt = EquipmentTypeGridsPanel.GetComponent<RectTransform>();
            if (rt != null) rt.localPosition = new Vector3(60, 0, 0);
            EquipmentTypeGridsPanel.gameObject.SetActive(false);
            var gridViews = EquipmentTypeGridsPanel.GetComponentsInChildren<TetrisGridView>(true);
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

                    OwnedTetrisGrids.Add(gv);
                }
            }
            vm.HasActivedGridPanel = EquipmentTypeGridsPanel != null;
        }

        public void SetGridPanelParent(Transform parent)
        {
            if (EquipmentTypeGridsPanel == null) return;
            EquipmentTypeGridsPanel.SetParent(parent, false);
            var rt = EquipmentTypeGridsPanel.GetComponent<RectTransform>();
            if (rt != null) rt.localPosition = new Vector3(60, 0, 0);
            if (_initialGridPanelParent != null)
                EquipmentTypeGridsPanel.gameObject.SetActive(parent != _initialGridPanelParent);
        }

        public void DestroyGridPanel()
        {
            if (EquipmentTypeGridsPanel != null)
            {
                Destroy(EquipmentTypeGridsPanel.gameObject);
                EquipmentTypeGridsPanel = null;
                OwnedTetrisGrids.Clear();
                if (_viewModel != null)
                    _viewModel.HasActivedGridPanel = false;
            }
        }
    }
}
