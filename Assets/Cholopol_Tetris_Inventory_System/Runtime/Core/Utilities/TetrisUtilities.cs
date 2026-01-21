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
using Cholopol.TIS.MVVM;
using Cholopol.TIS.MVVM.ViewModels;
using Cholopol.TIS.SaveLoadSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cholopol.TIS
{
    public static class TetrisUtilities
    {
        #region Rotation Methods

        public static class RotationHelper
        {
            public static Dir GetNextDir(Dir dir)
            {
                return dir switch
                {
                    Dir.Down => Dir.Left,
                    Dir.Left => Dir.Up,
                    Dir.Up => Dir.Right,
                    Dir.Right => Dir.Down,
                    _ => Dir.Down
                };
            }

            public static int GetRotationAngle(Dir dir)
            {
                return dir switch
                {
                    Dir.Down => 0,
                    Dir.Left => 90,
                    Dir.Up => 180,
                    Dir.Right => 270,
                    _ => 0
                };
            }

            // Rotate clockwise 90 degrees
            public static List<Vector2Int> RotatePointsClockwise(List<Vector2Int> points)
            {
                List<Vector2Int> rotatedPoints = new();
                foreach (var point in points)
                {
                    rotatedPoints.Add(new Vector2Int(-point.y, point.x));
                }
                return rotatedPoints;
            }

            public static Vector2Int GetRotationOffset(Dir dir, int width, int height)
            {
                return dir switch
                {
                    Dir.Down => new Vector2Int(0, 0),
                    Dir.Left => new Vector2Int(width - 1, 0),
                    Dir.Up => new Vector2Int(width - 1, height - 1),
                    Dir.Right => new Vector2Int(0, height - 1),
                    _ => Vector2Int.zero
                };
            }

            // Rotation points set
            public static List<Vector2Int> RotatePoints(List<Vector2Int> points, Dir dir)
            {
                List<Vector2Int> rotatedPoints = new();

                foreach (var point in points)
                {
                    int x = point.x;
                    int y = point.y;

                    switch (dir)
                    {
                        case Dir.Left:
                            rotatedPoints.Add(new Vector2Int(-y, x));
                            break;
                        case Dir.Up:
                            rotatedPoints.Add(new Vector2Int(-x, -y));
                            break;
                        case Dir.Right:
                            rotatedPoints.Add(new Vector2Int(y, -x));
                            break;
                        case Dir.Down:
                            rotatedPoints.Add(new Vector2Int(x, y));
                            break;
                        default:
                            return points;
                    }
                }

                return rotatedPoints;
            }

            public static Vector2Int GetDirForwardVector(Dir dir)
            {
                switch (dir)
                {
                    default:
                    case Dir.Down: return new Vector2Int(0, -1);
                    case Dir.Left: return new Vector2Int(-1, 0);
                    case Dir.Up: return new Vector2Int(0, +1);
                    case Dir.Right: return new Vector2Int(+1, 0);
                }
            }

            public static Dir GetDir(Vector2Int from, Vector2Int to)
            {
                if (from.x < to.x)
                {
                    return Dir.Right;
                }
                else
                {
                    if (from.x > to.x)
                    {
                        return Dir.Left;
                    }
                    else
                    {
                        if (from.y < to.y)
                        {
                            return Dir.Up;
                        }
                        else
                        {
                            return Dir.Down;
                        }
                    }
                }
            }

            public static bool IsRotated(Dir dir)
            {
                return dir == Dir.Left || dir == Dir.Right;
            }

        }

        #endregion

        public static class InventoryLogicHelper
        {
            public static void TriggerPointerEnter(GameObject targetUIObject)
            {
                IPointerEnterHandler handler = targetUIObject.GetComponent<IPointerEnterHandler>();
                if (handler != null)
                {
                    PointerEventData eventData = new PointerEventData(EventSystem.current);
                    eventData.pointerEnter = targetUIObject;
                    eventData.position = Input.mousePosition;
                    handler.OnPointerEnter(eventData);
                }
            }

            public static bool IsPlacingIntoSelfOwnedContainer(TetrisItemVM item, TetrisItemContainerVM targetContainer)
            {
                if (item == null || targetContainer == null) return false;

                var itemGuid = item.Guid;
                if (targetContainer.RelatedTetrisItem != null)
                {
                    if (ReferenceEquals(targetContainer.RelatedTetrisItem, item)) return true;
                    if (!string.IsNullOrEmpty(itemGuid) && targetContainer.RelatedTetrisItem.Guid == itemGuid) return true;
                }

                if (targetContainer is TetrisGridVM grid)
                {
                    if (!string.IsNullOrEmpty(itemGuid) && !string.IsNullOrEmpty(grid.GridGuid))
                    {
                        return grid.GridGuid.StartsWith(itemGuid + ":");
                    }
                }

                return false;
            }

            public static bool CanPlaceAt(TetrisGridVM grid, TetrisItemVM itemVM, int posX, int posY)
            {
                if (grid == null || itemVM == null) return false;
                if (posX < 0 || posY < 0) return false;
                if (posX + itemVM.Width > grid.GridSizeWidth) return false;
                if (posY + itemVM.Height > grid.GridSizeHeight) return false;
                var coords = itemVM.TetrisCoordinateSet;
                if (coords == null) return false;

                for (int i = 0; i < coords.Count; i++)
                {
                    var v2i = coords[i];
                    int x = posX + v2i.x + itemVM.RotationOffset.x;
                    int y = posY + v2i.y + itemVM.RotationOffset.y;
                    if (x < 0 || y < 0 || x >= grid.GridSizeWidth || y >= grid.GridSizeHeight) return false;
                    if (grid.HasItem(x, y)) return false;
                }
                return true;
            }

            public static bool HasAnyFreeSpot(TetrisGridVM grid, TetrisItemVM itemVM)
            {
                if (grid == null || itemVM == null) return false;
                for (int row = 0; row < grid.GridSizeHeight; row++)
                {
                    for (int column = 0; column < grid.GridSizeWidth; column++)
                    {
                        if (CanPlaceAt(grid, itemVM, column, row)) return true;
                    }
                }
                return false;
            }

            public static bool TryPlaceAdjacent(TetrisGridVM grid, TetrisItemVM newItemVm, TetrisItemVM referenceVM, IInventoryService service)
            {
                if (grid == null || newItemVm == null || referenceVM == null || service == null) return false;

                int refX = referenceVM.LocalGridCoordinate.x;
                int refY = referenceVM.LocalGridCoordinate.y;
                int refW = referenceVM.Width;
                int refH = referenceVM.Height;

                int newW = newItemVm.Width;
                int newH = newItemVm.Height;

                int vStart = refY - (newH - 1);
                int vEnd = refY + refH - 1;
                int hStart = refX - (newW - 1);
                int hEnd = refX + refW - 1;

                for (int y = vStart; y <= vEnd; y++)
                {
                    if (CanPlaceAt(grid, newItemVm, refX - newW, y))
                    {
                        if (service.PlaceOnGrid(newItemVm, grid, new Vector2Int(refX - newW, y), null)) return true;
                    }
                }

                for (int y = vStart; y <= vEnd; y++)
                {
                    if (CanPlaceAt(grid, newItemVm, refX + refW, y))
                    {
                        if (service.PlaceOnGrid(newItemVm, grid, new Vector2Int(refX + refW, y), null)) return true;
                    }
                }

                for (int x = hStart; x <= hEnd; x++)
                {
                    if (CanPlaceAt(grid, newItemVm, x, refY - newH))
                    {
                        if (service.PlaceOnGrid(newItemVm, grid, new Vector2Int(x, refY - newH), null)) return true;
                    }
                }

                for (int x = hStart; x <= hEnd; x++)
                {
                    if (CanPlaceAt(grid, newItemVm, x, refY + refH))
                    {
                        if (service.PlaceOnGrid(newItemVm, grid, new Vector2Int(x, refY + refH), null)) return true;
                    }
                }

                return false;
            }

            public static bool TryMergeStack(TetrisItemVM target, TetrisItemVM donor)
            {
                if (donor == null || target == null || target.ItemDetails == null || donor.ItemDetails == null) return false;
                if (target.ItemDetails.itemID != donor.ItemDetails.itemID) return false;
                if (target.MaxStack <= 0) return false;
                if (target.CurrentStack >= target.MaxStack) return false;
                int transfer = Mathf.Min(donor.CurrentStack, target.MaxStack - target.CurrentStack);
                if (transfer <= 0) return false;
                target.CurrentStack += transfer;
                donor.CurrentStack -= transfer;
                return true;
            }
        }

        public static class ItemRarityColorHelper
        {
            public static Color GetColor(ItemRarity rarity)
            {
                switch (rarity)
                {
                    case ItemRarity.Common: return Settings.ItemRarityCommonColor;
                    case ItemRarity.Uncommon: return Settings.ItemRarityUncommonColor;
                    case ItemRarity.Rare: return Settings.ItemRarityRareColor;
                    case ItemRarity.Epic: return Settings.ItemRarityEpicColor;
                    case ItemRarity.Legendary: return Settings.ItemRarityLegendaryColor;
                    case ItemRarity.Artifact: return Settings.ItemRarityArtifactColor;
                    default: return Settings.ItemRarityCommonColor;
                }
            }
        }

        public static class MigrationHandler
        {
            public static DataSlot Process(SaveFileWrapper<DataSlot> file)
            {
                if (file == null) return new DataSlot();
                if (string.IsNullOrEmpty(file.Timestamp))
                    file.Timestamp = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                return file.Payload ?? new DataSlot();
            }
        }



    }

}
