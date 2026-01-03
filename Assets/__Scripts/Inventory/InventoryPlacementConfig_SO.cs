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

using System.Collections.Generic;
using Cholopol.TIS.MVVM.ViewModels;
using UnityEngine;

namespace Cholopol.TIS
{
    public readonly struct InventoryPlacementContext
    {
        public readonly TetrisItemVM Item;
        public readonly TetrisItemContainerVM TargetContainer;
        public readonly TetrisGridVM TargetGrid;
        public readonly TetrisSlotVM TargetSlot;
        public readonly Vector2Int Origin;
        public readonly int ShapeWidth;
        public readonly int ShapeHeight;
        public readonly IReadOnlyList<Vector2Int> ShapeCoordinates;
        public readonly Vector2Int ShapeRotationOffset;

        private InventoryPlacementContext(
            TetrisItemVM item,
            TetrisItemContainerVM targetContainer,
            Vector2Int origin,
            int shapeWidth,
            int shapeHeight,
            IReadOnlyList<Vector2Int> shapeCoordinates,
            Vector2Int shapeRotationOffset)
        {
            Item = item;
            TargetContainer = targetContainer;
            TargetGrid = targetContainer as TetrisGridVM;
            TargetSlot = targetContainer as TetrisSlotVM;
            Origin = origin;
            ShapeWidth = shapeWidth;
            ShapeHeight = shapeHeight;
            ShapeCoordinates = shapeCoordinates;
            ShapeRotationOffset = shapeRotationOffset;
        }

        public static InventoryPlacementContext ForItem(TetrisItemVM item, TetrisItemContainerVM targetContainer, Vector2Int origin)
        {
            var coords = item != null ? item.TetrisCoordinateSet : null;
            var rotOffset = item != null ? item.RotationOffset : Vector2Int.zero;
            int w = item != null ? item.Width : 0;
            int h = item != null ? item.Height : 0;
            return new InventoryPlacementContext(item, targetContainer, origin, w, h, coords, rotOffset);
        }

        public static InventoryPlacementContext ForGhost(TetrisItemVM item, TetrisItemGhostVM ghost, TetrisItemContainerVM targetContainer, Vector2Int origin)
        {
            var coords = ghost != null ? ghost.TetrisCoordinateSet : null;
            var rotOffset = ghost != null ? ghost.RotationOffset : Vector2Int.zero;
            int w = ghost != null ? ghost.Width : (item != null ? item.Width : 0);
            int h = ghost != null ? ghost.Height : (item != null ? item.Height : 0);
            return new InventoryPlacementContext(item, targetContainer, origin, w, h, coords, rotOffset);
        }
    }

    [CreateAssetMenu(fileName = "InventoryPlacementConfig", menuName = "Inventory/Placement Config")]
    public class InventoryPlacementConfig_SO : ScriptableObject
    {
        [Header("Rules")]
        [SerializeField] private bool blockSelfOwnedContainer = true;
        [SerializeField] private bool blockOutOfBounds = true;
        [SerializeField] private bool blockSlotOccupied = true;
        [SerializeField] private bool blockSlotTypeMismatch = true;

        public bool BlockSelfOwnedContainer => blockSelfOwnedContainer;
        public bool BlockOutOfBounds => blockOutOfBounds;
        public bool BlockSlotOccupied => blockSlotOccupied;
        public bool BlockSlotTypeMismatch => blockSlotTypeMismatch;

        [Header("Highlight Colors")]
        [SerializeField] private bool overrideHighlightPalette = false;
        [SerializeField] private InventoryHighlightPalette highlightPalette = default;
        [SerializeField] private List<InventoryPlacementBlockColorOverride> invalidReasonColors = new();

        public InventoryHighlightPalette HighlightPalette => highlightPalette;
        public IReadOnlyList<InventoryPlacementBlockColorOverride> InvalidReasonColors => invalidReasonColors;

        public InventoryHighlightPalette ResolveHighlightPalette()
        {
            return overrideHighlightPalette ? highlightPalette : InventoryHighlightPalette.DefaultFromSettings();
        }
        public bool Evaluate(in InventoryPlacementContext context, out InventoryPlacementBlockReason reason)
        {
            reason = InventoryPlacementBlockReason.None;

            if (blockOutOfBounds && context.TargetGrid != null && context.ShapeWidth > 0 && context.ShapeHeight > 0)
            {
                if (!context.TargetGrid.BoundryCheck(context.Origin.x, context.Origin.y, context.ShapeWidth, context.ShapeHeight))
                {
                    reason = InventoryPlacementBlockReason.OutOfBounds;
                    return false;
                }
            }

            if (context.TargetSlot != null)
            {
                if (blockSlotOccupied && context.TargetSlot.RelatedTetrisItem != null)
                {
                    reason = InventoryPlacementBlockReason.SlotOccupied;
                    return false;
                }
                if (blockSlotTypeMismatch && context.Item != null && context.Item.SlotType != context.TargetSlot.SlotType)
                {
                    reason = InventoryPlacementBlockReason.SlotTypeMismatch;
                    return false;
                }
            }

            if (blockSelfOwnedContainer &&
                context.Item != null &&
                context.TargetContainer != null &&
                TetrisUtilities.InventoryLogicHelper.IsPlacingIntoSelfOwnedContainer(context.Item, context.TargetContainer))
            {
                reason = InventoryPlacementBlockReason.SelfOwnedContainer;
                return false;
            }

            reason = InventoryPlacementBlockReason.None;
            return true;
        }

        public Color GetInvalidColor(InventoryPlacementBlockReason reason)
        {
            if (invalidReasonColors != null)
            {
                for (int i = 0; i < invalidReasonColors.Count; i++)
                {
                    var entry = invalidReasonColors[i];
                    if (entry.Reason == reason) return entry.Color;
                }
            }
            return ResolveHighlightPalette().Invalid;
        }

        public static InventoryPlacementConfig_SO GetActiveConfig()
        {
            return InventoryManager.Instance != null ? InventoryManager.Instance.PlacementConfig : null;
        }

        public static bool EvaluateActive(in InventoryPlacementContext context, out InventoryPlacementBlockReason reason)
        {
            var cfg = GetActiveConfig();
            if (cfg != null) return cfg.Evaluate(context, out reason);
            return EvaluateDefault(in context, out reason);
        }

        public static InventoryHighlightPalette GetActiveHighlightPalette()
        {
            var cfg = GetActiveConfig();
            return cfg != null ? cfg.ResolveHighlightPalette() : InventoryHighlightPalette.DefaultFromSettings();
        }

        public static Color GetActiveInvalidColor(InventoryPlacementBlockReason reason)
        {
            var cfg = GetActiveConfig();
            if (cfg != null) return cfg.GetInvalidColor(reason);
            return InventoryHighlightPalette.DefaultFromSettings().Invalid;
        }

        private static bool EvaluateDefault(in InventoryPlacementContext context, out InventoryPlacementBlockReason reason)
        {
            reason = InventoryPlacementBlockReason.None;

            if (context.TargetGrid != null && context.ShapeWidth > 0 && context.ShapeHeight > 0)
            {
                if (!context.TargetGrid.BoundryCheck(context.Origin.x, context.Origin.y, context.ShapeWidth, context.ShapeHeight))
                {
                    reason = InventoryPlacementBlockReason.OutOfBounds;
                    return false;
                }
            }

            if (context.TargetSlot != null)
            {
                if (context.TargetSlot.RelatedTetrisItem != null)
                {
                    reason = InventoryPlacementBlockReason.SlotOccupied;
                    return false;
                }
                if (context.Item != null && context.Item.SlotType != context.TargetSlot.SlotType)
                {
                    reason = InventoryPlacementBlockReason.SlotTypeMismatch;
                    return false;
                }
            }

            if (context.Item != null &&
                context.TargetContainer != null &&
                TetrisUtilities.InventoryLogicHelper.IsPlacingIntoSelfOwnedContainer(context.Item, context.TargetContainer))
            {
                reason = InventoryPlacementBlockReason.SelfOwnedContainer;
                return false;
            }

            reason = InventoryPlacementBlockReason.None;
            return true;
        }
    }
}
