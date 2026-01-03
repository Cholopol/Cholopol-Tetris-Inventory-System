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
using UnityEngine;
using UnityEngine.UI;

namespace Cholopol.TIS.Debug
{
    public class InfiniteItemListRowView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text nameText;
        [SerializeField] private Text statsText;
        [SerializeField] private Button addButton;

        private ItemDetails details;
        private Action<ItemDetails> onAdd;

        public int CurrentItemId => details != null ? details.itemID : -1;

        private void OnEnable()
        {
            if (addButton != null) addButton.onClick.AddListener(HandleAddClicked);
        }

        private void OnDisable()
        {
            if (addButton != null) addButton.onClick.RemoveListener(HandleAddClicked);
        }

        public void SetData(ItemDetails details, Action<ItemDetails> onAdd)
        {
            this.details = details;
            this.onAdd = onAdd;

            if (nameText != null)
            {
                if (details == null) nameText.text = string.Empty;
                else nameText.text = !string.IsNullOrEmpty(details.itemName) ? details.itemName : details.itemID.ToString();
            }

            if (icon != null)
            {
                icon.sprite = details != null ? details.itemIcon : null;
                icon.enabled = icon.sprite != null;
            }
        }

        public void SetCounts(int viewModelCount, int viewCount)
        {
            if (statsText == null) return;
            if (details == null)
            {
                statsText.text = string.Empty;
                return;
            }
            statsText.text = $"ViewModel:{viewModelCount} View:{viewCount}";
        }

        private void HandleAddClicked()
        {
            onAdd?.Invoke(details);
        }
    }
}
