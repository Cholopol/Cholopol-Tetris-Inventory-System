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
using Cholopol.TIS.MVVM;
using Cholopol.TIS.MVVM.ViewModels;
using Loxodon.Framework.Contexts;
using Loxodon.Framework.Interactivity;
using Loxodon.Framework.ViewModels;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static Cholopol.TIS.TetrisUtilities;

public class TetrisItemGhostVM : ViewModelBase, ITetrisRotatable
{
    public struct GhostInitData
    {
        public Vector3 WorldPosition;
        public Vector2 Pivot;
        public Vector2 Size;
        public Dir Direction;
    }

    private int _onGridPositionX;
    private int _onGridPositionY;
    private ItemDetails _details;
    private Vector2 _size;
    private Dir _dir = Dir.Down;
    private bool _rotated;
    private List<Vector2Int> _coordinateSet;
    private Vector2Int _rotationOffset;
    private Sprite _icon;
    private Vector2Int _oldPosition;
    private Color _originalGhostColor;
    private Color _draggingGhostColor;
    private TetrisItemVM _selectedItem;
    private TetrisItemVM overlapItem;
    private TetrisItemContainerVM _originContainerOnDrag;
    private TetrisItemContainerVM _targetContainerOnDrop;
    private PlaceState _placeState = PlaceState.InvalidPos;

    public int OnGridPositionX { get => _onGridPositionX; set => Set(ref _onGridPositionX, value); }
    public int OnGridPositionY { get => _onGridPositionY; set => Set(ref _onGridPositionY, value); }
    public Vector2 Size { get => _size; set => Set(ref _size, value); }
    public Dir Direction { get => _dir; set { if (Set(ref _dir, value)) UpdateRotation(); } }
    public bool Rotated { get => _rotated; set { if (Set(ref _rotated, value)) UpdateRotation(); } }
    public List<Vector2Int> TetrisCoordinateSet { get => _coordinateSet; set => Set(ref _coordinateSet, value); }
    public Vector2Int RotationOffset { get => _rotationOffset; set => Set(ref _rotationOffset, value); }
    public Sprite Icon { get => _icon; set => Set(ref _icon, value); }
    public Vector2Int OldPosition { get => _oldPosition; set => Set(ref _oldPosition, value); }
    public Color DraggingGhostColor { get => _draggingGhostColor; set => Set(ref _draggingGhostColor, value); }
    public TetrisItemVM SelectedItem { get => _selectedItem; set => Set(ref _selectedItem, value); }
    public TetrisItemContainerVM OriginContainerOnDrag { get => _originContainerOnDrag; set => Set(ref _originContainerOnDrag, value); }
    public TetrisItemContainerVM TargetContaineOnDrop { get => _targetContainerOnDrop; set => Set(ref _targetContainerOnDrop, value); }
    private PlaceState PlaceState { get => _placeState; set => Set(ref _placeState, value); }
    public int Width => Rotated ? ItemDetails.yHeight : ItemDetails.xWidth;
    public int Height => Rotated ? ItemDetails.xWidth : ItemDetails.yHeight;
    public bool OnDragging { get; set; } = false;

    public readonly InteractionRequest<GhostInitData> InitializeFromItemRequest = new();
    public readonly InteractionRequest<Dir> OnRotateRequest = new();

    public ItemDetails ItemDetails
    {
        get => _details;
        set
        {
            if (Set(ref _details, value))
            {
                Icon = _details != null ? _details.itemUI : null;
                UpdateRotation();

            }
        }
    }

    public TetrisItemGhostVM()
    {
        _originalGhostColor = Color.white;
        _draggingGhostColor = new Color(_originalGhostColor.r, _originalGhostColor.g, _originalGhostColor.b, 0f);
    }

    public void OnUpdate()
    {
        if (InventoryManager.Instance != null &&
                InventoryManager.Instance.selectedTetrisItemGridVM != null)
        {
            Vector2Int positionOnGrid = InventoryManager.Instance.GetGhostTileGridOriginPosition();
            _onGridPositionX = positionOnGrid.x;
            _onGridPositionY = positionOnGrid.y;
        }
    }

    public void OnBeginDrag()
    {
        SelectedItem?.BeginDragDim(0.2f);
        DraggingGhostColor = new Color(_originalGhostColor.r, _originalGhostColor.g, _originalGhostColor.b, 0.8f);
        if (Icon == null && SelectedItem != null && SelectedItem.ItemDetails != null)
        {
            Icon = SelectedItem.ItemDetails.itemUI;
        }
        TetrisItemMediator.Instance.CacheItemState(SelectedItem);
        TetrisItemMediator.Instance.CacheGhostState(this);

        if (SelectedItem != null)
        {
            OriginContainerOnDrag = SelectedItem.CurrentTetrisContainer;
        }

        OnBeginAction(SelectedItem, OriginContainerOnDrag);
    }

    public void OnDrag()
    {

    }

    public void OnEndDrag()
    {
        SelectedItem?.EndDragDim();
        DraggingGhostColor = new Color(_originalGhostColor.r, _originalGhostColor.g, _originalGhostColor.b, 0f);
        Icon = null;
        if (TargetContaineOnDrop is TetrisGridVM &&
            (InventoryManager.Instance == null || InventoryManager.Instance.selectedTetrisItemGridVM == null))
        {
            TargetContaineOnDrop = null;
        }
        var targetContainer = TargetContaineOnDrop;
        OnEndAction(targetContainer);
        UpdatePlaceState();
    }

    private void SelectFromItemVM(TetrisItemVM item)
    {
        if (item == null) return;
        CopyStateFromItemVM(item);
        UpdateSizeForContainer(item.CurrentTetrisContainer);
    }

    private void CopyStateFromItemVM(TetrisItemVM item)
    {
        if (item == null) return;
        SelectedItem = item;
        OriginContainerOnDrag = item.CurrentTetrisContainer;
        ItemDetails = item.ItemDetails;
        Rotated = item.Rotated;
        Direction = item.Direction;
        RotationOffset = item.RotationOffset;
        OnGridPositionX = item.LocalGridCoordinate.x;
        OnGridPositionY = item.LocalGridCoordinate.y;
        TetrisCoordinateSet = item.TetrisCoordinateSet;
    }

    public void OnPointerDown(PointerEventData eventData)
    {

    }

    public void Rotate()
    {
        if (!OnDragging) return;
        if (ItemDetails == null) return;
        Direction = RotationHelper.GetNextDir(Direction);
        Rotated = RotationHelper.IsRotated(Direction);
        TetrisItemMediator.Instance.CacheGhostState(this);
        OnRotateRequest.Raise(Direction);
    }

    private void UpdateRotation()
    {
        if (ItemDetails == null) return;
        var basePoints = InventoryManager.Instance.GetTetrisCoordinateSet(ItemDetails.tetrisPieceShape);
        _coordinateSet = RotationHelper.RotatePoints(basePoints, Direction);
        RotationOffset = RotationHelper.GetRotationOffset(Direction, Width, Height);
        RaisePropertyChanged(nameof(Width));
        RaisePropertyChanged(nameof(Height));
    }

    /// <summary>
    /// Refresh Ghost visual size based on container type
    /// </summary>
    public void UpdateSizeForContainer(TetrisItemContainerVM container)
    {
        if (ItemDetails == null) return;

        if (container is TetrisGridVM grid)
        {
            Size = new Vector2(ItemDetails.xWidth * grid.LocalGridTileSizeWidth, ItemDetails.yHeight * grid.LocalGridTileSizeHeight);
            return;
        }

        if (container is TetrisSlotVM slot)
        {
            Size = slot.SlotSize;
            return;
        }

        Size = new Vector2(ItemDetails.xWidth * Settings.gridTileSizeWidth, ItemDetails.yHeight * Settings.gridTileSizeHeight);
    }

    private void OnBeginAction(TetrisItemVM selectItem, TetrisItemContainerVM originContainer)
    {
        if (originContainer is TetrisGridVM)
        {
            var gridVM = originContainer as TetrisGridVM;
            gridVM.RemoveTetrisItem(
                selectItem,
                selectItem.LocalGridCoordinate.x,
                selectItem.LocalGridCoordinate.y,
                selectItem.RotationOffset,
                selectItem.TetrisCoordinateSet,
                false);
            var cache = Context.GetApplicationContext().GetService<IInventoryTreeCache>();
            if (cache != null && !string.IsNullOrEmpty(gridVM.GridGuid))
            {
                cache.RemoveFromContainer(gridVM.GridGuid, selectItem.Guid);
            }
        }
        else if (originContainer is TetrisSlotVM)
        {
            var slotVM = originContainer as TetrisSlotVM;
            slotVM.RemoveTetrisItem(false);
        }
    }

    private void OnEndAction(TetrisItemContainerVM targetContainer)
    {
        if (SelectedItem == null || targetContainer == null)
        {
            PlaceState = PlaceState.InvalidPos;
            overlapItem = null;
            return;
        }

        if (targetContainer is TetrisGridVM)
        {
            var targetGridVM = targetContainer as TetrisGridVM;
            var origin = new Vector2Int(OnGridPositionX, OnGridPositionY);
            var placementContext = InventoryPlacementContext.ForGhost(SelectedItem, this, targetContainer, origin);
            var service = Context.GetApplicationContext().GetService<IInventoryService>();
            if (service == null || !service.CanPlace(placementContext, out _))
            {
                PlaceState = PlaceState.InvalidPos;
                overlapItem = null;
                return;
            }
            foreach (Vector2Int v2i in TetrisCoordinateSet)
            {
                PlaceState = targetGridVM.HasItem(
                    OnGridPositionX + v2i.x + RotationOffset.x,
                    OnGridPositionY + v2i.y + RotationOffset.y) ?
                PlaceState.OnGridHasItem : PlaceState.OnGridNoItem;
                if (PlaceState == PlaceState.OnGridHasItem)
                {
                    if (overlapItem == null)
                    {
                        overlapItem = targetGridVM.GetTetrisItemVM(
                        OnGridPositionX + v2i.x + RotationOffset.x,
                        OnGridPositionY + v2i.y + RotationOffset.y);
                        return;
                    }
                    else
                    {
                        //If find multiple overlapping items in the range
                        if (overlapItem != targetGridVM.GetTetrisItemVM(
                        OnGridPositionX + v2i.x + RotationOffset.x,
                        OnGridPositionY + v2i.y + RotationOffset.y))
                        {
                            overlapItem = null;
                            return;
                        }
                    }
                }
                else
                {
                    overlapItem = null;
                }
            }
            return;
        }
        else if (targetContainer is TetrisSlotVM)
        {
            var placementContext = InventoryPlacementContext.ForGhost(SelectedItem, this, targetContainer, Vector2Int.zero);
            var service = Context.GetApplicationContext().GetService<IInventoryService>();
            if (service == null)
            {
                PlaceState = PlaceState.InvalidPos;
                overlapItem = null;
                return;
            }

            if (!service.CanPlace(placementContext, out var blockedReason))
            {
                PlaceState = blockedReason == InventoryPlacementBlockReason.SlotOccupied
                    ? PlaceState.OnSlotHasItem
                    : PlaceState.InvalidPos;
                overlapItem = null;
                return;
            }

            PlaceState = PlaceState.OnSlotNoItem;
            overlapItem = null;
            return;
        }
    }

    private void UpdatePlaceState()
    {
        switch (PlaceState)
        {
            case PlaceState.OnGridHasItem:
                PlaceOnOverlapItem(SelectedItem);
                break;
            case PlaceState.OnSlotHasItem:
                ResetState(SelectedItem);
                break;
            case PlaceState.OnGridNoItem:
                PlaceOnGrid(SelectedItem);
                break;
            case PlaceState.OnSlotNoItem:
                PlaceOnSlot(SelectedItem);
                break;
            case PlaceState.InvalidPos:
                ResetState(SelectedItem);
                break;
        }
    }

    private void PlaceOnOverlapItem(TetrisItemVM selectedTetrisItem)
    {
        bool canStack = overlapItem != null
            && selectedTetrisItem != null
            && overlapItem.ItemDetails.itemID == selectedTetrisItem.ItemDetails.itemID
            && selectedTetrisItem.ItemDetails.maxStack > 1;

        if (canStack)
        {
            var service = Context.GetApplicationContext().GetService<IInventoryService>();
            if (service != null)
            {
                canStack = service.TryStack(selectedTetrisItem, overlapItem);
            }
            else
            {
                canStack = InventoryLogicHelper.TryMergeStack(overlapItem, selectedTetrisItem);
            }
        }

        if (canStack)
        {
            selectedTetrisItem.SetItemData();
            overlapItem.SetItemData();

            if (selectedTetrisItem.CurrentStack <= 0)
            {
                var finalHoverItem = overlapItem;
                var otherGrid = (selectedTetrisItem.CurrentTetrisContainer as TetrisGridVM) ?? (OriginContainerOnDrag as TetrisGridVM);
                string itemGuid = selectedTetrisItem.Guid;

                if (otherGrid != null)
                {
                    var coord = selectedTetrisItem.LocalGridCoordinate;
                    otherGrid.RemoveTetrisItem(
                        selectedTetrisItem,
                        coord.x,
                        coord.y,
                        selectedTetrisItem.RotationOffset,
                        selectedTetrisItem.TetrisCoordinateSet,
                        true);
                }
                selectedTetrisItem.RemoveItemData();
                TetrisItemFactory.UnregisterAndDestroyUIByGuid(itemGuid, true);
                SelectFromItemVM(finalHoverItem);
                overlapItem = null;
                TargetContaineOnDrop = null;
                return;
            }

            // Partial stacking: restore original position without swapping
            var partialHoverItem = overlapItem;
            ResetState(selectedTetrisItem);
            SelectFromItemVM(partialHoverItem);
            overlapItem = null;
            TargetContaineOnDrop = null;
            return;
        }

        var targetGrid = TargetContaineOnDrop as TetrisGridVM;
        if (targetGrid == null)
            targetGrid = InventoryManager.Instance != null ? InventoryManager.Instance.selectedTetrisItemGridVM : null;
        if (targetGrid != null)
        {
            var targetPos = InventoryManager.Instance.GetGhostTileGridOriginPosition();
            var service = Context.GetApplicationContext().GetService<IInventoryService>();
            bool swapped = service != null ? service.TryQuickExchange(targetGrid, this, targetPos) : targetGrid.TryQuickExchange(this, targetPos);
            if (swapped)
            {
                DestroyOriginView(targetGrid);
                overlapItem = null;
                TargetContaineOnDrop = null;
                return;
            }
        }

        ResetState(selectedTetrisItem);
        if (overlapItem != null)
        {
            SelectFromItemVM(overlapItem);
        }
        overlapItem = null;
        TargetContaineOnDrop = null;
    }

    private void PlaceOnGrid(TetrisItemVM selectedTetrisItem)
    {
        TetrisItemMediator.Instance.ApplyStateToItem(selectedTetrisItem);
        var service = Context.GetApplicationContext().GetService<IInventoryService>();
        var grid = TargetContaineOnDrop as TetrisGridVM;
        var originPos = InventoryManager.Instance != null ? InventoryManager.Instance.GetGhostTileGridOriginPosition() : new Vector2Int(OnGridPositionX, OnGridPositionY);
        bool canPlace = service != null && service.PlaceOnGrid(
            selectedTetrisItem,
            grid,
            originPos,
            TargetContaineOnDrop as TetrisSlotVM);
        if (canPlace)
        {
            DestroyOriginView(TargetContaineOnDrop);
        }
        else
        {
            ResetState(selectedTetrisItem);
        }
    }

    private void PlaceOnSlot(TetrisItemVM selectedTetrisItem)
    {
        TetrisItemMediator.Instance.ApplyStateToItem(selectedTetrisItem);
        var service = Context.GetApplicationContext().GetService<IInventoryService>();
        var slot = TargetContaineOnDrop as TetrisSlotVM;
        bool canPlace = service != null && slot != null && service.PlaceOnSlot(selectedTetrisItem, slot);

        if (canPlace)
        {
            DestroyOriginView(TargetContaineOnDrop);
        }
        else
        {
            ResetState(selectedTetrisItem);
        }
    }

    private void DestroyOriginView(TetrisItemContainerVM targetContainer)
    {
        if (SelectedItem == null) return;

        if (targetContainer == OriginContainerOnDrag) return;

        if (OriginContainerOnDrag is TetrisGridVM grid)
        {
            grid.RequestRemoveItemView(SelectedItem);
        }
        else if (OriginContainerOnDrag is TetrisSlotVM slot)
        {
            slot.RequestRemoveItemView(SelectedItem);
        }
    }

    private void ResetState(TetrisItemVM selectedTetrisItem)
    {
        TetrisItemMediator.Instance.ApplyStateToGhost(this);
        if (selectedTetrisItem.CurrentTetrisContainer is TetrisGridVM)
        {
            var service = Context.GetApplicationContext().GetService<IInventoryService>();
            bool ok = service != null && service.PlaceOnGrid(
                selectedTetrisItem,
                selectedTetrisItem.CurrentTetrisContainer as TetrisGridVM,
                new Vector2Int(selectedTetrisItem.LocalGridCoordinate.x, selectedTetrisItem.LocalGridCoordinate.y),
                selectedTetrisItem.CurrentTetrisContainer as TetrisSlotVM);
            if (!ok)
            {
                var grid = selectedTetrisItem.CurrentTetrisContainer as TetrisGridVM;
                grid?.TryPlaceTetrisItem(selectedTetrisItem, selectedTetrisItem.LocalGridCoordinate.x, selectedTetrisItem.LocalGridCoordinate.y);
            }
        }
        else
        {
            TetrisSlotVM slot = selectedTetrisItem.CurrentTetrisContainer as TetrisSlotVM;
            slot.TryPlaceTetrisItem(selectedTetrisItem);
        }

    }

    public void InitializeFromItem(TetrisItemVM itemViewModel, GhostInitData initData)
    {
        if (itemViewModel == null) return;
        if (!OnDragging)
        {
            InitializeFromItemRequest.Raise(initData);
            CopyStateFromItemVM(itemViewModel);
            Size = initData.Size;
        }
    }

}
