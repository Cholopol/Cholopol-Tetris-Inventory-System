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

using Cholopol.TIS.Utility;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cholopol.TIS.MVVM.ViewModels;
using Cholopol.TIS.MVVM.Views;
using Loxodon.Framework.Contexts;
using Cholopol.TIS.MVVM;

namespace Cholopol.TIS
{
    public class InventoryHighlight : MonoBehaviour
    {
        [SerializeField] RectTransform highlighter;
        [SerializeField] private GameObject highlightTilePrefab;
        private List<GameObject> activeTiles = new();
        private bool canQuickExchange = false;

        private void EnsureHighlighter()
        {
            if (highlighter == null || highlighter.Equals(null))
            {
                activeTiles.Clear();
                GameObject go = new GameObject("Highlighter");
                go.transform.SetParent(this.transform, false);
                highlighter = go.AddComponent<RectTransform>();
                highlighter.anchorMin = Vector2.zero;
                highlighter.anchorMax = Vector2.zero;
                highlighter.pivot = new Vector2(0, 1);
            }
                highlighter.localScale = Vector3.one;
        }

        public void UpdateShapeHighlightMVVM(TetrisItemVM selectedItemVM, TetrisItemGhostVM ghost, Vector2Int originPos, TetrisGridVM gridVM, TetrisGridView gridView)
        {
            EnsureHighlighter();
            ClearTiles();
            canQuickExchange = gridVM != null && ghost != null ? gridVM.CanQuickExchange(ghost, originPos) : false;

            if (gridVM == null || gridView == null || ghost == null) return;
            float tileW = gridVM.LocalGridTileSizeWidth;
            float tileH = gridVM.LocalGridTileSizeHeight;

            var item = selectedItemVM != null ? selectedItemVM : ghost.SelectedItem;
            var placementContext = InventoryPlacementContext.ForGhost(item, ghost, gridVM, originPos);
            var service = Context.GetApplicationContext().GetService<IInventoryService>();
            bool isPlacementValid = service != null
                ? service.CanPlace(placementContext, out var blockedReason)
                : InventoryPlacementConfig_SO.EvaluateActive(placementContext, out blockedReason);
            var palette = InventoryPlacementConfig_SO.GetActiveHighlightPalette();

            for (int i = 0; i < ghost.TetrisCoordinateSet.Count; i++)
            {
                var point = ghost.TetrisCoordinateSet[i];
                int gx = originPos.x + point.x + ghost.RotationOffset.x;
                int gy = originPos.y + point.y + ghost.RotationOffset.y;
                if (gx < 0 || gy < 0 || gx >= gridVM.GridSizeWidth || gy >= gridVM.GridSizeHeight) continue;

                Vector2 tilePos = new Vector2(
                    (point.x + ghost.RotationOffset.x) * tileW,
                    -((point.y + ghost.RotationOffset.y) * tileH)
                );

                GameObject tile = PoolManager.Instance.GetObject(highlightTilePrefab);
                SetColorMVVM(tile, gridVM, gx, gy, selectedItemVM, isPlacementValid, blockedReason, palette);
                tile.transform.SetParent(highlighter);
                tile.transform.localScale = Vector3.one;
                tile.transform.localPosition = tilePos;
                activeTiles.Add(tile);
            }
        }

        private void ClearTiles()
        {
            foreach (var tile in activeTiles)
            {
                if (tile != null)
                    PoolManager.Instance.PushObject(tile);
            }
            activeTiles.Clear();
        }

        public void Show(bool b)
        {
            EnsureHighlighter();
            highlighter.gameObject.SetActive(b);
        }

        public void SetParent(TetrisGridView targetView)
        {
            EnsureHighlighter();
            if (targetView == null) return;
            highlighter.SetParent(targetView.RectTransform);
            highlighter.transform.SetAsFirstSibling();
        }

        public void SetPosition(TetrisGridVM gridVM, int posX, int posY)
        {
            EnsureHighlighter();
            if (gridVM == null) return;
            float tileW = gridVM.LocalGridTileSizeWidth;
            float tileH = gridVM.LocalGridTileSizeHeight;
            Vector2 pos = new(
                posX* tileW,
                -(posY * tileH)
            );
            highlighter.localPosition = pos;
        }

        private void SetColorMVVM(
            GameObject tile,
            TetrisGridVM gridVM,
            int gx,
            int gy,
            TetrisItemVM selectedItemVM,
            bool isPlacementValid,
            InventoryPlacementBlockReason blockedReason,
            InventoryHighlightPalette palette)
        {
            var image = tile != null ? tile.GetComponent<Image>() : null;
            if (image == null) return;

            if (!isPlacementValid)
            {
                image.color = InventoryPlacementConfig_SO.GetActiveInvalidColor(blockedReason);
                return;
            }

            var occ = gridVM.TetrisItemOccupiedCells != null && gx >= 0 && gx < gridVM.GridSizeWidth && gy >= 0 && gy < gridVM.GridSizeHeight
                ? gridVM.TetrisItemOccupiedCells[gx, gy]
                : null;
            bool hasItem = occ != null;
            if (hasItem)
            {
                bool canStack = false;
                if (occ != null && occ.ItemDetails != null && selectedItemVM != null && selectedItemVM.ItemDetails != null)
                {
                    if (occ.ItemDetails.maxStack > 0 && occ.ItemDetails.itemID == selectedItemVM.ItemDetails.itemID)
                    {
                        canStack = true;
                    }
                }
                image.color = canStack ? palette.CanStack : (canQuickExchange ? palette.CanQuickExchange : palette.Invalid);
            }
            else
            {
                image.color = palette.ValidEmpty;
            }
        }

    }
}
