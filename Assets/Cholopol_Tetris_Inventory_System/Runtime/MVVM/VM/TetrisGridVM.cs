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
using Cholopol.TIS.SaveLoadSystem;
using Loxodon.Framework.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Cholopol.TIS.MVVM.ViewModels
{
    public class TetrisGridVM : TetrisItemContainerVM
    {
        private int _gridSizeWidth = 1;
        private int _gridSizeHeight = 1;
        private float _localGridUnitSizeWidth = 20f;
        private float _localGridUnitSizeHeight = 20f;
        private Vector2 _size;
        private Vector2 _gridFocusLocalPos = new Vector2();
        private Vector2Int _gridFocusCoordintesPos = new Vector2Int();
        private TetrisItemVM[,] _tetrisItemOccupiedCells;
        private Dictionary<string, TetrisItemVM> _ownerItemsDic = new();
        private int _gridPIndex = -1;
        private string _persistentGridGuid = string.Empty;

        public int GridSizeWidth { get => _gridSizeWidth; set => Set(ref _gridSizeWidth, value); }
        public int GridSizeHeight { get => _gridSizeHeight; set => Set(ref _gridSizeHeight, value); }
        public float LocalGridTileSizeWidth { get => _localGridUnitSizeWidth; set => Set(ref _localGridUnitSizeWidth, value); }
        public float LocalGridTileSizeHeight { get => _localGridUnitSizeHeight; set => Set(ref _localGridUnitSizeHeight, value); }
        public Vector2 Size { get => _size; set => Set(ref _size, value); }
        public Vector2 GridFocusLocalPos { get => _gridFocusLocalPos; set => Set(ref _gridFocusLocalPos, value); }
        public Vector2Int GridFocusCoordintesPos { get => _gridFocusCoordintesPos; set => Set(ref _gridFocusCoordintesPos, value); }
        public int GridPIndex { get => _gridPIndex; set => Set(ref _gridPIndex, value); }
        public string GridGuid { get => _persistentGridGuid; set { if (Set(ref _persistentGridGuid, value)) { PrimeFromCache(); } } }

        public TetrisItemVM[,] TetrisItemOccupiedCells
        {
            get => _tetrisItemOccupiedCells;
            set => Set(ref _tetrisItemOccupiedCells, value);
        }
        public override Dictionary<string, TetrisItemVM> OwnerItemsDic
        {
            get => _ownerItemsDic;
            set
            {
                if (Set(ref _ownerItemsDic, value))
                {
                    if (_ownerItemsDic != null)
                    {
                        foreach (var item in _ownerItemsDic.Values)
                        {
                            if (item != null)
                            {
                                item.CurrentTetrisContainer = this;
                            }
                        }
                        if (_ownerItemsDic.Count > 0 && (TetrisItemOccupiedCells == null || IsGridEmpty()))
                        {
                            RebuildOccupiedCells();
                        }
                    }
                }
            }
        }

        public event Action<TetrisItemVM, int, int> PlaceItemViewRequested;
        public event Action<TetrisItemVM> RemoveItemViewRequested;

        public TetrisGridVM(int width, int height)
        {
            UpdateGridSize(width, height);
            TetrisItemOccupiedCells = new TetrisItemVM[GridSizeWidth, GridSizeHeight];
        }

        public void UpdateGridSize(int width, int height)
        {
            _gridSizeWidth = width > 0 ? width : 1;
            _gridSizeHeight = height > 0 ? height : 1;
            GridSizeWidth = _gridSizeWidth;
            GridSizeHeight = _gridSizeHeight;
            Size = new Vector2(GridSizeWidth * _localGridUnitSizeWidth, GridSizeHeight * _localGridUnitSizeHeight);
        }

        public void ApplyConfig(int width, int height, float unitWidth, float unitHeight)
        {
            GridSizeWidth = width > 0 ? width : 1;
            GridSizeHeight = height > 0 ? height : 1;
            LocalGridTileSizeWidth = unitWidth;
            LocalGridTileSizeHeight = unitHeight;
            Size = new Vector2(GridSizeWidth * LocalGridTileSizeWidth, GridSizeHeight * LocalGridTileSizeHeight);
            TetrisItemOccupiedCells = new TetrisItemVM[GridSizeWidth, GridSizeHeight];
            if (OwnerItemsDic != null && OwnerItemsDic.Count > 0)
            {
                RebuildOccupiedCells();
            }
            PrimeFromCache();
        }

        private bool IsGridEmpty()
        {
            if (TetrisItemOccupiedCells == null) return true;
            foreach (var cell in TetrisItemOccupiedCells)
            {
                if (cell != null) return false;
            }
            return true;
        }

        private void RebuildOccupiedCells()
        {
            if (TetrisItemOccupiedCells == null)
            {
                TetrisItemOccupiedCells = new TetrisItemVM[GridSizeWidth, GridSizeHeight];
            }

            foreach (var item in OwnerItemsDic.Values)
            {
                if (item == null) continue;
                var coords = item.TetrisCoordinateSet;
                var offset = item.RotationOffset;
                var basePos = item.LocalGridCoordinate;

                if (coords == null) continue;

                for (int i = 0; i < coords.Count; i++)
                {
                    var c = coords[i];
                    int gx = basePos.x + c.x + offset.x;
                    int gy = basePos.y + c.y + offset.y;
                    if (gx >= 0 && gx < GridSizeWidth && gy >= 0 && gy < GridSizeHeight)
                    {
                        TetrisItemOccupiedCells[gx, gy] = item;
                    }
                }
            }
        }

        private void PrimeFromCache()
        {
            var id = GridGuid;
            if (string.IsNullOrEmpty(id)) return;
            var cache = Context.GetApplicationContext().GetService<IInventoryTreeCache>();
            if (cache == null) return;
            var items = cache.GetItems(id);
            if (items == null) return;

            var existing = OwnerItemsDic;
            var dict = new Dictionary<string, TetrisItemVM>();
            foreach (var data in items)
            {
                var details = InventorySaveLoadService.Instance != null ? InventorySaveLoadService.Instance.itemDataList_SO.GetItemDetailsByID(data.itemID) : null;
                var vm = TetrisItemFactory.GetOrCreateVM(details, data, this);
                if (vm != null && !string.IsNullOrEmpty(vm.Guid))
                {
                    dict[vm.Guid] = vm;
                }
            }
            if (existing != null && existing.Count > 0)
            {
                foreach (var kv in existing)
                {
                    var guid = kv.Key;
                    var vm = kv.Value;
                    if (vm == null || string.IsNullOrEmpty(guid)) continue;
                    if (!dict.ContainsKey(guid))
                    {
                        RemoveItemViewRequested?.Invoke(vm);
                    }
                }
            }

            TetrisItemOccupiedCells = new TetrisItemVM[GridSizeWidth, GridSizeHeight];
            OwnerItemsDic = dict;
        }

        /// <summary>
        /// Calculate the coordinates of position in the current grid
        /// </summary>
        public Vector2Int GetTileGridPosition(Vector2 gridWorldPosition, Vector2 mousePosition, float canvasScaleFactor)
        {
            GridFocusLocalPos = new Vector2(
                mousePosition.x - gridWorldPosition.x,
                gridWorldPosition.y - mousePosition.y
                );
            GridFocusCoordintesPos = new Vector2Int(
                (int)(GridFocusLocalPos.x / LocalGridTileSizeWidth / canvasScaleFactor),
                (int)(GridFocusLocalPos.y / LocalGridTileSizeHeight / canvasScaleFactor));

            return GridFocusCoordintesPos;
        }

        /// <summary>
        /// The grid coordinates are calculated as the TetrisItem UI pivot position
        /// </summary>
        public Vector2 CalculatePositionOnGrid(TetrisItemVM item, int posX, int posY)
        {
            return new Vector2(
                posX * LocalGridTileSizeWidth + LocalGridTileSizeWidth * item.Width / 2,
                -(posY * LocalGridTileSizeHeight + LocalGridTileSizeHeight * item.Height / 2)
            );
        }

        public override bool TryPlaceTetrisItem(TetrisItemVM tetrisItem, int posX, int posY)
        {
            if (tetrisItem == null) return false;
            TetrisItemVM overlapItem = null;
            if (OverlapCheck(tetrisItem, posX, posY, ref overlapItem) == false) return false;
            if (overlapItem != null
                && overlapItem.ItemDetails.itemID == tetrisItem.ItemDetails.itemID
                && tetrisItem.ItemDetails.maxStack != 0)
            {
                return PlaceOnOverlapItem(tetrisItem, overlapItem);
            }
            if (ValidPosCheck(tetrisItem, posX, posY) == false) return false;
            PlaceTetrisItem(tetrisItem, posX, posY);
            return true;
        }

        public bool PlaceOnOverlapItem(TetrisItemVM item, TetrisItemVM overlapltem)
        {
            if (TetrisUtilities.InventoryLogicHelper.TryMergeStack(overlapltem, item))
            {
                item.SetItemData();
                overlapltem.SetItemData();
                if (item.CurrentStack <= 0)
                {
                    TetrisGridVM otherGrid = (TetrisGridVM)item.CurrentTetrisContainer;
                    string itemGuid = item.Guid;
                    if (otherGrid != null)
                    {
                        var coord = item.LocalGridCoordinate;
                        otherGrid.RemoveTetrisItem(item, coord.x, coord.y, item.RotationOffset, item.TetrisCoordinateSet, true);
                    }
                    
                    item.RemoveItemData();
                    TetrisItemFactory.UnregisterAndDestroyUIByGuid(itemGuid, true);
                }
                return true;
            }

            else
                return false;
        }

        public override void PlaceTetrisItem(TetrisItemVM tetrisItem, int posX, int posY)
        {
            if (tetrisItem == null) return;
            tetrisItem.LocalGridCoordinate = new Vector2Int(posX, posY);
            tetrisItem.CurrentTetrisContainer = this;
            tetrisItem.UpdateSize(this);
            OwnerItemsDic[tetrisItem.Guid] = tetrisItem;
            if (TetrisItemOccupiedCells != null)
            {
                var coords = tetrisItem.TetrisCoordinateSet;
                var offset = tetrisItem.RotationOffset;
                for (int i = 0; i < coords.Count; i++)
                {
                    var c = coords[i];
                    int gx = posX + c.x + offset.x;
                    int gy = posY + c.y + offset.y;
                    if (gx >= 0 && gx < GridSizeWidth && gy >= 0 && gy < GridSizeHeight)
                    {
                        TetrisItemOccupiedCells[gx, gy] = tetrisItem;
                    }
                }
            }
            tetrisItem.SetItemData();
            PlaceItemViewRequested?.Invoke(tetrisItem, posX, posY);
        }

        public override void RemoveTetrisItem()
        {
            // Grid do not use parameter free removal by default; Keep compatible implementation, no operation.
        }

        public TetrisItemVM RemoveTetrisItem(TetrisItemVM toReturn, int x, int y, Vector2Int oldRotationOffset, List<Vector2Int> tetrisPieceShapePositions, bool destroyView = true)
        {
            if (toReturn == null) return null;
            CleanGridReference(x, y, oldRotationOffset, tetrisPieceShapePositions);
            if (OwnerItemsDic.ContainsKey(toReturn.Guid))
            {
                OwnerItemsDic.Remove(toReturn.Guid);
            }
            if (destroyView)
            {
                RemoveItemViewRequested?.Invoke(toReturn);
            }
            return toReturn;
        }

        public void RequestRemoveItemView(TetrisItemVM item)
        {
            RemoveItemViewRequested?.Invoke(item);
        }

        public bool BoundryCheck(int posX, int posY, int width, int height)
        {
            if (posX < 0 || posY < 0) return false;
            if (posX + width > GridSizeWidth) return false;
            if (posY + height > GridSizeHeight) return false;
            return true;
        }

        public bool PositionCheck(int posX, int posY)
        {
            if (posX < 0 || posY < 0) return false;
            if (posX >= GridSizeWidth || posY >= GridSizeHeight) return false;
            return true;
        }

        public bool OverlapCheck(TetrisItemVM item, int posX, int posY, ref TetrisItemVM overlapItem)
        {
            var coords = item.TetrisCoordinateSet;
            var offset = item.RotationOffset;
            for (int i = 0; i < coords.Count; i++)
            {
                var c = coords[i];
                int x = posX + c.x + offset.x;
                int y = posY + c.y + offset.y;
                var exist = TetrisItemOccupiedCells[x, y];
                if (exist != null)
                {
                    if (overlapItem == null)
                    {
                        overlapItem = exist;
                    }
                    else if (overlapItem != exist)
                    {
                        overlapItem = null;
                        return false;
                    }
                }
            }
            return true;
        }

        private bool ValidPosCheck(TetrisItemVM item, int posX, int posY)
        {
            var coords = item.TetrisCoordinateSet;
            var offset = item.RotationOffset;
            for (int i = 0; i < coords.Count; i++)
            {
                var c = coords[i];
                int x = posX + c.x + offset.x;
                int y = posY + c.y + offset.y;
                if (TetrisItemOccupiedCells[x, y] != null)
                    return false;
            }
            return true;
        }

        private void CleanGridReference(int startColumn, int startRow, Vector2Int oldRotationOffset, List<Vector2Int> tetrisItemGrids)
        {
            if (startColumn < 0 || startColumn >= GridSizeWidth) return;
            if (startRow < 0 || startRow >= GridSizeHeight) return;
            for (int i = 0; i < tetrisItemGrids.Count; i++)
            {
                var v = tetrisItemGrids[i];
                int x = startColumn + v.x + oldRotationOffset.x;
                int y = startRow + v.y + oldRotationOffset.y;
                if (x >= 0 && x < GridSizeWidth && y >= 0 && y < GridSizeHeight)
                    TetrisItemOccupiedCells[x, y] = null;
            }
        }

        public bool IsAreaVacantForItem(TetrisItemVM item, int posX, int posY)
        {
            var coords = item.TetrisCoordinateSet;
            var offset = item.RotationOffset;
            for (int i = 0; i < coords.Count; i++)
            {
                var c = coords[i];
                int x = posX + c.x + offset.x;
                int y = posY + c.y + offset.y;
                if (x < 0 || y < 0 || x >= GridSizeWidth || y >= GridSizeHeight) return false;
                if (TetrisItemOccupiedCells[x, y] != null) return false;
            }
            return true;
        }

        /// <summary>
        /// Build the full coverage cells occupied by a placed item on its current grid
        /// </summary>
        public List<Vector2Int> BuildItemCoverageCells(TetrisItemVM item)
        {
            var result = new List<Vector2Int>();
            var coords = item.TetrisCoordinateSet;
            var offset = item.RotationOffset;
            var basePos = item.LocalGridCoordinate;
            for (int i = 0; i < coords.Count; i++)
            {
                var c = coords[i];
                int x = basePos.x + c.x + offset.x;
                int y = basePos.y + c.y + offset.y;
                result.Add(new Vector2Int(x, y));
            }
            return result;
        }

        public List<Vector2Int> BuildCoverageCellsAt(TetrisItemVM item, int posX, int posY)
        {
            var result = new List<Vector2Int>();
            var coords = item.TetrisCoordinateSet;
            var offset = item.RotationOffset;
            for (int i = 0; i < coords.Count; i++)
            {
                var c = coords[i];
                int x = posX + c.x + offset.x;
                int y = posY + c.y + offset.y;
                result.Add(new Vector2Int(x, y));
            }
            return result;
        }

        /// <summary>
        /// Calculate the correspondence between the grid cells of the target item and the original cells of the dragged item (based on ghost based target anchor points)
        /// </summary>
        public Dictionary<Vector2Int, Vector2Int> CalculateGridMapping(TetrisItemVM targetItem, TetrisItemVM draggedItem, Vector2Int ghostTargetPos)
        {
            var mapping = new Dictionary<Vector2Int, Vector2Int>();
            var originalDragged = BuildItemCoverageCells(draggedItem);
            var newDragged = BuildCoverageCellsAt(draggedItem, ghostTargetPos.x, ghostTargetPos.y);
            var targetCells = BuildItemCoverageCells(targetItem);
            Vector2Int ghostBase = ghostTargetPos;
            Vector2Int originalBase = draggedItem.LocalGridCoordinate;
            for (int i = 0; i < targetCells.Count; i++)
            {
                var targetCell = targetCells[i];
                for (int j = 0; j < newDragged.Count; j++)
                {
                    if (newDragged[j] == targetCell)
                    {
                        var rel = targetCell - ghostBase;
                        var originalCell = originalBase + rel;
                        for (int k = 0; k < originalDragged.Count; k++)
                        {
                            if (originalDragged[k] == originalCell)
                            {
                                mapping[targetCell] = originalCell;
                                break;
                            }
                        }
                        break;
                    }
                }
            }
            return mapping;
        }

        /// <summary>
        /// Build the coverage cells of a ghost shape on this grid at targetPos
        /// </summary>
        private HashSet<Vector2Int> BuildGhostCoverageCells(TetrisItemGhostVM ghost, Vector2Int targetPos)
        {
            var coverage = new HashSet<Vector2Int>();
            var shape = ghost.TetrisCoordinateSet;
            var offset = ghost.RotationOffset;
            for (int i = 0; i < shape.Count; i++)
            {
                var sp = shape[i];
                int x = targetPos.x + sp.x + offset.x;
                int y = targetPos.y + sp.y + offset.y;
                coverage.Add(new Vector2Int(x, y));
            }
            return coverage;
        }

        private Dictionary<Vector2Int, Vector2Int> CalculateMappingFromGhost(TetrisItemVM targetItem, TetrisItemGhostVM ghost, Vector2Int ghostPos)
        {
            var mapping = new Dictionary<Vector2Int, Vector2Int>();
            var targetCells = BuildItemCoverageCells(targetItem);

            var ghostShape = ghost.TetrisCoordinateSet;
            var originalShape = ghost.SelectedItem.TetrisCoordinateSet;

            var ghostOffset = ghost.RotationOffset;
            var originalOffset = ghost.SelectedItem.RotationOffset;
            var originalPos = ghost.SelectedItem.LocalGridCoordinate;

            for (int t = 0; t < targetCells.Count; t++)
            {
                var cell = targetCells[t];
                var relative = cell - ghostPos - ghostOffset;

                int index = -1;
                for (int k = 0; k < ghostShape.Count; k++)
                {
                    if (ghostShape[k] == relative)
                    {
                        index = k;
                        break;
                    }
                }

                if (index != -1 && index < originalShape.Count)
                {
                    var originalRel = originalShape[index];
                    var originCell = originalPos + originalOffset + originalRel;
                    mapping[cell] = originCell;
                }
            }
            return mapping;
        }

        public bool CanQuickExchange(TetrisItemGhostVM ghost, Vector2Int targetPos)
        {
            if (ghost == null || ghost.SelectedItem == null) return false;
            var ghostCoverage = BuildGhostCoverageCells(ghost, targetPos);

            // Use the coverage area to check the boundaries grid by grid and correctly handle irregular shapes after rotation.
            var overlapped = new HashSet<TetrisItemVM>();
            foreach (var cell in ghostCoverage)
            {
                if (cell.x < 0 || cell.x >= GridSizeWidth || cell.y < 0 || cell.y >= GridSizeHeight)
                    return false;
                var occ = TetrisItemOccupiedCells[cell.x, cell.y];
                if (occ != null) overlapped.Add(occ);
            }
            if (overlapped.Count == 0) return false;
            var fullyCovered = new List<TetrisItemVM>();
            foreach (var it in overlapped)
            {
                var cells = BuildItemCoverageCells(it);
                bool ok = true;
                for (int i = 0; i < cells.Count; i++)
                {
                    if (!ghostCoverage.Contains(cells[i])) { ok = false; break; }
                }
                if (!ok) return false;
                fullyCovered.Add(it);
            }
            if (fullyCovered.Count != overlapped.Count) return false;
            
            return true;
        }

        public bool TryQuickExchange(TetrisItemGhostVM ghost, Vector2Int targetPos)
        {
            if (ghost == null || ghost.SelectedItem == null) return false;
            var ghostCoverage = BuildGhostCoverageCells(ghost, targetPos);

            var overlapped = new HashSet<TetrisItemVM>();
            foreach (var cell in ghostCoverage)
            {
                if (cell.x < 0 || cell.x >= GridSizeWidth || cell.y < 0 || cell.y >= GridSizeHeight)
                    return false;
                var occ = TetrisItemOccupiedCells[cell.x, cell.y];
                if (occ != null) overlapped.Add(occ);
            }
            if (overlapped.Count == 0) return false;
            var fullyCovered = new List<TetrisItemVM>();
            foreach (var it in overlapped)
            {
                var cells = BuildItemCoverageCells(it);
                bool ok = true;
                for (int i = 0; i < cells.Count; i++)
                {
                    if (!ghostCoverage.Contains(cells[i])) { ok = false; break; }
                }
                if (!ok) return false;
                fullyCovered.Add(it);
            }
            var originGrid = ghost.SelectedItem.CurrentTetrisContainer as TetrisGridVM;
            if (originGrid == null) return false;
            var originalArea = BuildItemCoverageCells(ghost.SelectedItem);
            var originalAreaSet = new HashSet<Vector2Int>(originalArea);
            var removed = new List<(TetrisItemVM item, int posX, int posY, Dir dir, bool rotated, Vector2Int rotationOffset, List<Vector2Int> shapePos, Dictionary<Vector2Int, Vector2Int> gridMapping)>();
            for (int i = 0; i < fullyCovered.Count; i++)
            {
                var it = fullyCovered[i];
                var mapping = CalculateMappingFromGhost(it, ghost, targetPos);
                removed.Add((it, it.LocalGridCoordinate.x, it.LocalGridCoordinate.y, it.Direction, it.Rotated, it.RotationOffset, new List<Vector2Int>(it.TetrisCoordinateSet), mapping));
                RemoveTetrisItem(it, it.LocalGridCoordinate.x, it.LocalGridCoordinate.y, it.RotationOffset, it.TetrisCoordinateSet, false);
            }
            var placed = new List<TetrisItemVM>();
            var unplaced = new List<TetrisItemVM>();
            for (int i = 0; i < removed.Count; i++)
            {
                var rec = removed[i];
                bool ok = TryPlaceItemWithGridMapping(rec.item, originGrid, rec.gridMapping, originalAreaSet);
                if (!ok)
                {
                    unplaced.Add(rec.item);
                    continue;
                }
                placed.Add(rec.item);
                rec.item.CurrentTetrisContainer = originGrid;
                rec.item.IsRaycastTargetEnabled = true;
                rec.item.SetItemData();
            }
            if (unplaced.Count > 0)
            {
                bool allPlaced = TryPlaceItemsInArea(unplaced, originalArea, originGrid);
                if (!allPlaced)
                {
                    for (int j = 0; j < placed.Count; j++)
                    {
                        var p = placed[j];
                        originGrid.RemoveTetrisItem(p, p.LocalGridCoordinate.x, p.LocalGridCoordinate.y, p.RotationOffset, p.TetrisCoordinateSet, originGrid != this);
                    }
                    for (int j = 0; j < removed.Count; j++)
                    {
                        var ori = removed[j];
                        ori.item.Direction = ori.dir;
                        ori.item.Rotated = ori.rotated;
                        ori.item.RotationOffset = ori.rotationOffset;
                        ori.item.TetrisCoordinateSet = ori.shapePos;
                        PlaceTetrisItem(ori.item, ori.posX, ori.posY);
                        ori.item.CurrentTetrisContainer = this;
                        ori.item.IsRaycastTargetEnabled = true;
                        ori.item.SetItemData();
                    }
                    return false;
                }
                for (int k = 0; k < unplaced.Count; k++)
                {
                    var it = unplaced[k];
                    it.CurrentTetrisContainer = originGrid;
                    it.IsRaycastTargetEnabled = true;
                    it.SetItemData();
                }
            }
            bool targetVacant = true;
            foreach (var cell in ghostCoverage)
            {
                if (!PositionCheck(cell.x, cell.y)) { targetVacant = false; break; }
                if (TetrisItemOccupiedCells[cell.x, cell.y] != null) { targetVacant = false; break; }
            }
            if (!targetVacant)
            {
                for (int j = 0; j < placed.Count; j++)
                {
                    var p = placed[j];
                    originGrid.RemoveTetrisItem(p, p.LocalGridCoordinate.x, p.LocalGridCoordinate.y, p.RotationOffset, p.TetrisCoordinateSet, originGrid != this);
                }
                for (int j = 0; j < removed.Count; j++)
                {
                    var ori = removed[j];
                    ori.item.Direction = ori.dir;
                    ori.item.Rotated = ori.rotated;
                    ori.item.RotationOffset = ori.rotationOffset;
                    ori.item.TetrisCoordinateSet = ori.shapePos;
                    PlaceTetrisItem(ori.item, ori.posX, ori.posY);
                    ori.item.CurrentTetrisContainer = this;
                    ori.item.IsRaycastTargetEnabled = true;
                    ori.item.SetItemData();
                }
                return false;
            }

            ghost.SelectedItem.Direction = ghost.Direction;
            ghost.SelectedItem.Rotated = ghost.Rotated;
            ghost.SelectedItem.RotationOffset = ghost.RotationOffset;
            ghost.SelectedItem.TetrisCoordinateSet = new List<Vector2Int>(ghost.TetrisCoordinateSet);

            PlaceTetrisItem(ghost.SelectedItem, targetPos.x, targetPos.y);

            if (originGrid != this)
            {
                for (int i = 0; i < removed.Count; i++)
                {
                    RequestRemoveItemView(removed[i].item);
                }
            }

            return true;
        }

        /// <summary>
        /// Calculate anchor points for individual items according to the grid mapping relationship and attempt to place them in the target (original) grid
        /// </summary>
        private bool TryPlaceItemWithGridMapping(TetrisItemVM item, TetrisGridVM originGrid, Dictionary<Vector2Int, Vector2Int> gridMapping, HashSet<Vector2Int> originalAreaSet)
        {
            // Collect all target cells that this item should occupy in the origin grid
            var targetCells = new HashSet<Vector2Int>();
            foreach (var kv in gridMapping)
            {
                targetCells.Add(kv.Value);
            }

            if (targetCells.Count == 0) return false;

            // Validate that all target cells are within the allowed original area and are empty
            foreach (var cell in targetCells)
            {
                if (cell.x < 0 || cell.x >= originGrid.GridSizeWidth || cell.y < 0 || cell.y >= originGrid.GridSizeHeight) return false;
                if (!originalAreaSet.Contains(cell)) return false;
                if (originGrid.TetrisItemOccupiedCells[cell.x, cell.y] != null) return false;
            }

            // Pattern Matching: Find a rotation direction that matches the shape of targetCells
            var basePoints = InventoryManager.Instance.GetTetrisCoordinateSet(item.ItemDetails.tetrisPieceShape);
            var dirList = new List<Dir> { Dir.Down, Dir.Left, Dir.Up, Dir.Right };
            
            Vector2Int T0 = targetCells.First();
            foreach (var cell in targetCells)
            {
                if (cell.y < T0.y)
                {
                    T0 = cell;
                }
                else if (cell.y == T0.y && cell.x < T0.x)
                {
                    T0 = cell;
                }
            }

            foreach (var dir in dirList)
            {
                bool rotatedFlag = TetrisUtilities.RotationHelper.IsRotated(dir);
                int w = rotatedFlag ? item.ItemDetails.yHeight : item.ItemDetails.xWidth;
                int h = rotatedFlag ? item.ItemDetails.xWidth : item.ItemDetails.yHeight;
                var rotatedPoints = TetrisUtilities.RotationHelper.RotatePoints(basePoints, dir);
                Vector2Int rotationOffset = TetrisUtilities.RotationHelper.GetRotationOffset(dir, w, h);

                // Try to align T0 with each point in rotatedPoints to find a valid Anchor
                for (int i = 0; i < rotatedPoints.Count; i++)
                {
                    var P_ref = rotatedPoints[i];
                    var anchor = T0 - P_ref - rotationOffset;
                    
                    // Verify if this anchor works for ALL points
                    bool match = true;
                    int matchCount = 0;
                    foreach (var p in rotatedPoints)
                    {
                        var expectedPos = anchor + p + rotationOffset;
                        if (targetCells.Contains(expectedPos))
                        {
                            matchCount++;
                        }
                        else
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match && matchCount == targetCells.Count)
                    {
                        // Found a valid match
                        if (originGrid.IsAreaVacantForItem(item, anchor.x, anchor.y))
                        {
                            item.Direction = dir;
                            item.Rotated = rotatedFlag;
                            item.RotationOffset = rotationOffset;
                            item.TetrisCoordinateSet = rotatedPoints;
                            originGrid.PlaceTetrisItem(item, anchor.x, anchor.y);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Attempt to place multiple items in a designated area; Actual placement according to the scanning order of the region
        /// </summary>
        private bool TryPlaceItemsInArea(List<TetrisItemVM> items, List<Vector2Int> availableArea, TetrisGridVM originGrid)
        {
            var areaSet = new HashSet<Vector2Int>(availableArea);
            var temp = new bool[originGrid.GridSizeWidth, originGrid.GridSizeHeight];
            for (int i = 0; i < availableArea.Count; i++)
            {
                var c = availableArea[i];
                if (c.x >= 0 && c.x < originGrid.GridSizeWidth && c.y >= 0 && c.y < originGrid.GridSizeHeight)
                    temp[c.x, c.y] = false;
            }
            items.Sort((a, b) => InventoryManager.Instance.GetTetrisCoordinateSet(b.ItemDetails.tetrisPieceShape).Count.CompareTo(InventoryManager.Instance.GetTetrisCoordinateSet(a.ItemDetails.tetrisPieceShape).Count));
            for (int idx = 0; idx < items.Count; idx++)
            {
                var item = items[idx];
                var basePoints = InventoryManager.Instance.GetTetrisCoordinateSet(item.ItemDetails.tetrisPieceShape);
                var dirList = new List<Dir> { Dir.Down, Dir.Left, Dir.Up, Dir.Right };
                bool itemPlaced = false;
                for (int i = 0; i < dirList.Count && !itemPlaced; i++)
                {
                    Dir dir = dirList[i];
                    bool rotatedFlag = TetrisUtilities.RotationHelper.IsRotated(dir);
                    int w = rotatedFlag ? item.ItemDetails.yHeight : item.ItemDetails.xWidth;
                    int h = rotatedFlag ? item.ItemDetails.xWidth : item.ItemDetails.yHeight;
                    var rotatedPoints = TetrisUtilities.RotationHelper.RotatePoints(basePoints, dir);
                    Vector2Int rotationOffset = TetrisUtilities.RotationHelper.GetRotationOffset(dir, w, h);
                    for (int a = 0; a < availableArea.Count && !itemPlaced; a++)
                    {
                        Vector2Int areaPos = availableArea[a];
                        bool canPlaceHere = true;
                        var requiredPositions = new List<Vector2Int>();
                        for (int s = 0; s < rotatedPoints.Count; s++)
                        {
                            Vector2Int sp = rotatedPoints[s];
                            int x = areaPos.x + sp.x + rotationOffset.x;
                            int y = areaPos.y + sp.y + rotationOffset.y;
                            if (x < 0 || x >= originGrid.GridSizeWidth || y < 0 || y >= originGrid.GridSizeHeight) { canPlaceHere = false; break; }
                            var abs = new Vector2Int(x, y);
                            if (!areaSet.Contains(abs)) { canPlaceHere = false; break; }
                            if (originGrid.TetrisItemOccupiedCells[x, y] != null) { canPlaceHere = false; break; }
                            requiredPositions.Add(abs);
                        }
                        if (canPlaceHere)
                        {
                            item.Direction = dir;
                            item.Rotated = rotatedFlag;
                            item.RotationOffset = rotationOffset;
                            item.TetrisCoordinateSet = rotatedPoints;
                            originGrid.PlaceTetrisItem(item, areaPos.x, areaPos.y);
                            itemPlaced = true;
                            break;
                        }
                    }
                }
                if (!itemPlaced) return false;
            }
            return true;
        }

        /// <summary>
        /// Check if the designated original area can accommodate all target items (without changing their shape or rotation)
        /// </summary>
        public bool CanFitAllItemsInArea(List<TetrisItemVM> items, List<Vector2Int> areaCells)
        {
            if (items == null || areaCells == null) return false;
            var areaSet = new HashSet<Vector2Int>(areaCells);
            var used = new HashSet<Vector2Int>();
            for (int i = 0; i < items.Count; i++)
            {
                var icells = BuildItemCoverageCells(items[i]);
                for (int k = 0; k < icells.Count; k++)
                {
                    var cell = icells[k];
                    if (!areaSet.Contains(cell)) return false;
                    if (used.Contains(cell)) return false;
                    used.Add(cell);
                }
            }
            return true;
        }


        public TetrisItemVM GetTetrisItemVM(int x, int y)
        {
            return TetrisItemOccupiedCells[x, y];
        }

        public bool HasItem(int x, int y)
        {
            return TetrisItemOccupiedCells[x, y] != null;
        }

        public override bool HasItem()
        {
            return OwnerItemsDic.Count > 0;
        }

    }
}
