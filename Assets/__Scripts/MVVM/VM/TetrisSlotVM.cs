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
using System;
using Cholopol.TIS;

namespace Cholopol.TIS.MVVM.ViewModels
{
    public class TetrisSlotVM : TetrisItemContainerVM
    {
        private InventorySlotType _slotType = InventorySlotType.Pocket;

        public InventorySlotType SlotType { get => _slotType; set => Set(ref _slotType, value); }

        private int _slotIndex = -1;
        public int SlotIndex { get => _slotIndex; set => Set(ref _slotIndex, value); }

        private UnityEngine.Vector2 _slotSize;
        public UnityEngine.Vector2 SlotSize { get => _slotSize; set => Set(ref _slotSize, value); }

        public event Action<TetrisItemVM> PlaceItemViewRequested;
        public event Action<TetrisItemVM> RemoveItemViewRequested;

        public TetrisSlotVM()
        {
        }

        public override bool TryPlaceTetrisItem(TetrisItemVM tetrisItem, int posX = 0, int posY = 0)
        {
            if (tetrisItem == null) return false;
            if (RelatedTetrisItem != null && RelatedTetrisItem != tetrisItem) return false;
            PlaceTetrisItem(tetrisItem, 0, 0);
            return true;
        }

        public override void PlaceTetrisItem(TetrisItemVM tetrisItem, int posX = 0, int posY = 0)
        {
            if (tetrisItem == null) return;
            RelatedTetrisItem = tetrisItem;
            tetrisItem.CurrentTetrisContainer = this;
            tetrisItem.UpdateSize(this);
            tetrisItem.SetItemData();
            PlaceItemViewRequested?.Invoke(tetrisItem);
        }

        public override void RemoveTetrisItem()
        {
            RemoveTetrisItem(true);
        }

        public void RemoveTetrisItem(bool destroyView)
        {
            var item = RelatedTetrisItem;
            if (item == null) return;
            RelatedTetrisItem = null;
            if (destroyView)
            {
                RemoveItemViewRequested?.Invoke(item);
            }
        }

        public void RequestRemoveItemView(TetrisItemVM item)
        {
            RemoveItemViewRequested?.Invoke(item);
        }
    }
}
