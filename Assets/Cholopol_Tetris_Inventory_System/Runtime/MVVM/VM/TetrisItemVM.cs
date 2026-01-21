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
using Loxodon.Framework.ViewModels;
using System.Collections.Generic;
using UnityEngine;
using Cholopol.TIS;
using Cholopol.TIS.SaveLoadSystem;
using Cholopol.TIS.Events;
using Loxodon.Framework.Contexts;

namespace Cholopol.TIS.MVVM.ViewModels
{
    public class TetrisItemVM : ViewModelBase, ITetrisRotatable
    {
        private static readonly ItemTextStyle _stackNumTextStyle = new ItemTextStyle(0f, 2f, 2f, 10, new Vector2(0.5f, 0.5f), minFontSize: 6, bestFit: true);
        public ItemTextStyle StackNumTextStyle => _stackNumTextStyle;

        private static readonly ItemTextStyle _itemNameTextStyle = new ItemTextStyle(0f, 2f, 0f, 8, new Vector2(0.5f, 0.5f),
            alignment: TextAnchor.UpperRight,
            minFontSize: 5,
            leftOffset: 0f,
            topOffset: 2f,
            horizontalOverflow: HorizontalWrapMode.Wrap,
            verticalOverflow: VerticalWrapMode.Overflow,
            lineSpacing: 0.5f,
            bestFit: true);
        public ItemTextStyle ItemNameTextStyle => _itemNameTextStyle;

        private TetrisItemContainerVM _currentTetrisContainer;
        private ItemDetails _details;
        private Vector2 _size;
        private Dir _dir = Dir.Down;
        private bool _rotated;
        private Vector2Int _localGridCoordinate;
        private List<Vector2Int> _coordinateSet;
        private Vector2Int _rotationOffset;
        private bool _isRaycastTargetEnabled = true;
        private Sprite _icon;
        private InventorySlotType _slotType = InventorySlotType.Pocket;
        private Color _imageColor = Color.white;
        private int _dragDimRefCount = 0;
        private Color _dragDimOriginalImageColor = Color.white;
        private bool _hasDragDimOriginal = false;
        private Color _rarityColor = Color.clear;
        private int _currentStack;
        private string _itemName = string.Empty;
        private bool hasActivedGridPanel;
        private List<TetrisGridVM> _ownedTetrisGridsVM = new List<TetrisGridVM>();

        public int MaxStack { get; private set; }
        public bool IsStackable => MaxStack > 1;
        public string Guid { get; set; }
        public int Width => Rotated ? ItemDetails.yHeight : ItemDetails.xWidth;
        public int Height => Rotated ? ItemDetails.xWidth : ItemDetails.yHeight;

        public ItemDetails ItemDetails
        {
            get => _details;
            set
            {
                if (Set(ref _details, value))
                {
                    Icon = _details != null ? _details.itemIcon : null;
                    SlotType = _details != null ? _details.inventorySlotType : InventorySlotType.Pocket;
                    RarityColor = _details != null ? TetrisUtilities.ItemRarityColorHelper.GetColor(_details.itemRarity) : Color.clear;
                    ItemName = _details != null && _details.localizedName != null && !_details.localizedName.IsEmpty ? _details.localizedName.GetLocalizedString() : string.Empty;
                    UpdateRotation();
                }
            }
        }

        public Dir Direction { get => _dir; set { if (Set(ref _dir, value)) UpdateRotation(); } }
        public bool Rotated { get => _rotated; set { if (Set(ref _rotated, value)) UpdateRotation(); } }
        public Vector2Int LocalGridCoordinate { get => _localGridCoordinate; set => Set(ref _localGridCoordinate, value); }
        public List<Vector2Int> TetrisCoordinateSet { get => _coordinateSet; set => Set(ref _coordinateSet, value); }
        public Vector2Int RotationOffset { get => _rotationOffset; set => Set(ref _rotationOffset, value); }
        public TetrisItemContainerVM CurrentTetrisContainer { get => _currentTetrisContainer; set => Set(ref _currentTetrisContainer, value); }
        public bool IsRaycastTargetEnabled { get => _isRaycastTargetEnabled; set => Set(ref _isRaycastTargetEnabled, value); }
        public Sprite Icon { get => _icon; set => Set(ref _icon, value); }
        public InventorySlotType SlotType { get => _slotType; set => Set(ref _slotType, value); }
        public Color ImageColor { get => _imageColor; set => Set(ref _imageColor, value); }
        public Color RarityColor { get => _rarityColor; set => Set(ref _rarityColor, value); }
        public int CurrentStack 
        { 
            get => _currentStack; 
            set => Set(ref _currentStack, value);
        }
        public string ItemName { get => _itemName; set => Set(ref _itemName, value); }
        public Vector2 Size { get => _size; set => Set(ref _size, value); }
        public bool HasActivedGridPanel { get => hasActivedGridPanel; set => Set(ref hasActivedGridPanel, value); }
        public List<TetrisGridVM> OwnedTetrisGridsVM { get => _ownedTetrisGridsVM; set => Set(ref _ownedTetrisGridsVM, value); }

        public TetrisItemVM(ItemDetails d, TetrisItemPersistentData data, TetrisItemContainerVM tetrisItemContainerVM)
        {
            CurrentTetrisContainer = tetrisItemContainerVM;
            ItemDetails = d;
            MaxStack = d != null ? d.maxStack : 0;
            if (data != null)
            {
                Direction = data.direction;
                LocalGridCoordinate = data.orginPosition;
                Rotated = TetrisUtilities.RotationHelper.IsRotated(Direction);
                Guid = !string.IsNullOrEmpty(data.itemGuid) ? data.itemGuid : System.Guid.NewGuid().ToString();
                CurrentStack = data.stack > 0 ? data.stack : 1;
            }
            else
            {
                Direction = d.dir;
                Rotated = TetrisUtilities.RotationHelper.IsRotated(Direction);
                CurrentStack = MaxStack > 0 ? MaxStack : 1;
                Guid = System.Guid.NewGuid().ToString();
            }
            
            EventBus.Instance.Subscribe(EventNames.LanguageChangedEvent, RefreshLocalizedName);
        }

        ~TetrisItemVM()
        {
            EventBus.Instance.Unsubscribe(EventNames.LanguageChangedEvent, RefreshLocalizedName);
        }

        public void RefreshLocalizedName()
        {
            if (_details?.localizedName != null && !_details.localizedName.IsEmpty)
                ItemName = _details.localizedName.GetLocalizedString();
        }

        public void Rotate()
        {
            Direction = TetrisUtilities.RotationHelper.GetNextDir(Direction);
            Rotated = TetrisUtilities.RotationHelper.IsRotated(Direction);
        }

        private void UpdateRotation()
        {
            if (ItemDetails == null) return;
            var basePoints = InventoryManager.Instance.GetTetrisCoordinateSet(ItemDetails.tetrisPieceShape);
            _coordinateSet = TetrisUtilities.RotationHelper.RotatePoints(basePoints, Direction);
            RotationOffset = TetrisUtilities.RotationHelper.GetRotationOffset(Direction, Width, Height);
            RaisePropertyChanged(nameof(Width));
            RaisePropertyChanged(nameof(Height));
            UpdateSize(CurrentTetrisContainer);
        }

        public void UpdateSize(TetrisItemContainerVM currentTetrisContainer)
        {
            if (currentTetrisContainer == null) return;
            if (currentTetrisContainer is TetrisGridVM)
            {
                var currentTetrisGrid = currentTetrisContainer as TetrisGridVM;
                Size = new Vector2(Width * currentTetrisGrid.LocalGridTileSizeWidth, Height * currentTetrisGrid.LocalGridTileSizeHeight);
            }
            else if (currentTetrisContainer is TetrisSlotVM)
            {
                var currentTetrisSlot = currentTetrisContainer as TetrisSlotVM;
                Size = currentTetrisSlot.SlotSize;
            }
            else
            {
                Size = new Vector2(Width * Settings.gridTileSizeWidth, Height * Settings.gridTileSizeHeight);
            }
        }

        /// <summary>
        /// When starting to drag and drop, darken the item icon and synchronize the darkening of all views bound to the VM by modifying its ImageColor.
        /// </summary>
        public void BeginDragDim(float darkenFactor = 0.2f)
        {
            if (darkenFactor < 0f) darkenFactor = 0f;
            if (darkenFactor > 1f) darkenFactor = 1f;

            if (_dragDimRefCount == 0)
            {
                _dragDimOriginalImageColor = ImageColor;
                _hasDragDimOriginal = true;

                ImageColor = new Color(
                    Mathf.Lerp(_dragDimOriginalImageColor.r, 0f, darkenFactor),
                    Mathf.Lerp(_dragDimOriginalImageColor.g, 0f, darkenFactor),
                    Mathf.Lerp(_dragDimOriginalImageColor.b, 0f, darkenFactor),
                    _dragDimOriginalImageColor.a
                );
            }

            _dragDimRefCount++;
        }

        /// <summary>
        /// When ending the drag, restore the color of the item icon and pair it with BeginDragDim.
        /// If there is an abnormal call order (End more than Begin), it will be automatically clamped to a safe state.
        /// </summary>
        public void EndDragDim()
        {
            if (_dragDimRefCount <= 0)
            {
                _dragDimRefCount = 0;
                return;
            }

            _dragDimRefCount--;

            if (_dragDimRefCount == 0 && _hasDragDimOriginal)
            {
                ImageColor = _dragDimOriginalImageColor;
                _hasDragDimOriginal = false;
            }
        }

        public void SetItemData()
        {
            if (CurrentStack <= 0)
            {
                RemoveItemData();
                return;
            }

            var guid = !string.IsNullOrEmpty(this.Guid) ? this.Guid : string.Empty;
            TetrisItemPersistentData gridItem = !string.IsNullOrEmpty(guid)
                ? InventorySaveLoadService.Instance.inventoryData_SO.GetTetrisItemPersistentDataByGuid(guid)
                : null;
            if (gridItem == null)
            {
                gridItem = new TetrisItemPersistentData();
                SetTetrisItemPersistentData(gridItem);
                InventorySaveLoadService.Instance.inventoryData_SO.UpsertPersistentData(gridItem);
                return;
            }
            SetTetrisItemPersistentData(gridItem);
        }

        public void RemoveItemData()
        {
            var guid = !string.IsNullOrEmpty(this.Guid) ? this.Guid : string.Empty;
            if (!string.IsNullOrEmpty(guid) && InventorySaveLoadService.Instance != null && InventorySaveLoadService.Instance.inventoryData_SO != null)
            {
                InventorySaveLoadService.Instance.inventoryData_SO.RemovePersistentDataByGuid(guid);
            }
        }

        public bool TryGetOwnedGridVM(string guid, out TetrisGridVM vm)
        {
            vm = null;
            if (string.IsNullOrEmpty(guid)) return false;
            if (_ownedTetrisGridsVM == null) return false;
            for (int i = 0; i < _ownedTetrisGridsVM.Count; i++)
            {
                var v = _ownedTetrisGridsVM[i];
                if (v != null && v.GridGuid == guid) { vm = v; return true; }
            }
            return false;
        }

        public TetrisGridVM GetOrCreateGridVM(int index)
        {
            if (_ownedTetrisGridsVM == null) _ownedTetrisGridsVM = new List<TetrisGridVM>();

            if (index < _ownedTetrisGridsVM.Count && _ownedTetrisGridsVM[index] != null)
            {
                var existingVm = _ownedTetrisGridsVM[index];
                var containerId = (!string.IsNullOrEmpty(this.Guid) ? this.Guid : System.Guid.NewGuid().ToString()) + ":" + index.ToString();
                if (existingVm.GridGuid != containerId)
                {
                    TetrisGridFactory.UnregisterVM(existingVm, false);
                    existingVm.GridGuid = containerId;
                    TetrisGridFactory.RegisterVM(containerId, existingVm);
                }
                var cache = Context.GetApplicationContext().GetService<IInventoryTreeCache>();
                if (cache != null)
                {
                    cache.GetOrCreateContainer(containerId);
                    cache.SetContainerOwner(containerId, this.Guid);
                }
                existingVm.RelatedTetrisItem = this;
                return existingVm;
            }

            // Initial size placeholder, will be updated by View binding via ApplyConfig
            var newVm = new TetrisGridVM(1, 1);
            var newContainerId = (!string.IsNullOrEmpty(this.Guid) ? this.Guid : System.Guid.NewGuid().ToString()) + ":" + index.ToString();
            newVm.GridGuid = newContainerId;
            TetrisGridFactory.RegisterVM(newContainerId, newVm);
            var cache2 = Context.GetApplicationContext().GetService<IInventoryTreeCache>();
            if (cache2 != null)
            {
                cache2.GetOrCreateContainer(newContainerId);
                cache2.SetContainerOwner(newContainerId, this.Guid);
            }
            newVm.RelatedTetrisItem = this;

            // Pad the list if necessary (though typical usage is sequential)
            while (_ownedTetrisGridsVM.Count <= index)
            {
                _ownedTetrisGridsVM.Add(null);
            }
            _ownedTetrisGridsVM[index] = newVm;
            
            return newVm;
        }

        /// <summary>
        /// Assign the relevant fields of TetrisItem to gridItem
        /// </summary>
        private void SetTetrisItemPersistentData(TetrisItemPersistentData gridItem)
        {
            if (gridItem == null) return;
            gridItem.itemID = ItemDetails != null ? ItemDetails.itemID : 0;

            gridItem.itemGuid = !string.IsNullOrEmpty(this.Guid) ? this.Guid : string.Empty;
            gridItem.isOnSlot = CurrentTetrisContainer is TetrisSlotVM;
            if (!gridItem.isOnSlot && CurrentTetrisContainer != null && CurrentTetrisContainer.GetType().Name.Contains("Slot"))
            {
                gridItem.isOnSlot = true;
            }

            if (gridItem.isOnSlot && CurrentTetrisContainer is TetrisSlotVM sv)
            {
                gridItem.slotIndex = sv.SlotIndex;
            }
            else
            {
                gridItem.slotIndex = -1;
            }

            gridItem.parentItemGuid = (CurrentTetrisContainer != null && CurrentTetrisContainer.RelatedTetrisItem != null && !gridItem.isOnSlot)
                ? CurrentTetrisContainer.RelatedTetrisItem.Guid
                : string.Empty;

            gridItem.orginPosition = LocalGridCoordinate;
            gridItem.direction = Direction;
            gridItem.stack = CurrentStack;
            if (CurrentTetrisContainer is TetrisGridVM gv)
            {
                gridItem.gridPIndex = (gv.GridPIndex > -1) ? gv.GridPIndex : -1;
                gridItem.persistentGridGuid = !string.IsNullOrEmpty(gv.GridGuid) ? gv.GridGuid : string.Empty;
            }
            else
            {
                gridItem.gridPIndex = -1;
                gridItem.persistentGridGuid = string.Empty;
            }

        }

    }
}
