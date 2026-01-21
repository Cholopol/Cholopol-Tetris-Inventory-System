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

using UnityEngine;

namespace Cholopol.TIS
{
    public class SlotLayoutController : MonoBehaviour
    {
        public RectTransform slotRect;
        public RectTransform gridPanelRect;
        private int childCount = 0;
        private bool isRemoved = false;

        private void Update()
        {
            if (gridPanelRect.childCount < childCount) isRemoved = true;

            if (HasGrid(gridPanelRect) && !isRemoved)
            {
                SetUp();
            }

            if (isRemoved)
            {
                gridPanelRect.sizeDelta = new Vector2(
                    gridPanelRect.rect.width,
                    50f);
                slotRect.sizeDelta = new Vector2(slotRect.rect.width, gridPanelRect.rect.height + 10f);
                isRemoved = false;
            }
        }

        public void SetUp()
        {
            gridPanelRect.sizeDelta = new Vector2(
                gridPanelRect.rect.width,
                gridPanelRect.GetChild(0).GetComponent<RectTransform>().rect.height);
            slotRect.sizeDelta = new Vector2(slotRect.rect.width, gridPanelRect.rect.height + 10f);
        }

        private bool HasGrid(RectTransform gridPanel)
        {
            if (gridPanel.childCount != childCount)
            {
                childCount = gridPanel.childCount;
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
