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
using System.Collections.Generic;
using UnityEngine;

namespace Cholopol.TIS
{
    public class TetrisItemMediator : Singleton<TetrisItemMediator>
    {
        [SerializeField] private TetrisItemGhostView _tetrisItemGhostView;

        private Dir _cachedDir;
        private bool _cachedRotated;
        private Vector2Int _cachedRotationOffset;
        private List<Vector2Int> _cachedShapePos;

        private TetrisItemVM _cachedOrginItem;
        private TetrisGridVM _cachedOrginGrid;
        private Dir _cachedItemDir;
        private bool _cachedItemRotated;
        private Vector2Int _cachedItemRotationOffset;
        private List<Vector2Int> _cachedItemShapePos;

        // Cache the rotation state of ghost
        public void CacheGhostState(TetrisItemGhostVM ghost)
        {
            _cachedDir = ghost.Direction;
            _cachedRotated = ghost.Rotated;
            _cachedRotationOffset = ghost.RotationOffset;
            _cachedShapePos = ghost.TetrisCoordinateSet;
        }

        // Cache the rotation state of the item
        public void CacheItemState(TetrisItemVM item)
        {
            _cachedOrginItem = item;
            _cachedOrginGrid = item.CurrentTetrisContainer as TetrisGridVM;
            _cachedItemDir = item.Direction;
            _cachedItemRotated = item.Rotated;
            _cachedItemRotationOffset = item.RotationOffset;
            _cachedItemShapePos = item.TetrisCoordinateSet;
        }

        // Synchronize cache status to TetrisItemGhost
        public void ApplyStateToGhost(TetrisItemGhostVM ghost)
        {
            ghost.Direction = _cachedItemDir;
            ghost.Rotated = _cachedItemRotated;
            ghost.RotationOffset = _cachedItemRotationOffset;
            ghost.TetrisCoordinateSet = _cachedItemShapePos;
            ghost.SelectedItem = _cachedOrginItem;
            ghost.OriginContainerOnDrag = _cachedOrginGrid;

        }

        // Synchronize cache status to TetrisItem
        public void ApplyStateToItem(TetrisItemVM item)
        {
            item.Direction = _cachedDir;
            item.Rotated = _cachedRotated;
            item.RotationOffset = _cachedRotationOffset;
            item.TetrisCoordinateSet = _cachedShapePos;

        }

        public void SyncGhostTargetDropedGrid(TetrisGridVM targetVM)
        {
            if (_tetrisItemGhostView == null || targetVM == null) return;
            var ghostVM = _tetrisItemGhostView.ViewModel;
            if (ghostVM == null || targetVM == null) return;
            if (!ghostVM.OnDragging) return;
            ghostVM.TargetContaineOnDrop = targetVM;
            ghostVM.UpdateSizeForContainer(targetVM);
        }

        public void SyncGhostTargetDropedSlot(TetrisSlotVM targetVM)
        {
            if (_tetrisItemGhostView == null || targetVM == null) return;
            var ghostVM = _tetrisItemGhostView.ViewModel;
            if (ghostVM == null || targetVM == null)
            {
                return;
            }
            if (!ghostVM.OnDragging) return;
            ghostVM.TargetContaineOnDrop = targetVM;
            ghostVM.UpdateSizeForContainer(targetVM);
        }

        public bool TrySyncGhostFromItem(TetrisItemVM itemVM, TetrisItemGhostVM.GhostInitData initData)
        {
            if (_tetrisItemGhostView == null || itemVM == null) return false;

            var ghostVM = _tetrisItemGhostView.ViewModel;
            if (ghostVM == null || ghostVM.OnDragging) return false;

            ghostVM.InitializeFromItem(itemVM, initData);
            return true;
        }

    }
}
