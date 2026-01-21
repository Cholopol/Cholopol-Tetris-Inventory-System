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
using System.Collections.Generic;
using UnityEngine;

namespace Cholopol.TIS
{
    public sealed class SpriteMeshRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        private TetrisPieceShape? _cachedShape;
        private Dir? _cachedDir;
        private bool _cachedUseRotatedMapping;
        private int _cachedGridWidth;
        private int _cachedGridHeight;
        private HashSet<Vector2Int> _cachedPoints;

        /// <summary>
        ///The Unity UI event system calls this method when performing radiographic testing to determine whether the given screen coordinate should be "hit" by the current UI element.
        ///This class is automatically retrieved and called by GraphicRaycaster at runtime through the ICanvasRaycastFilter interface,
        ///Therefore, it can take effect without explicit code references.
        /// </summary>
        /// <param name="sp">Screen coordinates (usually from pointer/touch position)</param>
        /// <param name="eventCamera">Event camera (may be null in Screen Space Overlay)</param>
        /// <returns>Return true to indicate that the UI element is allowed to hit; If false is returned, this hit is masked</returns>
        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            var rt = transform as RectTransform;
            if (rt == null) return true;

            var itemView = GetComponentInParent<TetrisItemView>(true);
            if (itemView != null &&
                itemView.ViewModel != null &&
                itemView.ViewModel.CurrentTetrisContainer is TetrisSlotVM)
            {
                return RectTransformUtility.RectangleContainsScreenPoint(rt, sp, eventCamera);
            }

            var ghostView = GetComponentInParent<TetrisItemGhostView>(true);
            if (ghostView != null && ghostView.ViewModel != null)
            {
                var ghostVm = ghostView.ViewModel;
                var container = ghostVm.SelectedItem != null ? ghostVm.SelectedItem.CurrentTetrisContainer : null;
                if (container == null) container = ghostVm.TargetContaineOnDrop;
                if (container == null) container = ghostVm.OriginContainerOnDrag;
                if (container is TetrisSlotVM)
                {
                    return RectTransformUtility.RectangleContainsScreenPoint(rt, sp, eventCamera);
                }
            }
            if (!TryGetShapeConfig(out var shape, out var baseWidth, out var baseHeight, out var dir, out var hasDir))
                return true;
            if (baseWidth <= 0 || baseHeight <= 0) return true;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, sp, eventCamera, out var localPoint))
                return false;

            var rect = rt.rect;
            if (rect.width <= 0f || rect.height <= 0f) return true;

            var useRotatedMapping = false;
            var gridWidth = baseWidth;
            var gridHeight = baseHeight;

            if (hasDir)
            {
                var rotatedFlag = TetrisUtilities.RotationHelper.IsRotated(dir);
                var rotatedWidth = rotatedFlag ? baseHeight : baseWidth;
                var rotatedHeight = rotatedFlag ? baseWidth : baseHeight;

                var rectAspect = rect.width / rect.height;
                var baseAspect = (float)baseWidth / baseHeight;
                var rotatedAspect = (float)rotatedWidth / rotatedHeight;

                useRotatedMapping = Mathf.Abs(rectAspect - rotatedAspect) < Mathf.Abs(rectAspect - baseAspect);
                if (useRotatedMapping)
                {
                    gridWidth = rotatedWidth;
                    gridHeight = rotatedHeight;
                }
            }

            if (_cachedShape != shape ||
                _cachedDir != (hasDir ? dir : null) ||
                _cachedUseRotatedMapping != useRotatedMapping ||
                _cachedGridWidth != gridWidth ||
                _cachedGridHeight != gridHeight ||
                _cachedPoints == null)
            {
                _cachedShape = shape;
                _cachedDir = hasDir ? dir : null;
                _cachedUseRotatedMapping = useRotatedMapping;
                _cachedGridWidth = gridWidth;
                _cachedGridHeight = gridHeight;
                _cachedPoints = BuildPoints(shape, hasDir ? dir : Dir.Down, useRotatedMapping, gridWidth, gridHeight);
            }

            if (_cachedPoints == null || _cachedPoints.Count == 0) return true;

            var nx = (localPoint.x - rect.xMin) / rect.width;
            var ny = (localPoint.y - rect.yMin) / rect.height;

            if (nx < 0f || nx >= 1f || ny < 0f || ny >= 1f) return false;

            var cellX = Mathf.FloorToInt(nx * gridWidth);
            var cellY = Mathf.FloorToInt((1f - ny) * gridHeight);

            if (cellX < 0) cellX = 0;
            else if (cellX >= gridWidth) cellX = gridWidth - 1;

            if (cellY < 0) cellY = 0;
            else if (cellY >= gridHeight) cellY = gridHeight - 1;

            return _cachedPoints.Contains(new Vector2Int(cellX, cellY));
        }

        /// <summary>
        /// Read the Tetris shape and size configuration from the parent view (normal item view or ghost view), and read the current orientation as far as possible
        /// </summary>
        /// <param name="shape">Output: square shapes</param>
        /// <param name="width">Output: grid width (xWidth)</param>
        /// <param name="height">Output: mesh height (yHeight)</param>
        /// <param name="dir">Output: square orientation</param>
        /// <param name="hasDir">Output: whether the orientation information is successfully read</param>
        /// <returns>Returns true, indicating that the shape and size are successfully read; Otherwise, return false</returns>
        private bool TryGetShapeConfig(out TetrisPieceShape shape, out int width, out int height, out Dir dir, out bool hasDir)
        {
            shape = default;
            width = 0;
            height = 0;
            dir = Dir.Down;
            hasDir = false;

            var itemView = GetComponentInParent<TetrisItemView>();
            if (itemView != null && itemView.ViewModel != null && itemView.ViewModel.ItemDetails != null)
            {
                shape = itemView.ViewModel.ItemDetails.tetrisPieceShape;
                width = itemView.ViewModel.ItemDetails.xWidth;
                height = itemView.ViewModel.ItemDetails.yHeight;
                dir = itemView.ViewModel.Direction;
                hasDir = true;
                return true;
            }

            var ghostView = GetComponentInParent<TetrisItemGhostView>();
            if (ghostView != null && ghostView.ViewModel != null && ghostView.ViewModel.ItemDetails != null)
            {
                shape = ghostView.ViewModel.ItemDetails.tetrisPieceShape;
                width = ghostView.ViewModel.ItemDetails.xWidth;
                height = ghostView.ViewModel.ItemDetails.yHeight;
                dir = ghostView.ViewModel.Direction;
                hasDir = true;
                return true;
            }

            return false;
        }

        /// <summary>
        ///The effective cell set of the shape in the grid is constructed to limit the ray detection hit within the "real occupied" grid range.
        ///When rotatePoints is true, the coordinates will be rotated by dir and the rotation offset will be applied,
        ///The rotated point set can still fall into the grid space of 0.. Width/height.
        /// </summary>
        /// <param name="rotatePoints">Whether to rotate the point set</param>
        /// <param name="width">Target grid width</param>
        /// <param name="height">Target mesh height</param>
        /// <returns>Occupied lattice set; Returns null when the point set cannot be obtained</returns>
        private static HashSet<Vector2Int> BuildPoints(TetrisPieceShape shape, Dir dir, bool rotatePoints, int width, int height)
        {
            var manager = InventoryManager.Instance;
            if (manager == null) return null;
            var basePoints = manager.GetTetrisCoordinateSet(shape);
            if (basePoints == null || basePoints.Count == 0) return null;

            List<Vector2Int> list = basePoints;
            Vector2Int rotationOffset = Vector2Int.zero;
            if (rotatePoints)
            {
                list = TetrisUtilities.RotationHelper.RotatePoints(basePoints, dir);
                rotationOffset = TetrisUtilities.RotationHelper.GetRotationOffset(dir, width, height);
            }

            var set = new HashSet<Vector2Int>();
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                if (rotatePoints) p += rotationOffset;
                set.Add(p);
            }
            return set;
        }
    }
}
